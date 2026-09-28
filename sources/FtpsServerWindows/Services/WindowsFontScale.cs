using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Documents;
using System.Windows.Interop;
using System.Windows.Threading;
using Microsoft.Win32;

namespace FtpsServerWindows.Services;

/// <summary>
/// Follows Windows text size and per-monitor display scale.
/// WPF does not apply Settings → Accessibility → Text size, and formatted text can
/// stay at the previous monitor's scale until a font property changes.
/// </summary>
internal static class WindowsFontScale
{
    internal const string UiKey = "AppFontSize";
    internal const string DocumentKey = "AppDocFontSize";
    internal const string Heading1Key = "AppHeading1FontSize";
    internal const string Heading2Key = "AppHeading2FontSize";
    internal const string Heading3Key = "AppHeading3FontSize";

    private const int WmSettingChange = 0x001A;
    private const double DesignFontSize = 12;

    private static readonly object HookedMarker = new();
    private static readonly ConditionalWeakTable<HwndSource, object> HookedSources = new();
    private static int _refreshQueued;
    private static int _nudgeQueued;
    private static double _unit = 1;

    internal static string HeadingKey(int level) => level switch
    {
        1 => Heading1Key,
        2 => Heading2Key,
        _ => Heading3Key,
    };

    internal static void Bind(FrameworkElement element, string resourceKey) =>
        element.SetResourceReference(TextElement.FontSizeProperty, resourceKey);

    internal static void Start()
    {
        Apply(nudge: false);
        SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
        SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
        EventManager.RegisterClassHandler(typeof(FrameworkElement), FrameworkElement.LoadedEvent, new RoutedEventHandler(OnElementLoaded));
    }

    internal static void Stop()
    {
        SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
        SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
    }

    private static void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e) =>
        QueueRefresh(nudge: false);

    private static void OnDisplaySettingsChanged(object? sender, EventArgs e) =>
        QueueRefresh(nudge: true);

    private static void OnElementLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement element || !ReferenceEquals(e.OriginalSource, element))
            return;

        if (element is Window window)
        {
            // A DynamicResource setter on the implicit Window style does not apply FontSize.
            window.SetResourceReference(TextElement.FontSizeProperty, UiKey);
            window.DpiChanged -= OnWindowDpiChanged;
            window.DpiChanged += OnWindowDpiChanged;
            if (PresentationSource.FromVisual(window) is HwndSource source && HookedSources.TryAdd(source, HookedMarker))
                source.AddHook(WndProc);

            PinThemedFonts(window);
            return;
        }

        if (element is MenuItem or ContextMenu or ToolTip)
            element.Dispatcher.BeginInvoke(() => PinIfThemed(element), DispatcherPriority.ContextIdle);
    }

    private static void PinThemedFonts(DependencyObject root)
    {
        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is FrameworkElement element)
                PinIfThemed(element);
            PinThemedFonts(child);
        }
    }

    private static void PinIfThemed(FrameworkElement element)
    {
        var valueSource = DependencyPropertyHelper.GetValueSource(element, TextElement.FontSizeProperty);
        if (valueSource.BaseValueSource is BaseValueSource.DefaultStyle or BaseValueSource.DefaultStyleTrigger
            or BaseValueSource.Style or BaseValueSource.StyleTrigger)
            element.SetResourceReference(TextElement.FontSizeProperty, UiKey);
    }

    private static void OnWindowDpiChanged(object sender, DpiChangedEventArgs e)
    {
        if (Math.Abs(e.OldDpi.DpiScaleX - e.NewDpi.DpiScaleX) < 0.001 &&
            Math.Abs(e.OldDpi.DpiScaleY - e.NewDpi.DpiScaleY) < 0.001)
            return;

        QueueRefresh(nudge: true);
    }

    private static IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WmSettingChange)
            QueueRefresh(nudge: false);
        return IntPtr.Zero;
    }

    private static void QueueRefresh(bool nudge)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher == null)
            return;

        if (nudge)
            Interlocked.Exchange(ref _nudgeQueued, 1);

        if (Interlocked.Exchange(ref _refreshQueued, 1) == 1)
            return;

        dispatcher.BeginInvoke(FlushRefresh, DispatcherPriority.ApplicationIdle);
    }

    private static void FlushRefresh()
    {
        var nudge = Interlocked.Exchange(ref _nudgeQueued, 0) == 1;
        Interlocked.Exchange(ref _refreshQueued, 0);
        Apply(nudge);
    }

    private static void Apply(bool nudge)
    {
        var app = Application.Current;
        if (app == null)
            return;

        var textScale = ReadTextScale();
        var unit = SystemFonts.MessageFontSize / DesignFontSize * textScale;
        if (!nudge && Math.Abs(unit - _unit) < 0.0001)
            return;

        _unit = unit;
        Set(app, UiKey, DesignFontSize * unit, nudge);
        Set(app, DocumentKey, 14 * unit, nudge);
        Set(app, Heading1Key, 22 * unit, nudge);
        Set(app, Heading2Key, 18 * unit, nudge);
        Set(app, Heading3Key, 16 * unit, nudge);
    }

    private static void Set(Application app, string key, double size, bool nudge)
    {
        if (nudge)
            app.Resources[key] = size + 0.01;
        app.Resources[key] = size;
    }

    private static double ReadTextScale()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Accessibility");
            var percent = key?.GetValue("TextScaleFactor") switch
            {
                int value => value,
                long value => (int)value,
                _ => 100,
            };

            if (percent < 100)
                percent = 100;
            else if (percent > 300)
                percent = 300;

            return percent / 100d;
        }
        catch (Exception ex) when (ex is System.IO.IOException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            return 1;
        }
    }
}
