using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace FtpsServerLibrary;

class FtpsServerClientSession(
    IFtpsServerLog log,
    TcpClient controlClient,
    List<FtpsServerUserAccount> users, X509Certificate2? certificate)
{
    private readonly IFtpsServerLog _log = log;
    private readonly TcpClient _controlClient = controlClient;
    private readonly List<FtpsServerUserAccount> _users = users;
    private readonly X509Certificate2? _certificate = certificate;

    private Stream? _controlStream;
    private StreamReader? _reader;
    private StreamWriter? _writer;

    private string? _username;
    private FtpsServerUserAccount? _currentUser;
    private bool _isAuthenticated;
    private FtpsServerVirtualPath? _currentPath;
    private string? _renameFrom;

    private TcpListener? _dataListener;
    private string _transferMode = "I"; // A = ASCII, I = Binary
    private bool _isPassiveMode;
    private readonly string _clientAddress = controlClient.Client.RemoteEndPoint?.ToString() ?? "unknown";

    // FTPS data connection protection level
    private FtpsServerDataConnectionProtection _dataProtection = FtpsServerDataConnectionProtection.Clear;

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
                _log.Debug($"[{_clientAddress}] >> {logLine}");

                var parts = line.Split([' '], 2);
                var command = parts[0].ToUpper();
                var argument = parts.Length > 1 ? parts[1] : "";

                await ProcessCommandAsync(command, argument);

                if (command == "QUIT")
                    break;
            }
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"[{_clientAddress}] Session error");
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
                    await HandlePbszAsync();
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
                    _log.Info($"[{_clientAddress}] User {_username ?? "anonymous"} logged out");
                    break;
                default:
                    await SendResponseAsync(502, $"Command '{command}' not implemented");
                    break;
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            _log.Warn($"[{_clientAddress}] Access denied: {ex.Message}");
            await SendResponseAsync(550, "Permission denied");
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"[{_clientAddress}] Command error: {command}");
            await SendResponseAsync(550, $"Error: {ex.Message}");
        }
    }

    private async Task HandleUserAsync(string username)
    {
        _username = username;
        _log.Info($"[{_clientAddress}] User login attempt: {username}");
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
            _currentPath = new FtpsServerVirtualPath("/");

            _log.Info($"[{_clientAddress}] User logged in: {_username}");
            await SendResponseAsync(230, "User logged in");
        }
        else
        {
            _log.Warn($"[{_clientAddress}] Failed login attempt for user: {_username}");
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

        if (argument.Equals("TLS", StringComparison.CurrentCultureIgnoreCase) || argument.Equals("SSL", StringComparison.CurrentCultureIgnoreCase))
        {
            await SendResponseAsync(234, "AUTH command ok. Expecting TLS Negotiation.");

            try
            {
                var sslStream = new SslStream(_controlStream!, false);
                await sslStream.AuthenticateAsServerAsync(_certificate, false, SslProtocols.Tls12 | SslProtocols.Tls13, false);

                _controlStream = sslStream;
                _reader = new StreamReader(_controlStream, Encoding.ASCII);
                _writer = new StreamWriter(_controlStream, Encoding.ASCII) { AutoFlush = true };

                _log.Info($"[{_clientAddress}] TLS enabled on control connection");
            }
            catch (Exception ex)
            {
                _log.Error(ex, $"[{_clientAddress}] TLS negotiation failed");
            }
        }
        else
        {
            await SendResponseAsync(504, "AUTH type not supported");
        }
    }

    private async Task HandlePbszAsync()
    {
        await SendResponseAsync(200, "PBSZ=0");
    }

    private async Task HandleProtAsync(string argument)
    {
        switch (argument.ToUpper())
        {
            case "P":
                _dataProtection = FtpsServerDataConnectionProtection.Protected;
                await SendResponseAsync(200, "Protection level set to Private");
                break;
            case "C":
                _dataProtection = FtpsServerDataConnectionProtection.Clear;
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

        await SendResponseAsync(257, $"\"{_currentPath?.ToString()}\" is current directory");
    }

    private async Task HandleCwdAsync(string directory)
    {
        if (!CheckAuthentication() || !CheckPermission(p => p.Read))
        {
            await SendResponseAsync(550, "Permission denied");
            return;
        }

        var path = ResolveVirtualPath(directory);
        try
        {
            var fullPath = ResolveFullPath(directory);

            if (Directory.Exists(fullPath))
            {
                _currentPath = path;
                _log.Debug($"[{_clientAddress}] Changed directory to: {_currentPath}");
                await SendResponseAsync(250, "Directory changed");
            }
            else
            {
                await SendResponseAsync(550, "Directory not found");
            }
        }
        catch (UnauthorizedAccessException e)
        {
            _log.Error(e, $"[{_clientAddress}] Attempt to change directory to: {directory}");
            await SendResponseAsync(550, "Directory not found");
        }
    }

    private async Task HandleCdupAsync()
    {
        if (!CheckAuthentication() || !CheckPermission(p => p.Read) || _currentPath is null)
        {
            await SendResponseAsync(550, "Permission denied");
            return;
        }

        _currentPath = _currentPath.GoUp();
        await SendResponseAsync(250, "Directory changed");
    }

    private async Task HandleMkdAsync(string directory)
    {
        if (!CheckAuthentication() || !CheckPermission(p => p.Write))
        {
            await SendResponseAsync(550, "Permission denied");
            return;
        }

        var path = ResolveVirtualPath(directory);
        var fullPath = ResolveFullPath(directory);

        try
        {
            Directory.CreateDirectory(fullPath);
            _log.Info($"[{_clientAddress}] Created directory: {path}");
            await SendResponseAsync(257, $"\"{path}\" created");
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"[{_clientAddress}] Failed to create directory: {fullPath}");
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

        var path = ResolveVirtualPath(directory);
        var fullPath = ResolveFullPath(directory);

        try
        {
            if (Directory.Exists(fullPath))
            {
                Directory.Delete(fullPath, true);
                _log.Info($"[{_clientAddress}] Deleted directory: {fullPath}");
                await SendResponseAsync(250, "Directory removed");
            }
            else
            {
                await SendResponseAsync(550, "Directory not found");
            }
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"[{_clientAddress}] Failed to delete directory: {fullPath}");
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

        var path = ResolveVirtualPath(filename);
        var fullPath = ResolveFullPath(filename);

        try
        {
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                _log.Info($"[{_clientAddress}] Deleted file: {fullPath}");
                await SendResponseAsync(250, "File deleted");
            }
            else
            {
                await SendResponseAsync(550, "File not found");
            }
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"[{_clientAddress}] Failed to delete file: {fullPath}");
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

        var path = ResolveVirtualPath(filename);
        var fullPath = ResolveFullPath(filename);

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

        var path = ResolveVirtualPath(filename);
        var fullPath = ResolveFullPath(filename);

        try
        {
            if (File.Exists(_renameFrom))
            {
                File.Move(_renameFrom, fullPath);
                _log.Info($"[{_clientAddress}] Renamed file: {_renameFrom} -> {fullPath}");
                await SendResponseAsync(250, "File renamed");
            }
            else if (Directory.Exists(_renameFrom))
            {
                Directory.Move(_renameFrom, fullPath);
                _log.Info($"[{_clientAddress}] Renamed directory: {_renameFrom} -> {fullPath}");
                await SendResponseAsync(250, "Directory renamed");
            }
            else
            {
                await SendResponseAsync(550, "Rename failed");
            }
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"[{_clientAddress}] Rename {_renameFrom}->{fullPath} failed");
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
        _log.Debug($"[{_clientAddress}] {response}");
        await SendResponseAsync(227, response);
    }

    private async Task HandleListAsync(string path)
    {
        if (!CheckAuthentication() || !CheckPermission(p => p.Read) || _currentPath is null || _currentUser is null)
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
        catch (Exception)
        {
            await SendResponseAsync(425, "Can't open data connection");
            return;
        }

        var targetPath = string.IsNullOrEmpty(path) ? _currentPath : ResolveVirtualPath(path);
        var fullPath = targetPath.GetRealPath(_currentUser.RootFolder);

        await SendResponseAsync(150, "Opening data connection");

        try
        {
            using (dataClient)
            {
                Stream dataStream = dataClient.GetStream();

                // Apply SSL/TLS if protection is enabled
                if (_dataProtection == FtpsServerDataConnectionProtection.Protected && _certificate != null)
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
                        _log.Debug($"[{_clientAddress}] Listing {entries.Length} items from: {targetPath}");

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

            _log.Debug($"[{_clientAddress}] List transfer complete");
            await SendResponseAsync(226, "Transfer complete");
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"[{_clientAddress}] List failed");
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
        if (!CheckAuthentication() || !CheckPermission(p => p.Read) || _currentPath is null || _currentUser is null)
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
        catch (Exception)
        {
            await SendResponseAsync(425, "Can't open data connection");
            return;
        }

        var targetPath = string.IsNullOrEmpty(path) ? _currentPath : ResolveVirtualPath(path);
        var fullPath = targetPath.GetRealPath(_currentUser.RootFolder);

        await SendResponseAsync(150, "Opening data connection");

        try
        {
            using (dataClient)
            {
                Stream dataStream = dataClient.GetStream();

                // Apply SSL/TLS if protection is enabled
                if (_dataProtection == FtpsServerDataConnectionProtection.Protected && _certificate != null)
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
            _log.Error(ex, $"[{_clientAddress}] List failed");
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
        catch (Exception)
        {
            await SendResponseAsync(425, "Can't open data connection");
            return;
        }

        var path = ResolveVirtualPath(filename);
        var fullPath = ResolveFullPath(filename);

        if (!File.Exists(fullPath))
        {
            dataClient.Dispose();
            await SendResponseAsync(550, "File not found");
            return;
        }

        var fileInfo = new FileInfo(fullPath);
        _log.Info($"[{_clientAddress}] Downloading: {path} ({fileInfo.Length} bytes)");

        await SendResponseAsync(150, $"Opening data connection for {Path.GetFileName(filename)} ({fileInfo.Length} bytes)");

        try
        {
            using (dataClient)
            {
                Stream dataStream = dataClient.GetStream();

                // Apply SSL/TLS if protection is enabled
                if (_dataProtection == FtpsServerDataConnectionProtection.Protected && _certificate != null)
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

            _log.Info($"[{_clientAddress}] Download complete: {path}");
            await SendResponseAsync(226, "Transfer complete");
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"[{_clientAddress}] Download failed: {path}");
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
        catch (Exception)
        {
            await SendResponseAsync(425, "Can't open data connection");
            return;
        }

        var path = ResolveVirtualPath(filename);
        var fullPath = ResolveFullPath(filename);

        _log.Info($"[{_clientAddress}] Uploading: {path}");

        await SendResponseAsync(150, $"Opening data connection for {Path.GetFileName(filename)}");

        try
        {
            using (dataClient)
            {
                Stream dataStream = dataClient.GetStream();

                // Apply SSL/TLS if protection is enabled
                if (_dataProtection == FtpsServerDataConnectionProtection.Protected && _certificate != null)
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
            _log.Info($"[{_clientAddress}] Upload complete: {path} ({fileInfo.Length} bytes)");
            await SendResponseAsync(226, "Transfer complete");
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"[{_clientAddress}] Upload failed: {path}");
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

        var fullPath = ResolveFullPath(filename);

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
            _log.Error(ex, $"[{_clientAddress}] SIZE command failed");
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

        var fullPath = ResolveFullPath(filename);

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
            _log.Error(ex, $"[{_clientAddress}] MDTM command failed");
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

    private bool CheckPermission(Func<FtpsServerUserPermissions, bool> check)
    {
        if (_currentUser == null)
            return false;
        return check(_currentUser.Permissions);
    }

    private FtpsServerVirtualPath ResolveVirtualPath(string path)
    {
        if (_currentPath is null)
            return new FtpsServerVirtualPath("/");

        return _currentPath.Append(path);
    }

    private string ResolveFullPath(string virtualPath)
    {
        if (_currentPath is null ||
            _currentUser is null)
            return string.Empty;

        return _currentPath
            .Append(virtualPath)
            .GetRealPath(_currentUser.RootFolder);
    }

    private async Task SendResponseAsync(int code, string message)
    {
        var response = $"{code} {message}";
        _log.Debug($"[{_clientAddress}] << {response}");
        await _writer!.WriteLineAsync(response);
    }
}
