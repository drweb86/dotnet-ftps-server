using FtpsServerConsole.Resources;
using FtpsServerLibrary;

namespace FtpsServerConsole;

internal static class ConfigurationValidator
{
    // Same limits as the desktop and Avalonia configuration screen.
    internal const int PortMinimum = 2121;
    internal const int PortMaximum = 65535;
    internal const int MaxConnectionsMinimum = 2;

    internal static List<string> Validate(FtpsServerConfiguration config)
    {
        var errors = new List<string>();
        var settings = config.ServerSettings;

        if (settings.Port is int port && (port < PortMinimum || port > PortMaximum))
            errors.Add(Strings.Format(Strings.ConsoleInvalidPort, PortMinimum, PortMaximum));

        if (settings.MaxConnections is int maxConnections &&
            (maxConnections < MaxConnectionsMinimum || maxConnections > FtpsServerSettings.MaxConnectionsUpperBound))
        {
            errors.Add(Strings.Format(
                Strings.ConsoleInvalidMaxConnections,
                MaxConnectionsMinimum,
                FtpsServerSettings.MaxConnectionsUpperBound));
        }

        if (settings.Ip is not null && !System.Net.IPAddress.TryParse(settings.Ip, out _))
            errors.Add(Strings.ConsoleInvalidIp);

        ValidateCertificate(settings, errors);
        ValidateUsers(config, errors);
        return errors;
    }

    private static void ValidateCertificate(FtpsServerSettings settings, List<string> errors)
    {
        var path = settings.CertificatePath;
        if (!string.IsNullOrWhiteSpace(path))
        {
            if (Directory.Exists(path))
                errors.Add(Strings.Format(Strings.ConsoleCertIsFolder, path));
            else if (!File.Exists(path))
                errors.Add(Strings.Format(Strings.ConsoleCertMissing, path));
            else if (!HasCertificateExtension(path))
                errors.Add(Strings.ConsoleCertExtension);
        }

        var hasName = settings.CertificateStoreName is not null;
        var hasLocation = settings.CertificateStoreLocation is not null;
        var hasSubject = !string.IsNullOrWhiteSpace(settings.CertificateStoreSubject);
        if ((hasName || hasLocation || hasSubject) && !(hasName && hasLocation && hasSubject))
            errors.Add(Strings.ConsoleCertStoreIncomplete);
    }

    internal static bool HasCertificateExtension(string path)
    {
        var extension = Path.GetExtension(path);
        return extension.Equals(".pfx", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".pem", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".der", StringComparison.OrdinalIgnoreCase);
    }

    private static void ValidateUsers(FtpsServerConfiguration config, List<string> errors)
    {
        if (config.Users.Count == 0)
            errors.Add(Strings.ConsoleUsersRequired);

        foreach (var user in config.Users)
            ValidateUser(user, errors);

        foreach (var group in config.Users
            .Select(user => user.Login?.Trim())
            .Where(login => !string.IsNullOrEmpty(login))
            .GroupBy(login => login!, StringComparer.OrdinalIgnoreCase))
        {
            if (group.Count() > 1)
                errors.Add(Strings.Format(Strings.ConsoleDuplicateUser, group.First()));
        }
    }

    private static void ValidateUser(FtpsServerUserAccount user, List<string> errors)
    {
        void Add(string message)
        {
            errors.Add(string.IsNullOrWhiteSpace(user.Login)
                ? message
                : Strings.Format(Strings.ConsoleUserIssue, user.Login, message));
        }

        if (string.IsNullOrWhiteSpace(user.Login))
            Add(Strings.ConsoleUserNameRequired);
        if (string.IsNullOrWhiteSpace(user.Password))
            Add(Strings.ConsolePasswordRequired);
        if (!user.Read && !user.Write)
            Add(Strings.ConsolePermissionRequired);

        if (string.IsNullOrWhiteSpace(user.Folder))
            Add(Strings.ConsoleFolderRequired);
        else if (File.Exists(user.Folder))
            Add(Strings.Format(Strings.ConsoleFolderIsFile, user.Folder));
        else if (!Directory.Exists(user.Folder))
            Add(Strings.Format(Strings.ConsoleFolderMissing, user.Folder));
    }
}
