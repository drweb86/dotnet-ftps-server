using FtpsServerMaui.Models;

namespace FtpsServerMaui.Services;

public interface IFtpsService
{
    bool IsRunning { get; }
    event EventHandler<bool>? ServerStateChanged;

    Task StartServerAsync(ServerConfiguration configuration);
    Task StopServerAsync();
}
