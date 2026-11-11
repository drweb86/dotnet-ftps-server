using System.Net;
using System.Net.Sockets;
using System.Text;
using FluentFTP;
using FluentFTP.Proxy.AsyncProxy;

namespace FtpsServerLibrary.Tests.FluentFtp;

public sealed class AndroidEmulatorFixture : IAsyncLifetime
{
    AdbSocksBridge? _bridge;
    internal ProtocolEndpoint Endpoint { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        var adb = HostProbe.AdbPath ?? HostProbe.FindAdb() ?? throw new InvalidOperationException("adb was not found.");
        var serial = HostProbe.EmulatorSerial ?? throw new InvalidOperationException("No emulator serial.");
        await Adb.EnsureServerAsync(adb, serial);
        _bridge = await AdbSocksBridge.StartAsync(adb, serial);
        var bridge = _bridge;
        Endpoint = new ProtocolEndpoint
        {
            Name = "Android emulator",
            Host = "127.0.0.1",
            Port = AndroidAccount.Port,
            User = AndroidAccount.User,
            Password = AndroidAccount.Password,
            OpenTcp = port => Adb.OpenTcpAsync(adb, serial, port),
            Connect = (protocol, user, password) => ConnectAsync(bridge, protocol, user, password),
        };
    }

    public Task DisposeAsync()
    {
        _bridge?.Dispose();
        return Task.CompletedTask;
    }

    static async Task<AsyncFtpClient> ConnectAsync(AdbSocksBridge bridge, System.Security.Authentication.SslProtocols protocol, string user, string password)
    {
        var client = new AsyncFtpClientSocks5Proxy(new FtpProxyProfile
        {
            ProxyHost = "127.0.0.1",
            ProxyPort = bridge.Port,
            FtpHost = "localhost",
            FtpPort = AndroidAccount.Port,
            FtpCredentials = new NetworkCredential(user, password),
        });
        ProtocolEndpoint.Apply(client.Config, protocol);
        try
        {
            await client.Connect();
            client.Encoding = new UTF8Encoding(false);
            return client;
        }
        catch
        {
            client.Dispose();
            throw;
        }
    }
}

static class AndroidAccount
{
    public static string User => Environment.GetEnvironmentVariable("FTPS_ANDROID_USER") ?? "u1";
    public static string Password => Environment.GetEnvironmentVariable("FTPS_ANDROID_PASSWORD") ?? "FtpTest55";
    public static int Port => int.TryParse(Environment.GetEnvironmentVariable("FTPS_ANDROID_PORT"), out var port) ? port : 2121;
}

static class Adb
{
    public static async Task EnsureServerAsync(string adb, string serial)
    {
        if (await BannerAsync(adb, serial) != null)
            return;

        await ShellAsync(adb, serial, "am start -n com.siarheikuchuk.ftpsserver.debug/com.siarheikuchuk.ftpsserver.MainActivity");
        var deadline = DateTime.UtcNow.AddSeconds(25);
        while (DateTime.UtcNow < deadline)
        {
            if (await BannerAsync(adb, serial) != null)
                return;
            var dump = await ShellAsync(adb, serial, "uiautomator dump /sdcard/uidump.xml >/dev/null; cat /sdcard/uidump.xml");
            if (dump.Contains("[ STOP ]", StringComparison.Ordinal))
            {
                if (await BannerAsync(adb, serial) != null)
                    return;
            }

            var match = System.Text.RegularExpressions.Regex.Match(
                dump,
                "text=\"\\[ START \\]\".{0,600}?bounds=\"\\[(\\d+),(\\d+)\\]\\[(\\d+),(\\d+)\\]\"");
            if (match.Success)
            {
                var x = (int.Parse(match.Groups[1].Value) + int.Parse(match.Groups[3].Value)) / 2;
                var y = (int.Parse(match.Groups[2].Value) + int.Parse(match.Groups[4].Value)) / 2;
                await ShellAsync(adb, serial, "input tap " + x + " " + y);
            }

            await Task.Delay(1000);
        }

        throw new TimeoutException("The Android FTPS server did not answer on port " + AndroidAccount.Port + ".");
    }

