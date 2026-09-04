using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Interactivity;
using Avalonia.Media;
using FtpsServerAppsShared.Helpers;
using FtpsServerAppsShared.Models;
using FtpsServerAvalonia.Controls;
using FtpsServerAvalonia.Helpers;
using FtpsServerAvalonia.Models;
using FtpsServerAvalonia.Resources;
using FtpsServerAvalonia.Services;
using FtpsServerLibrary;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace FtpsServerAvalonia.Views;

public partial class AndroidView : UserControl
{
    private FtpsServer? _server;
    private readonly AppSettings _settings;
    private readonly ObservableCollection<UserAccount> _users;
    private readonly ObservableCollection<LogEntry> _logEntries;
    private readonly UiLog? _uiLog;
    private CertificateInfo? _certificateInfo;
    private bool _isServerRunning;

    public bool IsServerRunning
    {
        get => _isServerRunning;
        set
        {
            if (_isServerRunning == value)
                return;

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
        MainMenu.ShowStartStopItems = false;
        MainMenu.UpdateServerStatus(IsServerRunning);

        _settings = SettingsManager.LoadSettings();
        _users = new ObservableCollection<UserAccount>(_settings.Users);
        _logEntries = [];
        _uiLog = new UiLog(_logEntries);

        UsersItemsControl.ItemsSource = _users;
        LogItemsControl.ItemsSource = _logEntries;

        // Auto-scroll logs when new entries are added
        _logEntries.CollectionChanged += (s, e) =>
        {
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
            {
                LogScrollViewer.ScrollToEnd();
            }
        };

        // Initialize DependencyProperties from settings
        Port = _settings.ServerPort;
        MaxConnections = _settings.MaxConnections;
        CertificateSource = _settings.CertificateSource;
        CertificatePath = _settings.CertificatePath;
        CertificatePassword = _settings.CertificatePassword;
        DataContext = this;
        UpdateServerStatus();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel?.InsetsManager is { } insetsManager)
        {
            insetsManager.DisplayEdgeToEdgePreference = true;
            insetsManager.IsSystemBarVisible = true;
            insetsManager.SafeAreaChanged += OnSafeAreaChanged;

            var SafeArea = insetsManager.SafeAreaPadding;
            var height = topLevel.InputPane?.OccludedRect.Height;
            // Apply initial safe area
            UpdateSafeAreaPadding(insetsManager.SafeAreaPadding, height);
        }
    }

