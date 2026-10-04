using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using FtpsServerAvalonia.Resources;

namespace FtpsServerAvalonia.Controls;

public partial class TextField : UserControl
{
    public static readonly StyledProperty<string?> LabelProperty =
        AvaloniaProperty.Register<TextField, string?>(nameof(Label));

    public static readonly StyledProperty<string?> TextProperty =
        AvaloniaProperty.Register<TextField, string?>(nameof(Text), defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public static readonly StyledProperty<string?> PlaceholderProperty =
        AvaloniaProperty.Register<TextField, string?>(nameof(Placeholder));

    public static readonly StyledProperty<char> PasswordCharProperty =
        AvaloniaProperty.Register<TextField, char>(nameof(PasswordChar));

    public static readonly StyledProperty<bool> IsPasswordProperty =
        AvaloniaProperty.Register<TextField, bool>(nameof(IsPassword));

    public static readonly StyledProperty<string?> RevealTextProperty =
        AvaloniaProperty.Register<TextField, string?>(nameof(RevealText));

    public static readonly StyledProperty<string?> ErrorProperty =
        AvaloniaProperty.Register<TextField, string?>(nameof(Error));

    public static readonly StyledProperty<string?> HelpProperty =
        AvaloniaProperty.Register<TextField, string?>(nameof(Help));

    private bool _revealed;

    static TextField()
    {
        IsPasswordProperty.Changed.AddClassHandler<TextField>((field, _) => field.UpdateMask());
    }

    public string? Label
    {
        get => GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public string? Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public string? Placeholder
    {
        get => GetValue(PlaceholderProperty);
        set => SetValue(PlaceholderProperty, value);
    }

    public char PasswordChar
    {
        get => GetValue(PasswordCharProperty);
        set => SetValue(PasswordCharProperty, value);
    }

    public bool IsPassword
    {
        get => GetValue(IsPasswordProperty);
        set => SetValue(IsPasswordProperty, value);
    }

    public string? RevealText
    {
        get => GetValue(RevealTextProperty);
        set => SetValue(RevealTextProperty, value);
    }

    public string? Error
    {
        get => GetValue(ErrorProperty);
        set => SetValue(ErrorProperty, value);
    }

    public string? Help
    {
        get => GetValue(HelpProperty);
        set => SetValue(HelpProperty, value);
    }

    public TextField()
    {
        InitializeComponent();
        UpdateMask();
    }

    private void Reveal_Click(object? sender, RoutedEventArgs e)
    {
        _revealed = !_revealed;
        UpdateMask();
    }

    private void UpdateMask()
    {
        if (Input == null)
            return;

        PasswordChar = IsPassword && !_revealed ? '●' : '\0';
        RevealText = _revealed ? Strings.PasswordHide : Strings.PasswordShow;
        Input.Margin = IsPassword ? new Thickness(0, 0, 8, 0) : new Thickness(0);
    }
}
