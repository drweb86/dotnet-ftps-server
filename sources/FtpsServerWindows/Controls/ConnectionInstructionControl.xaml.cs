using FtpsServerWindows.Resources;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace FtpsServerWindows.Controls;

public partial class ConnectionInstructionControl : UserControl
{
    public static readonly DependencyProperty InstructionTextProperty =
        DependencyProperty.Register(nameof(InstructionText), typeof(string), typeof(ConnectionInstructionControl),
            new PropertyMetadata(string.Empty, OnInstructionTextChanged));

    public string InstructionText
    {
        get => (string)GetValue(InstructionTextProperty);
        set => SetValue(InstructionTextProperty, value);
    }

    public ConnectionInstructionControl()
    {
        InitializeComponent();
    }

    private static void OnInstructionTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (ConnectionInstructionControl)d;
        if (control.InstructionTextBox != null)
            control.InstructionTextBox.Text = e.NewValue as string ?? string.Empty;
    }

    private void CopyConnectionDetails_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(InstructionText))
            return;

        Clipboard.SetText(InstructionText);
        CopyConnectionDetailsButton.Content = Strings.CertCopied;
        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            CopyConnectionDetailsButton.Content = Strings.ConfigCopyConnectionDetails;
        };
        timer.Start();
    }
}
