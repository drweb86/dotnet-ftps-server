using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using FtpsServerLibrary;
using NLog;

namespace FtpsServer
{

    class Program
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

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
                    Logger.Error("Configuration validation failed");
                    return;
                }

                // Create and start server
                var server = new FtpServer(config);
                
                Logger.Info("═══════════════════════════════════════════════════════");
                Logger.Info($"FTPS Server Starting...");
                Logger.Info($"IP Address: {config.ServerSettings.IpAddress}");
                Logger.Info($"Port: {config.ServerSettings.Port}");
                Logger.Info($"Root Directory: {Path.GetFullPath(config.ServerSettings.RootDirectory)}");
                Logger.Info($"Users Configured: {config.Users.Count}");
                Logger.Info("═══════════════════════════════════════════════════════");
                
                server.Start();
                
                Console.WriteLine("\nPress 'Q' to stop the server...");
                while (Console.ReadKey(true).Key != ConsoleKey.Q) { }
                
                Logger.Info("Shutting down server...");
                server.Stop();
                Logger.Info("Server stopped");
            }
            catch (Exception ex)
            {
                Logger.Fatal(ex, "Fatal error starting server");
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
  FtpsServer [options]
  FtpsServer --config <path-to-json>

Options:
  --config <path>              Path to JSON configuration file
  --ip <address>               IP address to bind (default: 127.0.0.1)
  --port <number>              Port number (default: 21990)
  --root <path>                Root directory path (default: ./ftproot)
  --cert <path>                Certificate file path (.pfx)
  --certpass <password>        Certificate password
  --user <name:pass:folder:permissions>    Add user
                               Permissions format: RWDCDR (Read,Write,Delete,CreateDir,DeleteDir,Rename)
                               Example: admin:pass123:/home/admin:RWDCDR
  --help                       Show this help message

Examples:
  FtpsServer --config appsettings.json
  FtpsServer --ip 0.0.0.0 --port 21 --root /var/ftp
  FtpsServer --user admin:pass123:/home/admin:RWDCDR --user guest:guest:/public:R
  FtpsServer --cert server.pfx --certpass mypassword

If no arguments are provided, the server looks for 'appsettings.json' in the current directory.
");
        }

        static ServerConfiguration? LoadConfiguration(string[] args)
        {
            ServerConfiguration config;

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
                    Logger.Error($"Failed to load configuration from {configPath}");
                    return null;
                }
                Logger.Info($"Configuration loaded from {configPath}");
            }
            else if (File.Exists("appsettings.json") && args.Length == 0)
            {
                // Load default config file if no arguments provided
                config = LoadFromJson("appsettings.json");
                if (config == null)
                {
                    Logger.Warn("Failed to load appsettings.json, using defaults");
                    config = new ServerConfiguration();
                }
                else
                {
                    Logger.Info("Configuration loaded from appsettings.json");
                }
            }
            else
            {
                // Create default configuration
                config = new ServerConfiguration();
            }

            // Override with command-line arguments
            config = ParseCommandLineArguments(args, config);

            return config;
        }

        static ServerConfiguration? LoadFromJson(string path)
        {
            try
            {
                var json = File.ReadAllText(path);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    ReadCommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true
                };
                return JsonSerializer.Deserialize<ServerConfiguration>(json, options);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Error loading configuration from {path}");
                return null;
            }
        }

        static ServerConfiguration ParseCommandLineArguments(string[] args, ServerConfiguration config)
        {
            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--ip":
                        if (i + 1 < args.Length)
                            config.ServerSettings.IpAddress = args[++i];
                        break;

                    case "--port":
                        if (i + 1 < args.Length && int.TryParse(args[++i], out int port))
                            config.ServerSettings.Port = port;
                        break;

                    case "--root":
                        if (i + 1 < args.Length)
                            config.ServerSettings.RootDirectory = args[++i];
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
                                var user = new UserAccount
                                {
                                    Username = userInfo[0],
                                    Password = userInfo[1],
                                    RootFolder = userInfo.Length > 2 ? userInfo[2] : "/",
                                    Permissions = userInfo.Length > 3 ? ParsePermissions(userInfo[3]) : new UserPermissions()
                                };
                                config.Users.Add(user);
                            }
                        }
                        break;
                }
            }

            return config;
        }

        static UserPermissions ParsePermissions(string permString)
        {
            return new UserPermissions
            {
                Read = permString.Contains('R'),
                Write = permString.Contains('W'),
            };
        }

        static bool ValidateConfiguration(ServerConfiguration config)
        {
            if (config.ServerSettings.Port < 1 || config.ServerSettings.Port > 65535)
            {
                Logger.Error($"Invalid port number: {config.ServerSettings.Port}");
                return false;
            }

            if (!IPAddress.TryParse(config.ServerSettings.IpAddress, out _))
            {
                Logger.Error($"Invalid IP address: {config.ServerSettings.IpAddress}");
                return false;
            }

            if (config.Users.Count == 0)
            {
                Logger.Warn("No users configured.");
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
                    Logger.Error($"Certificate file not found: {config.ServerSettings.CertificatePath}");
                    return false;
                }
            }

            return true;
        }
    }

    public class FtpServer
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        
        private readonly ServerConfiguration _config;
        private TcpListener? _listener;
        private bool _isRunning;
        private X509Certificate2? _serverCertificate;
        private int _activeConnections;

        public FtpServer(ServerConfiguration config)
        {
            _config = config;

            // Create root directory
            var rootPath = Path.GetFullPath(_config.ServerSettings.RootDirectory);
            if (!Directory.Exists(rootPath))
            {
                Directory.CreateDirectory(rootPath);
                Logger.Info($"Created root directory: {rootPath}");
            }

            // Create user directories
            foreach (var user in _config.Users)
            {
                var userPath = Path.Combine(rootPath, user.RootFolder.TrimStart('/'));
                if (!Directory.Exists(userPath))
                {
                    Directory.CreateDirectory(userPath);
                    Logger.Info($"Created user directory for {user.Username}: {userPath}");
                }
            }

            // Load certificate
            _serverCertificate = LoadCertificate();
        }

        public void Start()
        {
            try
            {
                _listener = new TcpListener(
                    IPAddress.Parse(_config.ServerSettings.IpAddress),
                    _config.ServerSettings.Port);
                
                _listener.Start();
                _isRunning = true;

                Logger.Info($"Server started successfully on {_config.ServerSettings.IpAddress}:{_config.ServerSettings.Port}");

                _ = Task.Run(AcceptClientsAsync);
            }
            catch (Exception ex)
            {
                Logger.Fatal(ex, "Failed to start server");
                throw;
            }
        }

        public void Stop()
        {
            _isRunning = false;
            _listener?.Stop();
            Logger.Info("Server stopped");
        }

        private async Task AcceptClientsAsync()
        {
            while (_isRunning)
            {
                try
                {
                    var client = await _listener!.AcceptTcpClientAsync();
                    var endpoint = client.Client.RemoteEndPoint;
                    
                    if (_activeConnections >= _config.ServerSettings.MaxConnections)
                    {
                        Logger.Warn($"Connection rejected from {endpoint}: Max connections reached");
                        client.Close();
                        continue;
                    }

                    _activeConnections++;
                    Logger.Info($"Client connected: {endpoint} (Active: {_activeConnections})");

                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await HandleClientAsync(client);
                        }
                        finally
                        {
                            _activeConnections--;
                            Logger.Info($"Client disconnected: {endpoint} (Active: {_activeConnections})");
                        }
                    });
                }
                catch (Exception ex)
                {
                    if (_isRunning)
                    {
                        Logger.Error(ex, "Error accepting client");
                    }
                }
            }
        }

        private async Task HandleClientAsync(TcpClient client)
        {
            var session = new ClientSession(
                client,
                _config.ServerSettings.RootDirectory,
                _config.Users,
                _serverCertificate);
            
            await session.HandleAsync();
        }

        private X509Certificate2? LoadCertificate()
        {
            try
            {
                if (!string.IsNullOrEmpty(_config.ServerSettings.CertificatePath))
                {
                    Logger.Info($"Loading certificate from {_config.ServerSettings.CertificatePath}");
                    
                    var cert = string.IsNullOrEmpty(_config.ServerSettings.CertificatePassword)
                        ? new X509Certificate2(_config.ServerSettings.CertificatePath)
                        : new X509Certificate2(_config.ServerSettings.CertificatePath, _config.ServerSettings.CertificatePassword);
                    
                    Logger.Info($"Certificate loaded: {cert.Subject}");
                    return cert;
                }

                // Try to find certificate in store
                using (var store = new X509Store(StoreName.My, StoreLocation.CurrentUser))
                {
                    store.Open(OpenFlags.ReadOnly);
                    var certs = store.Certificates.Find(
                        X509FindType.FindBySubjectName,
                        Environment.MachineName,
                        false);

                    if (certs.Count > 0)
                    {
                        Logger.Info($"Using certificate from store: {certs[0].Subject}");
                        return certs[0];
                    }
                }

                Logger.Warn("No certificate found. TLS will not be available.");
                Logger.Warn($"Generate a certificate using: New-SelfSignedCertificate -DnsName '{Environment.MachineName}' -CertStoreLocation 'Cert:\\CurrentUser\\My'");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error loading certificate");
            }

            return null;
        }
    }

    public class ClientSession
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly TcpClient _controlClient;
        private readonly string _rootDirectory;
        private readonly List<UserAccount> _users;
        private readonly X509Certificate2? _certificate;

        private Stream? _controlStream;
        private StreamReader? _reader;
        private StreamWriter? _writer;

        private string? _username;
        private UserAccount? _currentUser;
        private bool _isAuthenticated;
        private string _currentDirectory;
        private bool _isTlsActive;
        private string? _renameFrom;

        private TcpListener? _dataListener;
        private string _transferMode = "I"; // A = ASCII, I = Binary
        private bool _isPassiveMode;
        private string _clientAddress;

        // FTPS data connection protection level
        private DataConnectionProtection _dataProtection = DataConnectionProtection.Clear;

        public ClientSession(TcpClient controlClient, string rootDirectory,
            List<UserAccount> users, X509Certificate2? certificate)
        {
            _controlClient = controlClient;
            _rootDirectory = rootDirectory;
            _users = users;
            _certificate = certificate;
            _currentDirectory = "/";
            _clientAddress = controlClient.Client.RemoteEndPoint?.ToString() ?? "unknown";
        }

        public async Task HandleAsync()
        {
            try
            {
                _controlStream = _controlClient.GetStream();
                _reader = new StreamReader(_controlStream, Encoding.UTF8);
                _writer = new StreamWriter(_controlStream, Encoding.UTF8) { AutoFlush = true };

                await SendResponseAsync(220, "FTPS Server Ready");

                string? line;
                while ((line = await _reader.ReadLineAsync()) != null)
                {
                    // Don't log passwords
                    var logLine = line.StartsWith("PASS ", StringComparison.OrdinalIgnoreCase)
                        ? "PASS ****"
                        : line;
                    Logger.Debug($"[{_clientAddress}] >> {logLine}");

                    var parts = line.Split(new[] { ' ' }, 2);
                    var command = parts[0].ToUpper();
                    var argument = parts.Length > 1 ? parts[1] : "";

                    await ProcessCommandAsync(command, argument);

                    if (command == "QUIT")
                        break;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"[{_clientAddress}] Session error");
            }
            finally
            {
                _controlClient?.Close();
                _dataListener?.Stop();
            }
        }

        private async Task ProcessCommandAsync(string command, string argument)
        {
            try
            {
                switch (command)
                {
                    case "USER":
                        await HandleUserAsync(argument);
                        break;
                    case "PASS":
                        await HandlePassAsync(argument);
                        break;
                    case "AUTH":
                        await HandleAuthAsync(argument);
                        break;
                    case "PBSZ":
                        await HandlePbszAsync(argument);
                        break;
                    case "PROT":
                        await HandleProtAsync(argument);
                        break;
                    case "PWD":
                    case "XPWD":
                        await HandlePwdAsync();
                        break;
                    case "CWD":
                    case "XCWD":
                        await HandleCwdAsync(argument);
                        break;
                    case "CDUP":
                    case "XCUP":
                        await HandleCdupAsync();
                        break;
                    case "MKD":
                    case "XMKD":
                        await HandleMkdAsync(argument);
                        break;
                    case "RMD":
                    case "XRMD":
                        await HandleRmdAsync(argument);
                        break;
                    case "DELE":
                        await HandleDeleAsync(argument);
                        break;
                    case "RNFR":
                        await HandleRnfrAsync(argument);
                        break;
                    case "RNTO":
                        await HandleRntoAsync(argument);
                        break;
                    case "TYPE":
                        await HandleTypeAsync(argument);
                        break;
                    case "PASV":
                        await HandlePasvAsync();
                        break;
                    case "LIST":
                        await HandleListAsync(argument);
                        break;
                    case "NLST":
                        await HandleNlstAsync(argument);
                        break;
                    case "RETR":
                        await HandleRetrAsync(argument);
                        break;
                    case "STOR":
                        await HandleStorAsync(argument);
                        break;
                    case "SIZE":
                        await HandleSizeAsync(argument);
                        break;
                    case "MDTM":
                        await HandleMdtmAsync(argument);
                        break;
                    case "SYST":
                        await SendResponseAsync(215, "UNIX Type: L8");
                        break;
                    case "FEAT":
                        await HandleFeatAsync();
                        break;
                    case "OPTS":
                        await SendResponseAsync(200, "OK");
                        break;
                    case "NOOP":
                        await SendResponseAsync(200, "OK");
                        break;
                    case "QUIT":
                        await SendResponseAsync(221, "Goodbye");
                        Logger.Info($"[{_clientAddress}] User {_username ?? "anonymous"} logged out");
                        break;
                    default:
                        await SendResponseAsync(502, $"Command '{command}' not implemented");
                        break;
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                Logger.Warn($"[{_clientAddress}] Access denied: {ex.Message}");
                await SendResponseAsync(550, "Permission denied");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"[{_clientAddress}] Command error: {command}");
                await SendResponseAsync(550, $"Error: {ex.Message}");
            }
        }

        private async Task HandleUserAsync(string username)
        {
            _username = username;
            Logger.Info($"[{_clientAddress}] User login attempt: {username}");
            await SendResponseAsync(331, "Password required");
        }

        private async Task HandlePassAsync(string password)
        {
            if (string.IsNullOrEmpty(_username))
            {
                await SendResponseAsync(503, "Login with USER first");
                return;
            }

            var user = _users.FirstOrDefault(u => u.Username == _username);
            if (user != null && user.Password == password)
            {
                _currentUser = user;
                _isAuthenticated = true;
                _currentDirectory = user.RootFolder;
                
                Logger.Info($"[{_clientAddress}] User logged in: {_username}");
                await SendResponseAsync(230, "User logged in");
            }
            else
            {
                Logger.Warn($"[{_clientAddress}] Failed login attempt for user: {_username}");
                _username = null;
                await SendResponseAsync(530, "Login incorrect");
            }
        }

        private async Task HandleAuthAsync(string argument)
        {
            if (_certificate == null)
            {
                await SendResponseAsync(502, "TLS not available");
                return;
            }

            if (argument.ToUpper() == "TLS" || argument.ToUpper() == "SSL")
            {
                await SendResponseAsync(234, "AUTH command ok. Expecting TLS Negotiation.");

                try
                {
                    var sslStream = new SslStream(_controlStream!, false);
                    await sslStream.AuthenticateAsServerAsync(_certificate, false, SslProtocols.Tls12 | SslProtocols.Tls13, false);

                    _controlStream = sslStream;
                    _reader = new StreamReader(_controlStream, Encoding.ASCII);
                    _writer = new StreamWriter(_controlStream, Encoding.ASCII) { AutoFlush = true };
                    _isTlsActive = true;

                    Logger.Info($"[{_clientAddress}] TLS enabled on control connection");
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, $"[{_clientAddress}] TLS negotiation failed");
                }
            }
            else
            {
                await SendResponseAsync(504, "AUTH type not supported");
            }
        }

        private async Task HandlePbszAsync(string argument)
        {
            await SendResponseAsync(200, "PBSZ=0");
        }

        private async Task HandleProtAsync(string argument)
        {
            switch (argument.ToUpper())
            {
                case "P":
                    _dataProtection = DataConnectionProtection.Protected;
                    await SendResponseAsync(200, "Protection level set to Private");
                    break;
                case "C":
                    _dataProtection = DataConnectionProtection.Clear;
                    await SendResponseAsync(200, "Protection level set to Clear");
                    break;
                default:
                    await SendResponseAsync(504, "PROT type not supported");
                    break;
            }
        }

        private async Task HandlePwdAsync()
        {
            if (!CheckAuthentication())
            {
                await SendResponseAsync(530, "Not logged in");
                return;
            }

            await SendResponseAsync(257, $"\"{_currentDirectory}\" is current directory");
        }

        private async Task HandleCwdAsync(string directory)
        {
            if (!CheckAuthentication() || !CheckPermission(p => p.Read))
            {
                await SendResponseAsync(550, "Permission denied");
                return;
            }

            var newPath = ResolvePath(directory);
            var fullPath = GetFullPath(newPath);

            if (Directory.Exists(fullPath))
            {
                _currentDirectory = newPath;
                Logger.Debug($"[{_clientAddress}] Changed directory to: {_currentDirectory}");
                await SendResponseAsync(250, "Directory changed");
            }
            else
            {
                await SendResponseAsync(550, "Directory not found");
            }
        }

        private async Task HandleCdupAsync()
        {
            if (!CheckAuthentication() || !CheckPermission(p => p.Read))
            {
                await SendResponseAsync(550, "Permission denied");
                return;
            }

            if (_currentDirectory != _currentUser!.RootFolder)
            {
                var parent = Path.GetDirectoryName(_currentDirectory.TrimEnd('/').Replace('/', Path.DirectorySeparatorChar));
                if (parent != null)
                {
                    _currentDirectory = parent.Replace(Path.DirectorySeparatorChar, '/');
                    if (!_currentDirectory.StartsWith("/"))
                        _currentDirectory = "/" + _currentDirectory;
                    
                    // Ensure we don't go above user's root
                    if (!_currentDirectory.StartsWith(_currentUser.RootFolder))
                    {
                        _currentDirectory = _currentUser.RootFolder;
                    }
                }
            }

            await SendResponseAsync(250, "Directory changed");
        }

        private async Task HandleMkdAsync(string directory)
        {
            if (!CheckAuthentication() || !CheckPermission(p => p.Write))
            {
                await SendResponseAsync(550, "Permission denied");
                return;
            }

            var newPath = ResolvePath(directory);
            var fullPath = GetFullPath(newPath);

            try
            {
                Directory.CreateDirectory(fullPath);
                Logger.Info($"[{_clientAddress}] Created directory: {newPath}");
                await SendResponseAsync(257, $"\"{newPath}\" created");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"[{_clientAddress}] Failed to create directory: {newPath}");
                await SendResponseAsync(550, $"Cannot create directory: {ex.Message}");
            }
        }

        private async Task HandleRmdAsync(string directory)
        {
            if (!CheckAuthentication() || !CheckPermission(p => p.Write))
            {
                await SendResponseAsync(550, "Permission denied");
                return;
            }

            var path = ResolvePath(directory);
            var fullPath = GetFullPath(path);

            try
            {
                if (Directory.Exists(fullPath))
                {
                    Directory.Delete(fullPath, true);
                    Logger.Info($"[{_clientAddress}] Deleted directory: {path}");
                    await SendResponseAsync(250, "Directory removed");
                }
                else
                {
                    await SendResponseAsync(550, "Directory not found");
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"[{_clientAddress}] Failed to delete directory: {path}");
                await SendResponseAsync(550, $"Cannot remove directory: {ex.Message}");
            }
        }

        private async Task HandleDeleAsync(string filename)
        {
            if (!CheckAuthentication() || !CheckPermission(p => p.Write))
            {
                await SendResponseAsync(550, "Permission denied");
                return;
            }

            var path = ResolvePath(filename);
            var fullPath = GetFullPath(path);

            try
            {
                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                    Logger.Info($"[{_clientAddress}] Deleted file: {path}");
                    await SendResponseAsync(250, "File deleted");
                }
                else
                {
                    await SendResponseAsync(550, "File not found");
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"[{_clientAddress}] Failed to delete file: {path}");
                await SendResponseAsync(550, $"Cannot delete file: {ex.Message}");
            }
        }

        private async Task HandleRnfrAsync(string filename)
        {
            if (!CheckAuthentication() || !CheckPermission(p => p.Write))
            {
                await SendResponseAsync(550, "Permission denied");
                return;
            }

            var path = ResolvePath(filename);
            var fullPath = GetFullPath(path);

            if (File.Exists(fullPath) || Directory.Exists(fullPath))
            {
                _renameFrom = fullPath;
                await SendResponseAsync(350, "Ready for RNTO");
            }
            else
            {
                await SendResponseAsync(550, "File/directory not found");
            }
        }

        private async Task HandleRntoAsync(string filename)
        {
            if (!CheckAuthentication() || !CheckPermission(p => p.Write))
            {
                await SendResponseAsync(550, "Permission denied");
                return;
            }

            if (string.IsNullOrEmpty(_renameFrom))
            {
                await SendResponseAsync(503, "RNFR required first");
                return;
            }

            var path = ResolvePath(filename);
            var fullPath = GetFullPath(path);

            try
            {
                if (File.Exists(_renameFrom))
                {
                    File.Move(_renameFrom, fullPath);
                    Logger.Info($"[{_clientAddress}] Renamed file: {_renameFrom} -> {fullPath}");
                    await SendResponseAsync(250, "File renamed");
                }
                else if (Directory.Exists(_renameFrom))
                {
                    Directory.Move(_renameFrom, fullPath);
                    Logger.Info($"[{_clientAddress}] Renamed directory: {_renameFrom} -> {fullPath}");
                    await SendResponseAsync(250, "Directory renamed");
                }
                else
                {
                    await SendResponseAsync(550, "Rename failed");
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"[{_clientAddress}] Rename failed");
                await SendResponseAsync(550, $"Rename failed: {ex.Message}");
            }
            finally
            {
                _renameFrom = null;
            }
        }

        private async Task HandleTypeAsync(string type)
        {
            if (!CheckAuthentication())
            {
                await SendResponseAsync(530, "Not logged in");
                return;
            }

            _transferMode = type.ToUpper();
            await SendResponseAsync(200, $"Type set to {_transferMode}");
        }

        private async Task HandlePasvAsync()
        {
            if (!CheckAuthentication())
            {
                await SendResponseAsync(530, "Not logged in");
                return;
            }

            _dataListener?.Stop();
            _dataListener = new TcpListener(IPAddress.Any, 0);
            _dataListener.Start();

            var endpoint = (IPEndPoint)_dataListener.LocalEndpoint;
            _isPassiveMode = true;

            var localIp = ((IPEndPoint)_controlClient.Client.LocalEndPoint!).Address;
            var ipBytes = localIp.GetAddressBytes();
            var port = endpoint.Port;

            var response = $"Entering Passive Mode ({ipBytes[0]},{ipBytes[1]},{ipBytes[2]},{ipBytes[3]},{port / 256},{port % 256})";
            Logger.Debug($"[{_clientAddress}] {response}");
            await SendResponseAsync(227, response);
        }

        private async Task HandleListAsync(string path)
        {
            if (!CheckAuthentication() || !CheckPermission(p => p.Read))
            {
                await SendResponseAsync(550, "Permission denied");
                return;
            }

            if (!_isPassiveMode || _dataListener == null)
            {
                await SendResponseAsync(425, "Use PASV first");
                return;
            }

            // Accept connection BEFORE sending 150 response
            TcpClient dataClient;
            try
            {
                dataClient = await _dataListener.AcceptTcpClientAsync();
            }
            catch (Exception ex)
            {
                await SendResponseAsync(425, "Can't open data connection");
                return;
            }

            var targetPath = string.IsNullOrEmpty(path) ? _currentDirectory : ResolvePath(path);
            var fullPath = GetFullPath(targetPath);

            await SendResponseAsync(150, "Opening data connection");

            try
            {
                using (dataClient)
                {
                    Stream dataStream = dataClient.GetStream();

                    // Apply SSL/TLS if protection is enabled
                    if (_dataProtection == DataConnectionProtection.Protected && _certificate != null)
                    {
                        var sslStream = new SslStream(dataStream, false);
                        await sslStream.AuthenticateAsServerAsync(_certificate, false, SslProtocols.Tls12 | SslProtocols.Tls13, false);
                        dataStream = sslStream;
                    }

                    using (dataStream)
                    using (var dataWriter = new StreamWriter(dataStream, Encoding.UTF8) { AutoFlush = true })
                    {
                        if (Directory.Exists(fullPath))
                        {
                            var entries = Directory.GetFileSystemEntries(fullPath);
                            Logger.Debug($"[{_clientAddress}] Listing {entries.Length} items from: {targetPath}");

                            foreach (var entry in entries)
                            {
                                var info = new FileInfo(entry);
                                var isDirectory = (info.Attributes & FileAttributes.Directory) == FileAttributes.Directory;

                                var permissions = isDirectory ? "drwxr-xr-x" : "-rw-r--r--";
                                var size = isDirectory ? "0" : info.Length.ToString();
                                var modified = info.LastWriteTime.ToString("MMM dd HH:mm");
                                var name = Path.GetFileName(entry);

                                var line = $"{permissions} 1 owner group {size,15} {modified} {name}";
                                await dataWriter.WriteLineAsync(line);
                            }
                        }
                    }
                }

                Logger.Debug($"[{_clientAddress}] List transfer complete");
                await SendResponseAsync(226, "Transfer complete");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"[{_clientAddress}] List failed");
                await SendResponseAsync(550, $"List failed: {ex.Message}");
            }
            finally
            {
                _dataListener?.Stop();
                _dataListener = null;
                _isPassiveMode = false;
            }
        }

        private async Task HandleNlstAsync(string path)
        {
            if (!CheckAuthentication() || !CheckPermission(p => p.Read))
            {
                await SendResponseAsync(550, "Permission denied");
                return;
            }

            if (!_isPassiveMode || _dataListener == null)
            {
                await SendResponseAsync(425, "Use PASV first");
                return;
            }

            // Accept connection BEFORE sending 150 response
            TcpClient dataClient;
            try
            {
                dataClient = await _dataListener.AcceptTcpClientAsync();
            }
            catch (Exception ex)
            {
                await SendResponseAsync(425, "Can't open data connection");
                return;
            }

            var targetPath = string.IsNullOrEmpty(path) ? _currentDirectory : ResolvePath(path);
            var fullPath = GetFullPath(targetPath);

            await SendResponseAsync(150, "Opening data connection");

            try
            {
                using (dataClient)
                {
                    Stream dataStream = dataClient.GetStream();

                    // Apply SSL/TLS if protection is enabled
                    if (_dataProtection == DataConnectionProtection.Protected && _certificate != null)
                    {
                        var sslStream = new SslStream(dataStream, false);
                        await sslStream.AuthenticateAsServerAsync(_certificate, false, SslProtocols.Tls12 | SslProtocols.Tls13, false);
                        dataStream = sslStream;
                    }

                    using (dataStream)
                    using (var dataWriter = new StreamWriter(dataStream, Encoding.UTF8) { AutoFlush = true })
                    {
                        if (Directory.Exists(fullPath))
                        {
                            var entries = Directory.GetFileSystemEntries(fullPath);

                            foreach (var entry in entries)
                            {
                                var name = Path.GetFileName(entry);
                                await dataWriter.WriteLineAsync(name);
                            }
                        }
                    }
                }

                await SendResponseAsync(226, "Transfer complete");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"[{_clientAddress}] List failed");
                await SendResponseAsync(550, $"List failed: {ex.Message}");
            }
            finally
            {
                _dataListener?.Stop();
                _dataListener = null;
                _isPassiveMode = false;
            }
        }

        private async Task HandleRetrAsync(string filename)
        {
            if (!CheckAuthentication() || !CheckPermission(p => p.Read))
            {
                await SendResponseAsync(550, "Permission denied");
                return;
            }

            if (!_isPassiveMode || _dataListener == null)
            {
                await SendResponseAsync(425, "Use PASV first");
                return;
            }

            // Accept connection BEFORE sending 150 response
            TcpClient dataClient;
            try
            {
                dataClient = await _dataListener.AcceptTcpClientAsync();
            }
            catch (Exception ex)
            {
                await SendResponseAsync(425, "Can't open data connection");
                return;
            }

            var path = ResolvePath(filename);
            var fullPath = GetFullPath(path);

            if (!File.Exists(fullPath))
            {
                dataClient.Dispose();
                await SendResponseAsync(550, "File not found");
                return;
            }

            var fileInfo = new FileInfo(fullPath);
            Logger.Info($"[{_clientAddress}] Downloading: {path} ({fileInfo.Length} bytes)");

            await SendResponseAsync(150, $"Opening data connection for {Path.GetFileName(filename)} ({fileInfo.Length} bytes)");

            try
            {
                using (dataClient)
                {
                    Stream dataStream = dataClient.GetStream();

                    // Apply SSL/TLS if protection is enabled
                    if (_dataProtection == DataConnectionProtection.Protected && _certificate != null)
                    {
                        var sslStream = new SslStream(dataStream, false);
                        await sslStream.AuthenticateAsServerAsync(_certificate, false, SslProtocols.Tls12 | SslProtocols.Tls13, false);
                        dataStream = sslStream;
                    }

                    using (dataStream)
                    using (var fileStream = File.OpenRead(fullPath))
                    {
                        await fileStream.CopyToAsync(dataStream);
                    }
                }

                Logger.Info($"[{_clientAddress}] Download complete: {path}");
                await SendResponseAsync(226, "Transfer complete");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"[{_clientAddress}] Download failed: {path}");
                await SendResponseAsync(550, $"Transfer failed: {ex.Message}");
            }
            finally
            {
                _dataListener?.Stop();
                _dataListener = null;
                _isPassiveMode = false;
            }
        }

        private async Task HandleStorAsync(string filename)
        {
            if (!CheckAuthentication() || !CheckPermission(p => p.Write))
            {
                await SendResponseAsync(550, "Permission denied");
                return;
            }

            if (!_isPassiveMode || _dataListener == null)
            {
                await SendResponseAsync(425, "Use PASV first");
                return;
            }

            // Accept connection BEFORE sending 150 response
            TcpClient dataClient;
            try
            {
                dataClient = await _dataListener.AcceptTcpClientAsync();
            }
            catch (Exception ex)
            {
                await SendResponseAsync(425, "Can't open data connection");
                return;
            }

            var path = ResolvePath(filename);
            var fullPath = GetFullPath(path);

            Logger.Info($"[{_clientAddress}] Uploading: {path}");

            await SendResponseAsync(150, $"Opening data connection for {Path.GetFileName(filename)}");

            try
            {
                using (dataClient)
                {
                    Stream dataStream = dataClient.GetStream();

                    // Apply SSL/TLS if protection is enabled
                    if (_dataProtection == DataConnectionProtection.Protected && _certificate != null)
                    {
                        var sslStream = new SslStream(dataStream, false);
                        await sslStream.AuthenticateAsServerAsync(_certificate, false, SslProtocols.Tls12 | SslProtocols.Tls13, false);
                        dataStream = sslStream;
                    }

                    using (dataStream)
                    using (var fileStream = File.Create(fullPath))
                    {
                        await dataStream.CopyToAsync(fileStream);
                    }
                }

                var fileInfo = new FileInfo(fullPath);
                Logger.Info($"[{_clientAddress}] Upload complete: {path} ({fileInfo.Length} bytes)");
                await SendResponseAsync(226, "Transfer complete");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"[{_clientAddress}] Upload failed: {path}");
                await SendResponseAsync(550, $"Transfer failed: {ex.Message}");
            }
            finally
            {
                _dataListener?.Stop();
                _dataListener = null;
                _isPassiveMode = false;
            }
        }

        private async Task HandleSizeAsync(string filename)
        {
            if (!CheckAuthentication() || !CheckPermission(p => p.Read))
            {
                await SendResponseAsync(550, "Permission denied");
                return;
            }

            var path = ResolvePath(filename);
            var fullPath = GetFullPath(path);

            try
            {
                if (File.Exists(fullPath))
                {
                    var fileInfo = new FileInfo(fullPath);
                    await SendResponseAsync(213, fileInfo.Length.ToString());
                }
                else
                {
                    await SendResponseAsync(550, "File not found");
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"[{_clientAddress}] SIZE command failed");
                await SendResponseAsync(550, $"Error: {ex.Message}");
            }
        }

        private async Task HandleMdtmAsync(string filename)
        {
            if (!CheckAuthentication() || !CheckPermission(p => p.Read))
            {
                await SendResponseAsync(550, "Permission denied");
                return;
            }

            var path = ResolvePath(filename);
            var fullPath = GetFullPath(path);

            try
            {
                if (File.Exists(fullPath))
                {
                    var fileInfo = new FileInfo(fullPath);
                    var timestamp = fileInfo.LastWriteTimeUtc.ToString("yyyyMMddHHmmss");
                    await SendResponseAsync(213, timestamp);
                }
                else
                {
                    await SendResponseAsync(550, "File not found");
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"[{_clientAddress}] MDTM command failed");
                await SendResponseAsync(550, $"Error: {ex.Message}");
            }
        }

        private async Task HandleFeatAsync()
        {
            await _writer!.WriteLineAsync("211-Features:");
            await _writer.WriteLineAsync(" AUTH TLS");
            await _writer.WriteLineAsync(" PBSZ");
            await _writer.WriteLineAsync(" PROT");
            await _writer.WriteLineAsync(" SIZE");
            await _writer.WriteLineAsync(" MDTM");
            await _writer.WriteLineAsync(" UTF8");
            await _writer.WriteLineAsync("211 End");
        }

        private bool CheckAuthentication()
        {
            return _isAuthenticated && _currentUser != null;
        }

        private bool CheckPermission(Func<UserPermissions, bool> check)
        {
            if (_currentUser == null)
                return false;
            return check(_currentUser.Permissions);
        }

        private string ResolvePath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return _currentDirectory;

            if (path.StartsWith("/"))
            {
                // Absolute path - make relative to user's root
                if (!path.StartsWith(_currentUser!.RootFolder))
                {
                    path = _currentUser.RootFolder.TrimEnd('/') + "/" + path.TrimStart('/');
                }
                return path;
            }

            // Relative path
            var current = _currentDirectory.TrimEnd('/');
            var resolved = $"{current}/{path}".Replace("//", "/");
            
            // Normalize path
            var parts = resolved.Split('/', StringSplitOptions.RemoveEmptyEntries).ToList();
            for (int i = 0; i < parts.Count; i++)
            {
                if (parts[i] == "..")
                {
                    if (i > 0)
                    {
                        parts.RemoveAt(i);
                        parts.RemoveAt(i - 1);
                        i -= 2;
                    }
                }
                else if (parts[i] == ".")
                {
                    parts.RemoveAt(i);
                    i--;
                }
            }

            resolved = "/" + string.Join("/", parts);
            
            // Ensure within user's root
            if (!resolved.StartsWith(_currentUser!.RootFolder))
            {
                resolved = _currentUser.RootFolder;
            }

            return resolved;
        }

        private string GetFullPath(string virtualPath)
        {
            var relativePath = virtualPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            var fullPath = Path.Combine(_rootDirectory, relativePath);

            // Security check
            var normalizedPath = Path.GetFullPath(fullPath);
            if (!normalizedPath.StartsWith(_rootDirectory))
            {
                throw new UnauthorizedAccessException("Access denied");
            }

            // Check if within user's root
            var userRoot = Path.Combine(_rootDirectory, _currentUser!.RootFolder.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            var normalizedUserRoot = Path.GetFullPath(userRoot);
            
            if (!normalizedPath.StartsWith(normalizedUserRoot))
            {
                throw new UnauthorizedAccessException("Access denied - outside user root");
            }

            return normalizedPath;
        }

        private async Task SendResponseAsync(int code, string message)
        {
            var response = $"{code} {message}";
            Logger.Debug($"[{_clientAddress}] << {response}");
            await _writer!.WriteLineAsync(response);
        }
    }

    enum DataConnectionProtection
    {
        Clear,
        Protected
    }
}
