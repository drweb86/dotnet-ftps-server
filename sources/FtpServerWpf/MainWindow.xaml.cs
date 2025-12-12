using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using FtpsServerApp.Models;
using FtpsServerApp.Services;
using FtpsServerLibrary;
using Microsoft.Win32;

namespace FtpsServerApp
{
    public partial class MainWindow : Window
    {
        private FtpsServer? _server;
        private FtpsLogger? _logger;
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

        public MainWindow()
        {
            InitializeComponent();
            _settings = SettingsManager.LoadSettings();
            _users = new ObservableCollection<UserAccount>(_settings.Users);
            UsersItemsControl.ItemsSource = _users;
            
            LoadSettings();
            DataContext = this;
        }

        private void LoadSettings()
        {
            // Load mode
            if (_settings.IsSimpleMode)
            {
                SimpleModeButton.IsChecked = true;
            }
            else
            {
                AdvancedModeButton.IsChecked = true;
            }

            // Load simple mode settings
            SimpleRootFolderTextBox.Text = _settings.SimpleRootFolder;
            SimpleReadCheckBox.IsChecked = _settings.SimpleReadPermission;
            SimpleWriteCheckBox.IsChecked = _settings.SimpleWritePermission;

            // Load advanced mode settings
            ServerIpTextBox.Text = _settings.ServerIp;
            ServerPortTextBox.Text = _settings.ServerPort.ToString();
            MaxConnectionsTextBox.Text = _settings.MaxConnections.ToString();

            // Certificate settings
            if (_settings.CertificateSource == CertificateSourceType.SelfSigned)
            {
                SelfSignedCertButton.IsChecked = true;
            }
            else
            {
                FileCertButton.IsChecked = true;
                CertPathTextBox.Text = _settings.CertificatePath;
            }
        }

        private void SaveSettings()
        {
            _settings.IsSimpleMode = SimpleModeButton.IsChecked == true;
            
            // Simple mode
            _settings.SimpleRootFolder = SimpleRootFolderTextBox.Text;
            _settings.SimpleReadPermission = SimpleReadCheckBox.IsChecked == true;
            _settings.SimpleWritePermission = SimpleWriteCheckBox.IsChecked == true;

            // Advanced mode
            _settings.ServerIp = ServerIpTextBox.Text;
            if (int.TryParse(ServerPortTextBox.Text, out int port))
                _settings.ServerPort = port;
            if (int.TryParse(MaxConnectionsTextBox.Text, out int maxConn))
                _settings.MaxConnections = maxConn;

            _settings.CertificateSource = SelfSignedCertButton.IsChecked == true 
                ? CertificateSourceType.SelfSigned 
                : CertificateSourceType.FromFile;
            _settings.CertificatePath = CertPathTextBox.Text;
            _settings.CertificatePassword = CertPasswordBox.Password;

            _settings.Users = _users.ToList();

            SettingsManager.SaveSettings(_settings);
        }

        private void ModeChanged(object sender, RoutedEventArgs e)
        {
            if (SimpleModePanel is null)
                return;
            if (AdvancedModePanel is null)
                return;

            if (SimpleModeButton.IsChecked == true)
            {
                SimpleModePanel.Visibility = Visibility.Visible;
                AdvancedModePanel.Visibility = Visibility.Collapsed;
            }
            else
            {
                SimpleModePanel.Visibility = Visibility.Collapsed;
                AdvancedModePanel.Visibility = Visibility.Visible;
            }
        }

        private void CertificateSourceChanged(object sender, RoutedEventArgs e)
        {
            if (CertFilePanel != null)
            {
                CertFilePanel.Visibility = FileCertButton.IsChecked == true 
                    ? Visibility.Visible 
                    : Visibility.Collapsed;
            }
        }

        private void BrowseSimpleFolder_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFolderDialog()
            {
                Title = "Select root folder for FTPS server",
            };

            if (dialog.ShowDialog() == true)
            {
                SimpleRootFolderTextBox.Text = dialog.FolderName;
            }
        }

