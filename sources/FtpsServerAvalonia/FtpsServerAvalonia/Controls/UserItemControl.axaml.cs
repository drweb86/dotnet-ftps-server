using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using FtpsServerAvalonia.Models;
using FtpsServerAvalonia.Resources;
using FtpsServerAvalonia.Services;
using System;
using System.Linq;
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

                        // ADD test code here.

                        // test 1 - list of files.
                        var testFolder = await topLevel.StorageProvider.OpenFolderBookmarkAsync(bookmark);
                        var items = await testFolder.GetItemsAsync().ToListAsync();
                        var item = items!.Single(x => x.Name == "subfolder file 2");
                        var props = await item.GetBasicPropertiesAsync();
                        UiLog.Current?.Info($"Folder items 2: Size={props.Size} // DateModified={props.DateModified} // DateModified.UtcDateTime={props.DateModified?.UtcDateTime} // DateCreated={props.DateCreated} // DateCreated.UtcDateTime={props.DateCreated?.UtcDateTime}");
                        UiLog.Current?.Info("Avalonia versions: Avalonia=12.0.3, Avalonia.Android=12.0.3, Avalonia.Themes.Fluent=12.0.3, Avalonia.Fonts.Inter=12.0.3, Avalonia.Diagnostics=12.0.3, Avalonia.Desktop=12.0.3, Avalonia.iOS=12.0.3, Avalonia.Browser=12.0.3");

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
