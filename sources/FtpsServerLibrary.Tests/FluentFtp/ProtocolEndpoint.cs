using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using FluentFTP;
using FluentFTP.Exceptions;

namespace FtpsServerLibrary.Tests.FluentFtp;

sealed class ProtocolEndpoint
{
    public required string Name { get; init; }
    public required string Host { get; init; }
    public required int Port { get; init; }
    public required string User { get; init; }
    public required string Password { get; init; }
    public string? ReaderUser { get; init; }
    public string? ReaderPassword { get; init; }
    public string? SeedPath { get; init; }
    public byte[]? Seed { get; init; }
    public Func<SslProtocols, string, string, Task<AsyncFtpClient>>? Connect { get; init; }
    public Func<int, Task<TcpClient>>? OpenTcp { get; init; }

    public Task<AsyncFtpClient> OpenAsync(SslProtocols protocol, string user, string password)
    {
        if (Connect != null)
            return Connect(protocol, user, password);
        return DirectClient(protocol, user, password);
    }

    public async Task<TcpClient> OpenSocketAsync(int port)
    {
        if (OpenTcp != null)
            return await OpenTcp(port);
        var tcp = new TcpClient { NoDelay = true };
        await tcp.ConnectAsync(Host, port);
        return tcp;
    }

    async Task<AsyncFtpClient> DirectClient(SslProtocols protocol, string user, string password)
    {
        var client = new AsyncFtpClient(Host, user, password, Port, ClientConfig(protocol));
        try
        {
            await client.Connect();
            client.Encoding = new UTF8Encoding(false);
            return client;
        }
        catch
        {
            client.Dispose();
            throw;
        }
    }

    public static FtpConfig ClientConfig(SslProtocols protocol)
    {
        var config = new FtpConfig();
        Apply(config, protocol);
        return config;
    }

    public static void Apply(FtpConfig config, SslProtocols protocol)
    {
        config.EncryptionMode = FtpEncryptionMode.Explicit;
        config.DataConnectionType = FtpDataConnectionType.AutoPassive;
        config.DataConnectionEncryption = true;
        config.ValidateAnyCertificate = true;
        config.SslProtocols = protocol;
        config.ConnectTimeout = 20000;
        config.ReadTimeout = 20000;
        config.DataConnectionConnectTimeout = 20000;
        config.DataConnectionReadTimeout = 20000;
        config.RetryAttempts = 0;
        config.SslSessionLength = 0;
    }
}

static class FluentFtpProtocolSuite
{
    public static async Task RunAsync(ProtocolEndpoint endpoint, SslProtocols protocol)
    {
        var stamp = protocol.ToString().ToLowerInvariant() + "-" + Guid.NewGuid().ToString("N")[..8];
        var created = new List<string>();
        await using var client = await endpoint.OpenAsync(protocol, endpoint.User, endpoint.Password);
        try
        {
            await CommandsAsync(client);
            await RoundTripAsync(client, "/" + stamp + ".bin", created);
            await UnicodeAsync(client, "/файл-" + stamp + ".txt", created);
            await DirectoryAsync(client, "/Папка-" + stamp, created);
            await ManyTransfersAsync(client, "/сессия-" + stamp, created);
            if (endpoint.ReaderUser != null && endpoint.SeedPath != null && endpoint.Seed != null)
                await ReadOnlyAsync(endpoint, protocol);
            await WrongPasswordAsync(endpoint, protocol);
            await AcceptCloseNotifyAsync(endpoint, protocol, "/notify-" + stamp + ".bin");
            await RejectBareCloseAsync(endpoint, protocol, "/bare-" + stamp + ".bin");
        }
        finally
        {
            foreach (var path in created.AsEnumerable().Reverse())
            {
                try { await client.DeleteFile(path); } catch { /* directory or already gone */ }
                try { await client.DeleteDirectory(path); } catch { /* file or already gone */ }
            }
        }
    }

