using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;

namespace FtpsServerLibrary;

public class FtpsServer
{
    private readonly FtpsServerConfiguration _config;
    private readonly IFtpsServerLog _log;
    private TcpListener? _listener;
    private bool _isRunning;
    private readonly X509Certificate2? _serverCertificate;
    private int _activeConnections;

    public FtpsServer(IFtpsServerLog log, FtpsServerConfiguration config)
    {
        _log = log;
        _config = config;

        // Create user directories
        foreach (var user in _config.Users)
        {
            var userPath = user.RootFolder.TrimStart('/');
            if (!Directory.Exists(userPath))
            {
                Directory.CreateDirectory(userPath);
                _log.Info($"Created user directory for {user.Username}: {userPath}");
            }
        }

        // Load certificate
        _serverCertificate = LoadCertificate();
    }

    public void Start()
    {
        try
        {
            _listener = new TcpListener(
                IPAddress.Parse(_config.ServerSettings.IpAddress),
                _config.ServerSettings.Port);
            
            _listener.Start();
            _isRunning = true;

            _log.Info($"Server started successfully on {_config.ServerSettings.IpAddress}:{_config.ServerSettings.Port}");

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
                
                if (_activeConnections >= _config.ServerSettings.MaxConnections)
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

    private X509Certificate2? LoadCertificate()
    {
        try
        {
            if (!string.IsNullOrEmpty(_config.ServerSettings.CertificatePath))
            {
                _log.Info($"Loading certificate from {_config.ServerSettings.CertificatePath}");

                var cert = string.IsNullOrEmpty(_config.ServerSettings.CertificatePassword)
                    ? X509CertificateLoader.LoadCertificateFromFile(_config.ServerSettings.CertificatePath)
                    : X509CertificateLoader.LoadPkcs12FromFile(_config.ServerSettings.CertificatePath, _config.ServerSettings.CertificatePassword);

                _log.Info($"Certificate loaded: {cert.Subject}");
                return cert;
            }

            // Try to find certificate in store
            using (var store = new X509Store(StoreName.My, StoreLocation.CurrentUser))
            {
                store.Open(OpenFlags.ReadOnly);
                var certs = store.Certificates.Find(
                    X509FindType.FindBySubjectName,
                    Environment.MachineName,
                    false);

                if (certs.Count > 0)
                {
                    _log.Info($"Using certificate from store: {certs[0].Subject}");
                    return certs[0];
                }
            }

            _log.Warn("No certificate found. TLS will not be available.");
            _log.Warn($"Generate a certificate using: New-SelfSignedCertificate -DnsName '{Environment.MachineName}' -CertStoreLocation 'Cert:\\CurrentUser\\My'");
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Error loading certificate");
        }

        return null;
    }
}
