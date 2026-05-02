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
                        UiLog.Current?.Info($"Folder items: {props.DateModified} // {props.DateModified?.UtcDateTime} // {props.DateCreated} // {props.DateCreated?.UtcDateTime} ");

                        // test 2
                        var testFolder2 = await topLevel.StorageProvider.OpenFolderBookmarkAsync(bookmark);
                        var subfolder2 = (await testFolder2.GetItemsAsync().ToListAsync()).Single(x => x.Name == "subfolder") as IStorageFolder;
                        var file = await subfolder2.CreateFileAsync("subfolder file 3");
                        await using (var stream = await file.OpenWriteAsync())
                        {
                            var bytes = System.Text.Encoding.UTF8.GetBytes("Test content written from FTPS Server");
                            await stream.WriteAsync(bytes);
                        }
                        UiLog.Current?.Info($"Verify file created subfolder/subfolder file 3");
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
