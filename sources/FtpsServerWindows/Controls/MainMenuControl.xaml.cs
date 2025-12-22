using FtpsServerAppsShared.Services;
using System.Windows;
using System.Windows.Controls;

namespace FtpsServerWindows
{
    public partial class MainMenuControl : UserControl
    {
        public static readonly RoutedEvent StartStopClickedEvent =
            EventManager.RegisterRoutedEvent(nameof(StartStopClicked), RoutingStrategy.Bubble,
                typeof(RoutedEventHandler), typeof(MainMenuControl));

        public event RoutedEventHandler StartStopClicked
        {
            add { AddHandler(StartStopClickedEvent, value); }
            remove { RemoveHandler(StartStopClickedEvent, value); }
        }

        public MainMenuControl()
        {
            InitializeComponent();

            // Set menu item header with version
            var version = CopyrightInfo.Version;
            AboutMenuItem.Header = "FTPS Server - V" + CopyrightInfo.Version.ToString(3); ;
        }

        private void StartStopButton_Click(object sender, RoutedEventArgs e)
        {
            RaiseEvent(new RoutedEventArgs(StartStopClickedEvent));
        }

        public void UpdateServerStatus(bool isRunning)
        {
            Dispatcher.Invoke(() =>
            {
                if (isRunning)
                {
                    PlayIcon.Visibility = Visibility.Collapsed;
                    StopIcon.Visibility = Visibility.Visible;
                }
                else
                {
                    PlayIcon.Visibility = Visibility.Visible;
                    StopIcon.Visibility = Visibility.Collapsed;
                }
            });
        }
    }
}
