using FtpsServerLibrary;

namespace FtpsServerAppsShared.Services;

public sealed class CompositeFtpsServerLog : IFtpsServerLog
{
    private readonly IFtpsServerLog[] _logs;

    public CompositeFtpsServerLog(params IFtpsServerLog[] logs)
    {
        _logs = logs;
    }

    public void Debug(string message)
    {
        foreach (var log in _logs)
            log.Debug(message);
    }

    public void Error(Exception ex, string message)
    {
        foreach (var log in _logs)
            log.Error(ex, message);
    }

    public void Fatal(Exception ex, string message)
    {
        foreach (var log in _logs)
            log.Fatal(ex, message);
    }

    public void Info(string message)
    {
        foreach (var log in _logs)
            log.Info(message);
    }

    public void Warn(string message)
    {
        foreach (var log in _logs)
            log.Warn(message);
    }
}
