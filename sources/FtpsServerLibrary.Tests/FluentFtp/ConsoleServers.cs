using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace FtpsServerLibrary.Tests.FluentFtp;

public sealed class WindowsConsoleFixture : IAsyncLifetime
{
    ConsoleProcess? _process;
    string _root = "";
    internal ProtocolEndpoint Endpoint { get; private set; } = null!;

    public Task InitializeAsync() => StartAsync(linux: false);

    public Task DisposeAsync()
    {
        _process?.Dispose();
        TryDelete(_root);
        return Task.CompletedTask;
    }

    internal async Task StartAsync(bool linux)
    {
        _root = Path.Combine(Path.GetTempPath(), "ftps-fluent-" + Guid.NewGuid().ToString("N"));
        var writeRoot = Path.Combine(_root, "write");
        var readRoot = Path.Combine(_root, "read");
        Directory.CreateDirectory(writeRoot);
        Directory.CreateDirectory(readRoot);
        var seed = "seed-from-console"u8.ToArray();
        await File.WriteAllBytesAsync(Path.Combine(readRoot, "seed.txt"), seed);
        var cert = Path.Combine(_root, "server.pfx");
        await File.WriteAllBytesAsync(cert, TestCertificate.Export());
        if (linux)
            await LinuxConsole.PublishAsync(Path.Combine(Path.GetTempPath(), "ftps-console-linux-x64"));

        await FtpsTestGate.Start.WaitAsync();
        try
        {
            var port = FreePort();
            _process = linux
                ? await LinuxConsole.StartAsync(port, cert, writeRoot, readRoot)
                : WindowsConsole.Start(port, cert, writeRoot, readRoot);
            await _process.WaitForBannerAsync(linux ? TimeSpan.FromMinutes(3) : TimeSpan.FromSeconds(40));
            Endpoint = new ProtocolEndpoint
            {
                Name = linux ? "Linux console" : "Windows console",
                Host = linux ? "localhost" : "127.0.0.1",
                Port = port,
                User = "writer",
                Password = "writer-pass",
                ReaderUser = "reader",
                ReaderPassword = "reader-pass",
                SeedPath = "/seed.txt",
                Seed = seed,
            };
        }
        finally
        {
            FtpsTestGate.Start.Release();
        }
    }

    static int FreePort()
    {
        for (var port = 25021; port < 26000; port++)
        {
            try
            {
                using var listener = new TcpListener(IPAddress.Loopback, port);
                listener.Start();
                return port;
            }
            catch (SocketException)
            {
            }
        }

        throw new InvalidOperationException("No free TCP port from 25021.");
    }

