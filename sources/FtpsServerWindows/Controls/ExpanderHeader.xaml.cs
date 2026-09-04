using System.Windows;
using System.Windows.Controls;

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
            IconText.Text = Icon;
        if (TitleText != null)
            TitleText.Text = Text;
    }
}
