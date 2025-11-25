using FtpsServerMaui.Models;
using System.Text.Json;

namespace FtpsServerMaui.Services;

public class ConfigurationService : IConfigurationService
{
    private readonly string _configFilePath;

    public ConfigurationService()
    {
        var appDataPath = FileSystem.AppDataDirectory;
        _configFilePath = Path.Combine(appDataPath, "server-config.json");
    }

    public async Task SaveConfigurationAsync(ServerConfiguration configuration)
    {
        try
        {
            var json = JsonSerializer.Serialize(configuration, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            await File.WriteAllTextAsync(_configFilePath, json);
        }
        catch (Exception ex)
        {
            // Log error but don't throw
            System.Diagnostics.Debug.WriteLine($"Failed to save configuration: {ex.Message}");
        }
    }

    public async Task<ServerConfiguration?> LoadConfigurationAsync()
    {
        try
        {
            if (!File.Exists(_configFilePath))
                return null;

            var json = await File.ReadAllTextAsync(_configFilePath);
            return JsonSerializer.Deserialize<ServerConfiguration>(json);
        }
        catch (Exception ex)
        {
            // Log error but don't throw
            System.Diagnostics.Debug.WriteLine($"Failed to load configuration: {ex.Message}");
            return null;
        }
    }
}
