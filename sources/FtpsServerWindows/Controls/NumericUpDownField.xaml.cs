using System.Windows;
using System.Windows.Controls;

namespace FtpsServerWindows.Controls;

public partial class NumericUpDownField : UserControl
{
    public static readonly DependencyProperty LabelProperty =
        DependencyProperty.Register(nameof(Label), typeof(string), typeof(NumericUpDownField));

    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(nameof(Value), typeof(int), typeof(NumericUpDownField),
            new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public static readonly DependencyProperty MinimumProperty =
        DependencyProperty.Register(nameof(Minimum), typeof(int), typeof(NumericUpDownField),
            new PropertyMetadata(0));

    public static readonly DependencyProperty MaximumProperty =
        DependencyProperty.Register(nameof(Maximum), typeof(int), typeof(NumericUpDownField),
            new PropertyMetadata(int.MaxValue));

    public static readonly DependencyProperty HelpProperty =
        DependencyProperty.Register(nameof(Help), typeof(string), typeof(NumericUpDownField));

    public static readonly DependencyProperty ErrorProperty =
        DependencyProperty.Register(nameof(Error), typeof(string), typeof(NumericUpDownField));

    public string? Label { get => (string?)GetValue(LabelProperty); set => SetValue(LabelProperty, value); }
    public int Value { get => (int)GetValue(ValueProperty); set => SetValue(ValueProperty, value); }
    public int Minimum { get => (int)GetValue(MinimumProperty); set => SetValue(MinimumProperty, value); }
    public int Maximum { get => (int)GetValue(MaximumProperty); set => SetValue(MaximumProperty, value); }
    public string? Help { get => (string?)GetValue(HelpProperty); set => SetValue(HelpProperty, value); }
    public string? Error { get => (string?)GetValue(ErrorProperty); set => SetValue(ErrorProperty, value); }

    public NumericUpDownField()
    {
        InitializeComponent();
    }
}
