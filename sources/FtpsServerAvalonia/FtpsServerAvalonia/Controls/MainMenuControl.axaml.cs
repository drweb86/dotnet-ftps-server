using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using FtpsServerAppsShared.Services;
using FtpsServerAvalonia.Resources;
using System;

namespace FtpsServerAvalonia.Controls
{
    public partial class MainMenuControl : UserControl
    {
        public static readonly RoutedEvent<RoutedEventArgs> StartStopClickedEvent =
            RoutedEvent.Register<MainMenuControl, RoutedEventArgs>(nameof(StartStopClicked), RoutingStrategies.Bubble);

        public event EventHandler<RoutedEventArgs> StartStopClicked
        {
            add => AddHandler(StartStopClickedEvent, value);
            remove => RemoveHandler(StartStopClickedEvent, value);
        }

        public MainMenuControl()
        {
            InitializeComponent();

            // Set menu item header with version
            var version = CopyrightInfo.Version;
            AboutMenuItem.Header = string.Format(Strings.MenuAboutFormat, CopyrightInfo.Version.ToString(3));
        }

        private void StartStopButton_Click(object? sender, RoutedEventArgs e)
        {
            RaiseEvent(new RoutedEventArgs(StartStopClickedEvent));
        }

        public void UpdateServerStatus(bool isRunning)
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (isRunning)
                {
                    PlayIcon.IsVisible = false;
                    StopIcon.IsVisible = true;
                }
                else
                {
                    PlayIcon.IsVisible = true;
                    StopIcon.IsVisible = false;
                }
            });
        }
    }
}
