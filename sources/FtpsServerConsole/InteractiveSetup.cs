using FtpsServerConsole.Resources;
using FtpsServerLibrary;
using System.Globalization;

namespace FtpsServerConsole;

internal static class InteractiveSetup
{
    private enum FolderAccess
    {
        ReadWrite,
        Read,
        Write
    }

    internal static FtpsServerConfiguration? Run(FtpsServerConfiguration seed)
    {
        var previousCancel = Console.TreatControlCAsInput;
        try
        {
            Console.TreatControlCAsInput = true;
            CliPrompt.Begin();
            return Ask(seed);
        }
        catch (OperationCanceledException)
        {
            CliPrompt.Outro(Strings.ConsoleCancelled);
            return null;
        }
        finally
        {
            CliPrompt.End();
            Console.TreatControlCAsInput = previousCancel;
        }
    }

    private static FtpsServerConfiguration Ask(FtpsServerConfiguration seed)
    {
        var settings = seed.ServerSettings;
        CliPrompt.Intro(Strings.ConsoleInteractiveTitle, Strings.ConsoleInteractiveHint);

        settings.Ip = CliPrompt.AskText(
            Strings.ConsolePromptIp,
            hint: null,
            defaultValue: string.IsNullOrWhiteSpace(settings.Ip) ? "0.0.0.0" : settings.Ip.Trim(),
            allowEmpty: false,
            secret: false,
            validate: value => System.Net.IPAddress.TryParse(value.Trim(), out _) ? null : Strings.ConsoleInvalidIp).Trim();

        settings.Port = AskInt(
            Strings.ConsolePromptPort,
            settings.Port ?? 2121,
            value => value is >= ConfigurationValidator.PortMinimum and <= ConfigurationValidator.PortMaximum
                ? null
                : Strings.Format(Strings.ConsoleInvalidPort, ConfigurationValidator.PortMinimum, ConfigurationValidator.PortMaximum));

        settings.MaxConnections = AskInt(
            Strings.ConsolePromptMaxConnections,
            settings.MaxConnections ?? 10,
            value => value is >= ConfigurationValidator.MaxConnectionsMinimum and <= FtpsServerSettings.MaxConnectionsUpperBound
                ? null
                : Strings.Format(
                    Strings.ConsoleInvalidMaxConnections,
                    ConfigurationValidator.MaxConnectionsMinimum,
                    FtpsServerSettings.MaxConnectionsUpperBound));

        var previousCert = settings.CertificatePath;
        var certPath = CliPrompt.AskText(
            Strings.ConsolePromptCertPath,
            Strings.ConsoleCertEmptyHint,
            defaultValue: string.IsNullOrWhiteSpace(previousCert) ? null : previousCert.Trim(),
            allowEmpty: true,
            secret: false,
            validate: ValidateCertificatePath).Trim();
        settings.CertificatePath = string.IsNullOrEmpty(certPath) ? null : certPath;

        if (!string.IsNullOrEmpty(settings.CertificatePath))
        {
            var password = CliPrompt.AskText(
                Strings.ConsolePromptCertPassword,
                Strings.ConsoleCertPasswordHint,
                defaultValue: null,
                allowEmpty: true,
                secret: true,
                validate: null).Trim();
            if (password.Length > 0)
                settings.CertificatePassword = password;
        }

        seed.Users.Clear();
        CliPrompt.Section(Strings.ConsoleUsersTitle, Strings.ConsoleUsersHint);
        var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        while (true)
        {
            var username = CliPrompt.AskText(
                Strings.ConsolePromptUsername,
                Strings.ConsoleUsernameHint,
                defaultValue: null,
                allowEmpty: true,
                secret: false,
                validate: value =>
                {
                    var login = value.Trim();
                    if (login.Length == 0)
                        return null;
                    return seed.Users.Any(user => user.Login.Equals(login, StringComparison.OrdinalIgnoreCase))
                        ? Strings.Format(Strings.ConsoleDuplicateUser, login)
                        : null;
                }).Trim();

            if (username.Length == 0)
            {
                if (seed.Users.Count == 0)
                {
                    CliPrompt.Error(Strings.ConsoleUsersRequired);
                    continue;
                }

                break;
            }

            var password = CliPrompt.AskText(
                Strings.ConsolePromptPassword,
                hint: null,
                defaultValue: null,
                allowEmpty: false,
                secret: true,
                validate: value => string.IsNullOrWhiteSpace(value) ? Strings.ConsolePasswordRequired : null).Trim();

            var folder = AskFolder(string.IsNullOrWhiteSpace(desktop) ? null : desktop);
            var access = CliPrompt.AskSelect(
                Strings.ConsolePromptPermissions,
                [
                    (FolderAccess.ReadWrite, Strings.ConsolePermissionReadWrite),
                    (FolderAccess.Read, Strings.ConsolePermissionRead),
                    (FolderAccess.Write, Strings.ConsolePermissionWrite)
                ],
                selectedIndex: 0);

            seed.Users.Add(new FtpsServerUserAccount
            {
                Login = username,
                Password = password,
                Folder = folder,
                Read = access is FolderAccess.ReadWrite or FolderAccess.Read,
                Write = access is FolderAccess.ReadWrite or FolderAccess.Write
            });
            var accessLabel = access switch
            {
                FolderAccess.Read => Strings.ConsolePermissionRead,
                FolderAccess.Write => Strings.ConsolePermissionWrite,
                _ => Strings.ConsolePermissionReadWrite
            };
            CliPrompt.Success(Strings.Format(Strings.ConsoleUserAdded, username, accessLabel));
        }

        CliPrompt.Outro(Strings.Format(Strings.ConsoleConfigComplete, seed.Users.Count));
        OfferSave(seed);
        return seed;
    }