    static void TryDelete(string path)
    {
        try
        {
            if (Directory.Exists(path))
                Directory.Delete(path, true);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}

public sealed class LinuxConsoleFixture : IAsyncLifetime
{
    readonly WindowsConsoleFixture _inner = new();

    internal ProtocolEndpoint Endpoint => _inner.Endpoint;

    public async Task InitializeAsync()
    {
        if (!HostProbe.WslResponds)
            return;
        await _inner.StartAsync(linux: true);
    }

    public Task DisposeAsync() => _inner.DisposeAsync();
}

static class WindowsConsole
{
    public static ConsoleProcess Start(int port, string cert, string writeRoot, string readRoot)
    {
        var dll = Path.Combine(AppContext.BaseDirectory, "ftps-server.dll");
        if (!File.Exists(dll))
            throw new FileNotFoundException("ftps-server.dll was not copied to the test output.", dll);

        var process = new Process
        {
            StartInfo = new ProcessStartInfo(DotNetHost.Path)
            {
                WorkingDirectory = AppContext.BaseDirectory,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            }
        };
        process.StartInfo.ArgumentList.Add("exec");
        process.StartInfo.ArgumentList.Add(dll);
        AddServerArguments(process.StartInfo, port, cert, writeRoot, readRoot);
        return new ConsoleProcess(process, port);
    }

    public static void AddServerArguments(ProcessStartInfo info, int port, string cert, string writeRoot, string readRoot)
    {
        info.ArgumentList.Add("--ip");
        info.ArgumentList.Add("127.0.0.1");
        info.ArgumentList.Add("--port");
        info.ArgumentList.Add(port.ToString(System.Globalization.CultureInfo.InvariantCulture));
        info.ArgumentList.Add("--cert");
        info.ArgumentList.Add(cert);
        info.ArgumentList.Add("--certpass");
        info.ArgumentList.Add(TestCertificate.Password);
        info.ArgumentList.Add("--user");
        info.ArgumentList.Add("writer#writer-pass#" + writeRoot + "#RW");
        info.ArgumentList.Add("--user");
        info.ArgumentList.Add("reader#reader-pass#" + readRoot + "#R");
    }
}

static class LinuxConsole
{
    const int PassiveFirst = 40000;
    const int PassiveLast = 40199;

    public static async Task<ConsoleProcess> StartAsync(int port, string cert, string writeRoot, string readRoot)
    {
        var published = Path.Combine(Path.GetTempPath(), "ftps-console-linux-x64");
        await PublishAsync(published);
        var root = Path.GetDirectoryName(cert)!;
        if (!writeRoot.StartsWith(root, StringComparison.OrdinalIgnoreCase)
            || !readRoot.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Linux share folders must sit next to the certificate.");
        var scriptPath = Path.Combine(root, "start-ftps.sh");
        var portText = port.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var script = string.Join('\n', new[]
        {
            "#!/bin/bash",
            "set -eu",
            "export DEBIAN_FRONTEND=noninteractive",
            "mount -o remount,rw /proc/sys",
            "printf '%s\\n' '" + PassiveFirst + " " + PassiveLast + "' > /proc/sys/net/ipv4/ip_local_port_range",
            "apt-get update -qq",
            "pkg=$(apt-cache search --names-only '^libicu[0-9]+$' | awk 'NR==1 { print $1 }')",
            "test -n \"$pkg\"",
            "apt-get install -y -qq \"$pkg\"",
            "rm -rf /tmp/ftps",
            "mkdir -p /tmp/ftps",
            "cp -a /opt/ftps/. /tmp/ftps/",
            "chmod +x /tmp/ftps/ftps-server",
            "exec /tmp/ftps/ftps-server --ip 0.0.0.0 --port " + portText
                + " --cert /data/server.pfx --certpass " + TestCertificate.Password
                + " --user writer#writer-pass#/data/write#RW --user reader#reader-pass#/data/read#R",
        }) + "\n";
        await File.WriteAllTextAsync(scriptPath, script, new UTF8Encoding(false));

        var name = "ftps-it-" + portText;
        try
        {
            HostProbe.Run("wslc.exe", "rm -f " + name, Encoding.UTF8, TimeSpan.FromSeconds(20));
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException or TimeoutException or System.ComponentModel.Win32Exception)
        {
        }

        var process = new Process
        {
            StartInfo = new ProcessStartInfo("wslc.exe")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            }
        };
        process.StartInfo.ArgumentList.Add("run");
        process.StartInfo.ArgumentList.Add("--rm");
        process.StartInfo.ArgumentList.Add("--name");
        process.StartInfo.ArgumentList.Add(name);
        process.StartInfo.ArgumentList.Add("--cap-add");
        process.StartInfo.ArgumentList.Add("SYS_ADMIN");
        process.StartInfo.ArgumentList.Add("-p");
        process.StartInfo.ArgumentList.Add("127.0.0.1:" + portText + ":" + portText);
        process.StartInfo.ArgumentList.Add("-p");
        process.StartInfo.ArgumentList.Add("127.0.0.1:" + PassiveFirst + "-" + PassiveLast + ":" + PassiveFirst + "-" + PassiveLast);
        process.StartInfo.ArgumentList.Add("-v");
        process.StartInfo.ArgumentList.Add(published + ":/opt/ftps");
        process.StartInfo.ArgumentList.Add("-v");
        process.StartInfo.ArgumentList.Add(root + ":/data");
        process.StartInfo.ArgumentList.Add("ubuntu:latest");
        process.StartInfo.ArgumentList.Add("bash");
        process.StartInfo.ArgumentList.Add("/data/start-ftps.sh");
        return new ConsoleProcess(process, port, "kill " + name);
    }

    internal static async Task PublishAsync(string output)
    {
        Directory.CreateDirectory(output);
        var binary = Path.Combine(output, "ftps-server");
        var library = Path.Combine(AppContext.BaseDirectory, "FtpsServerLibrary.dll");
        var console = Path.Combine(AppContext.BaseDirectory, "ftps-server.dll");
        var newestInput = new[] { library, console }.Where(File.Exists).Select(File.GetLastWriteTimeUtc).DefaultIfEmpty().Max();
        if (File.Exists(binary) && File.GetLastWriteTimeUtc(binary) >= newestInput)
            return;

        var project = FindConsoleProject();
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo(DotNetHost.Path)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            }
        };
        process.StartInfo.ArgumentList.Add("publish");
        process.StartInfo.ArgumentList.Add(project);
        process.StartInfo.ArgumentList.Add("-c");
        process.StartInfo.ArgumentList.Add("Release");
        process.StartInfo.ArgumentList.Add("-r");
        process.StartInfo.ArgumentList.Add("linux-x64");
        process.StartInfo.ArgumentList.Add("--self-contained");
        process.StartInfo.ArgumentList.Add("true");
        process.StartInfo.ArgumentList.Add("-o");
        process.StartInfo.ArgumentList.Add(output);
        process.StartInfo.ArgumentList.Add("--nologo");
        process.StartInfo.ArgumentList.Add("-nodeReuse:false");
        process.StartInfo.Environment["MSBUILDDISABLENODEREUSE"] = "1";
        process.StartInfo.Environment["DOTNET_CLI_DO_NOT_USE_MSBUILD_SERVER"] = "1";
        process.Start();
        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();
        using var publishTimeout = new CancellationTokenSource(TimeSpan.FromMinutes(3));
        await process.WaitForExitAsync(publishTimeout.Token);
        var stdout = await stdoutTask.WaitAsync(TimeSpan.FromSeconds(10));
        var stderr = await stderrTask.WaitAsync(TimeSpan.FromSeconds(10));
        if (process.ExitCode != 0)
            throw new InvalidOperationException("linux-x64 publish failed.\n" + stdout + "\n" + stderr);
    }