    static async Task CommandsAsync(AsyncFtpClient client)
    {
        var syst = await client.Execute("SYST");
        Assert.Equal("215", syst.Code);
        Assert.Contains("UNIX", syst.Message, StringComparison.Ordinal);

        Assert.Equal("200", (await client.Execute("NOOP")).Code);
        Assert.Equal("/", Quoted(await client.Execute("PWD")));
        Assert.Equal("/", Quoted(await client.Execute("XPWD")));
        Assert.Equal("200", (await client.Execute("TYPE I")).Code);

        var features = Reply(await client.Execute("FEAT"));
        Assert.Contains("AUTH TLS", features, StringComparison.Ordinal);
        Assert.Contains("PBSZ", features, StringComparison.Ordinal);
        Assert.Contains("PROT", features, StringComparison.Ordinal);
        Assert.Contains("EPSV", features, StringComparison.Ordinal);
        Assert.Contains("SIZE", features, StringComparison.Ordinal);
        Assert.Contains("MDTM", features, StringComparison.Ordinal);
        Assert.Contains("UTF8", features, StringComparison.Ordinal);

        var utf8 = await client.Execute("OPTS UTF8 ON");
        Assert.Equal("200", utf8.Code);
    }

    static async Task RoundTripAsync(AsyncFtpClient client, string remote, List<string> created)
    {
        var payload = Encoding.UTF8.GetBytes("fluent-upload-" + remote);
        await UploadAsync(client, payload, remote, FtpRemoteExists.Overwrite);
        created.Add(remote);
        Assert.Equal(payload.LongLength, await client.GetFileSize(remote));

        var mdtm = await client.Execute("MDTM " + remote);
        Assert.Equal("213", mdtm.Code);
        Assert.Matches("^[0-9]{14}$", mdtm.Message.Trim());

        Assert.Equal(payload, await DownloadAsync(client, remote));
        var listing = await client.GetListing("/", FtpListOption.Auto);
        Assert.Contains(listing, item => item.Name == Path.GetFileName(remote));

        var shorter = "short"u8.ToArray();
        await UploadAsync(client, shorter, remote, FtpRemoteExists.Overwrite);
        Assert.Equal(shorter.LongLength, await client.GetFileSize(remote));
        Assert.Equal(shorter, await DownloadAsync(client, remote));
    }

    static async Task UnicodeAsync(AsyncFtpClient client, string remote, List<string> created)
    {
        var payload = "данные — café"u8.ToArray();
        await UploadAsync(client, payload, remote, FtpRemoteExists.Overwrite);
        created.Add(remote);
        Assert.Equal(payload, await DownloadAsync(client, remote));
        var listing = await client.GetListing("/", FtpListOption.Auto);
        Assert.Contains(listing, item => item.Name == Path.GetFileName(remote));
    }

    static async Task DirectoryAsync(AsyncFtpClient client, string remoteDir, List<string> created)
    {
        Assert.True(await client.CreateDirectory(remoteDir));
        created.Add(remoteDir);
        var folderName = Path.GetFileName(remoteDir);
        var root = await client.GetListing("/", FtpListOption.Auto);
        var folder = Assert.Single(root, item => item.Name == folderName);
        Assert.Equal(FtpObjectType.Directory, folder.Type);

        await client.SetWorkingDirectory(remoteDir);
        Assert.Equal(remoteDir, Quoted(await client.Execute("PWD")));
        var noteName = "заметка.txt";
        var note = remoteDir + "/" + noteName;
        var payload = "заметка — café"u8.ToArray();
        await UploadAsync(client, payload, note, FtpRemoteExists.Overwrite);
        var here = await client.GetListing(remoteDir, FtpListOption.Auto);
        Assert.Contains(here, item => item.Name == noteName && item.Type == FtpObjectType.File);

        var renamedName = "заметка-2.txt";
        var renamed = remoteDir + "/" + renamedName;
        Assert.True(await client.MoveFile(note, renamed, FtpRemoteExists.Overwrite));
        Assert.Equal(payload, await DownloadAsync(client, renamed));
        await client.DeleteFile(renamed);
        await client.SetWorkingDirectory("/");
        await client.DeleteDirectory(remoteDir);
        created.Remove(remoteDir);
    }

