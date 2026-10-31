using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace FtpsServerLibrary.Tests;

// A TCP close on a TLS record boundary used to look like the end of STOR.
[Collection("FtpsServer")]
public sealed class TruncatedUploadTests : IDisposable
{
    private const string Login = "writer";
    private const string Password = "writer-pass";

    private readonly string _folder = Directory.CreateTempSubdirectory("ftps-trunc-").FullName;
    private readonly X509Certificate2 _certificate = CreateCertificate();
    private readonly List<string> _errors = [];
    private readonly FtpsServer _server;
    private readonly int _port;

    public TruncatedUploadTests()
    {
        FtpsTestGate.Start.Wait();
        try
        {
            _port = FreePort();
            var config = new FtpsServerConfiguration
            {
                ServerSettings = new FtpsServerSettings
                {
                    Ip = IPAddress.Loopback.ToString(),
                    Port = _port,
                    X509Certificate = _certificate,
                },
                Users =
                [
                    new FtpsServerUserAccount
                    {
                        Login = Login,
                        Password = Password,
                        Folder = _folder,
                        Read = true,
                        Write = true,
                    },
                ],
            };
            _server = new FtpsServer(new QuietLog(_errors), config, new FtpsServerFileSystemProvider());
            _server.StartAsync().GetAwaiter().GetResult();
        }
        finally
        {
            FtpsTestGate.Start.Release();
        }
    }

    public void Dispose()
    {
        _server.Stop();
        _certificate.Dispose();
        Directory.Delete(_folder, true);
    }

    [Fact]
    public async Task CloseNotify_StoresTheUploadedBytes()
    {
        var payload = Payload();
        var reply = await UploadAsync(payload, "kept.bin", closeNotify: true);

        Assert.True(reply.StartsWith("226"), string.Join(Environment.NewLine, _errors));
        Assert.Equal(payload, await File.ReadAllBytesAsync(Path.Combine(_folder, "kept.bin")));
    }

    [Fact]
    public async Task TcpCloseWithoutCloseNotify_IsRejectedAndRemoved()
    {
        var payload = Payload();
        var reply = await UploadAsync(payload, "dropped.bin", closeNotify: false);

        Assert.StartsWith("550", reply);
        Assert.False(File.Exists(Path.Combine(_folder, "dropped.bin")));
    }

    private async Task<string> UploadAsync(byte[] payload, string name, bool closeNotify)
    {
        _errors.Clear();
        await using var control = await ControlClient.ConnectAsync(_port);
        Assert.StartsWith("331", await control.CommandAsync($"USER {Login}"));
        Assert.StartsWith("230", await control.CommandAsync($"PASS {Password}"));
        Assert.StartsWith("200", await control.CommandAsync("PBSZ 0"));
        Assert.StartsWith("200", await control.CommandAsync("PROT P"));
        Assert.StartsWith("200", await control.CommandAsync("TYPE I"));

        var passive = await control.CommandAsync("PASV");
        using var dataTcp = new TcpClient();
        await dataTcp.ConnectAsync(IPAddress.Loopback, ParsePassivePort(passive));

        var opening = control.CommandAsync($"STOR {name}");
        var opened = await opening;
        Assert.StartsWith("150", opened);

        var ssl = new SslStream(dataTcp.GetStream(), leaveInnerStreamOpen: true);
        await ssl.AuthenticateAsClientAsync(Tls13("localhost"));
        await ssl.WriteAsync(payload);
        await ssl.FlushAsync();

        if (closeNotify)
            await ssl.ShutdownAsync();

        // Half-close sends a FIN. After close_notify that is a finished upload.
        // Without it, the FIN lands on a TLS record boundary and must not count as one.
        dataTcp.Client.Shutdown(SocketShutdown.Send);
        var finished = await control.ReadLineAsync();
        Assert.NotNull(finished);
        try
        {
            await ssl.DisposeAsync();
        }
        catch (IOException)
        {
        }
        catch (SocketException)
        {
        }

        return finished;
    }

    private static byte[] Payload()
    {
        var payload = new byte[9000];
        for (var i = 0; i < payload.Length; i++)
            payload[i] = (byte)(i * 17);
        return payload;
    }

    private static int ParsePassivePort(string? reply)
    {
        Assert.NotNull(reply);
        Assert.StartsWith("227", reply);
        var open = reply.IndexOf('(');
        var close = reply.IndexOf(')', open + 1);
        var parts = reply[(open + 1)..close].Split(',');
        return int.Parse(parts[4]) * 256 + int.Parse(parts[5]);
    }

    private static SslClientAuthenticationOptions Tls13(string host) => new()
    {
        TargetHost = host,
        EnabledSslProtocols = SslProtocols.Tls13,
        RemoteCertificateValidationCallback = (_, _, _, _) => true,
    };

    private static int FreePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private static X509Certificate2 CreateCertificate()
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest("CN=localhost", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        using var ephemeral = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
        return X509CertificateLoader.LoadPkcs12(ephemeral.Export(X509ContentType.Pfx), null);
    }

    private sealed class ControlClient(TcpClient tcp, Stream stream) : IAsyncDisposable
    {
        private readonly byte[] _byte = new byte[1];

        public static async Task<ControlClient> ConnectAsync(int port)
        {
            var tcp = new TcpClient();
            await tcp.ConnectAsync(IPAddress.Loopback, port);
            var plain = new ControlClient(tcp, tcp.GetStream());
            Assert.StartsWith("220", await plain.ReadLineAsync());
            Assert.StartsWith("234", await plain.CommandAsync("AUTH TLS"));

            var ssl = new SslStream(tcp.GetStream(), false);
            await ssl.AuthenticateAsClientAsync(Tls13("localhost"));
            return new ControlClient(tcp, ssl);
        }

        public async Task<string?> CommandAsync(string line)
        {
            var bytes = Encoding.ASCII.GetBytes(line + "\r\n");
            await stream.WriteAsync(bytes);
            await stream.FlushAsync();
            return await ReadLineAsync();
        }

        public async Task<string?> ReadLineAsync()
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            var line = new StringBuilder();
            while (true)
            {
                if (await stream.ReadAsync(_byte, timeout.Token) == 0)
                    return line.Length == 0 ? null : line.ToString();
                if (_byte[0] == '\n')
                    return line.ToString().TrimEnd('\r');
                line.Append((char)_byte[0]);
            }
        }

        public async ValueTask DisposeAsync()
        {
            await stream.DisposeAsync();
            tcp.Dispose();
        }
    }

    private sealed class QuietLog(List<string> errors) : IFtpsServerLog
    {
        public void Debug(string message) { }
        public void Error(Exception ex, string message) => errors.Add(message + " " + ex);
        public void Fatal(Exception ex, string message) => errors.Add(message + " " + ex);
        public void Info(string message) { }
        public void Warn(string message) => errors.Add(message);
    }
}
