using FtpsServerAppsShared.Security;
using FtpsServerConsole.Resources;
using FtpsServerLibrary;
using NLog;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FtpsServerConsole;

internal static class ConfigurationFile
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    internal static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Converters =
        {
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
        }
    };

    private static readonly JsonSerializerOptions SaveOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters =
        {
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
        }
    };

    internal static FtpsServerConfiguration? Load(string path, bool required)
    {
        try
        {
            var json = File.ReadAllText(path);
            var config = JsonSerializer.Deserialize<FtpsServerConfiguration>(json, SerializerOptions);
            if (config == null)
            {
                Report(required, FailureMessage(path, required), null);
                return null;
            }

            UnprotectSecrets(config, path);
            Logger.Info(Strings.Format(Strings.ConsoleConfigLoaded, path));
            return config;
        }
        catch (Exception ex)
        {
            Report(required, FailureMessage(path, required), ex);
            return null;
        }
    }

    private static string FailureMessage(string path, bool required) =>
        required
            ? Strings.Format(Strings.ConsoleConfigLoadFailed, path)
            : Strings.ConsoleAppSettingsFailed;

    internal static bool Save(FtpsServerConfiguration config, string fileName)
    {
        try
        {
            var toSave = SecretProtector.IsSupported && HasPlaintextSecrets(config)
                ? CopyWithProtectedSecrets(config)
                : config;
            var json = JsonSerializer.Serialize(toSave, SaveOptions);
            File.WriteAllText(fileName, json);
            Logger.Info(Strings.Format(Strings.ConsoleConfigSavedLog, fileName));
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, Strings.Format(Strings.ConsoleConfigSaveFailed, ex.Message));
            CliPrompt.Error(Strings.Format(Strings.ConsoleConfigSaveFailed, ex.Message));
            return false;
        }
    }

    private static void Report(bool required, string message, Exception? exception)
    {
        if (exception == null)
        {
            if (required)
                Logger.Error(message);
            else
                Logger.Warn(message);
            return;
        }

        if (required)
            Logger.Error(exception, message);
        else
            Logger.Warn(exception, message);
    }

    // Secrets in the file are "enc::" values protected with an OS key. Plaintext still
    // loads, so older and hand-written files keep working.
    private static void UnprotectSecrets(FtpsServerConfiguration config, string path)
    {
        var hadPlaintext = HasPlaintextSecrets(config);

        if (!string.IsNullOrEmpty(config.ServerSettings.CertificatePassword))
            config.ServerSettings.CertificatePassword = SecretProtector.Unprotect(config.ServerSettings.CertificatePassword);
        foreach (var user in config.Users)
        {
            if (!string.IsNullOrEmpty(user.Password))
                user.Password = SecretProtector.Unprotect(user.Password);
        }

        if (hadPlaintext && SecretProtector.IsSupported)
            Logger.Warn(Strings.Format(Strings.ConsolePlaintextSecrets, path));
    }

    private static bool HasPlaintextSecrets(FtpsServerConfiguration config)
    {
        if (SecretProtector.IsPlaintextSecret(config.ServerSettings.CertificatePassword))
            return true;
        return config.Users.Any(user => SecretProtector.IsPlaintextSecret(user.Password));
    }

    private static FtpsServerConfiguration CopyWithProtectedSecrets(FtpsServerConfiguration config)
    {
        var copy = JsonSerializer.Deserialize<FtpsServerConfiguration>(
            JsonSerializer.Serialize(config, SerializerOptions), SerializerOptions) ?? new FtpsServerConfiguration();

        if (SecretProtector.IsPlaintextSecret(copy.ServerSettings.CertificatePassword))
            copy.ServerSettings.CertificatePassword = SecretProtector.Protect(copy.ServerSettings.CertificatePassword);
        foreach (var user in copy.Users)
        {
            if (SecretProtector.IsPlaintextSecret(user.Password))
                user.Password = SecretProtector.Protect(user.Password);
        }

        return copy;
    }
}
