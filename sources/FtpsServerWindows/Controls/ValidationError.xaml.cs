using System.Windows;
using System.Windows.Controls;

namespace FtpsServerWindows.Controls;

public partial class ValidationError : UserControl
{
    public static readonly DependencyProperty ErrorProperty =
        DependencyProperty.Register(nameof(Error), typeof(string), typeof(ValidationError));

    public string? Error
    {
        get => (string?)GetValue(ErrorProperty);
        set => SetValue(ErrorProperty, value);
    }

    public ValidationError()
    {
        InitializeComponent();
    }
}