    // TLS 1.2 must keep resuming the control session, and TLS 1.3 on Windows and
    // Android stops reusing one ticket after a handful of data connections and
    // finishes a new handshake. Both have to survive more than ten transfers.
    static async Task ManyTransfersAsync(AsyncFtpClient client, string remoteDir, List<string> created)
    {
        const int transfers = 12;
        Assert.True(await client.CreateDirectory(remoteDir));
        created.Add(remoteDir);
        var payload = "ещё"u8.ToArray();
        var paths = new string[transfers];
        for (var i = 0; i < transfers; i++)
        {
            paths[i] = remoteDir + "/файл-" + (i + 1).ToString("00", System.Globalization.CultureInfo.InvariantCulture) + ".txt";
            await UploadAsync(client, payload, paths[i], FtpRemoteExists.Overwrite);
            Assert.Equal(payload.LongLength, await client.GetFileSize(paths[i]));
        }

        var listing = await client.GetListing(remoteDir, FtpListOption.Auto);
        for (var i = 0; i < transfers; i++)
        {
            var name = Path.GetFileName(paths[i]);
            Assert.Contains(listing, item => item.Name == name && item.Type == FtpObjectType.File);
            Assert.Equal(payload, await DownloadAsync(client, paths[i]));
            await client.DeleteFile(paths[i]);
        }

        await client.DeleteDirectory(remoteDir);
        created.Remove(remoteDir);
    }

    static async Task ReadOnlyAsync(ProtocolEndpoint endpoint, SslProtocols protocol)
    {
        await using var client = await endpoint.OpenAsync(protocol, endpoint.ReaderUser!, endpoint.ReaderPassword!);
        Assert.Equal(endpoint.Seed, await DownloadAsync(client, endpoint.SeedPath!));
        var denied = await Assert.ThrowsAnyAsync<FtpException>(() =>
            client.UploadBytes("no"u8.ToArray(), "/nope.txt", FtpRemoteExists.Overwrite, createRemoteDir: false));
        AssertDenied(denied);
        var deleted = await Assert.ThrowsAnyAsync<FtpException>(() => client.DeleteFile(endpoint.SeedPath!));
        AssertDenied(deleted);
        Assert.Equal(endpoint.Seed, await DownloadAsync(client, endpoint.SeedPath!));
    }

    static async Task WrongPasswordAsync(ProtocolEndpoint endpoint, SslProtocols protocol)
    {
        var error = await Assert.ThrowsAnyAsync<FtpException>(() =>
            endpoint.OpenAsync(protocol, endpoint.User, endpoint.Password + "-wrong"));
        var text = error.ToString();
        Assert.Contains("530", text, StringComparison.Ordinal);
    }

    static async Task AcceptCloseNotifyAsync(ProtocolEndpoint endpoint, SslProtocols protocol, string remote)
    {
        var final = await RawStorAsync(endpoint, protocol, remote, closeNotify: true);
        Assert.StartsWith("226", final, StringComparison.Ordinal);
        await using var client = await endpoint.OpenAsync(protocol, endpoint.User, endpoint.Password);
        Assert.Equal(10L, await client.GetFileSize(remote));
        await client.DeleteFile(remote);
    }

    static async Task RejectBareCloseAsync(ProtocolEndpoint endpoint, SslProtocols protocol, string remote)
    {
        var final = await RawStorAsync(endpoint, protocol, remote, closeNotify: false);
        Assert.StartsWith("550", final, StringComparison.Ordinal);
        await using var client = await endpoint.OpenAsync(protocol, endpoint.User, endpoint.Password);
        var missing = await client.Execute("SIZE " + remote);
        Assert.Equal("550", missing.Code);
    }

