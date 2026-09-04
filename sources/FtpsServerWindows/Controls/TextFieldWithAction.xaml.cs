using System.Windows;
using System.Windows.Controls;

namespace FtpsServerWindows.Controls;

public partial class TextFieldWithAction : UserControl
{
    public static readonly DependencyProperty LabelProperty =
        DependencyProperty.Register(nameof(Label), typeof(string), typeof(TextFieldWithAction));

    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register(nameof(Text), typeof(string), typeof(TextFieldWithAction),
            new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public static readonly DependencyProperty ActionTextProperty =
        DependencyProperty.Register(nameof(ActionText), typeof(string), typeof(TextFieldWithAction));

    public static readonly DependencyProperty ErrorProperty =
        DependencyProperty.Register(nameof(Error), typeof(string), typeof(TextFieldWithAction));

    public static readonly DependencyProperty HelpProperty =
        DependencyProperty.Register(nameof(Help), typeof(string), typeof(TextFieldWithAction));

    public static readonly DependencyProperty IsReadOnlyProperty =
        DependencyProperty.Register(nameof(IsReadOnly), typeof(bool), typeof(TextFieldWithAction));

    public static readonly RoutedEvent ActionClickEvent =
        EventManager.RegisterRoutedEvent(nameof(ActionClick), RoutingStrategy.Bubble,
            typeof(RoutedEventHandler), typeof(TextFieldWithAction));

    public event RoutedEventHandler ActionClick
    {
        add => AddHandler(ActionClickEvent, value);
        remove => RemoveHandler(ActionClickEvent, value);
    }

    public string? Label { get => (string?)GetValue(LabelProperty); set => SetValue(LabelProperty, value); }
    public string? Text { get => (string?)GetValue(TextProperty); set => SetValue(TextProperty, value); }
    public string? ActionText { get => (string?)GetValue(ActionTextProperty); set => SetValue(ActionTextProperty, value); }
    public string? Error { get => (string?)GetValue(ErrorProperty); set => SetValue(ErrorProperty, value); }
    public string? Help { get => (string?)GetValue(HelpProperty); set => SetValue(HelpProperty, value); }
    public bool IsReadOnly { get => (bool)GetValue(IsReadOnlyProperty); set => SetValue(IsReadOnlyProperty, value); }

    public TextFieldWithAction()
    {
        InitializeComponent();
    }

    private void ActionButton_Click(object sender, RoutedEventArgs e)
    {
        RaiseEvent(new RoutedEventArgs(ActionClickEvent, this));
    }
}