    static async Task<string?> BannerAsync(string adb, string serial)
    {
        try
        {
            using var tcp = await OpenTcpAsync(adb, serial, AndroidAccount.Port);
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(4));
            var buffer = new byte[128];
            var read = await tcp.GetStream().ReadAsync(buffer, timeout.Token);
            var line = Encoding.ASCII.GetString(buffer, 0, read);
            return line.StartsWith("220", StringComparison.Ordinal) ? line : null;
        }
        catch (Exception ex) when (ex is IOException or SocketException or InvalidOperationException or OperationCanceledException)
        {
            return null;
        }
    }

    public static async Task<TcpClient> OpenTcpAsync(string adb, string serial, int port)
    {
        var tcp = new TcpClient { NoDelay = true };
        await tcp.ConnectAsync(IPAddress.Loopback, 5037);
        var stream = tcp.GetStream();
        await CommandAsync(stream, "host:transport:" + serial);
        await CommandAsync(stream, "tcp:" + port.ToString(System.Globalization.CultureInfo.InvariantCulture));
        return tcp;
    }

    static async Task CommandAsync(NetworkStream stream, string command)
    {
        var payload = Encoding.ASCII.GetBytes(command);
        var header = Encoding.ASCII.GetBytes(payload.Length.ToString("X4", System.Globalization.CultureInfo.InvariantCulture));
        await stream.WriteAsync(header);
        await stream.WriteAsync(payload);
        var status = Encoding.ASCII.GetString(await ReadExactAsync(stream, 4));
        if (status == "OKAY")
            return;
        if (status == "FAIL")
        {
            var length = int.Parse(Encoding.ASCII.GetString(await ReadExactAsync(stream, 4)), System.Globalization.NumberStyles.HexNumber);
            var message = Encoding.ASCII.GetString(await ReadExactAsync(stream, length));
            throw new InvalidOperationException(command + " failed: " + message);
        }

        throw new InvalidOperationException(command + " unexpected status " + status);
    }

    public static async Task<string> ShellAsync(string adb, string serial, string command)
    {
        using var process = new System.Diagnostics.Process
        {
            StartInfo = new System.Diagnostics.ProcessStartInfo(adb)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            }
        };
        process.StartInfo.ArgumentList.Add("-s");
        process.StartInfo.ArgumentList.Add(serial);
        process.StartInfo.ArgumentList.Add("shell");
        process.StartInfo.ArgumentList.Add(command);
        process.Start();
        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        return await stdoutTask + await stderrTask;
    }

    public static async Task<byte[]> ReadExactAsync(NetworkStream stream, int count)
    {
        var buffer = new byte[count];
        var offset = 0;
        while (offset < count)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(offset, count - offset));
            if (read == 0)
                throw new EndOfStreamException("expected " + count + " bytes, got " + offset);
            offset += read;
        }

        return buffer;
    }
}

sealed class AdbSocksBridge : IDisposable
{
    readonly CancellationTokenSource _cancel = new();
    readonly TcpListener _listener;
    readonly Task _acceptLoop;
    public int Port { get; }

    AdbSocksBridge(string adb, string serial)
    {
        _listener = new TcpListener(IPAddress.Loopback, 0);
        _listener.Start();
        Port = ((IPEndPoint)_listener.LocalEndpoint).Port;
        _acceptLoop = AcceptAsync(adb, serial, _cancel.Token);
    }

    public static Task<AdbSocksBridge> StartAsync(string adb, string serial)
    {
        return Task.FromResult(new AdbSocksBridge(adb, serial));
    }

    async Task AcceptAsync(string adb, string serial, CancellationToken cancel)
    {
        try
        {
            while (!cancel.IsCancellationRequested)
            {
                var client = await _listener.AcceptTcpClientAsync(cancel);
                _ = Task.Run(() => HandleAsync(client, adb, serial), CancellationToken.None);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (ObjectDisposedException)
        {
        }
    }

    static async Task HandleAsync(TcpClient client, string adb, string serial)
    {
        using (client)
        {
            var stream = client.GetStream();
            try
            {
                var greeting = await Adb.ReadExactAsync(stream, 2);
                var methods = await Adb.ReadExactAsync(stream, greeting[1]);
                if (greeting[0] != 5 || !methods.Contains((byte)0))
                {
                    await stream.WriteAsync(new byte[] { 5, 0xFF });
                    return;
                }

                await stream.WriteAsync(new byte[] { 5, 0 });
                var head = await Adb.ReadExactAsync(stream, 4);
                if (head[1] != 1)
                    throw new InvalidOperationException("SOCKS command " + head[1]);
                int port = head[3] switch
                {
                    1 => await ReadPortAsync(stream, 4),
                    3 => await ReadPortAsync(stream, (await Adb.ReadExactAsync(stream, 1))[0]),
                    4 => await ReadPortAsync(stream, 16),
                    _ => throw new InvalidOperationException("SOCKS address type " + head[3]),
                };

                using var device = await Adb.OpenTcpAsync(adb, serial, port);
                device.NoDelay = true;
                client.NoDelay = true;
                await stream.WriteAsync(new byte[] { 5, 0, 0, 1, 0, 0, 0, 0, 0, 0 });
                var deviceStream = device.GetStream();
                var clientToDevice = CopyThenShutdownAsync(stream, deviceStream, device.Client);
                var deviceToClient = CopyThenShutdownAsync(deviceStream, stream, client.Client);
                await Task.WhenAll(clientToDevice, deviceToClient);
            }
            catch (IOException)
            {
            }
            catch (SocketException)
            {
            }
            catch (InvalidOperationException)
            {
            }
        }
    }

    static async Task<int> ReadPortAsync(NetworkStream stream, int addressLength)
    {
        _ = await Adb.ReadExactAsync(stream, addressLength);
        var portBytes = await Adb.ReadExactAsync(stream, 2);
        return (portBytes[0] << 8) | portBytes[1];
    }

    static async Task CopyThenShutdownAsync(NetworkStream from, NetworkStream to, Socket destination)
    {
        try
        {
            await from.CopyToAsync(to);
            await to.FlushAsync();
            try { destination.Shutdown(SocketShutdown.Send); } catch (SocketException) { }
            catch (ObjectDisposedException) { }
        }
        catch (Exception ex) when (ex is IOException or ObjectDisposedException or SocketException)
        {
            try { destination.Shutdown(SocketShutdown.Send); } catch { }
        }
    }

    public void Dispose()
    {
        _cancel.Cancel();
        _listener.Stop();
        try { _acceptLoop.Wait(TimeSpan.FromSeconds(2)); } catch (AggregateException) { }
        _cancel.Dispose();
    }
}
