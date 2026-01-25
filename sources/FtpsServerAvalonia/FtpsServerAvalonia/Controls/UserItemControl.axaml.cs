using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using FtpsServerAvalonia.Models;
using FtpsServerAvalonia.Resources;
using System;
using System.Runtime.InteropServices;

namespace FtpsServerAvalonia.Controls
{
    public partial class UserItemControl : UserControl
    {
        public static readonly RoutedEvent<RoutedEventArgs> RemoveUserRequestedEvent =
            RoutedEvent.Register<UserItemControl, RoutedEventArgs>(
                nameof(RemoveUserRequested),
                RoutingStrategies.Bubble);

        public event EventHandler<RoutedEventArgs> RemoveUserRequested
        {
            add => AddHandler(RemoveUserRequestedEvent, value);
            remove => RemoveHandler(RemoveUserRequestedEvent, value);
        }

        public UserItemControl()
        {
            InitializeComponent();
        }

        private async void BrowseUserFolder_Click(object? sender, RoutedEventArgs e)
        {
            if (DataContext is UserAccount user)
            {
                var topLevel = TopLevel.GetTopLevel(this);
                if (topLevel == null) return;

                var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
                {
                    Title = string.Format(Strings.UserSelectFolderFormat, user.Login),
                    AllowMultiple = false
                });

                if (folders.Count > 0)
                {
                    var folder = folders[0];
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ||
                        RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    {
                        user.Folder = folder.Path.LocalPath.TrimEnd('/', '\\');
                        user.FolderBookmark = string.Empty;
                    }
                    else
                    {
                        // Android - save bookmark for persistent access
                        var bookmark = await folder.SaveBookmarkAsync();
                        if (bookmark is not null)
                        {
                            user.Folder = folder.Name;
                            user.FolderBookmark = bookmark;
                        }
                    }
                }
            }
        }

        private void RemoveUser_Click(object? sender, RoutedEventArgs e)
        {
            RaiseEvent(new RoutedEventArgs(RemoveUserRequestedEvent, this));
        }
    }
}
