using System.Windows;
using System.Windows.Controls;

namespace FtpsServerWindows.Controls;

public partial class CheckboxField : UserControl
{
    public static readonly DependencyProperty LabelProperty =
        DependencyProperty.Register(nameof(Label), typeof(string), typeof(CheckboxField));

    public static readonly DependencyProperty IsCheckedProperty =
        DependencyProperty.Register(nameof(IsChecked), typeof(bool), typeof(CheckboxField),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public static readonly DependencyProperty HelpProperty =
        DependencyProperty.Register(nameof(Help), typeof(string), typeof(CheckboxField));

    public static readonly DependencyProperty ErrorProperty =
        DependencyProperty.Register(nameof(Error), typeof(string), typeof(CheckboxField));

    public string? Label { get => (string?)GetValue(LabelProperty); set => SetValue(LabelProperty, value); }
    public bool IsChecked { get => (bool)GetValue(IsCheckedProperty); set => SetValue(IsCheckedProperty, value); }
    public string? Help { get => (string?)GetValue(HelpProperty); set => SetValue(HelpProperty, value); }
    public string? Error { get => (string?)GetValue(ErrorProperty); set => SetValue(ErrorProperty, value); }

    public CheckboxField()
    {
        InitializeComponent();
    }
}
