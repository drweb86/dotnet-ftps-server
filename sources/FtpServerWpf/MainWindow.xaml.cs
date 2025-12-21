using FtpsServerApp.Controls;
using FtpsServerApp.Models;
using FtpsServerApp.Services;
using FtpsServerLibrary;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace FtpsServerApp
{
    public partial class MainWindow : Window
    {
        private FtpsServer? _server;
        private AppSettings _settings;
        private ObservableCollection<UserAccount> _users;
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
            _settings.Users = _users.ToList();
            SettingsManager.SaveSettings(_settings);
        }

        private void AddUser_Click(object sender, RoutedEventArgs e)
        {
            var newUser = new UserAccount
            {
                Login = $"user{_users.Count + 1}",
                Password = "password",
                Folder = "",
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

        private void StartServer()
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
                        MessageBox.Show("Please select a certificate file.", "Error",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    config.ServerSettings.CertificatePath = _settings.CertificatePath;
                    config.ServerSettings.CertificatePassword = _settings.CertificatePassword;
                }

                    // Users
                    if (_users.Count == 0)
                    {
                        MessageBox.Show("Please add at least one user.", "Error", 
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    foreach (var user in _users)
                    {
                        if (string.IsNullOrWhiteSpace(user.Login) || 
                            string.IsNullOrWhiteSpace(user.Password) ||
                            string.IsNullOrWhiteSpace(user.Folder))
                        {
                            MessageBox.Show($"User {user.Login} has incomplete information.", "Error", 
                                MessageBoxButton.OK, MessageBoxImage.Warning);
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

                _server = new FtpsServer(new FtpsLogger(), config);
                _server.Start();
                
                IsServerRunning = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to start server: {ex.Message}", "Error", 
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
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error stopping server: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateServerStatus()
        {
            MainMenu.UpdateServerStatus(IsServerRunning);
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            StopServer();
            SaveSettings();
        }
    }
}
