using FtpsServerLibrary;
using FtpsServerMaui.Models;

namespace FtpsServerMaui.Services;

public class FtpsService(ILogService logService) : IFtpsService
{
    private readonly ILogService _logService = logService;
    private FtpsServer? _server;
    private bool _isRunning;

    public bool IsRunning
    {
        get => _isRunning;
        private set
        {
            if (_isRunning != value)
            {
                _isRunning = value;
                ServerStateChanged?.Invoke(this, value);
            }
        }
    }

    public event EventHandler<bool>? ServerStateChanged;

    public Task StartServerAsync(ServerConfiguration configuration)
    {
        return Task.Run(() =>
        {
            try
            {
                if (IsRunning)
                {
                    _logService.Warn("Server is already running");
                    return;
                }

                var config = new FtpsServerConfiguration
                {
                    ServerSettings = new FtpsServerSettings
                    {
                        Ip = configuration.Ip,
                        Port = configuration.Port,
                        MaxConnections = configuration.MaxConnections
                    }
                };

                // Configure certificate
                if (!configuration.UseSelfSignedCertificate && !string.IsNullOrEmpty(configuration.CertificatePath))
                {
                    config.ServerSettings.CertificatePath = configuration.CertificatePath;
                    config.ServerSettings.CertificatePassword = configuration.CertificatePassword;
                }

                // Configure users
                foreach (var user in configuration.Users)
                {
                    config.Users.Add(new FtpsServerUserAccount
                    {
                        Login = user.Username,
                        Password = user.Password,
                        Folder = user.Folder,
                        Read = user.ReadPermission,
                        Write = user.WritePermission
                    });
                }

                _server = new FtpsServer((IFtpsServerLog)_logService, config);
                _server.Start();

                IsRunning = true;
                _logService.Info($"FTPS Server started on {configuration.Ip}:{configuration.Port}");
            }
            catch (Exception ex)
            {
                _logService.Error(ex, "Failed to start FTPS server");
                IsRunning = false;
                throw;
            }
        });
    }

    public Task StopServerAsync()
    {
        return Task.Run(() =>
        {
            try
            {
                if (!IsRunning)
                {
                    _logService.Warn("Server is not running");
                    return;
                }

                _server?.Stop();
                _server = null;

                IsRunning = false;
                _logService.Info("FTPS Server stopped");
            }
            catch (Exception ex)
            {
                _logService.Error(ex, "Failed to stop FTPS server");
                throw;
            }
        });
    }
}
