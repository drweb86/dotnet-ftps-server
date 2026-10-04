using System;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace FtpsServerLibrary;

public class FtpsServer(IFtpsServerLog log, FtpsServerConfiguration config, IFtpsServerFileSystemProvider ftpsServerFileSystemProvider)
{
    private readonly FtpsServerConfiguration _config = config;
    private readonly IFtpsServerFileSystemProvider _ftpsServerFileSystemProvider = ftpsServerFileSystemProvider;
    private readonly IFtpsServerLog _log = log;
    private TcpListener? _listener;
    private bool _isRunning;
    private X509Certificate2? _serverCertificate;
    private int _activeConnections;
    private readonly FtpsLoginThrottle _loginThrottle = new();

    public X509Certificate2? LoadedCertificate { get; private set; }

    public async Task StartAsync()
    {
        try
        {
            _serverCertificate = await new FtpsCertificateLoader(_log, _ftpsServerFileSystemProvider)
                .LoadAsync(_config.ServerSettings);
            LoadedCertificate = _serverCertificate;

            // Create user directories
            foreach (var user in _config.Users)
            {
                _log.Info($"User {user.Login} directory {user.Folder}");

                if (!await _ftpsServerFileSystemProvider.DirectoryExists(user.Folder, []))
                {
                    await _ftpsServerFileSystemProvider.CreateDirectory(user.Folder, []);
                    _log.Info($"Created user directory for {user.Login}: {user.Folder}");
                }
            }

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

                if (endpoint is IPEndPoint remote && _loginThrottle.IsLocked(remote.Address, DateTime.UtcNow))
                {
                    _log.Warn($"Connection rejected from {endpoint}: too many failed logins");
                    client.Close();
                    continue;
                }

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
            _serverCertificate,
            _ftpsServerFileSystemProvider,
            _loginThrottle);
        
        await session.HandleAsync();
    }
}
