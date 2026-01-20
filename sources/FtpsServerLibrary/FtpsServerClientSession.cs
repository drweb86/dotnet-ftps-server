using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace FtpsServerLibrary;

class FtpsServerClientSession(
    IFtpsServerLog log,
    TcpClient controlClient,
    List<FtpsServerUserAccount> users,
    X509Certificate2? certificate,
    IFtpsServerFileSystemProvider fileSystemProvider)
{
    private readonly IFtpsServerLog _log = log;
    private readonly TcpClient _controlClient = controlClient;
    private readonly List<FtpsServerUserAccount> _users = users;
    private readonly X509Certificate2? _certificate = certificate;

    private System.IO.Stream? _controlStream;
    private System.IO.StreamReader? _reader;
    private System.IO.StreamWriter? _writer;
    private SslStream? _sslStream;

    private string? _username;
    private FtpsServerUserAccount? _currentUser;
    private bool _isAuthenticated;
    private FtpsServerVirtualPath? _currentPath;
    private string? _renameFrom;
    private FtpsServerVirtualPath? _renameFromPath;
    private Encoding _currentEncoding = Encoding.GetEncoding("ISO-8859-1");

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
            RecreateReaderWriter();

            await SendResponseAsync(220, "FTPS Server Ready");
            if (_reader is null)
                return;

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
                    await HandleOptsAsync(argument);
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

    private void RecreateReaderWriter()
    {
        if (_controlStream is null)
            return;
        _reader = new System.IO.StreamReader(_controlStream, _currentEncoding, leaveOpen: true);
        _writer = new System.IO.StreamWriter(_controlStream, _currentEncoding) { AutoFlush = true };
    }

    private async Task HandleOptsAsync(string argument)
    {
        var args = argument.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (args.Length >= 1 && args[0].Equals("UTF8", StringComparison.OrdinalIgnoreCase))
        {
            if (args.Length == 1 || args[1].Equals("ON", StringComparison.OrdinalIgnoreCase))
            {
                // Enable UTF-8 mode
                _currentEncoding = new UTF8Encoding(false); // Don't emit UTF-8 BOM

                // Recreate writer with new encoding
                if (_controlStream != null)
                {
                    _writer = new System.IO.StreamWriter(_controlStream, _currentEncoding)
                    {
                        AutoFlush = true,
                        NewLine = "\r\n" // Ensure proper FTP line endings
                    };

                    // Note: We don't recreate the reader here to avoid losing buffered commands
                    // The reader will continue to use its current encoding until we process
                    // the next command, at which point it will read from the new writer's buffer
                }

                await SendResponseAsync(200, "UTF8 mode enabled");
                _reader?.Dispose();
                if (_controlStream is not null)
                    _reader = new System.IO.StreamReader(_controlStream, _currentEncoding, leaveOpen: true);
                return;
            }
            else if (args[1].Equals("OFF", StringComparison.OrdinalIgnoreCase))
            {
                // Revert to ISO-8859-1
                _currentEncoding = Encoding.GetEncoding("ISO-8859-1");

                if (_controlStream != null)
                {
                    _writer = new System.IO.StreamWriter(_controlStream, _currentEncoding)
                    {
                        AutoFlush = true,
                        NewLine = "\r\n"
                    };
                }

                await SendResponseAsync(200, "UTF8 mode disabled");
                return;
            }
        }

        await SendResponseAsync(501, "Invalid OPTS command");
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

        var user = _users.FirstOrDefault(u => u.Login == _username);
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
                _sslStream = new SslStream(_controlStream!, false);
                await _sslStream.AuthenticateAsServerAsync(_certificate, false, SslProtocols.Tls12 | SslProtocols.Tls13, false);

                _controlStream = _sslStream;

                // Recreate reader/writer to use the encrypted stream with proper encoding
                RecreateReaderWriter();

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
        if (!CheckAuthentication() || !CheckPermission(true, false))
        {
            await SendResponseAsync(550, "Permission denied");
            return;
        }

        var path = ResolveVirtualPath(directory);
        try
        {
            if (await fileSystemProvider.DirectoryExists(_currentUser!.Folder, path.Segments))
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
        if (!CheckAuthentication() || !CheckPermission(true, false) || _currentPath is null)
        {
            await SendResponseAsync(550, "Permission denied");
            return;
        }

        _currentPath = _currentPath.GoUp();
        await SendResponseAsync(250, "Directory changed");
    }

    private async Task HandleMkdAsync(string directory)
    {
        if (!CheckAuthentication() || !CheckPermission(false, true))
        {
            await SendResponseAsync(550, "Permission denied");
            return;
        }

        var path = ResolveVirtualPath(directory);
        var fullPath = ResolveFullPath(directory);

        try
        {
            await fileSystemProvider.CreateDirectory(_currentUser!.Folder, path.Segments);
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
        if (!CheckAuthentication() || !CheckPermission(false, true))
        {
            await SendResponseAsync(550, "Permission denied");
            return;
        }

        var path = ResolveVirtualPath(directory);
        var fullPath = ResolveFullPath(directory);

        try
        {
            if (await fileSystemProvider.DirectoryExists(_currentUser!.Folder, path.Segments))
            {
                await fileSystemProvider.DirectoryDelete(_currentUser!.Folder, path.Segments);
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
        if (!CheckAuthentication() || !CheckPermission(false, true))
        {
            await SendResponseAsync(550, "Permission denied");
            return;
        }

        var path = ResolveVirtualPath(filename);
        var fullPath = ResolveFullPath(filename);

        try
        {
            if (await fileSystemProvider.FileExists(_currentUser!.Folder, path.Segments))
            {
                await fileSystemProvider.FileDelete(_currentUser!.Folder, path.Segments);

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
        if (!CheckAuthentication() || !CheckPermission(false, true))
        {
            await SendResponseAsync(550, "Permission denied");
            return;
        }

        _renameFromPath = ResolveVirtualPath(filename);
        var fullPath = ResolveFullPath(filename);

        if (await fileSystemProvider.FileExists(_currentUser!.Folder, _renameFromPath.Segments) ||
            await fileSystemProvider.DirectoryExists(_currentUser!.Folder, _renameFromPath.Segments))
        {
            _renameFrom = fullPath;
            _renameFromPath = null;
            await SendResponseAsync(350, "Ready for RNTO");
        }
        else
        {
            await SendResponseAsync(550, "File/directory not found");
        }
    }

    private async Task HandleRntoAsync(string filename)
    {
        if (!CheckAuthentication() || !CheckPermission(false, true))
        {
            await SendResponseAsync(550, "Permission denied");
            return;
        }

        if (string.IsNullOrEmpty(_renameFrom) || _renameFromPath is null)
        {
            await SendResponseAsync(503, "RNFR required first");
            return;
        }

        var path = ResolveVirtualPath(filename);
        var fullPath = ResolveFullPath(filename);

        try
        {
            if (await fileSystemProvider.FileExists(_currentUser!.Folder, _renameFromPath.Segments))
            {
                await fileSystemProvider.FileMove(_currentUser!.Folder, _renameFromPath.Segments, path.Segments);
                _log.Info($"[{_clientAddress}] Renamed file: {_renameFrom} -> {fullPath}");
                await SendResponseAsync(250, "File renamed");
            }
            else if (await fileSystemProvider.DirectoryExists(_currentUser!.Folder, _renameFromPath.Segments))
            {
                await fileSystemProvider.DirectoryMove(_currentUser!.Folder, _renameFromPath.Segments, path.Segments);
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
        if (!CheckAuthentication() || !CheckPermission(true, false) || _currentPath is null || _currentUser is null)
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

        await SendResponseAsync(150, "Opening data connection");

        try
        {
            using (dataClient)
            {
                System.IO.Stream dataStream = dataClient.GetStream();

                // Apply SSL/TLS if protection is enabled
                if (_dataProtection == FtpsServerDataConnectionProtection.Protected && _certificate != null)
                {
                    var sslStream = new SslStream(dataStream, false);
                    await sslStream.AuthenticateAsServerAsync(_certificate, false, SslProtocols.Tls12 | SslProtocols.Tls13, false);
                    dataStream = sslStream;
                }

                using (dataStream)
                using (var dataWriter = new System.IO.StreamWriter(dataStream, Encoding.UTF8) { AutoFlush = true })
                {
                    if (await fileSystemProvider.DirectoryExists(_currentUser!.Folder, targetPath.Segments))
                    {
                        var entries = await fileSystemProvider.DirectoryGetFileSystemEntries(_currentUser!.Folder, targetPath.Segments);
                        _log.Debug($"[{_clientAddress}] Listing {entries.Count()} items from: {targetPath}");

                        foreach (var entry in entries)
                        {
                            var permissions = entry.IsDirectory ? "drwxr-xr-x" : "-rw-r--r--";
                            var size = entry.IsDirectory ? "0" : entry.Length.ToString();
                            var modified = entry.LastWriteTime.ToString("MMM dd HH:mm");

                            var line = $"{permissions} 1 owner group {size,15} {modified} {entry.FileName}";
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
        if (!CheckAuthentication() || !CheckPermission(true, false) || _currentPath is null || _currentUser is null)
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

        await SendResponseAsync(150, "Opening data connection");

        try
        {
            using (dataClient)
            {
                System.IO.Stream dataStream = dataClient.GetStream();

                // Apply SSL/TLS if protection is enabled
                if (_dataProtection == FtpsServerDataConnectionProtection.Protected && _certificate != null)
                {
                    var sslStream = new SslStream(dataStream, false);
                    await sslStream.AuthenticateAsServerAsync(_certificate, false, SslProtocols.Tls12 | SslProtocols.Tls13, false);
                    dataStream = sslStream;
                }

                using (dataStream)
                using (var dataWriter = new System.IO.StreamWriter(dataStream, Encoding.UTF8) { AutoFlush = true })
                {
                    if (await fileSystemProvider.DirectoryExists(_currentUser.Folder, targetPath.Segments))
                    {
                        var entries = await fileSystemProvider.DirectoryGetFileSystemEntries(_currentUser!.Folder, targetPath.Segments);

                        foreach (var entry in entries)
                        {
                            await dataWriter.WriteLineAsync(entry.FileName);
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
        if (!CheckAuthentication() || !CheckPermission(true, false))
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

        if (!await fileSystemProvider.FileExists(_currentUser!.Folder, path.Segments))
        {
            dataClient.Dispose();
            await SendResponseAsync(550, "File not found");
            return;
        }

        _log.Info($"[{_clientAddress}] Downloading: {fullPath}");

        await SendResponseAsync(150, $"Opening data connection");

        try
        {
            using (dataClient)
            {
                System.IO.Stream dataStream = dataClient.GetStream();

                // Apply SSL/TLS if protection is enabled
                if (_dataProtection == FtpsServerDataConnectionProtection.Protected && _certificate != null)
                {
                    var sslStream = new SslStream(dataStream, false);
                    await sslStream.AuthenticateAsServerAsync(_certificate, false, SslProtocols.Tls12 | SslProtocols.Tls13, false);
                    dataStream = sslStream;
                }

                using (dataStream)
                using (var fileStream = await fileSystemProvider.FileOpenRead(_currentUser.Folder, path.Segments))
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
        if (!CheckAuthentication() || !CheckPermission(false, true))
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

        _log.Info($"[{_clientAddress}] Uploading: {fullPath}");

        await SendResponseAsync(150, $"Opening data connection for {fullPath}");

        try
        {
            using (dataClient)
            {
                System.IO.Stream dataStream = dataClient.GetStream();

                // Apply SSL/TLS if protection is enabled
                if (_dataProtection == FtpsServerDataConnectionProtection.Protected && _certificate != null)
                {
                    var sslStream = new SslStream(dataStream, false);
                    await sslStream.AuthenticateAsServerAsync(_certificate, false, SslProtocols.Tls12 | SslProtocols.Tls13, false);
                    dataStream = sslStream;
                }

                using (dataStream)
                using (var fileStream = await fileSystemProvider.FileCreate(_currentUser!.Folder, path.Segments))
                {
                    await dataStream.CopyToAsync(fileStream);
                }
            }

            _log.Info($"[{_clientAddress}] Upload complete.");
            await SendResponseAsync(226, "Transfer complete");
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"[{_clientAddress}] Upload failed: {fullPath}");
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
        if (!CheckAuthentication() || !CheckPermission(true, false))
        {
            await SendResponseAsync(550, "Permission denied");
            return;
        }

        var path = ResolveVirtualPath(filename);

        try
        {
            if (await fileSystemProvider.FileExists(_currentUser!.Folder, path.Segments))
            {
                var length = await fileSystemProvider.GetFileLength(_currentUser!.Folder, path.Segments);
                await SendResponseAsync(213, length.ToString());
            }
            else
            {
                await SendResponseAsync(550, "File not found");
            }
        }
        catch (Exception ex)
        {
            var fullPath = ResolveFullPath(filename);
            _log.Error(ex, $"[{_clientAddress}] {fullPath} SIZE command failed");
            await SendResponseAsync(550, $"Error: {ex.Message}");
        }
    }

    private async Task HandleMdtmAsync(string filename)
    {
        if (!CheckAuthentication() || !CheckPermission(true, false))
        {
            await SendResponseAsync(550, "Permission denied");
            return;
        }

        var path = ResolveVirtualPath(filename);

        try
        {
            if (await fileSystemProvider.FileExists(_currentUser!.Folder, path.Segments))
            {
                var lastWriteTimeUtc = await fileSystemProvider.GetFileLastWriteTimeUtc(_currentUser!.Folder, path.Segments);
                var timestamp = lastWriteTimeUtc.ToString("yyyyMMddHHmmss");
                await SendResponseAsync(213, timestamp);
            }
            else
            {
                await SendResponseAsync(550, "File not found");
            }
        }
        catch (Exception ex)
        {
            var fullPath = ResolveFullPath(filename);
            _log.Error(ex, $"[{_clientAddress}] {fullPath} MDTM command failed");
            await SendResponseAsync(550, $"Error: {ex.Message}");
        }
    }

    private async Task HandleFeatAsync()
    {
        // Store the feature lines in a list
        var featureLines = new List<string>
        {
            "211-Features:",
            " AUTH TLS",
            " PBSZ",
            " PROT",
            " SIZE",
            " MDTM",
            " UTF8",
            "211 End"
        };

        // Write each line using the current encoding
        foreach (var line in featureLines)
        {
            await _writer!.WriteLineAsync(line);
        }

        // Make sure everything is flushed
        await _writer!.FlushAsync();
    }

    private bool CheckAuthentication()
    {
        return _isAuthenticated && _currentUser != null;
    }

    private bool CheckPermission(bool read, bool write)
    {
        if (_currentUser == null)
            return false;
#pragma warning disable IDE0075 // Simplify conditional expression
        return (read ? _currentUser.Read : true) &&
            (write ? _currentUser.Write : true);
#pragma warning restore IDE0075 // Simplify conditional expression
    }

    private FtpsServerVirtualPath ResolveVirtualPath(string path)
    {
        if (_currentPath is null)
            return new FtpsServerVirtualPath(path);

        return _currentPath.Append(path);
    }

    private string ResolveFullPath(string virtualPath)
    {
        if (_currentPath is null ||
            _currentUser is null)
            return string.Empty;

        var result = _currentPath
            .Append(virtualPath);

        return fileSystemProvider
            .GetRealPath(_currentUser.Folder, result.Segments);
    }

    private async Task SendResponseAsync(int code, string message)
    {
        var response = $"{code} {message}";
        _log.Debug($"[{_clientAddress}] << {response}");

        // Use WriteAsync with the current encoding
        var bytes = _currentEncoding.GetBytes(response + "\r\n");
        await _controlStream!.WriteAsync(bytes);
        await _controlStream.FlushAsync();
    }
}