    static async Task<string> RawStorAsync(ProtocolEndpoint endpoint, SslProtocols protocol, string remote, bool closeNotify)
    {
        using var controlTcp = await endpoint.OpenSocketAsync(endpoint.Port);
        var plain = controlTcp.GetStream();
        _ = await ReadLineAsync(plain);
        await WriteLineAsync(plain, "AUTH TLS");
        var auth = await ReadLineAsync(plain);
        Assert.StartsWith("234", auth, StringComparison.Ordinal);
        using var control = new SslStream(plain, leaveInnerStreamOpen: true, (_, _, _, _) => true);
        using var controlHandshake = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        await control.AuthenticateAsClientAsync(ClientOptions(protocol), controlHandshake.Token);
        await WriteLineAsync(control, "USER " + endpoint.User);
        Assert.StartsWith("331", await ReadLineAsync(control), StringComparison.Ordinal);
        await WriteLineAsync(control, "PASS " + endpoint.Password);
        Assert.StartsWith("230", await ReadLineAsync(control), StringComparison.Ordinal);
        await WriteLineAsync(control, "PBSZ 0");
        Assert.StartsWith("200", await ReadLineAsync(control), StringComparison.Ordinal);
        await WriteLineAsync(control, "PROT P");
        Assert.StartsWith("200", await ReadLineAsync(control), StringComparison.Ordinal);
        await WriteLineAsync(control, "EPSV");
        var epsv = await ReadLineAsync(control);
        var match = System.Text.RegularExpressions.Regex.Match(epsv, @"\|\|\|(\d+)\|");
        Assert.True(match.Success, epsv);
        var dataPort = int.Parse(match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);

        using var dataTcp = await endpoint.OpenSocketAsync(dataPort);
        dataTcp.NoDelay = true;
        using var data = new SslStream(dataTcp.GetStream(), leaveInnerStreamOpen: true, (_, _, _, _) => true);
        var opening = WriteAndReadAsync(control, "STOR " + remote);
        using var dataHandshake = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        await data.AuthenticateAsClientAsync(ClientOptions(protocol), dataHandshake.Token);
        Assert.StartsWith("150", await opening, StringComparison.Ordinal);
        await data.WriteAsync("raw-upload"u8.ToArray());
        await data.FlushAsync();
        if (closeNotify)
            await data.ShutdownAsync();
        dataTcp.Client.Shutdown(SocketShutdown.Send);
        return await ReadLineAsync(control);
    }

    static SslClientAuthenticationOptions ClientOptions(SslProtocols protocol) => new()
    {
        TargetHost = "127.0.0.1",
        EnabledSslProtocols = protocol,
        CertificateRevocationCheckMode = X509RevocationMode.NoCheck,
    };

    static async Task<byte[]> DownloadAsync(AsyncFtpClient client, string remote)
    {
        var bytes = await client.DownloadBytes(remote, CancellationToken.None);
        Assert.True(bytes != null, "download failed " + client.LastReply.Code + " " + client.LastReply.Message);
        return bytes;
    }

    static async Task UploadAsync(AsyncFtpClient client, byte[] payload, string remote, FtpRemoteExists exists)
    {
        var status = await client.UploadBytes(payload, remote, exists, createRemoteDir: false);
        Assert.True(status == FtpStatus.Success, status + " " + client.LastReply.Code + " " + client.LastReply.Message);
    }

    static void AssertDenied(FtpException error)
    {
        var command = error as FtpCommandException ?? error.InnerException as FtpCommandException;
        Assert.NotNull(command);
        Assert.Equal("550", command.CompletionCode);
    }

    static string Quoted(FtpReply reply)
    {
        var message = reply.Message;
        var start = message.IndexOf('"');
        var end = message.IndexOf('"', start + 1);
        Assert.True(start >= 0 && end > start, message);
        return message[(start + 1)..end];
    }

    static string Reply(FtpReply reply)
    {
        var lines = reply.InfoMessages is null ? "" : string.Join("\n", reply.InfoMessages);
        return lines + "\n" + reply.Message;
    }

    static async Task<string> WriteAndReadAsync(Stream stream, string command)
    {
        await WriteLineAsync(stream, command);
        return await ReadLineAsync(stream);
    }

    static async Task WriteLineAsync(Stream stream, string command)
    {
        var bytes = Encoding.ASCII.GetBytes(command + "\r\n");
        await stream.WriteAsync(bytes);
        await stream.FlushAsync();
    }

    static async Task<string> ReadLineAsync(Stream stream)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        var buffer = new List<byte>();
        var one = new byte[1];
        while (true)
        {
            var read = await stream.ReadAsync(one, timeout.Token);
            if (read == 0)
                throw new EndOfStreamException("control connection closed");
            if (one[0] == (byte)'\n')
                return Encoding.UTF8.GetString(buffer.ToArray());
            if (one[0] != (byte)'\r')
                buffer.Add(one[0]);
        }
    }
}