    static string FindConsoleProject()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, "FtpsServerConsole", "FtpsServerConsole.csproj");
            if (File.Exists(candidate))
                return candidate;
            dir = dir.Parent;
        }

        throw new FileNotFoundException("FtpsServerConsole.csproj was not found above the test output.");
    }

}

sealed class ConsoleProcess : IDisposable
{
    readonly Process _process;
    readonly int _port;
    readonly string? _wslKill;
    readonly StringBuilder _log = new();

    public ConsoleProcess(Process process, int port, string? wslKill = null)
    {
        _process = process;
        _port = port;
        _wslKill = wslKill;
        _process.OutputDataReceived += (_, e) => { if (e.Data != null) lock (_log) _log.AppendLine(e.Data); };
        _process.ErrorDataReceived += (_, e) => { if (e.Data != null) lock (_log) _log.AppendLine(e.Data); };
        if (!_process.Start())
            throw new InvalidOperationException("Could not start the FTPS console.");
        _process.BeginOutputReadLine();
        _process.BeginErrorReadLine();
    }

    public async Task WaitForBannerAsync(TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow.Add(timeout);
        while (DateTime.UtcNow < deadline)
        {
            if (_process.HasExited)
                throw new InvalidOperationException("FTPS console exited " + _process.ExitCode + ".\n" + Log());
            try
            {
                using var tcp = new TcpClient();
                using var attempt = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                await tcp.ConnectAsync("127.0.0.1", _port, attempt.Token);
                var buffer = new byte[128];
                var read = await tcp.GetStream().ReadAsync(buffer, attempt.Token);
                var line = Encoding.ASCII.GetString(buffer, 0, read);
                if (line.StartsWith("220", StringComparison.Ordinal))
                    return;
            }
            catch (Exception ex) when (ex is SocketException or IOException or OperationCanceledException)
            {
            }

            await Task.Delay(200);
        }

        throw new TimeoutException("FTPS console did not accept a connection.\n" + Log());
    }

    public void Dispose()
    {
        if (_wslKill != null)
        {
            try
            {
                HostProbe.Run("wslc.exe", _wslKill, Encoding.UTF8, TimeSpan.FromSeconds(15));
            }
            catch (Exception ex) when (ex is IOException or InvalidOperationException or TimeoutException or System.ComponentModel.Win32Exception)
            {
            }
        }

        try
        {
            if (!_process.HasExited)
            {
                _process.Kill(entireProcessTree: true);
                _process.WaitForExit(5000);
            }
        }
        catch (InvalidOperationException)
        {
        }
        finally
        {
            _process.Dispose();
        }
    }

    string Log()
    {
        lock (_log)
            return _log.ToString();
    }

}

static class DotNetHost
{
    public static string Path { get; } = Resolve();

    static string Resolve()
    {
        var candidate = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            "dotnet",
            "dotnet.exe");
        return File.Exists(candidate) ? candidate : "dotnet";
    }
}

static class TestCertificate
{
    public const string Password = "test";

    public static byte[] Export()
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest("CN=ftps-fluent-integration", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var san = new SubjectAlternativeNameBuilder();
        san.AddIpAddress(IPAddress.Loopback);
        san.AddDnsName("localhost");
        request.CertificateExtensions.Add(san.Build());
        var created = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(30));
        return created.Export(X509ContentType.Pfx, Password);
    }
}
