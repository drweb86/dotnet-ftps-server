using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FtpsServer
{
    //class Program
    //{
    //    static void Main(string[] args)
    //    {
    //        Console.WriteLine("FTPS Server Starting...");
            
    //        // Create self-signed certificate for testing
    //        var server = new FtpServer("127.0.0.1", 21990, "./ftproot");
    //        server.AddUser("admin", "password123");
    //        server.AddUser("user", "user123");
            
    //        Console.WriteLine("FTPS Server started on port 21990");
    //        Console.WriteLine("Root directory: ./ftproot");
    //        Console.WriteLine("Press any key to stop...");
            
    //        server.Start();
    //        Console.ReadKey();
    //        server.Stop();
    //    }
    //}

    //public class FtpServer
    //{
    //    private readonly string _ipAddress;
    //    private readonly int _port;
    //    private readonly string _rootDirectory;
    //    private readonly Dictionary<string, string> _users;
    //    private TcpListener _listener;
    //    private bool _isRunning;
    //    private X509Certificate2 _serverCertificate;

    //    public FtpServer(string ipAddress, int port, string rootDirectory)
    //    {
    //        _ipAddress = ipAddress;
    //        _port = port;
    //        _rootDirectory = Path.GetFullPath(rootDirectory);
    //        _users = new Dictionary<string, string>();

    //        // Create root directory if it doesn't exist
    //        if (!Directory.Exists(_rootDirectory))
    //        {
    //            Directory.CreateDirectory(_rootDirectory);
    //        }

    //        // Generate self-signed certificate
    //        _serverCertificate = GenerateSelfSignedCertificate();
    //    }

    //    public void AddUser(string username, string password)
    //    {
    //        _users[username] = password;
    //    }

    //    public void Start()
    //    {
    //        _listener = new TcpListener(IPAddress.Parse(_ipAddress), _port);
    //        _listener.Start();
    //        _isRunning = true;

    //        Task.Run(() => AcceptClients());
    //    }

    //    public void Stop()
    //    {
    //        _isRunning = false;
    //        _listener?.Stop();
    //    }

    //    private async void AcceptClients()
    //    {
    //        while (_isRunning)
    //        {
    //            try
    //            {
    //                var client = await _listener.AcceptTcpClientAsync();
    //                Console.WriteLine($"Client connected: {client.Client.RemoteEndPoint}");
                    
    //                _ = Task.Run(() => HandleClient(client));
    //            }
    //            catch (Exception ex)
    //            {
    //                if (_isRunning)
    //                {
    //                    Console.WriteLine($"Error accepting client: {ex.Message}");
    //                }
    //            }
    //        }
    //    }

    //    private async Task HandleClient(TcpClient client)
    //    {
    //        var session = new ClientSession(client, _rootDirectory, _users, _serverCertificate);
    //        await session.HandleAsync();
    //    }

    //    private X509Certificate2 GenerateSelfSignedCertificate()
    //    {
    //        // For production, use a proper certificate
    //        // This is a simplified version - you should use a proper certificate
    //        try
    //        {
    //            // Try to load existing certificate
    //            string certPath = "server.pfx";
    //            if (File.Exists(certPath))
    //            {
    //                return new X509Certificate2(certPath, "password");
    //            }
    //        }
    //        catch { }

    //        // Create a basic certificate for testing
    //        // In production, generate proper certificate using PowerShell or OpenSSL
    //        Console.WriteLine("Warning: Using test certificate. Generate proper certificate for production!");
            
    //        // Return null for now - will be handled in AUTH TLS command
    //        return null;
    //    }
    //}

    //public class ClientSession
    //{
    //    private readonly TcpClient _controlClient;
    //    private readonly string _rootDirectory;
    //    private readonly Dictionary<string, string> _users;
    //    private readonly X509Certificate2 _certificate;
        
    //    private Stream _controlStream;
    //    private StreamReader _reader;
    //    private StreamWriter _writer;
        
    //    private string _username;
    //    private bool _isAuthenticated;
    //    private string _currentDirectory;
    //    private bool _isTlsEnabled;
    //    private string _renameFrom;
        
    //    private TcpListener _dataListener;
    //    private string _transferMode = "A"; // A = ASCII, I = Binary
    //    private bool _isPassiveMode;
    //    private IPEndPoint _passiveEndpoint;

    //    public ClientSession(TcpClient controlClient, string rootDirectory, 
    //        Dictionary<string, string> users, X509Certificate2 certificate)
    //    {
    //        _controlClient = controlClient;
    //        _rootDirectory = rootDirectory;
    //        _users = users;
    //        _certificate = certificate;
    //        _currentDirectory = "/";
    //    }

    //    public async Task HandleAsync()
    //    {
    //        try
    //        {
    //            _controlStream = _controlClient.GetStream();
    //            _reader = new StreamReader(_controlStream, Encoding.ASCII);
    //            _writer = new StreamWriter(_controlStream, Encoding.ASCII) { AutoFlush = true };

    //            await SendResponseAsync(220, "FTPS Server Ready");

    //            string line;
    //            while ((line = await _reader.ReadLineAsync()) != null)
    //            {
    //                Console.WriteLine($">> {line}");
                    
    //                var parts = line.Split(new[] { ' ' }, 2);
    //                var command = parts[0].ToUpper();
    //                var argument = parts.Length > 1 ? parts[1] : "";

    //                await ProcessCommandAsync(command, argument);
                    
    //                if (command == "QUIT")
    //                    break;
    //            }
    //        }
    //        catch (Exception ex)
    //        {
    //            Console.WriteLine($"Session error: {ex.Message}");
    //        }
    //        finally
    //        {
    //            _controlClient?.Close();
    //            _dataListener?.Stop();
    //        }
    //    }

    //    private async Task ProcessCommandAsync(string command, string argument)
    //    {
    //        try
    //        {
    //            switch (command)
    //            {
    //                case "USER":
    //                    await HandleUserAsync(argument);
    //                    break;
    //                case "PASS":
    //                    await HandlePassAsync(argument);
    //                    break;
    //                case "AUTH":
    //                    await HandleAuthAsync(argument);
    //                    break;
    //                case "PBSZ":
    //                    await HandlePbszAsync(argument);
    //                    break;
    //                case "PROT":
    //                    await HandleProtAsync(argument);
    //                    break;
    //                case "PWD":
    //                    await HandlePwdAsync();
    //                    break;
    //                case "CWD":
    //                    await HandleCwdAsync(argument);
    //                    break;
    //                case "CDUP":
    //                    await HandleCdupAsync();
    //                    break;
    //                case "MKD":
    //                case "XMKD":
    //                    await HandleMkdAsync(argument);
    //                    break;
    //                case "RMD":
    //                case "XRMD":
    //                    await HandleRmdAsync(argument);
    //                    break;
    //                case "DELE":
    //                    await HandleDeleAsync(argument);
    //                    break;
    //                case "RNFR":
    //                    await HandleRnfrAsync(argument);
    //                    break;
    //                case "RNTO":
    //                    await HandleRntoAsync(argument);
    //                    break;
    //                case "TYPE":
    //                    await HandleTypeAsync(argument);
    //                    break;
    //                case "PASV":
    //                    await HandlePasvAsync();
    //                    break;
    //                case "LIST":
    //                    await HandleListAsync(argument);
    //                    break;
    //                case "NLST":
    //                    await HandleNlstAsync(argument);
    //                    break;
    //                case "RETR":
    //                    await HandleRetrAsync(argument);
    //                    break;
    //                case "STOR":
    //                    await HandleStorAsync(argument);
    //                    break;
    //                case "SYST":
    //                    await SendResponseAsync(215, "UNIX Type: L8");
    //                    break;
    //                case "FEAT":
    //                    await HandleFeatAsync();
    //                    break;
    //                case "NOOP":
    //                    await SendResponseAsync(200, "OK");
    //                    break;
    //                case "QUIT":
    //                    await SendResponseAsync(221, "Goodbye");
    //                    break;
    //                default:
    //                    await SendResponseAsync(502, $"Command '{command}' not implemented");
    //                    break;
    //            }
    //        }
    //        catch (Exception ex)
    //        {
    //            Console.WriteLine($"Command error: {ex.Message}");
    //            await SendResponseAsync(550, $"Error: {ex.Message}");
    //        }
    //    }

    //    private async Task HandleUserAsync(string username)
    //    {
    //        _username = username;
    //        await SendResponseAsync(331, "Password required");
    //    }

    //    private async Task HandlePassAsync(string password)
    //    {
    //        if (string.IsNullOrEmpty(_username))
    //        {
    //            await SendResponseAsync(503, "Login with USER first");
    //            return;
    //        }

    //        if (_users.ContainsKey(_username) && _users[_username] == password)
    //        {
    //            _isAuthenticated = true;
    //            await SendResponseAsync(230, "User logged in");
    //        }
    //        else
    //        {
    //            _username = null;
    //            await SendResponseAsync(530, "Login incorrect");
    //        }
    //    }

    //    private async Task HandleAuthAsync(string argument)
    //    {
    //        if (argument.ToUpper() == "TLS" || argument.ToUpper() == "SSL")
    //        {
    //            await SendResponseAsync(234, "AUTH command ok. Expecting TLS Negotiation.");
                
    //            try
    //            {
    //                // Create a simple self-signed certificate for testing
    //                var cert = CreateTestCertificate();
                    
    //                var sslStream = new SslStream(_controlStream, false);
    //                await sslStream.AuthenticateAsServerAsync(cert, false, SslProtocols.Tls12, false);
                    
    //                _controlStream = sslStream;
    //                _reader = new StreamReader(_controlStream, Encoding.ASCII);
    //                _writer = new StreamWriter(_controlStream, Encoding.ASCII) { AutoFlush = true };
    //                _isTlsEnabled = true;
                    
    //                Console.WriteLine("TLS enabled on control connection");
    //            }
    //            catch (Exception ex)
    //            {
    //                Console.WriteLine($"TLS error: {ex.Message}");
    //                await SendResponseAsync(431, "TLS negotiation failed");
    //            }
    //        }
    //        else
    //        {
    //            await SendResponseAsync(504, "AUTH type not supported");
    //        }
    //    }

    //    private async Task HandlePbszAsync(string argument)
    //    {
    //        await SendResponseAsync(200, "PBSZ=0");
    //    }

    //    private async Task HandleProtAsync(string argument)
    //    {
    //        if (argument.ToUpper() == "P")
    //        {
    //            await SendResponseAsync(200, "PROT P ok");
    //        }
    //        else if (argument.ToUpper() == "C")
    //        {
    //            await SendResponseAsync(200, "PROT C ok");
    //        }
    //        else
    //        {
    //            await SendResponseAsync(504, "PROT type not supported");
    //        }
    //    }

    //    private async Task HandlePwdAsync()
    //    {
    //        if (!_isAuthenticated)
    //        {
    //            await SendResponseAsync(530, "Not logged in");
    //            return;
    //        }

    //        await SendResponseAsync(257, $"\"{_currentDirectory}\" is current directory");
    //    }

    //    private async Task HandleCwdAsync(string directory)
    //    {
    //        if (!_isAuthenticated)
    //        {
    //            await SendResponseAsync(530, "Not logged in");
    //            return;
    //        }

    //        var newPath = ResolvePath(directory);
    //        var fullPath = GetFullPath(newPath);

    //        if (Directory.Exists(fullPath))
    //        {
    //            _currentDirectory = newPath;
    //            await SendResponseAsync(250, "Directory changed");
    //        }
    //        else
    //        {
    //            await SendResponseAsync(550, "Directory not found");
    //        }
    //    }

    //    private async Task HandleCdupAsync()
    //    {
    //        if (!_isAuthenticated)
    //        {
    //            await SendResponseAsync(530, "Not logged in");
    //            return;
    //        }

    //        if (_currentDirectory != "/")
    //        {
    //            var parent = Path.GetDirectoryName(_currentDirectory.TrimEnd('/'));
    //            _currentDirectory = string.IsNullOrEmpty(parent) ? "/" : parent.Replace('\\', '/');
    //            if (!_currentDirectory.StartsWith("/"))
    //                _currentDirectory = "/" + _currentDirectory;
    //        }

    //        await SendResponseAsync(250, "Directory changed");
    //    }

    //    private async Task HandleMkdAsync(string directory)
    //    {
    //        if (!_isAuthenticated)
    //        {
    //            await SendResponseAsync(530, "Not logged in");
    //            return;
    //        }

    //        var newPath = ResolvePath(directory);
    //        var fullPath = GetFullPath(newPath);

    //        try
    //        {
    //            Directory.CreateDirectory(fullPath);
    //            await SendResponseAsync(257, $"\"{newPath}\" created");
    //        }
    //        catch (Exception ex)
    //        {
    //            await SendResponseAsync(550, $"Cannot create directory: {ex.Message}");
    //        }
    //    }

    //    private async Task HandleRmdAsync(string directory)
    //    {
    //        if (!_isAuthenticated)
    //        {
    //            await SendResponseAsync(530, "Not logged in");
    //            return;
    //        }

    //        var path = ResolvePath(directory);
    //        var fullPath = GetFullPath(path);

    //        try
    //        {
    //            if (Directory.Exists(fullPath))
    //            {
    //                Directory.Delete(fullPath, true);
    //                await SendResponseAsync(250, "Directory removed");
    //            }
    //            else
    //            {
    //                await SendResponseAsync(550, "Directory not found");
    //            }
    //        }
    //        catch (Exception ex)
    //        {
    //            await SendResponseAsync(550, $"Cannot remove directory: {ex.Message}");
    //        }
    //    }

    //    private async Task HandleDeleAsync(string filename)
    //    {
    //        if (!_isAuthenticated)
    //        {
    //            await SendResponseAsync(530, "Not logged in");
    //            return;
    //        }

    //        var path = ResolvePath(filename);
    //        var fullPath = GetFullPath(path);

    //        try
    //        {
    //            if (File.Exists(fullPath))
    //            {
    //                File.Delete(fullPath);
    //                await SendResponseAsync(250, "File deleted");
    //            }
    //            else
    //            {
    //                await SendResponseAsync(550, "File not found");
    //            }
    //        }
    //        catch (Exception ex)
    //        {
    //            await SendResponseAsync(550, $"Cannot delete file: {ex.Message}");
    //        }
    //    }

    //    private async Task HandleRnfrAsync(string filename)
    //    {
    //        if (!_isAuthenticated)
    //        {
    //            await SendResponseAsync(530, "Not logged in");
    //            return;
    //        }

    //        var path = ResolvePath(filename);
    //        var fullPath = GetFullPath(path);

    //        if (File.Exists(fullPath) || Directory.Exists(fullPath))
    //        {
    //            _renameFrom = fullPath;
    //            await SendResponseAsync(350, "Ready for RNTO");
    //        }
    //        else
    //        {
    //            await SendResponseAsync(550, "File/directory not found");
    //        }
    //    }

    //    private async Task HandleRntoAsync(string filename)
    //    {
    //        if (!_isAuthenticated)
    //        {
    //            await SendResponseAsync(530, "Not logged in");
    //            return;
    //        }

    //        if (string.IsNullOrEmpty(_renameFrom))
    //        {
    //            await SendResponseAsync(503, "RNFR required first");
    //            return;
    //        }

    //        var path = ResolvePath(filename);
    //        var fullPath = GetFullPath(path);

    //        try
    //        {
    //            if (File.Exists(_renameFrom))
    //            {
    //                File.Move(_renameFrom, fullPath);
    //                await SendResponseAsync(250, "File renamed");
    //            }
    //            else if (Directory.Exists(_renameFrom))
    //            {
    //                Directory.Move(_renameFrom, fullPath);
    //                await SendResponseAsync(250, "Directory renamed");
    //            }
    //            else
    //            {
    //                await SendResponseAsync(550, "Rename failed");
    //            }
    //        }
    //        catch (Exception ex)
    //        {
    //            await SendResponseAsync(550, $"Rename failed: {ex.Message}");
    //        }
    //        finally
    //        {
    //            _renameFrom = null;
    //        }
    //    }

    //    private async Task HandleTypeAsync(string type)
    //    {
    //        if (!_isAuthenticated)
    //        {
    //            await SendResponseAsync(530, "Not logged in");
    //            return;
    //        }

    //        _transferMode = type.ToUpper();
    //        await SendResponseAsync(200, $"Type set to {_transferMode}");
    //    }

    //    private async Task HandlePasvAsync()
    //    {
    //        if (!_isAuthenticated)
    //        {
    //            await SendResponseAsync(530, "Not logged in");
    //            return;
    //        }

    //        _dataListener?.Stop();
    //        _dataListener = new TcpListener(IPAddress.Any, 0);
    //        _dataListener.Start();

    //        var endpoint = (IPEndPoint)_dataListener.LocalEndpoint;
    //        _passiveEndpoint = endpoint;
    //        _isPassiveMode = true;

    //        var localIp = ((IPEndPoint)_controlClient.Client.LocalEndPoint).Address;
    //        var ipBytes = localIp.GetAddressBytes();
    //        var port = endpoint.Port;

    //        var response = $"Entering Passive Mode ({ipBytes[0]},{ipBytes[1]},{ipBytes[2]},{ipBytes[3]},{port / 256},{port % 256})";
    //        await SendResponseAsync(227, response);
    //    }

    //    private async Task HandleListAsync(string path)
    //    {
    //        if (!_isAuthenticated)
    //        {
    //            await SendResponseAsync(530, "Not logged in");
    //            return;
    //        }

    //        if (!_isPassiveMode || _dataListener == null)
    //        {
    //            await SendResponseAsync(425, "Use PASV first");
    //            return;
    //        }

    //        await SendResponseAsync(150, "Opening data connection");

    //        var targetPath = string.IsNullOrEmpty(path) ? _currentDirectory : ResolvePath(path);
    //        var fullPath = GetFullPath(targetPath);

    //        try
    //        {
    //            using (var dataClient = await _dataListener.AcceptTcpClientAsync())
    //            using (var dataStream = dataClient.GetStream())
    //            using (var dataWriter = new StreamWriter(dataStream, Encoding.ASCII) { AutoFlush = true })
    //            {
    //                if (Directory.Exists(fullPath))
    //                {
    //                    var entries = Directory.GetFileSystemEntries(fullPath);
                        
    //                    foreach (var entry in entries)
    //                    {
    //                        var info = new FileInfo(entry);
    //                        var isDirectory = (info.Attributes & FileAttributes.Directory) == FileAttributes.Directory;
                            
    //                        var permissions = isDirectory ? "drwxr-xr-x" : "-rw-r--r--";
    //                        var size = isDirectory ? "0" : info.Length.ToString();
    //                        var modified = info.LastWriteTime.ToString("MMM dd HH:mm");
    //                        var name = Path.GetFileName(entry);
                            
    //                        var line = $"{permissions} 1 owner group {size,15} {modified} {name}";
    //                        await dataWriter.WriteLineAsync(line);
    //                    }
    //                }
    //            }

    //            await SendResponseAsync(226, "Transfer complete");
    //        }
    //        catch (Exception ex)
    //        {
    //            await SendResponseAsync(550, $"List failed: {ex.Message}");
    //        }
    //        finally
    //        {
    //            _dataListener?.Stop();
    //            _dataListener = null;
    //            _isPassiveMode = false;
    //        }
    //    }

    //    private async Task HandleNlstAsync(string path)
    //    {
    //        if (!_isAuthenticated)
    //        {
    //            await SendResponseAsync(530, "Not logged in");
    //            return;
    //        }

    //        if (!_isPassiveMode || _dataListener == null)
    //        {
    //            await SendResponseAsync(425, "Use PASV first");
    //            return;
    //        }

    //        await SendResponseAsync(150, "Opening data connection");

    //        var targetPath = string.IsNullOrEmpty(path) ? _currentDirectory : ResolvePath(path);
    //        var fullPath = GetFullPath(targetPath);

    //        try
    //        {
    //            using (var dataClient = await _dataListener.AcceptTcpClientAsync())
    //            using (var dataStream = dataClient.GetStream())
    //            using (var dataWriter = new StreamWriter(dataStream, Encoding.ASCII) { AutoFlush = true })
    //            {
    //                if (Directory.Exists(fullPath))
    //                {
    //                    var entries = Directory.GetFileSystemEntries(fullPath);
                        
    //                    foreach (var entry in entries)
    //                    {
    //                        var name = Path.GetFileName(entry);
    //                        await dataWriter.WriteLineAsync(name);
    //                    }
    //                }
    //            }

    //            await SendResponseAsync(226, "Transfer complete");
    //        }
    //        catch (Exception ex)
    //        {
    //            await SendResponseAsync(550, $"List failed: {ex.Message}");
    //        }
    //        finally
    //        {
    //            _dataListener?.Stop();
    //            _dataListener = null;
    //            _isPassiveMode = false;
    //        }
    //    }

    //    private async Task HandleRetrAsync(string filename)
    //    {
    //        if (!_isAuthenticated)
    //        {
    //            await SendResponseAsync(530, "Not logged in");
    //            return;
    //        }

    //        if (!_isPassiveMode || _dataListener == null)
    //        {
    //            await SendResponseAsync(425, "Use PASV first");
    //            return;
    //        }

    //        var path = ResolvePath(filename);
    //        var fullPath = GetFullPath(path);

    //        if (!File.Exists(fullPath))
    //        {
    //            await SendResponseAsync(550, "File not found");
    //            return;
    //        }

    //        await SendResponseAsync(150, "Opening data connection");

    //        try
    //        {
    //            using (var dataClient = await _dataListener.AcceptTcpClientAsync())
    //            using (var dataStream = dataClient.GetStream())
    //            using (var fileStream = File.OpenRead(fullPath))
    //            {
    //                await fileStream.CopyToAsync(dataStream);
    //            }

    //            await SendResponseAsync(226, "Transfer complete");
    //        }
    //        catch (Exception ex)
    //        {
    //            await SendResponseAsync(550, $"Transfer failed: {ex.Message}");
    //        }
    //        finally
    //        {
    //            _dataListener?.Stop();
    //            _dataListener = null;
    //            _isPassiveMode = false;
    //        }
    //    }

    //    private async Task HandleStorAsync(string filename)
    //    {
    //        if (!_isAuthenticated)
    //        {
    //            await SendResponseAsync(530, "Not logged in");
    //            return;
    //        }

    //        if (!_isPassiveMode || _dataListener == null)
    //        {
    //            await SendResponseAsync(425, "Use PASV first");
    //            return;
    //        }

    //        var path = ResolvePath(filename);
    //        var fullPath = GetFullPath(path);

    //        await SendResponseAsync(150, "Opening data connection");

    //        try
    //        {
    //            using (var dataClient = await _dataListener.AcceptTcpClientAsync())
    //            using (var dataStream = dataClient.GetStream())
    //            using (var fileStream = File.Create(fullPath))
    //            {
    //                await dataStream.CopyToAsync(fileStream);
    //            }

    //            await SendResponseAsync(226, "Transfer complete");
    //        }
    //        catch (Exception ex)
    //        {
    //            await SendResponseAsync(550, $"Transfer failed: {ex.Message}");
    //        }
    //        finally
    //        {
    //            _dataListener?.Stop();
    //            _dataListener = null;
    //            _isPassiveMode = false;
    //        }
    //    }

    //    private async Task HandleFeatAsync()
    //    {
    //        await SendResponseAsync(211, "Features:");
    //        await _writer.WriteLineAsync(" AUTH TLS");
    //        await _writer.WriteLineAsync(" PBSZ");
    //        await _writer.WriteLineAsync(" PROT");
    //        await _writer.WriteLineAsync(" SIZE");
    //        await _writer.WriteLineAsync(" MDTM");
    //        await SendResponseAsync(211, "End");
    //    }

    //    private string ResolvePath(string path)
    //    {
    //        if (string.IsNullOrEmpty(path))
    //            return _currentDirectory;

    //        if (path.StartsWith("/"))
    //            return path;

    //        var current = _currentDirectory.TrimEnd('/');
    //        return $"{current}/{path}".Replace("//", "/");
    //    }

    //    private string GetFullPath(string virtualPath)
    //    {
    //        var relativePath = virtualPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
    //        var fullPath = Path.Combine(_rootDirectory, relativePath);
            
    //        // Security check: ensure path is within root directory
    //        var normalizedPath = Path.GetFullPath(fullPath);
    //        if (!normalizedPath.StartsWith(_rootDirectory))
    //        {
    //            throw new UnauthorizedAccessException("Access denied");
    //        }

    //        return normalizedPath;
    //    }

    //    private async Task SendResponseAsync(int code, string message)
    //    {
    //        var response = $"{code} {message}";
    //        Console.WriteLine($"<< {response}");
    //        await _writer.WriteLineAsync(response);
    //    }

    //    private X509Certificate2 CreateTestCertificate()
    //    {
    //        // This creates a test certificate in memory
    //        // For production, use a proper certificate from a CA
            
    //        // Create a basic self-signed certificate for testing
    //        // Note: This requires elevated permissions or you can use existing cert
            
    //        try
    //        {
    //            // Try to use existing certificate if available
    //            using (var store = new X509Store(StoreName.My, StoreLocation.CurrentUser))
    //            {
    //                store.Open(OpenFlags.ReadOnly);
    //                var certs = store.Certificates.Find(X509FindType.FindBySubjectName, 
    //                    Environment.MachineName, false);
                    
    //                if (certs.Count > 0)
    //                {
    //                    return certs[0];
    //                }
    //            }
    //        }
    //        catch { }

    //        // Fallback: create a minimal certificate
    //        // This is for testing only - use proper certificates in production
    //        Console.WriteLine("Warning: Creating test certificate. Use proper certificate in production!");
            
    //        // For testing purposes, we'll need to generate or use an existing certificate
    //        // You can generate one using: 
    //        // PowerShell: New-SelfSignedCertificate -DnsName "localhost" -CertStoreLocation "Cert:\CurrentUser\My"
            
    //        throw new InvalidOperationException(
    //            "No SSL certificate found. Please generate a certificate using:\n" +
    //            "PowerShell: New-SelfSignedCertificate -DnsName 'localhost' -CertStoreLocation 'Cert:\\CurrentUser\\My'");
    //    }
    //}
}
