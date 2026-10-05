using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using FluentFTP;
using FluentFTP.Exceptions;

namespace FtpsServerLibrary.Tests;

[Collection("FtpsServer")]
public sealed class FtpsServerOperationsTests : IAsyncLifetime
{
    private const string WriterLogin = "writer";
    private const string WriterPassword = "writer-pass";
    private const string ReaderLogin = "reader";
    private const string ReaderPassword = "reader-pass";
    private const string Utf8Login = "utf8";
    private const string Utf8Password = "пароль";
    private static readonly byte[] SeededText = Encoding.UTF8.GetBytes("чтение — café\n");

    private string _writeRoot = "";
    private string _readRoot = "";
    private int _port;
    private X509Certificate2? _certificate;
    private FtpsServer? _server;

    public async Task InitializeAsync()
    {
        _writeRoot = Path.Combine(Path.GetTempPath(), "ftps-ops-" + Guid.NewGuid().ToString("N"));
        _readRoot = Path.Combine(Path.GetTempPath(), "ftps-ops-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_writeRoot);
        var notes = Path.Combine(_readRoot, "заметки");
        Directory.CreateDirectory(notes);
        File.WriteAllBytes(Path.Combine(notes, "файл.txt"), SeededText);

        _certificate = CreateCertificate();
        await FtpsTestGate.Start.WaitAsync();
        try
        {
            _port = FreePort();
            var config = new FtpsServerConfiguration
            {
                ServerSettings = new FtpsServerSettings
                {
                    Ip = "127.0.0.1",
                    Port = _port,
                    MaxConnections = 20,
                    X509Certificate = _certificate,
                },
                Users =
                [
                    new FtpsServerUserAccount
                    {
                        Login = WriterLogin,
                        Password = WriterPassword,
                        Folder = _writeRoot,
                        Read = true,
                        Write = true,
                    },
                    new FtpsServerUserAccount
                    {
                        Login = ReaderLogin,
                        Password = ReaderPassword,
                        Folder = _readRoot,
                        Read = true,
                        Write = false,
                    },
                    new FtpsServerUserAccount
                    {
                        Login = Utf8Login,
                        Password = Utf8Password,
                        Folder = _writeRoot,
                        Read = true,
                        Write = true,
                    },
                ],
            };

            _server = new FtpsServer(new QuietLog(), config, new FtpsServerFileSystemProvider());
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
        TryDelete(_writeRoot);
        TryDelete(_readRoot);
        return Task.CompletedTask;
    }

    [Fact]
    public async Task ControlCommands_MatchTheImplementedFeatureSet()
    {
        await using var client = await ConnectAsync(write: true);

        var syst = await client.Execute("SYST");
        Assert.Equal("215", syst.Code);
        Assert.Contains("UNIX", syst.Message, StringComparison.Ordinal);

        var noop = await client.Execute("NOOP");
        Assert.Equal("200", noop.Code);

        var pwd = await client.Execute("XPWD");
        Assert.Equal("257", pwd.Code);
        Assert.Equal("/", QuotedPath(pwd));

        Assert.Equal("200", (await client.Execute("TYPE A")).Code);
        Assert.Equal("200", (await client.Execute("TYPE I")).Code);

        var features = ReplyText(await client.Execute("FEAT"));
        Assert.Contains("AUTH TLS", features, StringComparison.Ordinal);
        Assert.Contains("PBSZ", features, StringComparison.Ordinal);
        Assert.Contains("PROT", features, StringComparison.Ordinal);
        Assert.Contains("SIZE", features, StringComparison.Ordinal);
        Assert.Contains("MDTM", features, StringComparison.Ordinal);
        Assert.Contains("MLST type*;size*;modify*;perm*;", features, StringComparison.Ordinal);
        Assert.Contains("UTF8", features, StringComparison.Ordinal);
        Assert.Contains("EPSV", features, StringComparison.Ordinal);
        Assert.DoesNotContain("EPRT", features, StringComparison.Ordinal);
        Assert.DoesNotContain("REST", features, StringComparison.Ordinal);
        Assert.DoesNotContain("HASH", features, StringComparison.Ordinal);
        Assert.DoesNotContain("HOST", features, StringComparison.Ordinal);
        Assert.DoesNotContain("CCC", features, StringComparison.Ordinal);

        var utf8 = await client.Execute("OPTS UTF8 ON");
        Assert.Equal("200", utf8.Code);
        Assert.Contains("UTF8", utf8.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("/", QuotedPath(await client.Execute("PWD")));

        var mlstOpts = await client.Execute("OPTS MLST size;");
        Assert.Equal("200", mlstOpts.Code);
        Assert.Contains("size;", mlstOpts.Message, StringComparison.OrdinalIgnoreCase);
        var selected = ReplyText(await client.Execute("FEAT"));
        Assert.Contains("MLST type;size*;modify;perm;", selected, StringComparison.Ordinal);

        var quit = await client.Execute("QUIT");
        Assert.Equal("221", quit.Code);
    }

    [Fact]
    public async Task UnicodeDirectories_AreCreatedMovedAndRemovedOnDisk()
    {
        await using var client = await ConnectAsync(write: true);
        var folder = "/Папка/Под каталог 2";
        var renamed = "/Папка/подкаталог-新しい";

        Assert.True(await client.CreateDirectory(folder));
        Assert.True(Directory.Exists(Disk(_writeRoot, folder)));

        await client.SetWorkingDirectory(folder);
        Assert.Equal(folder, QuotedPath(await client.Execute("PWD")));
        Assert.Equal("250", (await client.Execute("CDUP")).Code);
        Assert.Equal("/Папка", QuotedPath(await client.Execute("PWD")));
        await client.SetWorkingDirectory("/");

        await client.Rename(folder, renamed);
        Assert.False(Directory.Exists(Disk(_writeRoot, folder)));
        Assert.True(Directory.Exists(Disk(_writeRoot, renamed)));

        await client.DeleteDirectory(renamed);
        await client.DeleteDirectory("/Папка");
        Assert.False(Directory.Exists(Disk(_writeRoot, "/Папка")));
    }

    [Fact]
    public async Task LegacyDirectoryAliases_ChangeTheSameDirectories()
    {
        await using var client = await ConnectAsync(write: true);

        Assert.True((await client.Execute("XMKD /日本語フォルダ")).Success);
        Assert.True(Directory.Exists(Disk(_writeRoot, "/日本語フォルダ")));
        Assert.Equal("250", (await client.Execute("XCWD /日本語フォルダ")).Code);
        Assert.Equal("/日本語フォルダ", QuotedPath(await client.Execute("PWD")));
        Assert.True((await client.Execute("XMKD café")).Success);
        Assert.Equal("250", (await client.Execute("XCUP")).Code);
        Assert.Equal("/", QuotedPath(await client.Execute("PWD")));
        Assert.Equal("250", (await client.Execute("XRMD /日本語フォルダ/café")).Code);
        Assert.Equal("250", (await client.Execute("XRMD /日本語フォルダ")).Code);
        Assert.False(Directory.Exists(Disk(_writeRoot, "/日本語フォルダ")));
    }

    [Fact]
    public async Task UnicodeUploadAndDownload_RoundTripTheBytesOnDisk()
    {
        await using var client = await ConnectAsync(write: true);
        var remote = "/Папка/Под каталог 2/документ №1.txt";
        var payload = Encoding.UTF8.GetBytes("Строка — 日本語\nвторой абзац\n");

        Assert.True(await client.CreateDirectory("/Папка/Под каталог 2"));
        var uploaded = await client.UploadBytes(payload, remote, FtpRemoteExists.NoCheck, createRemoteDir: false);
        Assert.Equal(FtpStatus.Success, uploaded);
        Assert.Equal(payload, File.ReadAllBytes(Disk(_writeRoot, remote)));

        var downloaded = await DownloadAsync(client, remote);
        Assert.Equal(payload, downloaded);

        var shorter = Encoding.UTF8.GetBytes("короче");
        var replaced = await client.UploadBytes(shorter, remote, FtpRemoteExists.Overwrite, createRemoteDir: false);
        Assert.Equal(FtpStatus.Success, replaced);
        Assert.Equal(shorter, File.ReadAllBytes(Disk(_writeRoot, remote)));
        Assert.Equal(shorter, await DownloadAsync(client, remote));
    }

    [Fact]
    public async Task SizeAndModifiedTime_MatchTheFileOnDisk()
    {
        await using var client = await ConnectAsync(write: true);
        var remote = "/café/naïve.dat";
        var payload = new byte[] { 0, 1, 2, 255, 0xE2, 0x82, 0xAC };
        Assert.True(await client.CreateDirectory("/café"));
        Assert.Equal(FtpStatus.Success, await client.UploadBytes(payload, remote, FtpRemoteExists.NoCheck, createRemoteDir: false));

        Assert.Equal(payload.Length, await client.GetFileSize(remote));

        var mdtm = await client.Execute("MDTM " + remote);
        Assert.Equal("213", mdtm.Code);
        Assert.Equal(File.GetLastWriteTimeUtc(Disk(_writeRoot, remote)).ToString("yyyyMMddHHmmss"), mdtm.Message.Trim());
    }

    [Fact]
    public async Task Listings_IncludeUnicodeNamesFromDisk()
    {
        await using var client = await ConnectAsync(write: true);
        var folder = "/Папка/Под каталог 2";
        var fileName = "документ №1.txt";
        var remote = folder + "/" + fileName;
        Assert.True(await client.CreateDirectory(folder));
        Assert.Equal(FtpStatus.Success, await client.UploadBytes("данные"u8.ToArray(), remote, FtpRemoteExists.NoCheck, createRemoteDir: false));

        var mlsd = await client.GetListing(folder, FtpListOption.Auto);
        var listed = Assert.Single(mlsd, item => item.Name == fileName);
        Assert.Equal(FtpObjectType.File, listed.Type);
        Assert.Equal("данные"u8.ToArray().Length, listed.Size);

        var unix = await client.GetListing(folder, FtpListOption.ForceList);
        Assert.Contains(unix, item => item.Name == fileName);

        var names = await client.GetNameListing(folder);
        Assert.Contains(names, name => name.Replace('\\', '/').TrimEnd('/').EndsWith(fileName, StringComparison.Ordinal));

        var mlst = ReplyText(await client.Execute("MLST " + remote));
        Assert.Contains("type=file", mlst, StringComparison.Ordinal);
        Assert.Contains(fileName, mlst, StringComparison.Ordinal);
        Assert.Contains("size=", mlst, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RenameAndDelete_UpdateTheFilesOnDisk()
    {
        await using var client = await ConnectAsync(write: true);
        var source = "/документы/черновик.txt";
        var destination = "/документы/готово.txt";
        var payload = "текст"u8.ToArray();
        Assert.True(await client.CreateDirectory("/документы"));
        Assert.Equal(FtpStatus.Success, await client.UploadBytes(payload, source, FtpRemoteExists.NoCheck, createRemoteDir: false));

        await client.Rename(source, destination);
        Assert.False(File.Exists(Disk(_writeRoot, source)));
        Assert.Equal(payload, File.ReadAllBytes(Disk(_writeRoot, destination)));

        await client.DeleteFile(destination);
        Assert.False(File.Exists(Disk(_writeRoot, destination)));
        Assert.True(Directory.Exists(Disk(_writeRoot, "/документы")));
    }

    [Fact]
    public async Task ReadOnlyAccount_DownloadsUnicodeFilesAndCannotChangeThem()
    {
        await using var client = await ConnectAsync(write: false);
        var remote = "/заметки/файл.txt";

        Assert.Equal(SeededText, await DownloadAsync(client, remote));
        var listing = await client.GetListing("/заметки", FtpListOption.Auto);
        Assert.Contains(listing, item => item.Name == "файл.txt");

        await ExpectDenied(() => client.UploadBytes("нет"u8.ToArray(), "/заметки/новый.txt", FtpRemoteExists.NoCheck, createRemoteDir: false));
        Assert.False(File.Exists(Disk(_readRoot, "/заметки/новый.txt")));

        await ExpectDenied(() => client.DeleteFile(remote));
        Assert.Equal(SeededText, File.ReadAllBytes(Disk(_readRoot, remote)));

        var mkdir = await client.Execute("MKD /новая");
        Assert.Equal("550", mkdir.Code);
        Assert.False(Directory.Exists(Disk(_readRoot, "/новая")));

        await ExpectDenied(() => client.Rename(remote, "/заметки/другое.txt"));
        Assert.True(File.Exists(Disk(_readRoot, remote)));
    }

    [Fact]
    public async Task SharedRoot_CannotBeRemovedOrRenamed()
    {
        await using var client = await ConnectAsync(write: true);

        var remove = await client.Execute("RMD /");
        Assert.Equal("550", remove.Code);
        Assert.False(remove.Success);

        var from = await client.Execute("RNFR /");
        if (from.Success)
        {
            var to = await client.Execute("RNTO /renamed-root");
            Assert.Equal("550", to.Code);
            Assert.False(to.Success);
        }
        else
        {
            Assert.Equal("550", from.Code);
        }

        Assert.True(Directory.Exists(_writeRoot));
        Assert.False(Directory.Exists(Disk(_writeRoot, "/renamed-root")));
    }

    [Fact]
    public async Task Utf8Password_LogsInAndWritesAFile()
    {
        // FluentFTP logs in before it turns UTF-8 on. A Russian password has to be sent
        // after OPTS UTF8 ON, which is what FileZilla and WinSCP do.
        await using var client = new Utf8LoginClient("127.0.0.1", Utf8Login, Utf8Password, _port, new FtpConfig
        {
            EncryptionMode = FtpEncryptionMode.Explicit,
            DataConnectionType = FtpDataConnectionType.PASV,
            DataConnectionEncryption = true,
            ValidateAnyCertificate = true,
            ConnectTimeout = 15000,
            ReadTimeout = 20000,
            DataConnectionConnectTimeout = 15000,
            DataConnectionReadTimeout = 20000,
            RetryAttempts = 0,
            SslSessionLength = 0,
        });
        client.Encoding = new UTF8Encoding(false);
        await client.Connect();

        var text = Encoding.UTF8.GetBytes("проверка\n");
        var status = await client.UploadBytes(text, "/проверка.txt", FtpRemoteExists.Overwrite, createRemoteDir: false);
        Assert.Equal(FtpStatus.Success, status);
        Assert.Equal(text, await File.ReadAllBytesAsync(Path.Combine(_writeRoot, "проверка.txt")));
    }

    private sealed class Utf8LoginClient : AsyncFtpClient
    {
        public Utf8LoginClient(string host, string user, string password, int port, FtpConfig config)
            : base(host, user, password, port, config)
        {
        }

        protected override async Task Authenticate(string userName, string password, string account, CancellationToken token)
        {
            var utf8 = await Execute("OPTS UTF8 ON", token);
            if (!utf8.Success)
                throw new InvalidOperationException(utf8.Message);
            await base.Authenticate(userName, password, account, token);
        }
    }

    private Task<AsyncFtpClient> ConnectAsync(bool write) =>
        ConnectAsync(write ? WriterLogin : ReaderLogin, write ? WriterPassword : ReaderPassword);

    private async Task<AsyncFtpClient> ConnectAsync(string login, string password)
    {
        var client = new AsyncFtpClient(
            "127.0.0.1",
            login,
            password,
            _port,
            new FtpConfig
            {
                EncryptionMode = FtpEncryptionMode.Explicit,
                DataConnectionType = FtpDataConnectionType.PASV,
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

    private static Task<byte[]> DownloadAsync(AsyncFtpClient client, string remote) =>
        client.DownloadBytes(remote, CancellationToken.None);

    private static async Task ExpectDenied(Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (FtpCommandException ex)
        {
            Assert.Equal("550", ex.CompletionCode);
            return;
        }
        catch (FtpException ex) when (ex.InnerException is FtpCommandException command)
        {
            Assert.Equal("550", command.CompletionCode);
            return;
        }

        Assert.Fail("Expected the server to refuse the command with 550");
    }

    private static string QuotedPath(FtpReply reply)
    {
        var message = reply.Message;
        var start = message.IndexOf('"');
        var end = message.IndexOf('"', start + 1);
        Assert.True(start >= 0 && end > start, message);
        return message[(start + 1)..end];
    }

    private static string ReplyText(FtpReply reply)
    {
        var lines = reply.InfoMessages is null ? "" : string.Join("\n", reply.InfoMessages);
        return lines + "\n" + reply.Message;
    }

    private static string Disk(string root, string ftpPath)
    {
        var relative = ftpPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        return Path.Combine(root, relative);
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
        var request = new CertificateRequest("CN=ftps-operations-test", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var created = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(2));
        return X509CertificateLoader.LoadPkcs12(
            created.Export(X509ContentType.Pfx, "test"),
            "test",
            X509KeyStorageFlags.Exportable);
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (Directory.Exists(path))
                Directory.Delete(path, true);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
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
