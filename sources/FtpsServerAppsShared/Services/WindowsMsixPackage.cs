using System.Runtime.InteropServices;

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
            // Load kernel32 only after the Windows check. A DllImport would put a
            // kernel32 P/Invoke into this cross-platform assembly and can fail
            // Linux, Android, and browser builds when the method is linked.
            if (!NativeLibrary.TryLoad("kernel32.dll", out var kernel))
                return false;

            try
            {
                if (!NativeLibrary.TryGetExport(kernel, "GetCurrentPackageFullName", out var export))
                    return false;

                var getName = Marshal.GetDelegateForFunctionPointer<GetCurrentPackageFullNameDelegate>(export);
                var length = 0;
                var result = getName(ref length, IntPtr.Zero);
                return result != AppModelErrorNoPackage;
            }
            finally
            {
                NativeLibrary.Free(kernel);
            }
        }
        catch
        {
            return false;
        }
    }

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    private delegate int GetCurrentPackageFullNameDelegate(ref int packageFullNameLength, IntPtr packageFullName);
}
