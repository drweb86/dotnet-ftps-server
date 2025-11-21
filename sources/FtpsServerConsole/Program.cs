using FtpsServerLibrary;
using NLog;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FtpsServerConsole;

class Program
{
    private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

    static void Main(string[] args)
    {
        try
        {
            ShowBanner();
            
            // Parse configuration
            var config = LoadConfiguration(args);
            
            if (config == null)
            {
                ShowHelp();
                return;
            }

            // Validate configuration
            if (!ValidateConfiguration(config))
            {
                _logger.Error("Configuration validation failed");
                return;
            }

            // Create and start server
            var server = new FtpsServer(new Log(), config);
            
            _logger.Info("═══════════════════════════════════════════════════════");
            _logger.Info($"FTPS Server Starting...");
            _logger.Info($"IP Address: {config.ServerSettings.Ip}");
            _logger.Info($"Port: {config.ServerSettings.Port}");
            _logger.Info($"Users Configured: {config.Users.Count}");
            _logger.Info("═══════════════════════════════════════════════════════");
            
            server.Start();
            
            Console.WriteLine("\nPress 'Q' to stop the server...");
            while (Console.ReadKey(true).Key != ConsoleKey.Q) { }
            
            _logger.Info("Shutting down server...");
            server.Stop();
            _logger.Info("Server stopped");
        }
        catch (Exception ex)
        {
            _logger.Fatal(ex, "Fatal error starting server");
            Console.WriteLine($"Fatal error: {ex.Message}");
        }
        finally
        {
            LogManager.Shutdown();
        }
    }

    static void ShowBanner()
    {
        Console.WriteLine(@"
╔═══════════════════════════════════════════════════════╗
║                                                       ║
║              FTPS Server with NLog                    ║
║              Version 2.0                              ║
║                                                       ║
╚═══════════════════════════════════════════════════════╝
");
    }

    static void ShowHelp()
    {
        Console.WriteLine(@"
Usage:
  ftps-server [options]
  ftps-server --config <path-to-json>

Options:
  --config <path>              Path to JSON configuration file

  --ip <address>
  The IP address server will be listening to.
  Optional parameter.
  Default value: 0.0.0.0.
  0.0.0.0 - listen on every available network interface.

  --port <number>
  The Port for server to listen to.
  Optional parameter.
  Default value: 2121.

  --cert <path>                Certificate file path (.pfx)
  --certpass <password>        Certificate password
  --user <name:pass:folder:permissions>    Add user
                               Permissions format: RW (Read,Write)
                               Example: admin:pass123:/home/admin:RWDCDR
  --help                       Show this help message

Examples:
  ftps-server --config appsettings.json
  ftps-server --ip 0.0.0.0 --port 21
  ftps-server --user admin:pass123:/home/admin:RW --user guest:guest:/public:R
  ftps-server --cert server.pfx --certpass mypassword

If no arguments are provided, the server looks for 'appsettings.json' in the current directory.
");
    }

    static FtpsServerConfiguration? LoadConfiguration(string[] args)
    {
        FtpsServerConfiguration? config;

        // Check for help
        if (args.Any(a => a == "--help" || a == "-h" || a == "/?"))
        {
            return null;
        }

        // Check for config file argument
        var configIndex = Array.IndexOf(args, "--config");
        if (configIndex >= 0 && configIndex + 1 < args.Length)
        {
            var configPath = args[configIndex + 1];
            config = LoadFromJson(configPath);
            if (config == null)
            {
                _logger.Error($"Failed to load configuration from {configPath}");
                return null;
            }
            _logger.Info($"Configuration loaded from {configPath}");
        }
        else if (File.Exists("appsettings.json") && args.Length == 0)
        {
            // Load default config file if no arguments provided
            config = LoadFromJson("appsettings.json");
            if (config == null)
            {
                _logger.Warn("Failed to load appsettings.json, using defaults");
                config = new FtpsServerConfiguration();
            }
            else
            {
                _logger.Info("Configuration loaded from appsettings.json");
            }
        }
        else
        {
            // Create default configuration
            config = new FtpsServerConfiguration();
        }

        // Override with command-line arguments
        config = ParseCommandLineArguments(args, config);

        return config;
    }

    static readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Converters =
        {
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
        }
    };

    static FtpsServerConfiguration? LoadFromJson(string path)
    {
        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<FtpsServerConfiguration>(json, _jsonSerializerOptions);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, $"Error loading configuration from {path}");
            return null;
        }
    }

    static FtpsServerConfiguration ParseCommandLineArguments(string[] args, FtpsServerConfiguration config)
    {
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--ip":
                    if (i + 1 < args.Length)
                        config.ServerSettings.Ip = args[++i];
                    break;

                case "--port":
                    if (i + 1 < args.Length && int.TryParse(args[++i], out int port))
                        config.ServerSettings.Port = port;
                    break;

                case "--cert":
                    if (i + 1 < args.Length)
                        config.ServerSettings.CertificatePath = args[++i];
                    break;

                case "--certpass":
                    if (i + 1 < args.Length)
                        config.ServerSettings.CertificatePassword = args[++i];
                    break;

                case "--user":
                    if (i + 1 < args.Length)
                    {
                        var userInfo = args[++i].Split(':');
                        if (userInfo.Length >= 2)
                        {
                            var user = new FtpsServerUserAccount
                            {
                                Username = userInfo[0],
                                Password = userInfo[1],
                                RootFolder = userInfo.Length > 2 ? userInfo[2] : "/",
                                Permissions = userInfo.Length > 3 ? ParsePermissions(userInfo[3]) : new FtpsServerUserPermissions()
                            };
                            config.Users.Add(user);
                        }
                    }
                    break;
            }
        }

        return config;
    }

    static FtpsServerUserPermissions ParsePermissions(string permString)
    {
        return new FtpsServerUserPermissions
        {
            Read = permString.Contains('R'),
            Write = permString.Contains('W'),
        };
    }

    static bool ValidateConfiguration(FtpsServerConfiguration config)
    {
        if (config.ServerSettings.Port < 1 || config.ServerSettings.Port > 65535)
        {
            _logger.Error($"Invalid port number: {config.ServerSettings.Port}");
            return false;
        }

        if (!IPAddress.TryParse(config.ServerSettings.Ip, out _))
        {
            _logger.Error($"Invalid IP address: {config.ServerSettings.Ip}");
            return false;
        }

        if (config.Users.Count == 0)
        {
            _logger.Warn("No users configured.");
            return false;
            //config.Users.Add(new UserAccount
            //{
            //    Username = "admin",
            //    Password = "admin",
            //    RootFolder = "/",
            //    Permissions = new UserPermissions()
            //});
        }

        if (!string.IsNullOrEmpty(config.ServerSettings.CertificatePath))
        {
            if (!File.Exists(config.ServerSettings.CertificatePath))
            {
                _logger.Error($"Certificate file not found: {config.ServerSettings.CertificatePath}");
                return false;
            }
        }

        return true;
    }
}
