using FtpsServerConsole.Resources;
using FtpsServerLibrary;
using System.Globalization;
using System.Security.Cryptography.X509Certificates;

namespace FtpsServerConsole;

internal sealed class CommandLineResult
{
    public required FtpsServerConfiguration Configuration { get; init; }
    public bool HelpRequested { get; init; }
    public List<string> Errors { get; init; } = [];
}

internal static class CommandLine
{
    internal static CommandLineResult Parse(string[] args)
    {
        var errors = new List<string>();
        if (args.Any(arg => arg is "--help" or "-h" or "/?"))
        {
            return new CommandLineResult
            {
                HelpRequested = true,
                Configuration = new FtpsServerConfiguration(),
                Errors = errors
            };
        }

        var config = LoadInitial(args, errors);
        ApplyArguments(args, config, errors);
        return new CommandLineResult
        {
            Configuration = config,
            Errors = errors
        };
    }

    internal static bool TryParseUser(string text, out FtpsServerUserAccount user)
    {
        var parts = text.Split('#');
        if (parts.Length != 4)
        {
            user = null!;
            return false;
        }

        var permissions = parts[3];
        user = new FtpsServerUserAccount
        {
            Login = parts[0],
            Password = parts[1],
            Folder = parts[2],
            Read = permissions.Contains('R', StringComparison.OrdinalIgnoreCase),
            Write = permissions.Contains('W', StringComparison.OrdinalIgnoreCase)
        };
        return true;
    }

    private static FtpsServerConfiguration LoadInitial(string[] args, List<string> errors)
    {
        var configIndex = Array.IndexOf(args, "--config");
        if (configIndex >= 0)
        {
            if (configIndex + 1 >= args.Length)
            {
                errors.Add(Strings.Format(Strings.ConsoleMissingOptionValue, "--config"));
                return new FtpsServerConfiguration();
            }

            var path = args[configIndex + 1];
            var loaded = ConfigurationFile.Load(path, required: true);
            if (loaded != null)
                return loaded;

            errors.Add(Strings.Format(Strings.ConsoleConfigLoadFailed, path));
            return new FtpsServerConfiguration();
        }

        if (args.Length == 0 && File.Exists("appsettings.json"))
            return ConfigurationFile.Load("appsettings.json", required: false) ?? new FtpsServerConfiguration();

        return new FtpsServerConfiguration();
    }

    private static void ApplyArguments(string[] args, FtpsServerConfiguration config, List<string> errors)
    {
        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--config":
                    i++;
                    break;

                case "--ip":
                    if (!TryTake(args, ref i, "--ip", errors, out var ip))
                        break;
                    config.ServerSettings.Ip = ip;
                    break;

                case "--port":
                    if (!TryTake(args, ref i, "--port", errors, out var portText))
                        break;
                    if (!int.TryParse(portText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var port))
                        errors.Add(Strings.Format(Strings.ConsoleInvalidNumber, "--port"));
                    else
                        config.ServerSettings.Port = port;
                    break;

                case "--maxconnections":
                    if (!TryTake(args, ref i, "--maxconnections", errors, out var maxText))
                        break;
                    if (!int.TryParse(maxText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var maxConnections))
                        errors.Add(Strings.Format(Strings.ConsoleInvalidNumber, "--maxconnections"));
                    else
                        config.ServerSettings.MaxConnections = maxConnections;
                    break;

                case "--cert":
                    if (!TryTake(args, ref i, "--cert", errors, out var cert))
                        break;
                    config.ServerSettings.CertificatePath = cert;
                    break;

                case "--certpass":
                    if (!TryTake(args, ref i, "--certpass", errors, out var certPass))
                        break;
                    config.ServerSettings.CertificatePassword = certPass;
                    break;

                case "--certstorename":
                    if (!TryTake(args, ref i, "--certstorename", errors, out var storeNameText))
                        break;
                    if (!Enum.TryParse<StoreName>(storeNameText, ignoreCase: true, out var storeName))
                        errors.Add(Strings.Format(Strings.ConsoleInvalidStoreName, storeNameText));
                    else
                        config.ServerSettings.CertificateStoreName = storeName;
                    break;

                case "--certstorelocation":
                    if (!TryTake(args, ref i, "--certstorelocation", errors, out var storeLocationText))
                        break;
                    if (!Enum.TryParse<StoreLocation>(storeLocationText, ignoreCase: true, out var storeLocation))
                        errors.Add(Strings.Format(Strings.ConsoleInvalidStoreLocation, storeLocationText));
                    else
                        config.ServerSettings.CertificateStoreLocation = storeLocation;
                    break;

                case "--certstoresubject":
                    if (!TryTake(args, ref i, "--certstoresubject", errors, out var subject))
                        break;
                    config.ServerSettings.CertificateStoreSubject = subject;
                    break;

                case "--user":
                    if (!TryTake(args, ref i, "--user", errors, out var userText))
                        break;
                    if (!TryParseUser(userText, out var user))
                        errors.Add(Strings.ConsoleUserArgument);
                    else
                        config.Users.Add(user);
                    break;
            }
        }
    }

    private static bool TryTake(string[] args, ref int index, string option, List<string> errors, out string value)
    {
        if (index + 1 >= args.Length)
        {
            errors.Add(Strings.Format(Strings.ConsoleMissingOptionValue, option));
            value = "";
            return false;
        }

        value = args[++index];
        return true;
    }
}
