using FtpsServerAppsShared.Security;
using FtpsServerWindows.Models;
using System;
using System.IO;
using System.Text.Json;

namespace FtpsServerWindows.Services
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
                    var settings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                    var migrate = HasPlaintextSecrets(settings);
                    UnprotectSecrets(settings);
                    if (migrate)
                        SaveSettings(settings);
                    return settings;
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
                var copy = JsonSerializer.Deserialize<AppSettings>(JsonSerializer.Serialize(settings)) ?? new AppSettings();
                ProtectSecrets(copy);
                File.WriteAllText(SettingsFile, JsonSerializer.Serialize(copy));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving settings: {ex.Message}");
            }
        }

        private static bool HasPlaintextSecrets(AppSettings settings)
        {
            if (SecretProtector.IsPlaintextSecret(settings.CertificatePassword))
                return true;

            return settings.Users?.Any(user => SecretProtector.IsPlaintextSecret(user.Password)) == true;
        }

        private static void UnprotectSecrets(AppSettings settings)
        {
            settings.CertificatePassword = SecretProtector.Unprotect(settings.CertificatePassword);
            if (settings.Users == null)
                return;

            foreach (var user in settings.Users)
                user.Password = SecretProtector.Unprotect(user.Password);
        }

        private static void ProtectSecrets(AppSettings settings)
        {
            settings.CertificatePassword = SecretProtector.Protect(settings.CertificatePassword);
            if (settings.Users == null)
                return;

            foreach (var user in settings.Users)
                user.Password = SecretProtector.Protect(user.Password);
        }
    }
}
