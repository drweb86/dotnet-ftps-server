namespace FtpsServerAppsShared.Services;

public static class OsSleepPreventionServiceFactory
{
    public static IOsSleepPreventionService Create()
    {
        if (OperatingSystem.IsWindows())
            return new WindowsOsSleepPreventionService();

        if (OperatingSystem.IsLinux())
            return new LinuxOsSleepPreventionService();

        return new NullOsSleepPreventionService();
    }
}
