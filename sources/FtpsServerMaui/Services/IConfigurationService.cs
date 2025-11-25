using FtpsServerMaui.Models;

namespace FtpsServerMaui.Services;

public interface IConfigurationService
{
    Task SaveConfigurationAsync(ServerConfiguration configuration);
    Task<ServerConfiguration?> LoadConfigurationAsync();
}
