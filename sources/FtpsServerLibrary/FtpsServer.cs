using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace FtpsServerLibrary;

public class FtpsServer(IFtpsServerLog log, FtpsServerConfiguration config, IFtpsServerFileSystemProvider ftpsServerFileSystemProvider)
{
    private readonly FtpsServerConfiguration _config = config;
    private readonly IFtpsServerFileSystemProvider _ftpsServerFileSystemProvider = ftpsServerFileSystemProvider;
    private readonly IFtpsServerLog _log = log;
    private readonly List<TcpListener> _listeners = new();
    private volatile bool _isRunning;
    private X509Certificate2? _serverCertificate;
    private int _activeConnections;
    private int _maxConnections = 10;
    private readonly FtpsLoginThrottle _loginThrottle = new();
    private readonly object _sessionsLock = new();
    private readonly List<FtpsServerClientSession> _sessions = new();

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

            var configuredMax = _config.ServerSettings.MaxConnections;
            _maxConnections = FtpsServerSettings.EffectiveMaxConnections(configuredMax);
            if (configuredMax is int requested && requested != _maxConnections)
                _log.Warn($"Max connections {requested} is outside 1..{FtpsServerSettings.MaxConnectionsUpperBound}. Using {_maxConnections}.");

            var actualIp = _config.ServerSettings.Ip ?? "0.0.0.0";
            var actualPort = _config.ServerSettings.Port ?? 2121;

            StartListeners(actualIp, actualPort);
            _isRunning = true;

            var bound = new System.Text.StringBuilder();
            foreach (var listener in _listeners)
            {
                if (bound.Length > 0)
                    bound.Append(" and ");
                bound.Append(listener.LocalEndpoint);
            }

            _log.Info($"FTPS Server started successfully on {bound} (Explicit encryption)");

            foreach (var listener in _listeners.ToArray())
                _ = Task.Run(() => AcceptClientsAsync(listener));
        }
        catch (Exception ex)
        {
            _log.Fatal(ex, "Failed to start server");
            throw;
        }
    }

    public void Stop()
    {
        FtpsServerClientSession[] sessions;
        lock (_sessionsLock)
        {
            _isRunning = false;
            sessions = _sessions.ToArray();
        }

        foreach (var listener in _listeners.ToArray())
        {
            try
            {
                listener.Stop();
            }
            catch (Exception)
            {
            }
        }

        _listeners.Clear();

        foreach (var session in sessions)
            session.Abort();

        // A certificate the library loaded keeps its private key in a temporary key container
        // on Windows; disposing it removes that container. A caller-supplied one stays theirs.
        if (_serverCertificate is not null && !ReferenceEquals(_serverCertificate, _config.ServerSettings.X509Certificate))
            _serverCertificate.Dispose();
        _serverCertificate = null;
        LoadedCertificate = null;

        _log.Info("Server stopped");
    }

    // 0.0.0.0 and :: mean every interface. One dual-stack socket accepts both families.
    // When the OS refuses a dual-stack socket, IPv4 and IPv6 are bound separately.
    private void StartListeners(string configuredIp, int port)
    {
        if (!IsWildcard(configuredIp))
        {
            Listen(IPAddress.Parse(configuredIp), port, dualStack: false);
            return;
        }

        try
        {
            Listen(IPAddress.IPv6Any, port, dualStack: true);
            return;
        }
        catch (Exception ex)
        {
            _log.Warn($"Dual-stack listen on [::]:{port} failed ({ex.Message}). Binding IPv4 and IPv6 separately.");
        }

        Exception? failure = null;
        try
        {
            Listen(IPAddress.Any, port, dualStack: false);
        }
        catch (Exception ex)
        {
            failure = ex;
            _log.Warn($"IPv4 listen on 0.0.0.0:{port} failed ({ex.Message}).");
        }

        try
        {
            Listen(IPAddress.IPv6Any, port, dualStack: false);
        }
        catch (Exception ex)
        {
            failure ??= ex;
            _log.Warn($"IPv6 listen on [::]:{port} failed ({ex.Message}).");
        }

        if (_listeners.Count == 0)
            throw failure ?? new InvalidOperationException("Could not bind a listen socket.");
    }

    private void Listen(IPAddress address, int port, bool dualStack)
    {
        var listener = new TcpListener(address, port);
        if (address.AddressFamily == AddressFamily.InterNetworkV6)
            listener.Server.DualMode = dualStack;
        listener.Start();
        _listeners.Add(listener);
    }

    private static bool IsWildcard(string configuredIp)
    {
        var text = configuredIp.Trim();
        return text.Length == 0 || text is "0.0.0.0" or "::" or "::0";
    }

    private async Task AcceptClientsAsync(TcpListener listener)
    {
        while (_isRunning)
        {
            try
            {
                var client = await listener.AcceptTcpClientAsync();
                var endpoint = client.Client.RemoteEndPoint;

                if (endpoint is IPEndPoint remote && _loginThrottle.IsLocked(remote.Address, DateTime.UtcNow))
                {
                    _log.Warn($"Connection rejected from {endpoint}: too many failed logins");
                    client.Close();
                    continue;
                }

                // Sessions decrement the counter on their own threads, so every change is atomic.
                if (Volatile.Read(ref _activeConnections) >= _maxConnections)
                {
                    _log.Warn($"Connection rejected from {endpoint}: Max connections reached");
                    client.Close();
                    continue;
                }

                var active = Interlocked.Increment(ref _activeConnections);
                _log.Info($"Client connected: {endpoint} (Active: {active})");

                var session = new FtpsServerClientSession(
                    _log,
                    client,
                    _config.Users,
                    _serverCertificate,
                    _ftpsServerFileSystemProvider,
                    _loginThrottle);

                lock (_sessionsLock)
                {
                    if (!_isRunning)
                    {
                        active = Interlocked.Decrement(ref _activeConnections);
                        client.Close();
                        _log.Info($"Client disconnected: {endpoint} (Active: {active})");
                        continue;
                    }

                    _sessions.Add(session);
                }

                _ = Task.Run(async () =>
                {
                    try
                    {
                        await session.HandleAsync();
                    }
                    finally
                    {
                        lock (_sessionsLock)
                            _sessions.Remove(session);
                        var remaining = Interlocked.Decrement(ref _activeConnections);
                        _log.Info($"Client disconnected: {endpoint} (Active: {remaining})");
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

}