        private void BrowseUserFolder_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is UserAccount user)
            {
                var dialog = new OpenFolderDialog
                {
                    Title = $"Select root folder for user {user.Login}",
                };

                if (dialog.ShowDialog() == true)
                {
                    user.Folder = dialog.FolderName;
                }
            }
        }

        private void BrowseCertificate_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Certificate Files (*.pfx;*.pem;*.der)|*.pfx;*.pem;*.der|All Files (*.*)|*.*",
                Title = "Select Certificate File"
            };

            if (dialog.ShowDialog() == true)
            {
                CertPathTextBox.Text = dialog.FileName;
            }
        }

        private void AddUser_Click(object sender, RoutedEventArgs e)
        {
            var newUser = new UserAccount
            {
                Login = $"user{_users.Count + 1}",
                Password = "password",
                Folder = "",
                ReadPermission = true,
                WritePermission = false
            };
            _users.Add(newUser);
        }

        private void RemoveUser_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is UserAccount user)
            {
                _users.Remove(user);
            }
        }

        private void UserPassword_Changed(object sender, RoutedEventArgs e)
        {
            if (sender is PasswordBox passwordBox && passwordBox.Tag is UserAccount user)
            {
                user.Password = passwordBox.Password;
            }
        }

        private void StartStopButton_Click(object sender, RoutedEventArgs e)
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

                _logger = new FtpsLogger();
                
                var config = new FtpsServerConfiguration();

                if (SimpleModeButton.IsChecked == true)
                {
                    // Simple mode configuration
                    if (string.IsNullOrWhiteSpace(SimpleRootFolderTextBox.Text))
                    {
                        MessageBox.Show("Please select a root folder.", "Error", 
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    config.ServerSettings.Ip = "0.0.0.0";
                    config.ServerSettings.Port = 2121;
                    config.ServerSettings.MaxConnections = 10;

                    config.Users.Add(new FtpsServerUserAccount
                    {
                        Login = "admin",
                        Password = "admin",
                        Folder = SimpleRootFolderTextBox.Text,
                        Read = SimpleReadCheckBox.IsChecked == true,
                        Write = SimpleWriteCheckBox.IsChecked == true
                    });
                }
                else
                {
                    // Advanced mode configuration
                    config.ServerSettings.Ip = ServerIpTextBox.Text;
                    
                    if (int.TryParse(ServerPortTextBox.Text, out int port))
                        config.ServerSettings.Port = port;
                    else
                        config.ServerSettings.Port = 2121;

                    if (int.TryParse(MaxConnectionsTextBox.Text, out int maxConn))
                        config.ServerSettings.MaxConnections = maxConn;
                    else
                        config.ServerSettings.MaxConnections = 10;

                    // Certificate configuration
                    if (FileCertButton.IsChecked == true)
                    {
                        if (string.IsNullOrWhiteSpace(CertPathTextBox.Text))
                        {
                            MessageBox.Show("Please select a certificate file.", "Error", 
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        config.ServerSettings.CertificatePath = CertPathTextBox.Text;
                        config.ServerSettings.CertificatePassword = CertPasswordBox.Password;
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
                            Read = user.ReadPermission,
                            Write = user.WritePermission
                        });
                    }
                }

                _server = new FtpsServer(_logger, config);
                _server.Start();
                
                IsServerRunning = true;
                
                var mode = SimpleModeButton.IsChecked == true ? "Simple" : "Advanced";
                MessageBox.Show($"FTPS Server started successfully in {mode} mode!", "Success", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
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
                _logger = null;
                IsServerRunning = false;
                
                MessageBox.Show("FTPS Server stopped successfully.", "Success", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error stopping server: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateServerStatus()
        {
            Dispatcher.Invoke(() =>
            {
                if (IsServerRunning)
                {
                    StatusText.Text = "Server Running";
                    StartStopButton.Content = "STOP SERVER";
                    StartStopButton.Background = (System.Windows.Media.Brush)FindResource("ErrorBrush");
                }
                else
                {
                    StatusText.Text = "Server Stopped";
                    StartStopButton.Content = "START SERVER";
                    StartStopButton.Background = (System.Windows.Media.Brush)FindResource("AccentCyanBrush");
                }
            });
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            StopServer();
            SaveSettings();
        }
    }
}
