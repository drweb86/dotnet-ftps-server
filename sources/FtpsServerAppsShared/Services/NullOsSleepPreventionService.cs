namespace FtpsServerAppsShared.Services;

/// <summary>
/// No-op fallback for platforms without an implemented sleep-prevention mechanism
/// (e.g. macOS, or app heads such as Android that manage wakefulness themselves).
/// </summary>
internal sealed class NullOsSleepPreventionService : IOsSleepPreventionService
{
    public void PreventSleep()
    {
    }

    public void StopPreventSleep()
    {
    }
}
