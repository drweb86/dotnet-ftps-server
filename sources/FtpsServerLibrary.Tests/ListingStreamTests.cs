using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using FluentFTP;
using FtpsServerLibrary.Tests.FluentFtp;

namespace FtpsServerLibrary.Tests;

// A listing used to collect every name before it wrote the first byte. FluentFTP then
// closed the data connection at its read timeout. This holds the directory after the
// first file name and requires that name to already be on the data connection.
[Collection("FtpsServer")]
public sealed class ListingStreamTests : IAsyncLifetime
{
    private const string Login = "writer";
    private const string Password = "writer-pass";

    private HoldingListingProvider _files = null!;
    private int _port;
    private X509Certificate2? _certificate;
    private FtpsServer? _server;

    public async Task InitializeAsync()
    {
        await FtpsTestGate.Start.WaitAsync();
        try
        {
            _port = FreePort();
            _certificate = CreateCertificate();
            _files = new HoldingListingProvider();
            var config = new FtpsServerConfiguration
            {
                ServerSettings = new FtpsServerSettings
                {
                    Ip = "127.0.0.1",
                    Port = _port,
                    MaxConnections = 4,
                    X509Certificate = _certificate,
                },
                Users =
                [
                    new FtpsServerUserAccount
                    {
                        Login = Login,
                        Password = Password,
                        Folder = "virtual",
                        Read = true,
                        Write = false,
                    },
                ],
            };
            _server = new FtpsServer(new QuietLog(), config, _files);
            await _server.StartAsync();
        }
        finally
        {
            FtpsTestGate.Start.Release();
        }
    }

    public Task DisposeAsync()
    {
        _files.Release();
        _server?.Stop();
        _certificate?.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public Task MlsdSendsTheFirstNameBeforeTheDirectoryFinishes()
        => AssertFirstNameArrivesWhileTheRestIsHeld("MLSD", FtpListOption.Auto);

    [Fact]
    public Task ListSendsTheFirstNameBeforeTheDirectoryFinishes()
        => AssertFirstNameArrivesWhileTheRestIsHeld("LIST", FtpListOption.ForceList);

    // FluentFTP's listing reads every line before it yields an item, so the early bytes
    // are read from the data socket. FluentFTP then lists the same folder itself.
    private async Task AssertFirstNameArrivesWhileTheRestIsHeld(string command, FtpListOption options)
    {
        _files.Arm();
        using var controlTcp = new TcpClient { NoDelay = true };
        await controlTcp.ConnectAsync(IPAddress.Loopback, _port);
        var plain = controlTcp.GetStream();
        Assert.StartsWith("220", await ReadLineAsync(plain), StringComparison.Ordinal);
        await WriteLineAsync(plain, "AUTH TLS");
        Assert.StartsWith("234", await ReadLineAsync(plain), StringComparison.Ordinal);
        using var control = new SslStream(plain, leaveInnerStreamOpen: true, (_, _, _, _) => true);
        using var handshake = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        await control.AuthenticateAsClientAsync(ClientOptions(), handshake.Token);
        await WriteLineAsync(control, "USER " + Login);
        Assert.StartsWith("331", await ReadLineAsync(control), StringComparison.Ordinal);
        await WriteLineAsync(control, "PASS " + Password);
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

        using var dataTcp = new TcpClient { NoDelay = true };
        await dataTcp.ConnectAsync(IPAddress.Loopback, dataPort);
        using var data = new SslStream(dataTcp.GetStream(), leaveInnerStreamOpen: true, (_, _, _, _) => true);
        var opening = WriteAndReadAsync(control, command);
        using var dataHandshake = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        await data.AuthenticateAsClientAsync(ClientOptions(), dataHandshake.Token);
        Assert.StartsWith("150", await opening, StringComparison.Ordinal);

        await _files.Holding.WaitAsync(TimeSpan.FromSeconds(5));
        var beforeRelease = await ReadTextAsync(data, "first.txt", TimeSpan.FromSeconds(5));
        Assert.Contains("first.txt", beforeRelease, StringComparison.Ordinal);
        Assert.DoesNotContain("second.txt", beforeRelease, StringComparison.Ordinal);

        _files.Release();
        var rest = await ReadTextAsync(data, "second.txt", TimeSpan.FromSeconds(5));
        Assert.Contains("second.txt", rest, StringComparison.Ordinal);
        var completed = await ReadLineAsync(control);
        Assert.StartsWith("226", completed, StringComparison.Ordinal);

        await using var client = OpenClient();
        await client.Connect();
        client.Encoding = new UTF8Encoding(false);
        var listing = await client.GetListing("/", options);
        Assert.Contains(listing, item => item.Name == "first.txt" && item.Type == FtpObjectType.File);
        Assert.Contains(listing, item => item.Name == "second.txt" && item.Type == FtpObjectType.File);
    }

    private AsyncFtpClient OpenClient()
    {
        var config = new FtpConfig();
        ProtocolEndpoint.Apply(config, SslProtocols.Tls12);
        return new AsyncFtpClient("127.0.0.1", Login, Password, _port, config);
    }

    private static SslClientAuthenticationOptions ClientOptions() => new()
    {
        TargetHost = "127.0.0.1",
        EnabledSslProtocols = SslProtocols.Tls12,
        CertificateRevocationCheckMode = X509RevocationMode.NoCheck,
    };

    private static async Task<string> ReadTextAsync(Stream stream, string until, TimeSpan timeout)
    {
        using var cancel = new CancellationTokenSource(timeout);
        var buffer = new byte[1024];
        var text = new StringBuilder();
        while (!text.ToString().Contains(until, StringComparison.Ordinal))
        {
            var read = await stream.ReadAsync(buffer, cancel.Token);
            Assert.NotEqual(0, read);
            text.Append(Encoding.UTF8.GetString(buffer, 0, read));
        }

        return text.ToString();
    }

    private static async Task<string> WriteAndReadAsync(Stream stream, string command)
    {
        await WriteLineAsync(stream, command);
        return await ReadLineAsync(stream);
    }

    private static async Task WriteLineAsync(Stream stream, string command)
    {
        var bytes = Encoding.ASCII.GetBytes(command + "\r\n");
        await stream.WriteAsync(bytes);
        await stream.FlushAsync();
    }

    private static async Task<string> ReadLineAsync(Stream stream)
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

    private static int FreePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }

