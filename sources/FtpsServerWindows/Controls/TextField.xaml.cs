using System.Windows;
using System.Windows.Controls;

namespace FtpsServerWindows.Controls;

public partial class TextField : UserControl
{
    public static readonly DependencyProperty LabelProperty =
        DependencyProperty.Register(nameof(Label), typeof(string), typeof(TextField));

    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register(nameof(Text), typeof(string), typeof(TextField),
            new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public static readonly DependencyProperty ErrorProperty =
        DependencyProperty.Register(nameof(Error), typeof(string), typeof(TextField));

    public static readonly DependencyProperty HelpProperty =
        DependencyProperty.Register(nameof(Help), typeof(string), typeof(TextField));

    public string? Label { get => (string?)GetValue(LabelProperty); set => SetValue(LabelProperty, value); }
    public string? Text { get => (string?)GetValue(TextProperty); set => SetValue(TextProperty, value); }
    public string? Error { get => (string?)GetValue(ErrorProperty); set => SetValue(ErrorProperty, value); }
    public string? Help { get => (string?)GetValue(HelpProperty); set => SetValue(HelpProperty, value); }

    public TextField()
    {
        InitializeComponent();
    }
}