    private void OnSafeAreaChanged(object? sender, SafeAreaChangedArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null)
            return;
        var height = topLevel.InputPane?.OccludedRect.Height;
        UpdateSafeAreaPadding(e.SafeAreaPadding, height);
    }

    private void UpdateSafeAreaPadding(Thickness safeArea, double? height)
    {
        // Add padding to the content panel to account for keyboard and system bars
        ContentPanel.Margin = new Thickness(24, 24 + safeArea.Top, 24, 24 + (height ?? safeArea.Bottom));
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
            Folder = string.Empty,
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
            if (!TryValidateForStart())
                return;

            var config = new FtpsServerConfiguration();

            config.ServerSettings.Ip = "0.0.0.0";
            config.ServerSettings.Port = _settings.ServerPort;
            config.ServerSettings.MaxConnections = _settings.MaxConnections;

            if (_settings.CertificateSource == CertificateSourceType.FromFile)
            {
                config.ServerSettings.CertificatePath = _settings.CertificatePath;
                config.ServerSettings.CertificatePassword = _settings.CertificatePassword;
            }

            foreach (var user in _users)
            {
                var folderValue = !string.IsNullOrEmpty(user.FolderBookmark)
                    ? AndroidFolderBookmarkSerializer.Serialise(new AndroidFolderBookmark(user.Folder, user.FolderBookmark))
                    : user.Folder;

                config.Users.Add(new FtpsServerUserAccount
                {
                    Login = user.Login,
                    Password = user.Password,
                    Folder = folderValue,
                    Read = true,
                    Write = !user.ReadonlyPermission
                });
            }

            var topLevel = TopLevel.GetTopLevel(App.Instance);
            if (topLevel is null)
                return;
            _server = new FtpsServer(_uiLog!, config, new AndroidFtpsServerFileSystemProvider(topLevel.StorageProvider));
            await _server.StartAsync();

            IsServerRunning = true;

            _certificateInfo = _server.LoadedCertificate != null
                ? CertificateInfoHelper.GetInfo(_server.LoadedCertificate)
                : null;
            RefreshConnectionInstruction();
        }
        catch (Exception ex)
        {
            ShowError(string.Format(Strings.ErrorStartServerFormat, ex.Message));
            IsServerRunning = false;
        }
    }

    private void StopServer()
    {
        if (!IsServerRunning)
            return;
        try
        {
            _server?.Stop();
            _server = null;
            IsServerRunning = false;
            _certificateInfo = null;
            ConnectionInstruction.InstructionText = string.Empty;
        }
        catch (Exception ex)
        {
            ShowError(string.Format(Strings.ErrorStopServerFormat, ex.Message));
        }
    }

    private void UpdateServerStatus()
    {
        MainMenu.UpdateServerStatus(IsServerRunning);
        AndroidStartStopText.Text = IsServerRunning ? Strings.MenuStop : Strings.MenuStart;
        AndroidStartStopText.Foreground = IsServerRunning ? Brushes.PaleVioletRed : Brushes.Green;
        ConnectionInstructionExpander.IsVisible = IsServerRunning;
        ConfigExpander.IsVisible = !IsServerRunning;
        UsersExpander.IsVisible = !IsServerRunning;
        LogsExpander.IsVisible = IsServerRunning;
        if (IsServerRunning)
            ValidationBar.Text = null;
        App.AndroidKeepAwakeService?.SetKeepScreenOn(IsServerRunning);
    }

    private string? ValidateUsers()
    {
        if (_users.Count == 0)
            return Strings.ErrorAddUser;

        foreach (var user in _users)
        {
            user.LoginError = string.IsNullOrWhiteSpace(user.Login) ? Strings.UserUsernameValidation : null;
            user.PasswordError = string.IsNullOrWhiteSpace(user.Password) ? Strings.UserPasswordValidation : null;
            var folderMissing = string.IsNullOrWhiteSpace(user.Folder) && string.IsNullOrWhiteSpace(user.FolderBookmark);
            user.FolderError = folderMissing ? Strings.UserFolderValidation : null;
            var error = user.LoginError ?? user.PasswordError ?? user.FolderError;
            if (error != null)
                return error;
        }

        return null;
    }

    private bool TryValidateForStart()
    {
        var configError = ServerConfig.Validate();
        var usersError = ValidateUsers();
        var error = configError ?? usersError;
        ValidationBar.Text = error;
        if (error == null)
            return true;
        if (configError != null)
            ConfigExpander.IsExpanded = true;
        else
            UsersExpander.IsExpanded = true;
        return false;
    }

    private void RefreshConnectionInstruction()
    {
        ConnectionInstruction.InstructionText = ConnectionDetails.Build(
            Port,
            _users,
            CertificateSource == CertificateSourceType.SelfSigned,
            _certificateInfo?.Sha256Fingerprint,
            _certificateInfo?.Sha1Fingerprint,
            android: true);
    }

    private void ShowError(string message)
    {
        ValidationBar.Text = message;
    }

    private void ClearLogs_Click(object sender, RoutedEventArgs e)
    {
        _logEntries.Clear();
    }

    // UserControl doesn't have OnClosed, so we need to handle detachment differently
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel?.InsetsManager is { } insetsManager)
        {
            insetsManager.SafeAreaChanged -= OnSafeAreaChanged;
        }

        base.OnDetachedFromVisualTree(e);
        StopServer();
        SaveSettings();
    }
}
