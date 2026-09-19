using System.Runtime.InteropServices;
using System.Text;

namespace FtpsServerAppsShared.Services;

public static class WindowsMsixPackage
{
    private const int AppModelErrorNoPackage = 15700;

    /// <summary>
    /// True when this process is running inside an MSIX package (Microsoft Store or sideloaded).
    /// Store installs are updated by the Store, not by the in-app GitHub checker.
    /// </summary>
    public static bool IsCurrentProcessPackaged { get; } = Detect();

    private static bool Detect()
    {
        if (!OperatingSystem.IsWindows())
            return false;

        try
        {
            var length = 0;
            var result = GetCurrentPackageFullName(ref length, null);
            return result != AppModelErrorNoPackage;
        }
        catch
        {
            return false;
        }
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
    private static extern int GetCurrentPackageFullName(
        ref int packageFullNameLength,
        StringBuilder? packageFullName);
}
