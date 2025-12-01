using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace FtpsServerLibrary;

public class FtpsServer
{
    private readonly FtpsServerConfiguration _config;
    private readonly IFtpsServerLog _log;
    private TcpListener? _listener;
    private bool _isRunning;
    private readonly X509Certificate2 _serverCertificate;
    private int _activeConnections;

    public FtpsServer(IFtpsServerLog log, FtpsServerConfiguration config)
    {
        _log = log;
        _config = config;

        // Create user directories
        foreach (var user in _config.Users)
        {
            user.Folder = new DirectoryInfo(user.Folder).FullName;

            var userPath = user.Folder.TrimStart('/');
            _log.Info($"User {user.Login} directory {userPath}");
            if (!Directory.Exists(userPath))
            {
                Directory.CreateDirectory(userPath);
                _log.Info($"Created user directory for {user.Login}: {userPath}");
            }
        }

        // Load certificate
        _serverCertificate = LoadCertificate();
    }

    public void Start()
    {
        try
        {
            var actualIp = _config.ServerSettings.Ip ?? "0.0.0.0";
            var actualPort = _config.ServerSettings.Port ?? 2121;

            _listener = new TcpListener(IPAddress.Parse(actualIp), actualPort);
            
            _listener.Start();
            _isRunning = true;

            _log.Info($"FTPS Server started successfully on {actualIp}:{actualPort} (Explicit encryption)");

            _ = Task.Run(AcceptClientsAsync);
        }
        catch (Exception ex)
        {
            _log.Fatal(ex, "Failed to start server");
            throw;
        }
    }

    public void Stop()
    {
        _isRunning = false;
        _listener?.Stop();
        _log.Info("Server stopped");
    }

    private async Task AcceptClientsAsync()
    {
        while (_isRunning)
        {
            try
            {
                var client = await _listener!.AcceptTcpClientAsync();
                var endpoint = client.Client.RemoteEndPoint;

                var actualMaxConnections = _config.ServerSettings.MaxConnections ?? 10;
                if (_activeConnections >= actualMaxConnections)
                {
                    _log.Warn($"Connection rejected from {endpoint}: Max connections reached");
                    client.Close();
                    continue;
                }

                _activeConnections++;
                _log.Info($"Client connected: {endpoint} (Active: {_activeConnections})");

                _ = Task.Run(async () =>
                {
                    try
                    {
                        await HandleClientAsync(client);
                    }
                    finally
                    {
                        _activeConnections--;
                        _log.Info($"Client disconnected: {endpoint} (Active: {_activeConnections})");
                    }
                });
            }
            catch (Exception ex)
            {
                if (_isRunning)
                {
                    _log.Error(ex, "Error accepting client");
                }
            }
        }
    }

    private async Task HandleClientAsync(TcpClient client)
    {
        var session = new FtpsServerClientSession(
            _log,
            client,
            _config.Users,
            _serverCertificate);
        
        await session.HandleAsync();
    }

