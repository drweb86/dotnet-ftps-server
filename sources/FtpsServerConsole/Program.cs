using FtpsServerApp.Helpers;
using FtpsServerAppsShared.Helpers;
using FtpsServerConsole.Resources;
using FtpsServerLibrary;
using NLog;
using System.Globalization;
using System.Reflection;

namespace FtpsServerConsole;

class Program
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    static async Task Main(string[] args)
    {
        try
        {
            ShowBanner();
            var parsed = CommandLine.Parse(args);
            if (parsed.HelpRequested)
            {
                ShowHelp();
                return;
            }

            if (parsed.Errors.Count > 0)
            {
                ReportErrors(parsed.Errors);
                Environment.ExitCode = 1;
                return;
            }

            var config = parsed.Configuration;
            if (config.Users.Count == 0)
            {
                ShowHelp();
                config = InteractiveSetup.Run(config) ?? throw new OperationCanceledException();
            }

            var errors = ConfigurationValidator.Validate(config);
            if (errors.Count > 0)
            {
                ReportErrors(errors);
                Environment.ExitCode = 1;
                return;
            }

            var server = new FtpsServer(new FileLog(), config, new FtpsServerFileSystemProvider());
            DescribeStartup(config);
            await server.StartAsync();
            DescribeCertificate(server);

            CliPrompt.Note(Strings.ConsolePressQ);
            while (Console.ReadKey(true).Key != ConsoleKey.Q) { }

            Logger.Info(Strings.ConsoleShuttingDown);
            server.Stop();
            Logger.Info(Strings.ConsoleServerStopped);
        }
        catch (OperationCanceledException)
        {
            Logger.Info(Strings.ConsoleCancelled);
            Environment.ExitCode = 1;
        }
        catch (Exception ex)
        {
            Logger.Fatal(ex, Strings.Format(Strings.ConsoleFatal, ex.Message));
            CliPrompt.Error(Strings.Format(Strings.ConsoleFatal, ex.Message));
            Environment.ExitCode = 1;
        }
        finally
        {
            LogManager.Shutdown();
        }
    }

    static void ShowBanner()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0";
        // The product name stays in English in every language.
        Console.WriteLine("FTPS Server " + Strings.Format(Strings.ConsoleCopyright, version, DateTime.Now.Year));
    }

    static void ShowHelp() => Console.WriteLine(Strings.ConsoleHelp);

    static void ReportErrors(IReadOnlyList<string> errors)
    {
        CliPrompt.Error(Strings.ConsoleValidationFailed);
        foreach (var error in errors)
        {
            CliPrompt.Error(error);
            Logger.Error(error);
        }
    }

    static void DescribeStartup(FtpsServerConfiguration config)
    {
        Logger.Info(Strings.ConsoleServerStarting);
        var listenAll = config.ServerSettings.Ip is null or "" or "0.0.0.0" or "::" or "::0";
        var ipAddress = listenAll ? Strings.ConsoleListenAll : config.ServerSettings.Ip;
        Logger.Info(Strings.Format(Strings.ConsoleIpAddress, ipAddress));
        if (listenAll)
        {
            foreach (var ni in NetworkHelper.GetMyLocalIps())
            {
                var ips = string.Join("; ", ni.Addresses);
                Logger.Info(Strings.Format(Strings.ConsoleNetworkLine, ni.InterfaceType, ni.Name, ni.Description, ips));
            }
        }

        Logger.Info(Strings.Format(Strings.ConsoleMachineName, Environment.MachineName));
        Logger.Info(Strings.Format(Strings.ConsolePortLabel, config.ServerSettings.Port));
        Logger.Info(Strings.Format(Strings.ConsoleUsersConfigured, config.Users.Count));
        Logger.Info(Strings.ConsoleEncryption);
    }

    static void DescribeCertificate(FtpsServer server)
    {
        if (server.LoadedCertificate == null)
            return;

        var certInfo = CertificateInfoHelper.GetInfo(server.LoadedCertificate);
        if (!certInfo.IsSelfSigned)
            return;

        Logger.Warn(Strings.ConsoleCertNoticeTitle);
        Logger.Warn(Strings.ConsoleCertNoticeBody);
        Logger.Info(Strings.ConsoleCertSection);
        Logger.Info(Strings.Format(Strings.ConsoleCertType, Strings.ConsoleCertTypeSelfSigned));
        Logger.Info(Strings.Format(Strings.ConsoleCertSubject, certInfo.Subject));
        Logger.Info(Strings.Format(Strings.ConsoleCertIssuer, certInfo.Issuer));
        Logger.Info(Strings.Format(Strings.ConsoleCertValid, certInfo.ValidFrom, certInfo.ValidTo));
        Logger.Info(Strings.Format(Strings.ConsoleCertSerial, certInfo.SerialNumber));
        Logger.Info(Strings.Format(Strings.ConsoleCertSha256, certInfo.Sha256Fingerprint));
        Logger.Info(Strings.Format(Strings.ConsoleCertSha1, certInfo.Sha1Fingerprint));
    }
}
