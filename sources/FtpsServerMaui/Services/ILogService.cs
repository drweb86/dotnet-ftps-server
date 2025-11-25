namespace FtpsServerMaui.Services;

public interface ILogService
{
    event EventHandler<string>? LogMessageReceived;
    void Debug(string message);
    void Info(string message);
    void Warn(string message);
    void Error(Exception ex, string message);
    void Fatal(Exception ex, string message);
    void Clear();
    List<string> GetRecentLogs(int count = 100);
}
