using FtpsServerAppsShared.Helpers;
using FtpsServerAppsShared.Models;
using FtpsServerAppsShared.Services;
using FtpsServerWindows.Controls;
using FtpsServerWindows.Helpers;
using FtpsServerWindows.Models;
using FtpsServerWindows.Services;
using FtpsServerWindows.Resources;
using FtpsServerConsole;
using FtpsServerLibrary;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace FtpsServerWindows
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


        public static readonly DependencyProperty PortProperty =
            DependencyProperty.Register(nameof(Port), typeof(int), typeof(MainWindow),
                new FrameworkPropertyMetadata(2121, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnPortChanged));

        public static readonly DependencyProperty MaxConnectionsProperty =
            DependencyProperty.Register(nameof(MaxConnections), typeof(int), typeof(MainWindow),
                new FrameworkPropertyMetadata(10, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnMaxConnectionsChanged));

        public static readonly DependencyProperty CertificateSourceProperty =
            DependencyProperty.Register(nameof(CertificateSource), typeof(CertificateSourceType), typeof(MainWindow),
                new FrameworkPropertyMetadata(CertificateSourceType.SelfSigned, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnCertificateSourceChanged));

        public static readonly DependencyProperty CertificatePathProperty =
            DependencyProperty.Register(nameof(CertificatePath), typeof(string), typeof(MainWindow),
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnCertificatePathChanged));

        public static readonly DependencyProperty CertificatePasswordProperty =
            DependencyProperty.Register(nameof(CertificatePassword), typeof(string), typeof(MainWindow),
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnCertificatePasswordChanged));


        public int Port
        {
            get => (int)GetValue(PortProperty);
            set => SetValue(PortProperty, value);
        }

        public int MaxConnections
        {
            get => (int)GetValue(MaxConnectionsProperty);
            set => SetValue(MaxConnectionsProperty, value);
        }

        public CertificateSourceType CertificateSource
        {
            get => (CertificateSourceType)GetValue(CertificateSourceProperty);
            set => SetValue(CertificateSourceProperty, value);
        }

        public string CertificatePath
        {
            get => (string)GetValue(CertificatePathProperty);
            set => SetValue(CertificatePathProperty, value);
        }

        public string CertificatePassword
        {
            get => (string)GetValue(CertificatePasswordProperty);
            set => SetValue(CertificatePasswordProperty, value);
        }



        public MainWindow()
        {
            InitializeComponent();
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

        private static void OnPortChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var window = (MainWindow)d;
            window._settings.ServerPort = (int)e.NewValue;
        }

        private static void OnMaxConnectionsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var window = (MainWindow)d;
            window._settings.MaxConnections = (int)e.NewValue;
        }

        private static void OnCertificateSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var window = (MainWindow)d;
            window._settings.CertificateSource = (CertificateSourceType)e.NewValue;
        }

        private static void OnCertificatePathChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var window = (MainWindow)d;
            window._settings.CertificatePath = (string)e.NewValue;
        }

        private static void OnCertificatePasswordChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var window = (MainWindow)d;
            window._settings.CertificatePassword = (string)e.NewValue;
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

        private void UserItemControl_RemoveUserRequested(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is UserAccount user)
            {
                _users.Remove(user);
            }
        }

        private async void MainMenu_StartStopClicked(object sender, RoutedEventArgs e)
        {
            if (IsServerRunning)
            {
                StopServer();
            }
            else
            {
                await StartServer();
            }
        }

        private async Task StartServer()
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
                MessageBox.Show(string.Format(Strings.ErrorStartServerFormat, ex.Message), Strings.ErrorTitle,
                    MessageBoxButton.OK, MessageBoxImage.Error);
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
                _osSleepPreventionService.StopPreventSleep();
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format(Strings.ErrorStopServerFormat, ex.Message), Strings.ErrorTitle,
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateServerStatus()
        {
            MainMenu.UpdateServerStatus(IsServerRunning);
            ConnectionInstructionExpander.Visibility = IsServerRunning ? Visibility.Visible : Visibility.Collapsed;
            ConfigExpander.Visibility = IsServerRunning ? Visibility.Collapsed : Visibility.Visible;
            UsersExpander.Visibility = IsServerRunning ? Visibility.Collapsed : Visibility.Visible;
            LogsExpander.Visibility = IsServerRunning ? Visibility.Visible : Visibility.Collapsed;
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
                user.FolderError = string.IsNullOrWhiteSpace(user.Folder) ? Strings.UserFolderValidation : null;
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
                _certificateInfo?.Sha1Fingerprint);
        }

        private void ClearLogs_Click(object sender, RoutedEventArgs e)
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
