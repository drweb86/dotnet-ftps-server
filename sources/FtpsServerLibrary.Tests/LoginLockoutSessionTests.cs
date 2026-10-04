using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace FtpsServerLibrary.Tests;

// The lockout must also stop connections that were opened before it started.
public class LoginLockoutSessionTests : IDisposable
{
    private const string Login = "alice";
    private const string Password = "right-password";

    private readonly string _folder = Directory.CreateTempSubdirectory("ftps-lockout-").FullName;
    private readonly X509Certificate2 _certificate = CreateCertificate();
    private readonly FtpsServer _server;
    private readonly int _port = FreePort();

    public LoginLockoutSessionTests()
    {
        var config = new FtpsServerConfiguration
        {
            ServerSettings = new FtpsServerSettings
            {
                Ip = IPAddress.Loopback.ToString(),
                Port = _port,
                X509Certificate = _certificate,
            },
            Users = [new FtpsServerUserAccount { Login = Login, Password = Password, Folder = _folder, Read = true, Write = true }],
        };
        _server = new FtpsServer(new NullLog(), config, new FtpsServerFileSystemProvider());
    }

    public void Dispose()
    {
        _server.Stop();
        _certificate.Dispose();
        Directory.Delete(_folder, true);
    }

    [Fact]
    public async Task OpenConnectionCannotLogInDuringLockout()
    {
        await _server.StartAsync();

        await using (var control = await TestClient.ConnectTlsAsync(_port))
        {
            Assert.StartsWith("331", await control.CommandAsync($"USER {Login}"));
            Assert.StartsWith("230", await control.CommandAsync($"PASS {Password}"));
        }

        await using var early = await TestClient.ConnectTlsAsync(_port);
        await using var attacker = await TestClient.ConnectTlsAsync(_port);

        for (var i = 1; i < FtpsLoginThrottle.FailureLimit; i++)
            await FailLoginAsync(attacker);

        await FailLoginAsync(early);
        Assert.Null(await early.ReadLineAsync());

        Assert.StartsWith("331", await attacker.CommandAsync($"USER {Login}"));
        Assert.StartsWith("530", await attacker.CommandAsync($"PASS {Password}"));
        Assert.Null(await attacker.ReadLineAsync());
    }

    private static async Task FailLoginAsync(TestClient client)
    {
        Assert.StartsWith("331", await client.CommandAsync($"USER {Login}"));
        Assert.StartsWith("530", await client.CommandAsync("PASS wrong"));
    }

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
        // Schannel cannot use an ephemeral key; a PFX round-trip gives it a key container.
        return X509CertificateLoader.LoadPkcs12(ephemeral.Export(X509ContentType.Pfx), null);
    }

    private sealed class TestClient(TcpClient tcp, Stream stream) : IAsyncDisposable
    {
        private readonly byte[] _byte = new byte[1];

        public static async Task<TestClient> ConnectTlsAsync(int port)
        {
            var tcp = new TcpClient();
            await tcp.ConnectAsync(IPAddress.Loopback, port);
            var plain = new TestClient(tcp, tcp.GetStream());
            Assert.StartsWith("220", await plain.ReadLineAsync());
            Assert.StartsWith("234", await plain.CommandAsync("AUTH TLS"));

            var ssl = new SslStream(tcp.GetStream(), false, (_, _, _, _) => true);
            await ssl.AuthenticateAsClientAsync("localhost");
            return new TestClient(tcp, ssl);
        }

        public async Task<string?> CommandAsync(string line)
        {
            await stream.WriteAsync(Encoding.ASCII.GetBytes(line + "\r\n"));
            await stream.FlushAsync();
            return await ReadLineAsync();
        }

        // Byte by byte, so nothing past the AUTH reply is buffered before the TLS handshake.
        public async Task<string?> ReadLineAsync()
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            var line = new StringBuilder();
            try
            {
                while (true)
                {
                    if (await stream.ReadAsync(_byte, timeout.Token) == 0)
                        return line.Length == 0 ? null : line.ToString();
                    if (_byte[0] == '\n')
                        return line.ToString().TrimEnd('\r');
                    line.Append((char)_byte[0]);
                }
            }
            catch (IOException)
            {
                return line.Length == 0 ? null : line.ToString();
            }
        }

        public async ValueTask DisposeAsync()
        {
            await stream.DisposeAsync();
            tcp.Dispose();
        }
    }

    private sealed class NullLog : IFtpsServerLog
    {
        public void Debug(string message) { }
        public void Error(Exception ex, string message) { }
        public void Fatal(Exception ex, string message) { }
        public void Info(string message) { }
        public void Warn(string message) { }
    }
}
