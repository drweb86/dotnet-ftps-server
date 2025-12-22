using FtpsServerWindows.Models;
using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;

namespace FtpsServerWindows.Controls
{
    public partial class UserItemControl : UserControl
    {
        public static readonly RoutedEvent RemoveUserRequestedEvent =
            EventManager.RegisterRoutedEvent(
                nameof(RemoveUserRequested),
                RoutingStrategy.Bubble,
                typeof(RoutedEventHandler),
                typeof(UserItemControl));

        public event RoutedEventHandler RemoveUserRequested
        {
            add => AddHandler(RemoveUserRequestedEvent, value);
            remove => RemoveHandler(RemoveUserRequestedEvent, value);
        }

        public UserItemControl()
        {
            InitializeComponent();
        }

        private void BrowseUserFolder_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is UserAccount user)
            {
                var dialog = new OpenFolderDialog
                {
                    Title = $"Select folder to share for user {user.Login}",
                };

                if (dialog.ShowDialog() == true)
                {
                    user.Folder = dialog.FolderName;
                }
            }
        }

        private void RemoveUser_Click(object sender, RoutedEventArgs e)
        {
            RaiseEvent(new RoutedEventArgs(RemoveUserRequestedEvent, this));
        }
    }
}
