using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace FtpsServerWindows.Controls;

public partial class ExpanderHeader : UserControl
{
    public static readonly DependencyProperty IconProperty =
        DependencyProperty.Register(nameof(Icon), typeof(string), typeof(ExpanderHeader),
            new PropertyMetadata(null, OnChanged));

    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register(nameof(Text), typeof(string), typeof(ExpanderHeader),
            new PropertyMetadata(null, OnChanged));

    public string? Icon
    {
        get => (string?)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public string? Text
    {
        get => (string?)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public ExpanderHeader()
    {
        InitializeComponent();
        Update();
    }

    private static void OnChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((ExpanderHeader)d).Update();

    private void Update()
    {
        if (IconText != null)
        {
            IconText.Text = Icon;
            IconText.Foreground = GlyphColor.For(Icon);
        }

        if (TitleText != null)
            TitleText.Text = Text;
    }

    private static class GlyphColor
    {
        private static readonly SolidColorBrush Clipboard = Freeze(125, 211, 252);
        private static readonly SolidColorBrush Lock = Freeze(251, 191, 36);
        private static readonly SolidColorBrush User = Freeze(147, 197, 253);
        private static readonly SolidColorBrush Logs = Freeze(252, 211, 77);
        private static readonly SolidColorBrush Fallback = Freeze(243, 244, 246);

        public static Brush For(string? icon) => icon switch
        {
            "📋" => Clipboard,
            "🔒" => Lock,
            "👨" or "👤" or "👥" => User,
            "📜" => Logs,
            _ => Fallback,
        };

        private static SolidColorBrush Freeze(byte r, byte g, byte b)
        {
            var brush = new SolidColorBrush(Color.FromRgb(r, g, b));
            brush.Freeze();
            return brush;
        }
    }
}
