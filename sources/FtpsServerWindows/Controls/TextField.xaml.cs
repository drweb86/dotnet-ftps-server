using FtpsServerWindows.Resources;
using System.Windows;
using System.Windows.Controls;

namespace FtpsServerWindows.Controls;

public partial class TextField : UserControl
{
    public static readonly DependencyProperty LabelProperty =
        DependencyProperty.Register(nameof(Label), typeof(string), typeof(TextField));

    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register(nameof(Text), typeof(string), typeof(TextField),
            new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnTextChanged));

    public static readonly DependencyProperty IsPasswordProperty =
        DependencyProperty.Register(nameof(IsPassword), typeof(bool), typeof(TextField),
            new PropertyMetadata(false, OnPasswordModeChanged));

    public static readonly DependencyProperty ErrorProperty =
        DependencyProperty.Register(nameof(Error), typeof(string), typeof(TextField));

    public static readonly DependencyProperty HelpProperty =
        DependencyProperty.Register(nameof(Help), typeof(string), typeof(TextField));

    private bool _revealed;
    private bool _sync;

    public string? Label { get => (string?)GetValue(LabelProperty); set => SetValue(LabelProperty, value); }
    public string? Text { get => (string?)GetValue(TextProperty); set => SetValue(TextProperty, value); }
    public bool IsPassword { get => (bool)GetValue(IsPasswordProperty); set => SetValue(IsPasswordProperty, value); }
    public string? Error { get => (string?)GetValue(ErrorProperty); set => SetValue(ErrorProperty, value); }
    public string? Help { get => (string?)GetValue(HelpProperty); set => SetValue(HelpProperty, value); }

    public TextField()
    {
        InitializeComponent();
        ApplyPassword();
    }

    private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var field = (TextField)d;
        if (field._sync || field.Secret == null)
            return;

        field._sync = true;
        field.Secret.Password = e.NewValue as string ?? "";
        field._sync = false;
    }

    private static void OnPasswordModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((TextField)d).ApplyPassword();

    private void Reveal_Click(object sender, RoutedEventArgs e)
    {
        _revealed = !_revealed;
        ApplyPassword();
    }

    private void Secret_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (_sync)
            return;

        _sync = true;
        Text = Secret.Password;
        _sync = false;
    }

    private void ApplyPassword()
    {
        if (Reveal == null || Secret == null || Input == null)
            return;

        Reveal.Visibility = IsPassword ? Visibility.Visible : Visibility.Collapsed;
        if (!IsPassword)
        {
            _revealed = false;
            Secret.Visibility = Visibility.Collapsed;
            Input.Visibility = Visibility.Visible;
            return;
        }

        Reveal.Content = _revealed ? Strings.PasswordHide : Strings.PasswordShow;
        if (_revealed)
        {
            Secret.Visibility = Visibility.Collapsed;
            Input.Visibility = Visibility.Visible;
            return;
        }

        _sync = true;
        Secret.Password = Text ?? "";
        _sync = false;
        Input.Visibility = Visibility.Collapsed;
        Secret.Visibility = Visibility.Visible;
    }
}
