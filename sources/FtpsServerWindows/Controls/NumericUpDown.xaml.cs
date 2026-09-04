using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace FtpsServerWindows.Controls
{
    public partial class NumericUpDown : UserControl
    {
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(nameof(Value), typeof(int), typeof(NumericUpDown),
                new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnValueChanged));

        public static readonly DependencyProperty MinimumProperty =
            DependencyProperty.Register(nameof(Minimum), typeof(int), typeof(NumericUpDown),
                new PropertyMetadata(0));

        public static readonly DependencyProperty MaximumProperty =
            DependencyProperty.Register(nameof(Maximum), typeof(int), typeof(NumericUpDown),
                new PropertyMetadata(int.MaxValue));
        
        public int Value
        {
            get => (int)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        public int Minimum
        {
            get => (int)GetValue(MinimumProperty);
            set => SetValue(MinimumProperty, value);
        }

        public int Maximum
        {
            get => (int)GetValue(MaximumProperty);
            set => SetValue(MaximumProperty, value);
        }

        public NumericUpDown()
        {
            InitializeComponent();
            ValueTextBox.Text = Value.ToString();
        }

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (NumericUpDown)d;
            control.ValueTextBox.Text = e.NewValue.ToString();
        }

        private void ValueTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsTextNumeric(e.Text);
        }

        private void ValueTextBox_TextChanged(object sender, TextChangedEventArgs e)
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
                else if (newValue > Maximum)
                    Value = Maximum;
                else
                    Value = newValue;
            }
        }

        private void UpButton_Click(object sender, RoutedEventArgs e)
        {
            if (Value < Maximum)
                Value++;
        }

        private void DownButton_Click(object sender, RoutedEventArgs e)
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
