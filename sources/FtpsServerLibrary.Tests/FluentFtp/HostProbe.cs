using System.ComponentModel;
using System.Diagnostics;
using System.Text;

namespace FtpsServerLibrary.Tests.FluentFtp;

static class HostProbe
{
    static readonly Lazy<bool> WslDistro = new(HasWslDistro);
    static readonly Lazy<bool> AndroidEmulator = new(HasAndroidEmulator);

    public static bool HasWsl => WslDistro.Value;
    public static bool HasAndroid => AndroidEmulator.Value;

    public static bool WslResponds => HasWsl;

    public static string? AdbPath { get; private set; }
    public static string? EmulatorSerial { get; private set; }

    static bool HasWslDistro()
    {
        var wslc = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "WSL", "wslc.exe");
        if (!File.Exists(wslc))
            return false;
        try
        {
            Run(wslc, "info", Encoding.UTF8, TimeSpan.FromSeconds(20));
            return true;
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException or Win32Exception or TimeoutException)
        {
            return false;
        }
    }

    static bool HasAndroidEmulator()
    {
        AdbPath = FindAdb();
        if (AdbPath == null)
            return false;

        try
        {
            var output = Run(AdbPath, "devices", Encoding.ASCII, TimeSpan.FromSeconds(15));
            foreach (var line in output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = line.Split((char[])[' ', '\t'], StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2 && parts[0].StartsWith("emulator-", StringComparison.Ordinal) && parts[1] == "device")
                {
                    EmulatorSerial = parts[0];
                    return true;
                }
            }
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException or Win32Exception)
        {
        }

        return false;
    }

    public static string? FindAdb()
    {
        var roots = new[]
        {
            Environment.GetEnvironmentVariable("ANDROID_HOME"),
            Environment.GetEnvironmentVariable("ANDROID_SDK_ROOT"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FtpsServerAndroidSdk"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Android", "Sdk"),
        };
        foreach (var root in roots)
        {
            if (string.IsNullOrWhiteSpace(root))
                continue;
            var adb = Path.Combine(root, "platform-tools", "adb.exe");
            if (File.Exists(adb))
                return adb;
        }

        return null;
    }

    public static string Run(string file, string arguments, Encoding encoding, TimeSpan timeout)
    {
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo(file, arguments)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = encoding,
            StandardErrorEncoding = encoding,
        };
        process.Start();
        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit((int)timeout.TotalMilliseconds))
        {
            try { process.Kill(entireProcessTree: true); } catch { /* already gone */ }
            throw new TimeoutException(file + " " + arguments);
        }

        var stdout = stdoutTask.GetAwaiter().GetResult();
        var stderr = stderrTask.GetAwaiter().GetResult();

        if (process.ExitCode != 0 && stdout.Length == 0)
            throw new InvalidOperationException(stderr.Length == 0 ? file + " exited " + process.ExitCode : stderr);
        return stdout + stderr;
    }
}

sealed class WslFactAttribute : FactAttribute
{
    public WslFactAttribute()
    {
        Timeout = 300000;
        if (!HostProbe.HasWsl)
            Skip = "wslc.exe is not available. Linux tests run with: wslc run --rm ubuntu:latest";
    }
}

sealed class AndroidEmulatorFactAttribute : FactAttribute
{
    public AndroidEmulatorFactAttribute()
    {
        Timeout = 300000;
        if (!HostProbe.HasAndroid)
            Skip = "No Android emulator is online in adb devices.";
    }
}
