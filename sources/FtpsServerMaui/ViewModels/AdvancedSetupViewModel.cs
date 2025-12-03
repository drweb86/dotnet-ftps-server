using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FtpsServerMaui.Models;
using FtpsServerMaui.Views;
using System.Collections.ObjectModel;

namespace FtpsServerMaui.ViewModels;

public partial class AdvancedSetupViewModel : CrossPlatformObservableObject
{
    private readonly IFtpsService _ftpsService;
    private readonly IConfigurationService _configService;
    private readonly IUserEditorService _userEditorService;

    private string _ip = "0.0.0.0";
    public string Ip
    {
        get => _ip;
        set => SetPropertyPlatformAgnostic(ref _ip, value);
    }

    private int _port = 2121;
    public int Port
    {
        get => _port;
        set => SetPropertyPlatformAgnostic(ref _port, value);
    }

    private int _maxConnections = 10;
    public int MaxConnections
    {
        get => _maxConnections;
        set => SetPropertyPlatformAgnostic(ref _maxConnections, value);
    }

    private bool _useSelfSignedCertificate = true;
    public bool UseSelfSignedCertificate
    {
        get => _useSelfSignedCertificate;
        set => SetPropertyPlatformAgnostic(ref _useSelfSignedCertificate, value);
    }

    private string _certificatePath = string.Empty;
    public string CertificatePath
    {
        get => _certificatePath;
        set => SetPropertyPlatformAgnostic(ref _certificatePath, value);
    }

    private string _certificatePassword = string.Empty;
    public string CertificatePassword
    {
        get => _certificatePassword;
        set => SetPropertyPlatformAgnostic(ref _certificatePassword, value);
    }

    private ObservableCollection<UserConfiguration> _users = [];
    public ObservableCollection<UserConfiguration> Users
    {
        get => _users;
        set => SetPropertyPlatformAgnostic(ref _users, value);
    }

    private UserConfiguration? _selectedUser;
    public UserConfiguration? SelectedUser
    {
        get => _selectedUser;
        set => SetPropertyPlatformAgnostic(ref _selectedUser, value);
    }

    public AdvancedSetupViewModel(IFtpsService ftpsService, IConfigurationService configService, IUserEditorService userEditorService)
    {
        _ftpsService = ftpsService;
        _configService = configService;
        _userEditorService = userEditorService;

        // Subscribe to user editor events
        _userEditorService.UserAdded += OnUserAdded;
        _userEditorService.UserUpdated += OnUserUpdated;

        LoadConfigurationAsync();
    }

    private async void LoadConfigurationAsync()
    {
        var config = await _configService.LoadConfigurationAsync();
        if (config != null)
        {
            Ip = config.Ip;
            Port = config.Port;
            MaxConnections = config.MaxConnections;
            UseSelfSignedCertificate = config.UseSelfSignedCertificate;
            CertificatePath = config.CertificatePath ?? string.Empty;
            CertificatePassword = config.CertificatePassword ?? string.Empty;

            Users.Clear();
            foreach (var user in config.Users)
            {
                Users.Add(user);
            }
        }
    }

    private readonly PickOptions _pickerOptions = new()
    {
        FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    { DevicePlatform.iOS, new[] { "public.item" } },
                    { DevicePlatform.Android, new[] { "*/*" } },
                    { DevicePlatform.WinUI, new[] { ".pfx", ".pem", ".der" } },
                    { DevicePlatform.macOS, new[] { "pfx", "pem", "der" } }
                })
    };

    [RelayCommand]
    private async Task BrowseCertificateAsync()
    {
        try
        {
            var result = await FilePicker.PickAsync(_pickerOptions);

            if (result != null)
            {
                CertificatePath = result.FullPath;
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Error", $"Failed to select certificate: {ex.Message}", "OK");
        }
    }

    [RelayCommand]
    private async Task AddUserAsync()
    {
        await Shell.Current.GoToAsync(nameof(UserEditorPage));
    }

    [RelayCommand]
    private async Task EditUserAsync()
    {
        if (SelectedUser == null)
        {
            await Shell.Current.DisplayAlertAsync("Info", "Please select a user to edit", "OK");
            return;
        }

        var parameters = new Dictionary<string, object>
        {
            { "EditingUser", SelectedUser }
        };

        await Shell.Current.GoToAsync(nameof(UserEditorPage), parameters);
    }

    [RelayCommand]
    private async Task DeleteUserAsync()
    {
        if (SelectedUser == null)
        {
            await Shell.Current.DisplayAlertAsync("Info", "Please select a user to delete", "OK");
            return;
        }

        var confirm = await Shell.Current.DisplayAlertAsync("Confirm",
            $"Are you sure you want to delete user '{SelectedUser.Username}'?",
            "Yes", "No");

        if (confirm)
        {
            Users.Remove(SelectedUser);
            SelectedUser = null;
        }
    }

    [RelayCommand]
    private async Task StartServerAsync()
    {
        if (Users.Count == 0)
        {
            await Shell.Current.DisplayAlertAsync("Error", "At least one user is required", "OK");
            return;
        }

        if (!UseSelfSignedCertificate && string.IsNullOrWhiteSpace(CertificatePath))
        {
            await Shell.Current.DisplayAlertAsync("Error", "Certificate path is required when not using self-signed certificate", "OK");
            return;
        }

        var configuration = new ServerConfiguration
        {
            Ip = Ip,
            Port = Port,
            MaxConnections = MaxConnections,
            UseSelfSignedCertificate = UseSelfSignedCertificate,
            CertificatePath = CertificatePath,
            CertificatePassword = CertificatePassword
        };

        foreach (var user in Users)
        {
            configuration.Users.Add(user);
        }

        try
        {
            await _ftpsService.StartServerAsync(configuration);
            await _configService.SaveConfigurationAsync(configuration);
            await Shell.Current.DisplayAlertAsync("Success",
                $"FTPS Server started!\n\nConnect using:\nHost: {await AdvancedSetupViewModel.GetLocalIpAddress()}\nPort: {Port}\nEncryption mode: Explicit",
                "OK");
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Error", $"Failed to start server: {ex.Message}", "OK");
        }
    }

    private static async Task<string> GetLocalIpAddress()
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

    public void OnUserAdded(UserConfiguration user)
    {
        Users.Add(user);
    }

    public void OnUserUpdated(UserConfiguration oldUser, UserConfiguration newUser)
    {
        var index = Users.IndexOf(oldUser);
        if (index >= 0)
        {
            Users[index] = newUser;
        }
    }

    private void OnUserAdded(object? sender, UserConfiguration user)
    {
        MainThread.BeginInvokeOnMainThread(() => Users.Add(user));
    }

    private void OnUserUpdated(object? sender, (UserConfiguration Original, UserConfiguration Updated) tuple)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            var index = Users.IndexOf(tuple.Original);
            if (index >= 0)
            {
                Users[index] = tuple.Updated;
            }
        });
    }

    public void RefreshUsers()
    {
        OnPropertyChanged(nameof(Users));
    }
}






