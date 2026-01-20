using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using FtpsServerAvalonia.Controls;
using FtpsServerAvalonia.Models;
using FtpsServerAvalonia.Services;
using FtpsServerAvalonia.Resources;
using FtpsServerConsole;
using FtpsServerLibrary;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;

namespace FtpsServerAvalonia.Views;

public partial class AndroidView : UserControl
{
    private FtpsServer? _server;
    private readonly AppSettings _settings;
    private readonly ObservableCollection<UserAccount> _users;
    private bool _isServerRunning;

    public bool IsServerRunning
    {
        get => _isServerRunning;
        set
        {
            _isServerRunning = value;
            UpdateServerStatus();
        }
    }


    private int _port = 2121;
    private int _maxConnections = 10;
    private CertificateSourceType _certificateSource = CertificateSourceType.SelfSigned;
    private string _certificatePath = string.Empty;
    private string _certificatePassword = string.Empty;

    public int Port
    {
        get => _port;
        set
        {
            _port = value;
            _settings?.ServerPort = value;
        }
    }

    public int MaxConnections
    {
        get => _maxConnections;
        set
        {
            _maxConnections = value;
            _settings?.MaxConnections = value;
        }
    }

    public CertificateSourceType CertificateSource
    {
        get => _certificateSource;
        set
        {
            _certificateSource = value;
            _settings?.CertificateSource = value;
        }
    }

    public string CertificatePath
    {
        get => _certificatePath;
        set
        {
            _certificatePath = value;
            _settings?.CertificatePath = value;
        }
    }

    public string CertificatePassword
    {
        get => _certificatePassword;
        set
        {
            _certificatePassword = value;
            _settings?.CertificatePassword = value;
        }
    }



    public AndroidView()
    {
        InitializeComponent();
        _settings = SettingsManager.LoadSettings();
        _users = new ObservableCollection<UserAccount>(_settings.Users);
        UsersItemsControl.ItemsSource = _users;

        // Initialize DependencyProperties from settings
        Port = _settings.ServerPort;
        MaxConnections = _settings.MaxConnections;
        CertificateSource = _settings.CertificateSource;
        CertificatePath = _settings.CertificatePath;
        CertificatePassword = _settings.CertificatePassword;

        DataContext = this;
    }

    private void SaveSettings()
    {
        _settings.Users = [.. _users];
        SettingsManager.SaveSettings(_settings);
    }

    private void AddUser_Click(object sender, RoutedEventArgs e)
    {
        var newUser = new UserAccount
        {
            Login = $"user{_users.Count + 1}",
            Password = $"password{_users.Count + 1}",
            Folder = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
            ReadonlyPermission = false,
        };
        _users.Add(newUser);
    }

    private void UserItemControl_RemoveUserRequested(object? sender, RoutedEventArgs e)
    {
        if (sender is Control element && element.DataContext is UserAccount user)
        {
            _users.Remove(user);
        }
    }

    private void MainMenu_StartStopClicked(object sender, RoutedEventArgs e)
    {
        if (IsServerRunning)
        {
            StopServer();
        }
        else
        {
            StartServer();
        }
    }

    private async void StartServer()
    {
        try
        {
            SaveSettings();

            var config = new FtpsServerConfiguration();

            // Server configuration
            config.ServerSettings.Ip = "0.0.0.0";
            config.ServerSettings.Port = _settings.ServerPort;
            config.ServerSettings.MaxConnections = _settings.MaxConnections;

            // Certificate configuration
            if (_settings.CertificateSource == CertificateSourceType.FromFile)
            {
                if (string.IsNullOrWhiteSpace(_settings.CertificatePath))
                {
                    await MessageBoxManager.GetMessageBoxStandard(Strings.ErrorTitle, Strings.ErrorSelectCertificate, ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Warning).ShowAsync();
                    return;
                }

                config.ServerSettings.CertificatePath = _settings.CertificatePath;
                config.ServerSettings.CertificatePassword = _settings.CertificatePassword;
            }

                // Users
                if (_users.Count == 0)
                {
                    await MessageBoxManager.GetMessageBoxStandard(Strings.ErrorTitle, Strings.ErrorAddUser, ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Warning).ShowAsync();
                    return;
                }

                foreach (var user in _users)
                {
                    if (string.IsNullOrWhiteSpace(user.Login) ||
                        string.IsNullOrWhiteSpace(user.Password) ||
                        string.IsNullOrWhiteSpace(user.Folder))
                    {
                        await MessageBoxManager.GetMessageBoxStandard(Strings.ErrorTitle, string.Format(Strings.ErrorIncompleteUserFormat, user.Login), ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Warning).ShowAsync();
                        return;
                    }

                    config.Users.Add(new FtpsServerUserAccount
                    {
                        Login = user.Login,
                        Password = user.Password,
                        Folder = user.Folder,
                        Read = true,
                        Write = !user.ReadonlyPermission
                    });
                }

            _server = new FtpsServer(new StubLog(), config, new FtpsServerFileSystemProvider());
            await _server.StartAsync();

            IsServerRunning = true;
        }
        catch (Exception ex)
        {
            await MessageBoxManager.GetMessageBoxStandard(Strings.ErrorTitle, string.Format(Strings.ErrorStartServerFormat, ex.Message), ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error).ShowAsync();
            IsServerRunning = false;
        }
    }

    private async void StopServer()
    {
        if (!IsServerRunning)
            return;
        try
        {
            _server?.Stop();
            _server = null;
            IsServerRunning = false;
        }
        catch (Exception ex)
        {
            await MessageBoxManager.GetMessageBoxStandard(Strings.ErrorTitle, string.Format(Strings.ErrorStopServerFormat, ex.Message), ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error).ShowAsync();
        }
    }

    private void UpdateServerStatus()
    {
        MainMenu.UpdateServerStatus(IsServerRunning);
    }

    // UserControl doesn't have OnClosed, so we need to handle detachment differently
    protected override void OnDetachedFromVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        StopServer();
        SaveSettings();
    }
}