    private static X509Certificate2 CreateCertificate()
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest("CN=ftps-listing-stream", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var created = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(2));
        return X509CertificateLoader.LoadPkcs12(
            created.Export(X509ContentType.Pfx, "test"),
            "test",
            X509KeyStorageFlags.Exportable);
    }

    private sealed class HoldingListingProvider : IFtpsServerFileSystemProvider
    {
        private TaskCompletionSource _release = Completed();
        private TaskCompletionSource _holding = Completed();

        public Task Holding => _holding.Task;

        public void Arm()
        {
            _release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            _holding = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        public void Release() => _release.TrySetResult();

        public async IAsyncEnumerable<FtpsServerFileSystemEntry> DirectoryGetFileSystemEntries(string userFolder, IEnumerable<string> parts)
        {
            var now = DateTime.UtcNow;
            yield return new FtpsServerFileSystemEntry(".", now, 0, true);
            yield return new FtpsServerFileSystemEntry("..", now, 0, true);
            yield return new FtpsServerFileSystemEntry("first.txt", now, 4, false);
            _holding.TrySetResult();
            await _release.Task;
            yield return new FtpsServerFileSystemEntry("second.txt", now, 5, false);
        }

        public Task<bool> DirectoryExists(string userFolder, IEnumerable<string> parts) => Task.FromResult(true);

        public Task<bool> FileExists(string userFolder, IEnumerable<string> parts) => Task.FromResult(false);

        public Task CreateDirectory(string userFolder, IEnumerable<string> parts) => Task.CompletedTask;

        public Task DirectoryDelete(string userFolder, IEnumerable<string> parts) => Task.CompletedTask;

        public Task FileDelete(string userFolder, IEnumerable<string> parts) => Task.CompletedTask;

        public Task DirectoryMove(string userFolder, IEnumerable<string> fromParts, IEnumerable<string> toParts) => Task.CompletedTask;

        public Task FileMove(string userFolder, IEnumerable<string> fromParts, IEnumerable<string> toParts) => Task.CompletedTask;

        public Task<Stream> FileCreate(string userFolder, IEnumerable<string> parts) => throw new NotSupportedException();

        public Task<Stream> FileOpenRead(string userFolder, IEnumerable<string> parts) => throw new NotSupportedException();

        public Task<DateTime> GetFileLastWriteTimeUtc(string userFolder, IEnumerable<string> parts) => Task.FromResult(DateTime.UtcNow);

        public Task<long> GetFileLength(string userFolder, IEnumerable<string> parts) => Task.FromResult(0L);

        public Task<string> GetFileName(string pickerFile) => Task.FromResult(pickerFile);

        private static TaskCompletionSource Completed()
        {
            var source = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            source.TrySetResult();
            return source;
        }
    }

    private sealed class QuietLog : IFtpsServerLog
    {
        public void Debug(string message)
        {
        }

        public void Info(string message)
        {
        }

        public void Warn(string message) => Console.WriteLine(message);

        public void Error(Exception ex, string message) => Console.WriteLine(message + " " + ex.Message);

        public void Fatal(Exception ex, string message) => Console.WriteLine(message + " " + ex.Message);
    }
}
