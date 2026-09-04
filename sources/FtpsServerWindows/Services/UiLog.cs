using FtpsServerLibrary;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;

namespace FtpsServerWindows.Services;

public class LogEntry
{
    public DateTime Timestamp { get; set; }
    public string Level { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public Brush Color { get; set; } = Brushes.White;

    public string FormattedMessage => $"[{Timestamp:HH:mm:ss}] {Level}: {Message}";
}

public class UiLog : IFtpsServerLog
{
    private readonly ObservableCollection<LogEntry> _logEntries;
    private const int MaxLogEntries = 500;
    private static readonly Brush DebugBrush = Brushes.Gray;
    private static readonly Brush ErrorBrush = new SolidColorBrush(Color.FromRgb(0xFF, 0x52, 0x52));
    private static readonly Brush FatalBrush = new SolidColorBrush(Color.FromRgb(0xFF, 0x17, 0x44));
    private static readonly Brush InfoBrush = new SolidColorBrush(Color.FromRgb(0x4F, 0xC3, 0xF7));
    private static readonly Brush WarnBrush = new SolidColorBrush(Color.FromRgb(0xFF, 0xD5, 0x4F));

    public UiLog(ObservableCollection<LogEntry> logEntries)
    {
        _logEntries = logEntries;
        ErrorBrush.Freeze();
        FatalBrush.Freeze();
        InfoBrush.Freeze();
        WarnBrush.Freeze();
    }

    private void AddEntry(string level, string message, Brush color)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            _logEntries.Add(new LogEntry
            {
                Timestamp = DateTime.Now,
                Level = level,
                Message = message,
                Color = color
            });

            while (_logEntries.Count > MaxLogEntries)
            {
                _logEntries.RemoveAt(0);
            }
        });
    }

    public void Debug(string message) => AddEntry("DEBUG", message, DebugBrush);

    public void Error(Exception ex, string message) =>
        AddEntry("ERROR", $"{message}: {ex.Message}", ErrorBrush);

    public void Fatal(Exception ex, string message) =>
        AddEntry("FATAL", $"{message}: {ex.Message}", FatalBrush);

    public void Info(string message) => AddEntry("INFO", message, InfoBrush);

    public void Warn(string message) => AddEntry("WARN", message, WarnBrush);
}
