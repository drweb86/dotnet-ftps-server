using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FtpsServerMaui.Helpers;
using FtpsServerMaui.Models;
using FtpsServerMaui.Services;

namespace FtpsServerMaui.ViewModels;

public partial class SimpleSetupViewModel : ObservableObject
{
    private readonly IFtpsService _ftpsService;
    private readonly IConfigurationService _configService;

    [ObservableProperty]
    private string _username = "ftpuser";

    [ObservableProperty]
    private string _password = "ftppass";

    [ObservableProperty]
    private string _rootFolder = string.Empty;

    [ObservableProperty]
    private int _port = 2121;

    public SimpleSetupViewModel(IFtpsService ftpsService, IConfigurationService configService)
    {
        _ftpsService = ftpsService;
        _configService = configService;

        // Set default root folder
        RootFolder = Path.Combine(FileSystem.AppDataDirectory, "FtpsRoot");
#if ANDROID
        RootFolder = "/storage/emulated/0/";
#endif
        LoadConfigurationAsync();
    }

    private async void LoadConfigurationAsync()
    {
        var config = await _configService.LoadConfigurationAsync();
        if (config != null)
        {
            var user = config.Users?.FirstOrDefault();

            Port = config.Port;

            if (user is not null)
            {
                Username = user.Username;
                Password = user.Password;
                RootFolder = user.Folder;
            }
        }
    }

    [RelayCommand]
    private async Task BrowseFolderAsync()
    {
        try
        {
            var selectedPath = await FolderPickerHelper.PickFolderAsync();
            if (!string.IsNullOrEmpty(selectedPath))
            {
                RootFolder = selectedPath;
            }
        }
        catch (Exception)
        {
            // Folder picker might not be available on all platforms
            await Shell.Current.DisplayAlertAsync("Info", 
                "Please enter the folder path manually. Default folder will be used if empty.", 
                "OK");
        }
    }

    [RelayCommand]
    private async Task StartServerAsync()
    {
        if (string.IsNullOrWhiteSpace(Username))
        {
            await Shell.Current.DisplayAlertAsync("Error", "Username is required", "OK");
            return;
        }

        if (string.IsNullOrWhiteSpace(Password))
        {
            await Shell.Current.DisplayAlertAsync("Error", "Password is required", "OK");
            return;
        }

        if (string.IsNullOrWhiteSpace(RootFolder))
        {
            RootFolder = Path.Combine(FileSystem.AppDataDirectory, "FtpsRoot");
        }

        // Create directory if it doesn't exist
        if (!Directory.Exists(RootFolder))
        {
            try
            {
                Directory.CreateDirectory(RootFolder);
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlertAsync("Error", $"Failed to create directory: {ex.Message}", "OK");
                return;
            }
        }

        var configuration = new ServerConfiguration
        {
            Ip = "0.0.0.0",
            Port = Port,
            MaxConnections = 10,
            UseSelfSignedCertificate = true
        };

        configuration.Users.Add(new UserConfiguration
        {
            Username = Username,
            Password = Password,
            Folder = RootFolder,
            ReadPermission = true,
            WritePermission = true
        });

        try
        {
            await _ftpsService.StartServerAsync(configuration);
            await _configService.SaveConfigurationAsync(configuration);
            await Shell.Current.DisplayAlertAsync("Success", 
                $"FTPS Server started!\n\nConnect using:\nHost: {await GetLocalIpAddress()}\nPort: {Port}\nUsername: {Username}\nPassword: {Password}\nEncryption mode: Explicit", 
                "OK");
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Error", $"Failed to start server: {ex.Message}", "OK");
        }
    }

    private async static Task<string> GetLocalIpAddress()
    {
        try
        {
            var addresses = await System.Net.Dns.GetHostAddressesAsync(System.Net.Dns.GetHostName());
            var ipv4 = addresses.FirstOrDefault(ip => ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
            return ipv4?.ToString() ?? "localhost";
        }
        catch
        {
            return "localhost";
        }
    }
}
