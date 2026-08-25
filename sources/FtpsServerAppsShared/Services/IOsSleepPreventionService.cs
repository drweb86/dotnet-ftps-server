namespace FtpsServerAppsShared.Services;

public interface IOsSleepPreventionService
{
    void PreventSleep();
    void StopPreventSleep();
}
