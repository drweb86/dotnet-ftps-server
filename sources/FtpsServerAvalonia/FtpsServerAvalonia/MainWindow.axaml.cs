using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using FtpsServerAppsShared.Helpers;
using FtpsServerAppsShared.Models;
using FtpsServerAppsShared.Services;
using FtpsServerAvalonia.Helpers;
using FtpsServerAvalonia.Models;
using FtpsServerAvalonia.Resources;
using FtpsServerAvalonia.Services;
using FtpsServerConsole;
using FtpsServerLibrary;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace FtpsServerAvalonia
{
    public partial class MainWindow : Window
    {
        private FtpsServer? _server;
        private readonly AppSettings _settings;
        private readonly ObservableCollection<UserAccount> _users;
        private readonly ObservableCollection<LogEntry> _logEntries;
        private readonly UiLog _uiLog;
        private readonly IOsSleepPreventionService _osSleepPreventionService = OsSleepPreventionServiceFactory.Create();
        private CertificateInfo? _certificateInfo;
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



        public MainWindow()
        {
            InitializeComponent();
            ExtendClientAreaToDecorationsHint = OperatingSystem.IsWindows() || OperatingSystem.IsMacOS() || OperatingSystem.IsLinux();
            MainMenu.ShowStartStopItems = false;
            _settings = SettingsManager.LoadSettings();
            _users = new ObservableCollection<UserAccount>(_settings.Users);
            _logEntries = [];
            _uiLog = new UiLog(_logEntries);
            UsersItemsControl.ItemsSource = _users;
            LogItemsControl.ItemsSource = _logEntries;
            _logEntries.CollectionChanged += (_, e) =>
            {
                if (e.Action == NotifyCollectionChangedAction.Add)
                    LogScrollViewer.ScrollToEnd();
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
                if (!TryValidateForStart())
                    return;

                var config = new FtpsServerConfiguration();

                // Server configuration
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
                    config.Users.Add(new FtpsServerUserAccount
                    {
                        Login = user.Login,
                        Password = user.Password,
                        Folder = user.Folder,
                        Read = true,
                        Write = !user.ReadonlyPermission
                    });
                }

                _server = new FtpsServer(new CompositeFtpsServerLog(new FileLog(), _uiLog), config, new FtpsServerFileSystemProvider());
                await _server.StartAsync();

                IsServerRunning = true;
                _osSleepPreventionService.PreventSleep();

                _certificateInfo = _server.LoadedCertificate != null
                    ? CertificateInfoHelper.GetInfo(_server.LoadedCertificate)
                    : null;
                RefreshConnectionInstruction();
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
                _certificateInfo = null;
                ConnectionInstruction.InstructionText = string.Empty;
                _osSleepPreventionService.StopPreventSleep();
            }
            catch (Exception ex)
            {
                await MessageBoxManager.GetMessageBoxStandard(Strings.ErrorTitle, string.Format(Strings.ErrorStopServerFormat, ex.Message), ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error).ShowAsync();
            }
        }

        private void UpdateServerStatus()
        {
            MainMenu.UpdateServerStatus(IsServerRunning);
            ConnectionInstructionExpander.IsVisible = IsServerRunning;
            ConfigExpander.IsVisible = !IsServerRunning;
            UsersExpander.IsVisible = !IsServerRunning;
            LogsExpander.IsVisible = IsServerRunning;
            StartStopButtonText.Text = IsServerRunning ? Strings.MenuStop : Strings.MenuStart;
            StartStopButtonText.Foreground = IsServerRunning ? Brushes.PaleVioletRed : Brushes.Green;
            if (IsServerRunning)
                ValidationBar.Text = null;
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
                android: false);
        }

        private void ClearLogs_Click(object? sender, RoutedEventArgs e)
        {
            _logEntries.Clear();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            StopServer();
            SaveSettings();
        }
    }
}
