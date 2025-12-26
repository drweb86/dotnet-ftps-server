using FtpsServerAvalonia.Models;
using System;
using System.IO;
using System.Text.Json;

namespace FtpsServerAvalonia.Services
{
    public class SettingsManager
    {
        private static readonly string SettingsDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FtpsServerApp");
        
        private static readonly string SettingsFile = Path.Combine(SettingsDirectory, "settings.json");

        public static AppSettings LoadSettings()
        {
            try
            {
                if (File.Exists(SettingsFile))
                {
                    var json = File.ReadAllText(SettingsFile);
                    return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading settings: {ex.Message}");
            }

            return new AppSettings();
        }

        public static void SaveSettings(AppSettings settings)
        {
            try
            {
                Directory.CreateDirectory(SettingsDirectory);
                var json = JsonSerializer.Serialize(settings);
                File.WriteAllText(SettingsFile, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving settings: {ex.Message}");
            }
        }
    }
}
