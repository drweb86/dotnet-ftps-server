using Avalonia;
using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Threading;
using FtpsServerAvalonia.Resources;
using System;
using System.Threading.Tasks;

namespace FtpsServerAvalonia.Controls;

public partial class ConnectionInstructionControl : UserControl
{
    public static readonly StyledProperty<string> InstructionTextProperty =
        AvaloniaProperty.Register<ConnectionInstructionControl, string>(nameof(InstructionText), defaultValue: string.Empty);

    public static readonly StyledProperty<bool> ShowTitleProperty =
        AvaloniaProperty.Register<ConnectionInstructionControl, bool>(nameof(ShowTitle), defaultValue: true);

    public static readonly StyledProperty<bool> ShowShareButtonProperty =
        AvaloniaProperty.Register<ConnectionInstructionControl, bool>(nameof(ShowShareButton), defaultValue: false);

    public string InstructionText
    {
        get => GetValue(InstructionTextProperty);
        set => SetValue(InstructionTextProperty, value);
    }

    public bool ShowTitle
    {
        get => GetValue(ShowTitleProperty);
        set => SetValue(ShowTitleProperty, value);
    }

    public bool ShowShareButton
    {
        get => GetValue(ShowShareButtonProperty);
        set => SetValue(ShowShareButtonProperty, value);
    }

    public ConnectionInstructionControl()
    {
        InitializeComponent();
        ShareConnectionDetailsButton.IsVisible = ShowShareButton;
        TitleText.IsVisible = ShowTitle;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == InstructionTextProperty && InstructionTextBlock != null)
            InstructionTextBlock.Text = change.GetNewValue<string>();
        else if (change.Property == ShowTitleProperty && TitleText != null)
        {
            var showTitle = change.GetNewValue<bool>();
            TitleText.IsVisible = showTitle;
            if (!showTitle)
            {
                OuterBorder.Margin = new Thickness(0);
                OuterBorder.BorderThickness = new Thickness(0);
                OuterBorder.Padding = new Thickness(0);
            }
        }
        else if (change.Property == ShowShareButtonProperty && ShareConnectionDetailsButton != null)
            ShareConnectionDetailsButton.IsVisible = change.GetNewValue<bool>();
    }

    private async void CopyConnectionDetails_Click(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(InstructionText))
            return;

        await (TopLevel.GetTopLevel(this)?.Clipboard?.SetTextAsync(InstructionText) ?? Task.CompletedTask);
        CopyConnectionDetailsButton.Content = Strings.CertCopied;
        DispatcherTimer.RunOnce(
            () => CopyConnectionDetailsButton.Content = Strings.ConfigCopyConnectionDetails,
            TimeSpan.FromSeconds(2));
    }

    private void ShareConnectionDetails_Click(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(InstructionText))
            return;

        App.AndroidShareService?.ShareText(Strings.ConnectionDetailsShareSubject, InstructionText);
    }
}
