using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace FtpsServerWindows.Controls;

public partial class MessageBar : UserControl
{
    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register(nameof(Text), typeof(string), typeof(MessageBar),
            new PropertyMetadata(null, OnTextChanged));

    public static readonly DependencyProperty KindProperty =
        DependencyProperty.Register(nameof(Kind), typeof(MessageBarKind), typeof(MessageBar),
            new PropertyMetadata(MessageBarKind.Error, OnKindChanged));

    public string? Text
    {
        get => (string?)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public MessageBarKind Kind
    {
        get => (MessageBarKind)GetValue(KindProperty);
        set => SetValue(KindProperty, value);
    }

    public MessageBar()
    {
        InitializeComponent();
        UpdateKind();
        UpdateVisibility();
    }

    private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((MessageBar)d).UpdateVisibility();

    private void UpdateVisibility()
        => Visibility = string.IsNullOrEmpty(Text) ? Visibility.Collapsed : Visibility.Visible;

    private static void OnKindChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((MessageBar)d).UpdateKind();

    private void UpdateKind()
    {
        if (BarBorder == null)
            return;
        BarBorder.Background = Kind == MessageBarKind.Success
            ? (Brush)FindResource("SuccessBrush")
            : (Brush)FindResource("ErrorBrush");
    }
}
