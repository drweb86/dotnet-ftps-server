using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using System;

namespace FtpsServerAvalonia.Controls;

public partial class TextFieldWithAction : UserControl
{
    public static readonly StyledProperty<string?> LabelProperty =
        AvaloniaProperty.Register<TextFieldWithAction, string?>(nameof(Label));

    public static readonly StyledProperty<string?> TextProperty =
        AvaloniaProperty.Register<TextFieldWithAction, string?>(nameof(Text), defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public static readonly StyledProperty<string?> PlaceholderProperty =
        AvaloniaProperty.Register<TextFieldWithAction, string?>(nameof(Placeholder));

    public static readonly StyledProperty<string?> ActionTextProperty =
        AvaloniaProperty.Register<TextFieldWithAction, string?>(nameof(ActionText));

    public static readonly StyledProperty<string?> ErrorProperty =
        AvaloniaProperty.Register<TextFieldWithAction, string?>(nameof(Error));

    public static readonly StyledProperty<string?> HelpProperty =
        AvaloniaProperty.Register<TextFieldWithAction, string?>(nameof(Help));

    public static readonly StyledProperty<bool> IsReadOnlyProperty =
        AvaloniaProperty.Register<TextFieldWithAction, bool>(nameof(IsReadOnly));

    public static readonly RoutedEvent<RoutedEventArgs> ActionClickEvent =
        RoutedEvent.Register<TextFieldWithAction, RoutedEventArgs>(nameof(ActionClick), RoutingStrategies.Bubble);

    public event EventHandler<RoutedEventArgs> ActionClick
    {
        add => AddHandler(ActionClickEvent, value);
        remove => RemoveHandler(ActionClickEvent, value);
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

    public string? ActionText
    {
        get => GetValue(ActionTextProperty);
        set => SetValue(ActionTextProperty, value);
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

    public bool IsReadOnly
    {
        get => GetValue(IsReadOnlyProperty);
        set => SetValue(IsReadOnlyProperty, value);
    }

    public TextFieldWithAction()
    {
        InitializeComponent();
    }

    private void ActionButton_Click(object? sender, RoutedEventArgs e)
    {
        RaiseEvent(new RoutedEventArgs(ActionClickEvent, this));
    }
}