    private X509Certificate2 LoadCertificate()
    {
        if (_config.ServerSettings.X509Certificate is not null)
        {
            _log.Info($"Loading certificate from X509Certificate.");
            return _config.ServerSettings.X509Certificate;
        }

        if (_config.ServerSettings.CertificateBytes is not null)
        {
            _log.Info($"Loading certificate from CertificateBytes.");
            return X509CertificateLoader.LoadCertificate(_config.ServerSettings.CertificateBytes);
        }

        if (_config.ServerSettings.CertificatePkcs12Bytes is not null)
        {
            _log.Info($"Loading certificate from CertificatePkcs12Bytes.");
            return X509CertificateLoader.LoadPkcs12(_config.ServerSettings.CertificatePkcs12Bytes, _config.ServerSettings.CertificatePassword);
        }

        if (!string.IsNullOrEmpty(_config.ServerSettings.CertificatePath))
        {
            _log.Info($"Loading certificate from {_config.ServerSettings.CertificatePath}");

            var extension = Path.GetExtension(_config.ServerSettings.CertificatePath);
            if (extension is null)
            {
                var exception = new InvalidDataException($"Certificate path extension {extension} is not recognizable! CertificatePath extension must end with .pem, .der, .pfx.");
                _log.Fatal(exception, exception.Message);
                throw exception;
            }

            switch (extension.ToLower())
            {
                case ".pem":
                case ".der":
                    return X509CertificateLoader.LoadCertificateFromFile(_config.ServerSettings.CertificatePath);

                case ".pfx":
                    return X509CertificateLoader.LoadPkcs12FromFile(_config.ServerSettings.CertificatePath, _config.ServerSettings.CertificatePassword);

                default:
                    var exception = new InvalidDataException($"Certificate path extension {extension} is not recognizable! CertificatePath extension must end with .pem, .der, .pfx.");
                    _log.Fatal(exception, exception.Message);
                    throw exception;
            }
        }

        if (_config.ServerSettings.CertificateStoreLocation is not null &&
            _config.ServerSettings.CertificateStoreName is not null &&
            _config.ServerSettings.CertificateStoreSubject is not null)
        {
            _log.Info($"Loading certificate from Certificate Store StoreName={_config.ServerSettings.CertificateStoreName} Location={_config.ServerSettings.CertificateStoreLocation} Subject={_config.ServerSettings.CertificateStoreSubject}");

            using var store = new X509Store(_config.ServerSettings.CertificateStoreName.Value, _config.ServerSettings.CertificateStoreLocation.Value);
            store.Open(OpenFlags.ReadOnly);
            var certs = store.Certificates.Find(
                X509FindType.FindBySubjectName,
                _config.ServerSettings.CertificateStoreSubject,
                false);

            if (certs.Count == 1)
            {
                _log.Info($"Certificate was found.");
                return certs[0];
            }
            if (certs.Count > 1)
            {
                var exception = new InvalidDataException($"More than 1 certificate found!");
                _log.Fatal(exception, exception.Message);
                throw exception;
            }
            else
            {
                var exception = new InvalidDataException($"No certificates found!");
                _log.Fatal(exception, exception.Message);
                throw exception;
            }
        }

        _log.Info($"Getting or creating self-signed certificate.");
        return GetOrCreateCertificate(_config.ServerSettings);
    }

    private static X509Certificate2 CreateSelfSignedServerCertificate(string password)
    {
        SubjectAlternativeNameBuilder sanBuilder = new();
        sanBuilder.AddIpAddress(IPAddress.Loopback);
        sanBuilder.AddIpAddress(IPAddress.IPv6Loopback);
        sanBuilder.AddDnsName("localhost");
        sanBuilder.AddDnsName(Environment.MachineName);

        X500DistinguishedName distinguishedName = new($"CN=FtpsServerLibrary-SelfSigned-Certificates");

        using RSA rsa = RSA.Create(2048);
        var request = new CertificateRequest(distinguishedName, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        request.CertificateExtensions.Add(
            new X509KeyUsageExtension(X509KeyUsageFlags.DataEncipherment | X509KeyUsageFlags.KeyEncipherment | X509KeyUsageFlags.DigitalSignature, false));


        request.CertificateExtensions.Add(
           new X509EnhancedKeyUsageExtension(
               [new Oid("1.3.6.1.5.5.7.3.1")], false));

        request.CertificateExtensions.Add(sanBuilder.Build());

        var certificate = request.CreateSelfSigned(new DateTimeOffset(DateTime.UtcNow.AddDays(-1)), new DateTimeOffset(DateTime.UtcNow.AddDays(3650)));
        return X509CertificateLoader.LoadPkcs12(certificate.Export(X509ContentType.Pfx, password), password, X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet);
    }

    private X509Certificate2 GetOrCreateCertificate(FtpsServerSettings ftpsServerSettings)
    {
        var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FtpsServerLibrary",
            "Certificates");
        Directory.CreateDirectory(directory);

        var certificateFile = Path.Combine(directory, "Self-Signed.pfx")!;
        var password = ftpsServerSettings.CertificatePassword ?? "test";

        X509Certificate2? certificate = null;

        if (File.Exists(certificateFile))
        {
            _log.Info($"Loading self-signed certificate from file {certificateFile}.");
            try
            {
                certificate = X509CertificateLoader.LoadPkcs12FromFile(certificateFile, password, X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet);

                // Check if certificate is still valid (with some buffer time)
                if (certificate.NotAfter > DateTime.UtcNow.AddDays(7) &&
                    certificate.NotBefore <= DateTime.UtcNow)
                {
                    return certificate;
                }

                // Certificate is expired or expiring soon
                certificate.Dispose();
                certificate = null;
            }
            catch
            {
                // Certificate is corrupted or password changed
                certificate?.Dispose();
                certificate = null;
            }
        }

        // Generate new certificate
        _log.Info($"Creating self-signed certificate for file {certificateFile}.");
        certificate = CreateSelfSignedServerCertificate(password);
        var pfxBytes = certificate.Export(X509ContentType.Pfx, password);
        File.WriteAllBytes(certificateFile, pfxBytes);

        return certificate;
    }
}
