using FtpsServerAppsShared.Services;
using FtpsServerLibrary;
using NLog;
using System.Globalization;
using System.Net;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
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
            
            if (config == null || config.Users.Count == 0)
            {
                ShowHelp();

                Console.WriteLine("\n--- Interactive Configuration ---");
                Console.WriteLine("Press Enter to accept default values shown in [brackets]\n");

                config = CreateInteractiveConfiguration();
                if (config == null)
                {
                    _logger.Info("Configuration cancelled by user");
                    return;
                }
            }

            // Validate configuration
            if (!ValidateConfiguration(config))
            {
                _logger.Error("Configuration validation failed");
                return;
            }

            // Create and start server
            var server = new FtpsServer(new Log(), config);
            
            _logger.Info($"FTPS Server Starting...");
            _logger.Info($"IP Address: {config.ServerSettings.Ip}");
            _logger.Info($"Port: {config.ServerSettings.Port}");
            _logger.Info($"Users Configured: {config.Users.Count}");
            _logger.Info($"Encryption: Explicit");

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
        var version = Assembly.GetExecutingAssembly().GetName().Version ?? throw new InvalidProgramException("Failed to get assembly from !");
        var copyright = string.Format(CultureInfo.CurrentUICulture, CopyrightInfo.Copyright);
        Console.WriteLine(copyright);
    }

    static void ShowHelp()
    {
        Console.WriteLine(@"
Usage:
  ftps-server [options]
  ftps-server --config <path-to-json>

Options:

  --help
  Show this help message.

  --config <path to configuration json>
  Path to JSON configuration file.

  --ip <address>
  The IP address server will be listening to.
  Optional parameter.
  Default value: 0.0.0.0.
  0.0.0.0 - listen on every available network interface.

  --port <number>
  The Port for server to listen to.
  Optional parameter.
  Default value: 2121.

  Certificate source:

  --cert <path to pfx, pem or der file>
  PEM, DER or PKCS#12 PFX file.
  Optional parameter.
  PFX file is opened with CertificatePassword (if specified).
  
  --certpass <password>
  Certificate password.
  Optional parameter.
  When specified, will be used for opening certificate from cert argument.
  
  --certstorename <store name>
  Certificate store name. Possible values: AuthRoot, CertificateAuthority, My, Root, TrustedPublisher.
  Optional parameter.
  Used when certstorename, certstorelocation and certstoresubject are together specified.

  --certstorelocation <user>
  Certificate store name. Possible values: AuthRoot, CertificateAuthority, My, Root, TrustedPublisher.
  Used when certstorename, certstorelocation and certstoresubject are together specified.
  Optional parameter.

  --certstoresubject <store subject>
  Certificate store subject by which certificate will be searched in certificate store and location. 
  Used when certstorename, certstorelocation and certstoresubject are together specified.
  Optional parameter.

  --user ""admin#admin#F:\ftp server\admin#RW""
  User with login admin and password admin with foilder F:\ftp server\admin with Read and Write permissions.

  --user ""reader#read123#F:\ftp server\reader#R""
  User with login admin and password read123 with foilder F:\ftp server\reader with Read permission.

  --user ""dropbox#dropbox123#F:\ftp server\dropbox#W""
  User with login admin and password dropbox123 with foilder F:\ftp server\dropbox with Write permission.

Examples:
  ftps-server --config settings.json
  ftps-server --ip 0.0.0.0 --port 2121
  ftps-server --user ""admin#admin#F:\ftp server\admin#RW""
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

                case "--maxconnections":
                    if (i + 1 < args.Length && int.TryParse(args[++i], out int maxConnections))
                        config.ServerSettings.MaxConnections = maxConnections;
                    break;

                case "--cert":
                    if (i + 1 < args.Length)
                        config.ServerSettings.CertificatePath = args[++i];
                    break;

                case "--certpass":
                    if (i + 1 < args.Length)
                        config.ServerSettings.CertificatePassword = args[++i];
                    break;

                case "--certstorename":
                    if (i + 1 < args.Length && Enum.TryParse<StoreName>(args[++i], out var storeName))
                        config.ServerSettings.CertificateStoreName = storeName;
                    break;

                case "--certstorelocation":
                    if (i + 1 < args.Length && Enum.TryParse<StoreLocation>(args[++i], out var storeLocation))
                        config.ServerSettings.CertificateStoreLocation = storeLocation;
                    break;

                case "--certstoresubject":
                    if (i + 1 < args.Length)
                        config.ServerSettings.CertificateStoreSubject = args[++i];
                    break;

                case "--user":
                    if (i + 1 < args.Length)
                    {
                        var userInfo = args[++i].Split('#');
                        if (userInfo.Length == 4)
                        {
                            var user = new FtpsServerUserAccount
                            {
                                Login = userInfo[0],
                                Password = userInfo[1],
                                Folder = userInfo[2],
                                Read = userInfo[3].ToUpper().Contains('R'),
                                Write = userInfo[3].ToUpper().Contains('W'),
                            };
                            config.Users.Add(user);
                        }
                    }
                    break;
            }
        }

        return config;
    }

    static FtpsServerConfiguration? CreateInteractiveConfiguration()
    {
        try
        {
            var config = new FtpsServerConfiguration();

            // IP Address
            Console.Write($"IP Address [0.0.0.0]: ");
            var ip = Console.ReadLine()?.Trim();
            if (!string.IsNullOrEmpty(ip))
            {
                config.ServerSettings.Ip = ip;
            }

            // Port
            Console.Write($"Port [2121]: ");
            var portInput = Console.ReadLine()?.Trim();
            if (!string.IsNullOrEmpty(portInput) && int.TryParse(portInput, out int port))
            {
                config.ServerSettings.Port = port;
            }

            // Certificate (optional)
            Console.Write("Certificate path (optional, press Enter to skip): ");
            var certPath = Console.ReadLine()?.Trim();
            if (!string.IsNullOrEmpty(certPath))
            {
                config.ServerSettings.CertificatePath = certPath;

                Console.Write("Certificate password (optional, press Enter to skip): ");
                var certPass = Console.ReadLine()?.Trim();
                if (!string.IsNullOrEmpty(certPass))
                {
                    config.ServerSettings.CertificatePassword = certPass;
                }
            }

            // Users
            Console.WriteLine("\n=== User Configuration ===");
            Console.WriteLine("At least one user is required.");
            Console.WriteLine("Permissions: R (Read), W (Write), RW (Read and Write)\n");

            int userCount = 1;
            while (true)
            {
                Console.WriteLine($"--- User {userCount} ---");

                Console.Write("Username (or press Enter to finish): ");
                var username = Console.ReadLine()?.Trim();

                if (string.IsNullOrEmpty(username))
                {
                    if (config.Users.Count == 0)
                    {
                        Console.WriteLine("At least one user is required!\n");
                        continue;
                    }
                    break;
                }

                Console.Write("Password: ");
                var password = Console.ReadLine()?.Trim();
                if (string.IsNullOrEmpty(password))
                {
                    Console.WriteLine("Password cannot be empty!\n");
                    continue;
                }

                Console.Write("Folder path: ");
                var folder = Console.ReadLine()?.Trim();
                if (string.IsNullOrEmpty(folder))
                {
                    Console.WriteLine("Folder path cannot be empty!\n");
                    continue;
                }

                if (!Directory.Exists(folder))
                {
                    Console.Write($"Directory '{folder}' does not exist. Create it? (y/n): ");
                    var createDir = Console.ReadLine()?.Trim().ToLower();
                    if (createDir == "y" || createDir == "yes")
                    {
                        try
                        {
                            Directory.CreateDirectory(folder);
                            Console.WriteLine($"Directory created: {folder}");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Failed to create directory: {ex.Message}\n");
                            continue;
                        }
                    }
                    else
                    {
                        Console.WriteLine("User not added. Directory must exist.\n");
                        continue;
                    }
                }

                Console.Write("Permissions (R/W/RW) [RW]: ");
                var permissions = Console.ReadLine()?.Trim().ToUpper();
                if (string.IsNullOrEmpty(permissions))
                {
                    permissions = "RW";
                }

                var user = new FtpsServerUserAccount
                {
                    Login = username,
                    Password = password,
                    Folder = folder,
                    Read = permissions.Contains('R'),
                    Write = permissions.Contains('W'),
                };

                config.Users.Add(user);
                Console.WriteLine($"User '{user.Login}' added successfully with {permissions} permissions.\n");
                userCount++;
            }

            Console.WriteLine($"\nConfiguration complete! {config.Users.Count} user(s) configured.");

            // Ask if user wants to save configuration
            Console.Write("\nSave this configuration to a file? (y/n): ");
            var saveConfig = Console.ReadLine()?.Trim().ToLower();
            if (saveConfig == "y" || saveConfig == "yes")
            {
                Console.Write("Configuration file name [ftps-config.json]: ");
                var fileName = Console.ReadLine()?.Trim();
                if (string.IsNullOrEmpty(fileName))
                {
                    fileName = "ftps-config.json";
                }

                if (!fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                {
                    fileName += ".json";
                }

                if (SaveConfigurationToFile(config, fileName))
                {
                    Console.WriteLine($"\nConfiguration saved to: {Path.GetFullPath(fileName)}");
                    Console.WriteLine($"\nTo use this configuration later, run:");
                    Console.WriteLine($"  ftps-server --config {fileName}");
                }
            }

            return config;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error during interactive configuration");
            Console.WriteLine($"Error: {ex.Message}");
            return null;
        }
    }

    static bool SaveConfigurationToFile(FtpsServerConfiguration config, string fileName)
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                Converters =
                {
                    new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
                }
            };

            var json = JsonSerializer.Serialize(config, options);
            File.WriteAllText(fileName, json);
            _logger.Info($"Configuration saved to {fileName}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, $"Failed to save configuration to {fileName}");
            Console.WriteLine($"Failed to save configuration: {ex.Message}");
            return false;
        }
    }

    static bool ValidateConfiguration(FtpsServerConfiguration config)
    {
        if (config.ServerSettings.Port.HasValue && config.ServerSettings.Port < 1 || config.ServerSettings.Port > 65535)
        {
            _logger.Error($"Invalid port number: {config.ServerSettings.Port}");
            return false;
        }

        if (config.ServerSettings.MaxConnections.HasValue && config.ServerSettings.MaxConnections.Value < 1)
        {
            _logger.Error($"Invalid maximum connections number: {config.ServerSettings.MaxConnections}");
            return false;
        }

        if (config.ServerSettings.Ip != null && !IPAddress.TryParse(config.ServerSettings.Ip, out _))
        {
            _logger.Error($"Invalid IP address: {config.ServerSettings.Ip}");
            return false;
        }

        if (config.Users.Count == 0)
        {
            _logger.Warn("No users configured.");
            return false;
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
