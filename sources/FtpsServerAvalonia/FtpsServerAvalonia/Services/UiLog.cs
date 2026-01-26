using Avalonia.Threading;
using FtpsServerLibrary;
using System;
using System.Collections.ObjectModel;

namespace FtpsServerAvalonia.Services;

public class LogEntry
{
    public DateTime Timestamp { get; set; }
    public string Level { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Color { get; set; } = "#FFFFFF";

    public string FormattedMessage => $"[{Timestamp:HH:mm:ss}] {Level}: {Message}";
}

public class UiLog : IFtpsServerLog
{
    private readonly ObservableCollection<LogEntry> _logEntries;
    private const int MaxLogEntries = 500;

    public UiLog(ObservableCollection<LogEntry> logEntries)
    {
        _logEntries = logEntries;
    }

    private void AddEntry(string level, string message, string color)
    {
        Dispatcher.UIThread.Post(() =>
        {
            _logEntries.Add(new LogEntry
            {
                Timestamp = DateTime.Now,
                Level = level,
                Message = message,
                Color = color
            });

            // Keep log size manageable
            while (_logEntries.Count > MaxLogEntries)
            {
                _logEntries.RemoveAt(0);
            }
        });
    }

    public void Debug(string message)
    {
        AddEntry("DEBUG", message, "#808080");
    }

    public void Error(Exception ex, string message)
    {
        AddEntry("ERROR", $"{message}: {ex.Message}", "#FF5252");
    }

    public void Fatal(Exception ex, string message)
    {
        AddEntry("FATAL", $"{message}: {ex.Message}", "#FF1744");
    }

    public void Info(string message)
    {
        AddEntry("INFO", message, "#4FC3F7");
    }

    public void Warn(string message)
    {
        AddEntry("WARN", message, "#FFD54F");
    }
}