    private static int AskInt(string label, int defaultValue, Func<int, string?> validate)
    {
        var text = CliPrompt.AskText(
            label,
            hint: null,
            defaultValue: defaultValue.ToString(CultureInfo.CurrentCulture),
            allowEmpty: false,
            secret: false,
            validate: value => int.TryParse(value.Trim(), NumberStyles.Integer, CultureInfo.CurrentCulture, out var number)
                ? validate(number)
                : Strings.Format(Strings.ConsoleInvalidNumber, label));
        return int.Parse(text.Trim(), NumberStyles.Integer, CultureInfo.CurrentCulture);
    }

    private static string? ValidateCertificatePath(string value)
    {
        var path = value.Trim();
        if (path.Length == 0)
            return null;
        if (Directory.Exists(path))
            return Strings.Format(Strings.ConsoleCertIsFolder, path);
        if (!File.Exists(path))
            return Strings.Format(Strings.ConsoleCertMissing, path);
        if (!ConfigurationValidator.HasCertificateExtension(path))
            return Strings.ConsoleCertExtension;
        return null;
    }

    private static string AskFolder(string? defaultFolder)
    {
        while (true)
        {
            var folder = CliPrompt.AskText(
                Strings.ConsolePromptFolder,
                hint: null,
                defaultValue: defaultFolder,
                allowEmpty: false,
                secret: false,
                validate: value => string.IsNullOrWhiteSpace(value) ? Strings.ConsoleFolderRequired : null).Trim();

            if (File.Exists(folder))
            {
                CliPrompt.Error(Strings.Format(Strings.ConsoleFolderIsFile, folder));
                continue;
            }

            if (Directory.Exists(folder))
                return folder;

            if (!CliPrompt.AskConfirm(Strings.Format(Strings.ConsoleCreateFolder, folder), defaultYes: true))
            {
                CliPrompt.Error(Strings.Format(Strings.ConsoleFolderMissing, folder));
                continue;
            }

            try
            {
                Directory.CreateDirectory(folder);
                CliPrompt.Success(Strings.Format(Strings.ConsoleFolderCreated, folder));
                return folder;
            }
            catch (Exception ex)
            {
                CliPrompt.Error(Strings.Format(Strings.ConsoleFolderCreateFailed, ex.Message));
            }
        }
    }

    private static void OfferSave(FtpsServerConfiguration config)
    {
        if (!CliPrompt.AskConfirm(Strings.ConsoleSaveConfig, defaultYes: false))
            return;

        while (true)
        {
            var fileName = CliPrompt.AskText(
                Strings.ConsolePromptConfigFile,
                hint: null,
                defaultValue: "ftps-config.json",
                allowEmpty: false,
                secret: false,
                validate: value => string.IsNullOrWhiteSpace(value) ? Strings.ConsolePromptConfigFile : null).Trim();

            if (!fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                fileName += ".json";

            if (File.Exists(fileName) &&
                !CliPrompt.AskConfirm(Strings.Format(Strings.ConsoleReplaceFile, fileName), defaultYes: false))
                continue;

            if (!ConfigurationFile.Save(config, fileName))
                return;

            var fullPath = Path.GetFullPath(fileName);
            CliPrompt.Success(Strings.Format(Strings.ConsoleConfigSaved, fullPath));
            if (SecretProtectorSupported())
                CliPrompt.Note(Strings.ConsoleSecretsEncrypted);
            CliPrompt.Note(Strings.Format(Strings.ConsoleConfigRunHint, fileName));
            return;
        }
    }

    private static bool SecretProtectorSupported() => FtpsServerAppsShared.Security.SecretProtector.IsSupported;
}
