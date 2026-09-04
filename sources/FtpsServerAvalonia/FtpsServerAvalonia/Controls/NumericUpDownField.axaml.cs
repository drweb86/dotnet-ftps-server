using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;

namespace FtpsServerAvalonia.Controls;

public partial class NumericUpDownField : UserControl
{
    public static readonly StyledProperty<string?> LabelProperty =
        AvaloniaProperty.Register<NumericUpDownField, string?>(nameof(Label));

    public static readonly StyledProperty<int> ValueProperty =
        AvaloniaProperty.Register<NumericUpDownField, int>(nameof(Value), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<int> MinimumProperty =
        AvaloniaProperty.Register<NumericUpDownField, int>(nameof(Minimum));

    public static readonly StyledProperty<int> MaximumProperty =
        AvaloniaProperty.Register<NumericUpDownField, int>(nameof(Maximum), int.MaxValue);

    public static readonly StyledProperty<string?> HelpProperty =
        AvaloniaProperty.Register<NumericUpDownField, string?>(nameof(Help));

    public static readonly StyledProperty<string?> ErrorProperty =
        AvaloniaProperty.Register<NumericUpDownField, string?>(nameof(Error));

    public string? Label
    {
        get => GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public int Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public int Minimum
    {
        get => GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }

    public int Maximum
    {
        get => GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    public string? Help
    {
        get => GetValue(HelpProperty);
        set => SetValue(HelpProperty, value);
    }

    public string? Error
    {
        get => GetValue(ErrorProperty);
        set => SetValue(ErrorProperty, value);
    }

    public NumericUpDownField()
    {
        InitializeComponent();
    }
}
