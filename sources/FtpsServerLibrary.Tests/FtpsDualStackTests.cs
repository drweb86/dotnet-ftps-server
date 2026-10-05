using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using FluentFTP;

namespace FtpsServerLibrary.Tests;

[Collection("FtpsServer")]
public sealed class FtpsDualStackTests : IAsyncLifetime
{
    private const string Login = "writer";
    private const string Password = "writer-pass";

    private string _root = "";
    private readonly List<string> _serverErrors = new();
    private int _port;
    private X509Certificate2? _certificate;
    private FtpsServer? _server;

    public async Task InitializeAsync()
    {
        _root = Path.Combine(Path.GetTempPath(), "ftps-dual-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
        _certificate = CreateCertificate();
        await FtpsTestGate.Start.WaitAsync();
        try
        {
            _port = FreePort();
            var config = new FtpsServerConfiguration
            {
                ServerSettings = new FtpsServerSettings
                {
                    Ip = "0.0.0.0",
                    Port = _port,
                    MaxConnections = 20,
                    X509Certificate = _certificate,
                },
                Users =
                [
                    new FtpsServerUserAccount
                    {
                        Login = Login,
                        Password = Password,
                        Folder = _root,
                        Read = true,
                        Write = true,
                    },
                ],
            };

            _server = new FtpsServer(new QuietLog(_serverErrors), config, new FtpsServerFileSystemProvider());
            await _server.StartAsync();
        }
        finally
        {
            FtpsTestGate.Start.Release();
        }
    }

    public Task DisposeAsync()
    {
        _server?.Stop();
        _certificate?.Dispose();
        try
        {
            if (Directory.Exists(_root))
                Directory.Delete(_root, true);
        }
        catch (IOException)
        {
        }

        return Task.CompletedTask;
    }

    [Fact]
    public async Task OneServer_TransfersOverIpv4AndIpv6AtTheSameTime()
    {
        var ipv4Bytes = Encoding.UTF8.GetBytes("IPv4 — café\n");
        var ipv6Bytes = Encoding.UTF8.GetBytes("IPv6 — 日本語\n");

        await using var ipv4 = await ConnectAsync("127.0.0.1", FtpDataConnectionType.PASV, FtpIpVersion.IPv4);
        await using var ipv6 = await ConnectAsync("::1", FtpDataConnectionType.EPSV, FtpIpVersion.IPv6);

        var features = ReplyText(await ipv4.Execute("FEAT"));
        Assert.Contains("EPSV", features, StringComparison.Ordinal);
        Assert.DoesNotContain("EPRT", features, StringComparison.Ordinal);

        await ipv4.CreateDirectory("/папка");
        var uploads = await Task.WhenAll(
            ipv4.UploadBytes(ipv4Bytes, "/папка/ipv4.txt", FtpRemoteExists.Overwrite, createRemoteDir: false),
            ipv6.UploadBytes(ipv6Bytes, "/папка/ipv6.txt", FtpRemoteExists.Overwrite, createRemoteDir: false));
        var serverLog = string.Join(Environment.NewLine, _serverErrors);
        Assert.True(uploads[0] == FtpStatus.Success, $"IPv4 upload {uploads[0]}: {ipv4.LastReply.Code} {ipv4.LastReply.Message}{Environment.NewLine}{serverLog}");
        Assert.True(uploads[1] == FtpStatus.Success, $"IPv6 upload {uploads[1]}: {ipv6.LastReply.Code} {ipv6.LastReply.Message}{Environment.NewLine}{serverLog}");

        Assert.Equal(ipv4Bytes, await File.ReadAllBytesAsync(Path.Combine(_root, "папка", "ipv4.txt")));
        Assert.Equal(ipv6Bytes, await File.ReadAllBytesAsync(Path.Combine(_root, "папка", "ipv6.txt")));
        var fromIpv6 = await ipv6.DownloadBytes("/папка/ipv4.txt", CancellationToken.None);
        Assert.True(fromIpv6 is not null, "IPv6 download failed: " + ipv6.LastReply);
        Assert.Equal(ipv4Bytes, fromIpv6);
        var fromIpv4 = await ipv4.DownloadBytes("/папка/ipv6.txt", CancellationToken.None);
        Assert.True(fromIpv4 is not null, $"IPv4 download failed: {ipv4.LastReply.Code} {ipv4.LastReply.Message}{Environment.NewLine}{string.Join(Environment.NewLine, _serverErrors)}");
        Assert.Equal(ipv6Bytes, fromIpv4);

        Assert.Equal("522", (await ipv6.Execute("PASV")).Code);
        Assert.Equal("522", (await ipv6.Execute("EPSV 1")).Code);
        Assert.Equal("229", (await ipv4.Execute("EPSV")).Code);
        Assert.Equal("522", (await ipv4.Execute("EPSV 2")).Code);
        Assert.Equal("200", (await ipv4.Execute("EPSV ALL")).Code);
        Assert.Equal("501", (await ipv4.Execute("PASV")).Code);
    }

    private async Task<AsyncFtpClient> ConnectAsync(string host, FtpDataConnectionType data, FtpIpVersion family)
    {
        var client = new AsyncFtpClient(host, Login, Password, _port, new FtpConfig
        {
            EncryptionMode = FtpEncryptionMode.Explicit,
            DataConnectionType = data,
            InternetProtocolVersions = family,
            DataConnectionEncryption = true,
            ValidateAnyCertificate = true,
            ConnectTimeout = 15000,
            ReadTimeout = 20000,
            DataConnectionConnectTimeout = 15000,
            DataConnectionReadTimeout = 20000,
            RetryAttempts = 0,
            SslSessionLength = 0,
        });

        try
        {
            await client.Connect();
            return client;
        }
        catch
        {
            client.Dispose();
            throw;
        }
    }

    private static string ReplyText(FtpReply reply)
    {
        var lines = reply.InfoMessages is null ? "" : string.Join("\n", reply.InfoMessages);
        return lines + "\n" + reply.Message;
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
        var request = new CertificateRequest("CN=ftps-dual-stack-test", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var created = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(2));
        return X509CertificateLoader.LoadPkcs12(
            created.Export(X509ContentType.Pfx, "test"),
            "test",
            X509KeyStorageFlags.Exportable);
    }

    private sealed class QuietLog(List<string> errors) : IFtpsServerLog
    {
        public void Debug(string message)
        {
        }

        public void Info(string message)
        {
        }

        public void Warn(string message) => errors.Add(message);

        public void Error(Exception ex, string message) => errors.Add(message + " " + ex);

        public void Fatal(Exception ex, string message) => errors.Add(message + " " + ex);
    }
}
