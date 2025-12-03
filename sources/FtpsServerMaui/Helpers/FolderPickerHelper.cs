namespace FtpsServerMaui.Helpers;

public static class FolderPickerHelper
{
    public static async Task<string?> PickFolderAsync()
    {
#if WINDOWS
        try
        {
            var folderPicker = new Windows.Storage.Pickers.FolderPicker
            {
                SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.ComputerFolder
            };

            // Get window handle for WinUI
            var window = Application.Current?.Windows[0]?.Handler?.PlatformView as Microsoft.UI.Xaml.Window;
            if (window != null)
            {
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
                WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, hwnd);
            }

            folderPicker.FileTypeFilter.Add("*");

            var folder = await folderPicker.PickSingleFolderAsync();
            return folder?.Path;
        }
        catch (Exception)
        {
            return null;
        }
#elif ANDROID
        // Android doesn't have a native folder picker in MAUI
        // Return null to indicate manual entry is needed
        await Shell.Current.DisplayAlertAsync("Folder Selection",
            "Please enter the folder path manually.\n\n" +
            "Common Android paths:\n" +
            "• /storage/emulated/0/FtpsRoot\n" +
            "• /sdcard/FtpsRoot\n" +
            "• /storage/emulated/0/Documents/FtpsRoot\n\n" +
            "The app will create the folder if it doesn't exist.",
            "OK");
        return null;
#elif IOS || MACCATALYST
        // iOS/macOS have restrictions on folder access
        await Shell.Current.DisplayAlertAsync("Folder Selection",
            "Due to platform security restrictions, please enter the folder path manually.\n\n" +
            "Note: The app can only access its own sandboxed directory and user-selected files.\n\n" +
            "Leave empty to use the default app directory.",
            "OK");
        return null;
#else
        await Shell.Current.DisplayAlert("Folder Selection", 
            "Please enter the folder path manually.", 
            "OK");
        return null;
#endif
    }
}
