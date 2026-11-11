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
            await _process.WaitForBannerAsync();
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
    public static async Task<ConsoleProcess> StartAsync(int port, string cert, string writeRoot, string readRoot)
    {
        var published = Path.Combine(Path.GetTempPath(), "ftps-console-linux-x64");
        await PublishAsync(published);
        var scriptPath = Path.Combine(Path.GetDirectoryName(cert)!, "start-ftps.sh");
        var remotePublish = "/tmp/ftps-console-linux";
        var lines = new[]
        {
            "#!/bin/sh",
            "set -eu",
            "rm -rf " + remotePublish,
            "mkdir -p " + remotePublish,
            "cp -a " + Quote(WslPath(published)) + "/. " + remotePublish + "/",
            "chmod +x " + remotePublish + "/ftps-server",
            "echo $$ > /tmp/ftps-it-" + port.ToString(System.Globalization.CultureInfo.InvariantCulture) + ".pid",
            "cd " + remotePublish,
            "exec ./ftps-server " + string.Join(' ', ServerWords(port, WslPath(cert), WslPath(writeRoot), WslPath(readRoot))),
        };
        await File.WriteAllTextAsync(scriptPath, string.Join('\n', lines) + "\n", new UTF8Encoding(false));

        var process = new Process
        {
            StartInfo = new ProcessStartInfo("wsl.exe")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            }
        };
        process.StartInfo.ArgumentList.Add("-u");
        process.StartInfo.ArgumentList.Add("root");
        process.StartInfo.ArgumentList.Add("--");
        process.StartInfo.ArgumentList.Add("bash");
        process.StartInfo.ArgumentList.Add(WslPath(scriptPath));
        var kill = "bash -lc \"kill -9 $(cat /tmp/ftps-it-" + port.ToString(System.Globalization.CultureInfo.InvariantCulture) + ".pid) 2>/dev/null || true\"";
        return new ConsoleProcess(process, port, kill);
    }

    static IEnumerable<string> ServerWords(int port, string cert, string writeRoot, string readRoot)
    {
        yield return "--ip";
        yield return "127.0.0.1";
        yield return "--port";
        yield return port.ToString(System.Globalization.CultureInfo.InvariantCulture);
        yield return "--cert";
        yield return Quote(cert);
        yield return "--certpass";
        yield return Quote(TestCertificate.Password);
        yield return "--user";
        yield return Quote("writer#writer-pass#" + writeRoot + "#RW");
        yield return "--user";
        yield return Quote("reader#reader-pass#" + readRoot + "#R");
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
        process.Start();
        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync().WaitAsync(TimeSpan.FromMinutes(5));
        var stdout = await stdoutTask;
        var stderr = await stderrTask;
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

    public static string WslPath(string windowsPath)
    {
        var full = Path.GetFullPath(windowsPath);
        var drive = char.ToLowerInvariant(full[0]);
        return "/mnt/" + drive + full[2..].Replace('\\', '/');
    }

    static string Quote(string value) => "'" + value.Replace("'", "'\\''") + "'";
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

    public async Task WaitForBannerAsync()
    {
        var deadline = DateTime.UtcNow.AddSeconds(40);
        while (DateTime.UtcNow < deadline)
        {
            if (_process.HasExited)
                throw new InvalidOperationException("FTPS console exited " + _process.ExitCode + ".\n" + Log());
            try
            {
                using var tcp = new TcpClient();
                await tcp.ConnectAsync("127.0.0.1", _port);
                var buffer = new byte[128];
                var read = await tcp.GetStream().ReadAsync(buffer);
                var line = Encoding.ASCII.GetString(buffer, 0, read);
                if (line.StartsWith("220", StringComparison.Ordinal))
                    return;
            }
            catch (SocketException)
            {
            }
            catch (IOException)
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
                HostProbe.Run("wsl.exe", "-u root -- " + _wslKill, Encoding.UTF8, TimeSpan.FromSeconds(10));
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
