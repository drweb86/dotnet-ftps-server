using System;
using System.Collections.Generic;
using System.Globalization;
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
    private FtpsServerUserAccount? _user;
    private bool _isAuthenticated;
    private FtpsServerVirtualPath _path = new();
    private FtpsServerVirtualPath? _renameFrom;
    private Encoding _currentEncoding = Encoding.GetEncoding("ISO-8859-1");

    private TcpListener? _dataListener;
    private string _transferMode = "I"; // A = ASCII, I = Binary
    private bool _isPassiveMode;
    private readonly string _clientAddress = controlClient.Client.RemoteEndPoint?.ToString() ?? "unknown";

    // FTPS data connection protection level
    private FtpsServerDataConnectionProtection _dataProtection = FtpsServerDataConnectionProtection.Clear;


    private void Log(string command, string text)
    {
        _log.Info($"[{_user?.Login}] {command}: {text}");
    }

    private void LogError(Exception exception, string text)
    {
        _log.Error(exception, $"[{_user?.Login}]: {text}");
    }

    private void LogError(string text)
    {
        _log.Error(new Exception(text), $"[{_user?.Login}]: {text}");
    }

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
            _user = user;
            _isAuthenticated = true;
            _path = new FtpsServerVirtualPath();

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

        var currentDir = _path.ToFtpsPath();
        Log("get current dir", currentDir);
        await SendResponseAsync(257, $"\"{currentDir}\" is current directory");
    }

    private async Task HandleCwdAsync(string directory)
    {
        if (!CheckAuthentication() || !CheckPermission(true, false))
        {
            await SendResponseAsync(550, "Permission denied");
            return;
        }

        var resultPath = _path.Append(directory);
        Log("change directory", resultPath.ToFtpsPath());
        try
        {
            if (await fileSystemProvider.DirectoryExists(_user!.Folder, resultPath.Segments))
            {
                _path = resultPath;
                await SendResponseAsync(250, "Directory changed");
            }
            else
            {
                await SendResponseAsync(550, "Directory not found");
            }
        }
        catch (UnauthorizedAccessException e)
        {
            LogError(e, $"[{_clientAddress}] Attempt to change directory to: {directory}");
            await SendResponseAsync(550, "Directory not found");
        }
    }

    private async Task HandleCdupAsync()
    {
        if (!CheckAuthentication() || !CheckPermission(true, false) || _path is null)
        {
            await SendResponseAsync(550, "Permission denied");
            return;
        }

        _path = _path.GoUp();
        var currentDir = _path.ToFtpsPath();
        Log("change directory up", currentDir);
        await SendResponseAsync(250, "Directory changed");
    }

    private async Task HandleMkdAsync(string directory)
    {
        if (!CheckAuthentication() || !CheckPermission(false, true))
        {
            await SendResponseAsync(550, "Permission denied");
            return;
        }

        var path = _path.Append(directory);
        var ftpsPath = path.ToFtpsPath();
        Log("create directory", ftpsPath);

        try
        {
            await fileSystemProvider.CreateDirectory(_user!.Folder, path.Segments);
            await SendResponseAsync(257, $"\"{ftpsPath}\" created");
        }
        catch (Exception ex)
        {
            LogError(ex, $"Failed to create directory: {ftpsPath}");
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

        var path = _path.Append(directory);
        var ftpsPath = path.ToFtpsPath();
        Log("delete directory", ftpsPath);

        try
        {
            if (await fileSystemProvider.DirectoryExists(_user!.Folder, path.Segments))
            {
                await fileSystemProvider.DirectoryDelete(_user!.Folder, path.Segments);
                await SendResponseAsync(250, "Directory removed");
            }
            else
            {
                LogError("Failed to delete directory");
                await SendResponseAsync(550, "Directory not found");
            }
        }
        catch (Exception ex)
        {
            LogError(ex, $"Failed to delete directory: {ftpsPath}");
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

        var path = _path.Append(filename);
        var ftpsPath = path.ToFtpsPath();
        Log("delete file", ftpsPath);

        try
        {
            if (await fileSystemProvider.FileExists(_user!.Folder, path.Segments))
            {
                await fileSystemProvider.FileDelete(_user!.Folder, path.Segments);
                await SendResponseAsync(250, "File deleted");
            }
            else
            {
                LogError("File not found");
                await SendResponseAsync(550, "File not found");
            }
        }
        catch (Exception ex)
        {
            LogError(ex, $"Failed to delete file: {ftpsPath}");
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

        var path = _path.Append(filename);
        var ftpsPath = path.ToFtpsPath();
        Log("rename from", ftpsPath);
        _renameFrom = path;

        if (await fileSystemProvider.FileExists(_user!.Folder, _renameFrom.Segments) ||
            await fileSystemProvider.DirectoryExists(_user!.Folder, _renameFrom.Segments))
        {
            await SendResponseAsync(350, "Ready for RNTO");
        }
        else
        {
            _renameFrom = null;
            LogError("rename from file/directory does not exist");
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

        if (_renameFrom is null)
        {
            await SendResponseAsync(503, "RNFR required first");
            return;
        }

        var path = _path.Append(filename);
        var ftpsPath = path.ToFtpsPath();
        Log("rename to", ftpsPath);

        try
        {
            if (await fileSystemProvider.FileExists(_user!.Folder, _renameFrom.Segments))
            {
                await fileSystemProvider.FileMove(_user!.Folder, _renameFrom.Segments, path.Segments);
                await SendResponseAsync(250, "File renamed");
            }
            else if (await fileSystemProvider.DirectoryExists(_user!.Folder, _renameFrom.Segments))
            {
                await fileSystemProvider.DirectoryMove(_user!.Folder, _renameFrom.Segments, path.Segments);
                await SendResponseAsync(250, "Directory renamed");
            }
            else
            {
                LogError("rename to file/directory does not exist");
                await SendResponseAsync(550, "Rename failed");
            }
        }
        catch (Exception ex)
        {
            LogError(ex, "rename failed");
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

    private async Task HandleListAsync(string directory)
    {
        if (!CheckAuthentication() || !CheckPermission(true, false) || _path is null || _user is null)
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

        var path = _path.Append(directory ?? ".");
        var ftpsPath = path.ToFtpsPath();
        Log("get directory contents", ftpsPath);

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
                    if (await fileSystemProvider.DirectoryExists(_user!.Folder, path.Segments))
                    {
                        var entries = await fileSystemProvider.DirectoryGetFileSystemEntries(_user!.Folder, path.Segments);
                        foreach (var entry in entries)
                        {
                            var permissions = entry.IsDirectory ? "drwxr-xr-x" : "-rw-r--r--";
                            var size = entry.IsDirectory ? "0" : entry.Length.ToString();
                            var modified = entry.LastWriteTime.ToString("MMM dd HH:mm", CultureInfo.InvariantCulture);

                            var line = $"{permissions} 1 owner group {size,15} {modified} {entry.FileName}";
                            dataWriter.NewLine = "\r\n";
                            await dataWriter.WriteLineAsync(line);
                        }
                    }
                }
            }

            await SendResponseAsync(226, "Transfer complete");
        }
        catch (Exception ex)
        {
            LogError(ex, "List failed");
            await SendResponseAsync(550, $"List failed: {ex.Message}");
        }
        finally
        {
            _dataListener?.Stop();
            _dataListener = null;
            _isPassiveMode = false;
        }
    }

    private async Task HandleNlstAsync(string directory)
    {
        if (!CheckAuthentication() || !CheckPermission(true, false) || _path is null || _user is null)
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

        var path = _path.Append(directory ?? ".");
        var ftpsPath = path.ToFtpsPath();
        Log("get directory contents 2", ftpsPath);

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
                    if (await fileSystemProvider.DirectoryExists(_user.Folder, path.Segments))
                    {
                        var entries = await fileSystemProvider.DirectoryGetFileSystemEntries(_user!.Folder, path.Segments);

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
            LogError(ex, "List failed");
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

        var path = _path.Append(filename);
        var ftpsPath = path.ToFtpsPath();
        Log("download", ftpsPath);

        if (!await fileSystemProvider.FileExists(_user!.Folder, path.Segments))
        {
            dataClient.Dispose();
            await SendResponseAsync(550, "File not found");
            return;
        }

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
                using (var fileStream = await fileSystemProvider.FileOpenRead(_user.Folder, path.Segments))
                {
                    await fileStream.CopyToAsync(dataStream);
                }
            }

            _log.Info($"[{_clientAddress}] Download complete: {path}");
            await SendResponseAsync(226, "Transfer complete");
        }
        catch (Exception ex)
        {
            LogError(ex, $"Download failed: {ftpsPath}");
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

        var path = _path.Append(filename);
        var ftpsPath = path.ToFtpsPath();
        Log("upload", ftpsPath);

        await SendResponseAsync(150, $"Opening data connection for {ftpsPath}");

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
                using (var fileStream = await fileSystemProvider.FileCreate(_user!.Folder, path.Segments))
                {
                    await dataStream.CopyToAsync(fileStream);
                }
            }

            await SendResponseAsync(226, "Transfer complete");
        }
        catch (Exception ex)
        {
            LogError(ex, $"Upload failed: {ftpsPath}");
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

        var path = _path.Append(filename);
        var ftpsPath = path.ToFtpsPath();
        Log("get size", ftpsPath);

        try
        {
            if (await fileSystemProvider.FileExists(_user!.Folder, path.Segments))
            {
                var length = await fileSystemProvider.GetFileLength(_user!.Folder, path.Segments);
                await SendResponseAsync(213, length.ToString());
            }
            else
            {
                await SendResponseAsync(550, "File not found");
            }
        }
        catch (Exception ex)
        {
            LogError(ex, $"{ftpsPath} SIZE command failed");
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

        var path = _path.Append(filename);
        var ftpsPath = path.ToFtpsPath();
        Log("get modified time", ftpsPath);

        try
        {
            if (await fileSystemProvider.FileExists(_user!.Folder, path.Segments))
            {
                var lastWriteTimeUtc = await fileSystemProvider.GetFileLastWriteTimeUtc(_user!.Folder, path.Segments);
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
            LogError(ex, $"{ftpsPath} MDTM command failed");
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
        return _isAuthenticated && _user != null;
    }

    private bool CheckPermission(bool read, bool write)
    {
        if (_user == null)
            return false;
#pragma warning disable IDE0075 // Simplify conditional expression
        return (read ? _user.Read : true) &&
            (write ? _user.Write : true);
#pragma warning restore IDE0075 // Simplify conditional expression
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
