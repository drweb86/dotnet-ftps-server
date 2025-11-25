using FtpsServerLibrary;
using System.Collections.Concurrent;

namespace FtpsServerMaui.Services;

public class LogService : ILogService, IFtpsServerLog
{
    private readonly ConcurrentQueue<string> _logMessages = new();
    private const int MaxLogMessages = 1000;

    public event EventHandler<string>? LogMessageReceived;

    private void AddLog(string level, string message)
    {
        var logEntry = $"[{DateTime.Now:HH:mm:ss}] [{level}] {message}";
        _logMessages.Enqueue(logEntry);

        while (_logMessages.Count > MaxLogMessages)
        {
            _logMessages.TryDequeue(out _);
        }

        LogMessageReceived?.Invoke(this, logEntry);
    }

    public void Debug(string message) => AddLog("DEBUG", message);
    public void Info(string message) => AddLog("INFO", message);
    public void Warn(string message) => AddLog("WARN", message);
    public void Error(Exception ex, string message) => AddLog("ERROR", $"{message}: {ex.Message}");
    public void Fatal(Exception ex, string message) => AddLog("FATAL", $"{message}: {ex.Message}");

    public void Clear()
    {
        _logMessages.Clear();
    }

    public List<string> GetRecentLogs(int count = 100)
    {
        return _logMessages.TakeLast(count).ToList();
    }
}
