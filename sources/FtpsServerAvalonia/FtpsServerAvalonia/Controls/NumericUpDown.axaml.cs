using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Text.RegularExpressions;

namespace FtpsServerAvalonia.Controls
{
    public partial class NumericUpDown : UserControl
    {
        public static readonly StyledProperty<int> ValueProperty =
            AvaloniaProperty.Register<NumericUpDown, int>(nameof(Value), defaultValue: 0, defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

        public static readonly StyledProperty<int> MinimumProperty =
            AvaloniaProperty.Register<NumericUpDown, int>(nameof(Minimum), defaultValue: 0);

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

        public NumericUpDown()
        {
            InitializeComponent();
            ValueTextBox.Text = Value.ToString();
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == ValueProperty)
            {
                ValueTextBox.Text = change.GetNewValue<int>().ToString();
            }
        }

        private void ValueTextBox_TextChanged(object? sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ValueTextBox.Text))
            {
                Value = Minimum;
                return;
            }

            if (int.TryParse(ValueTextBox.Text, out int newValue))
            {
                if (newValue < Minimum)
                    Value = Minimum;
                else
                    Value = newValue;
            }
        }

        private void UpButton_Click(object? sender, RoutedEventArgs e)
        {
             Value++;
        }

        private void DownButton_Click(object? sender, RoutedEventArgs e)
        {
            if (Value > Minimum)
                Value--;
        }

        private static bool IsTextNumeric(string text)
        {
            return Regex.IsMatch(text, "^[0-9]+$");
        }
    }
}
