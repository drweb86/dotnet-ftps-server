using FtpsServerLibrary;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

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
    // The panel is only a few hundred pixels tall. A short tail is enough to read,
    // and a longer list means more UI work while files are being copied.
    private const int MaxLogEntries = 100;
    private const int CollapsedLogEntries = 2;

    private readonly ObservableCollection<LogEntry> _logEntries;
    private readonly List<LogEntry> _retained = new(MaxLogEntries);
    private readonly object _gate = new();
    private bool _showInUi;
    private bool _flushPosted;
    private static readonly Brush DebugBrush = Brushes.Gray;
    private static readonly Brush ErrorBrush = new SolidColorBrush(Color.FromRgb(0xFF, 0x52, 0x52));
    private static readonly Brush FatalBrush = new SolidColorBrush(Color.FromRgb(0xFF, 0x17, 0x44));
    private static readonly Brush InfoBrush = new SolidColorBrush(Color.FromRgb(0x4F, 0xC3, 0xF7));
    private static readonly Brush WarnBrush = new SolidColorBrush(Color.FromRgb(0xFF, 0xD5, 0x4F));

    public event EventHandler? ViewUpdated;

    public UiLog(ObservableCollection<LogEntry> logEntries)
    {
        _logEntries = logEntries;
        ErrorBrush.Freeze();
        FatalBrush.Freeze();
        InfoBrush.Freeze();
        WarnBrush.Freeze();
    }

    public bool ShowInUi
    {
        set
        {
            lock (_gate)
            {
                if (_showInUi == value)
                    return;
                _showInUi = value;
                if (!_showInUi)
                    TrimRetained(CollapsedLogEntries);
                _flushPosted = true;
            }

            PostFlush();
        }
    }

    public void Clear()
    {
        lock (_gate)
        {
            _retained.Clear();
            _flushPosted = true;
        }

        PostFlush();
    }

    private void AddEntry(string level, string message, Brush color)
    {
        var entry = new LogEntry
        {
            Timestamp = DateTime.Now,
            Level = level,
            Message = message,
            Color = color
        };

        lock (_gate)
        {
            _retained.Add(entry);
            TrimRetained(_showInUi ? MaxLogEntries : CollapsedLogEntries);
            if (!_showInUi || _flushPosted)
                return;
            _flushPosted = true;
        }

        PostFlush();
    }

    private void PostFlush()
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher == null)
            return;
        if (dispatcher.CheckAccess())
            Flush();
        else
            dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(Flush));
    }

    private void Flush()
    {
        LogEntry[] snapshot;
        bool show;
        lock (_gate)
        {
            _flushPosted = false;
            show = _showInUi;
            snapshot = show ? _retained.ToArray() : [];
        }

        if (!show)
        {
            if (_logEntries.Count > 0)
                _logEntries.Clear();
            return;
        }

        ApplySnapshot(snapshot);
        ViewUpdated?.Invoke(this, EventArgs.Empty);
    }

    private void TrimRetained(int limit)
    {
        while (_retained.Count > limit)
            _retained.RemoveAt(0);
    }

    private void ApplySnapshot(LogEntry[] snapshot)
    {
        if (snapshot.Length == 0)
        {
            if (_logEntries.Count > 0)
                _logEntries.Clear();
            return;
        }

        while (_logEntries.Count > 0 && !ReferenceEquals(_logEntries[0], snapshot[0]))
            _logEntries.RemoveAt(0);

        if (_logEntries.Count > snapshot.Length)
            _logEntries.Clear();

        for (var i = 0; i < _logEntries.Count; i++)
        {
            if (!ReferenceEquals(_logEntries[i], snapshot[i]))
            {
                _logEntries.Clear();
                break;
            }
        }

        for (var i = _logEntries.Count; i < snapshot.Length; i++)
            _logEntries.Add(snapshot[i]);
    }

    public void Debug(string message) => AddEntry("DEBUG", message, DebugBrush);

    public void Error(Exception ex, string message) =>
        AddEntry("ERROR", $"{message}: {ex.Message}", ErrorBrush);

    public void Fatal(Exception ex, string message) =>
        AddEntry("FATAL", $"{message}: {ex.Message}", FatalBrush);

    public void Info(string message) => AddEntry("INFO", message, InfoBrush);

    public void Warn(string message) => AddEntry("WARN", message, WarnBrush);
}
