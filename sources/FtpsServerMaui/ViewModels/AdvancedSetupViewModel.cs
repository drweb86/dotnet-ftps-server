using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FtpsServerMaui.Models;
using FtpsServerMaui.Services;
using FtpsServerMaui.Views;
using System.Collections.ObjectModel;

namespace FtpsServerMaui.ViewModels;

public partial class AdvancedSetupViewModel : ObservableObject
{
    private readonly IFtpsService _ftpsService;
    private readonly IConfigurationService _configService;
    private readonly IUserEditorService _userEditorService;

    [ObservableProperty]
    private string _ip = "0.0.0.0";

    [ObservableProperty]
    private int _port = 2121;

    [ObservableProperty]
    private int _maxConnections = 10;

    [ObservableProperty]
    private bool _useSelfSignedCertificate = true;

    [ObservableProperty]
    private string _certificatePath = string.Empty;

    [ObservableProperty]
    private string _certificatePassword = string.Empty;

    [ObservableProperty]
    private ObservableCollection<UserConfiguration> _users = new();

    [ObservableProperty]
    private UserConfiguration? _selectedUser;

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

    [RelayCommand]
    private async Task BrowseCertificateAsync()
    {
        try
        {
            var result = await FilePicker.PickAsync(new PickOptions
            {
                FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    { DevicePlatform.iOS, new[] { "public.item" } },
                    { DevicePlatform.Android, new[] { "*/*" } },
                    { DevicePlatform.WinUI, new[] { ".pfx", ".pem", ".der" } },
                    { DevicePlatform.macOS, new[] { "pfx", "pem", "der" } }
                })
            });

            if (result != null)
            {
                CertificatePath = result.FullPath;
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"Failed to select certificate: {ex.Message}", "OK");
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
            await Shell.Current.DisplayAlert("Info", "Please select a user to edit", "OK");
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
            await Shell.Current.DisplayAlert("Info", "Please select a user to delete", "OK");
            return;
        }

        var confirm = await Shell.Current.DisplayAlert("Confirm",
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
            await Shell.Current.DisplayAlert("Error", "At least one user is required", "OK");
            return;
        }

        if (!UseSelfSignedCertificate && string.IsNullOrWhiteSpace(CertificatePath))
        {
            await Shell.Current.DisplayAlert("Error", "Certificate path is required when not using self-signed certificate", "OK");
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
            await Shell.Current.DisplayAlert("Success",
                $"FTPS Server started on {Ip}:{Port}",
                "OK");
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"Failed to start server: {ex.Message}", "OK");
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






