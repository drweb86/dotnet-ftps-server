using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using FtpsServerApp.Helpers;
using FtpsServerAvalonia.Models;
using System;
using System.Linq;

namespace FtpsServerAvalonia.Controls
{
    public partial class ServerConfigurationControl : UserControl
    {
        public static readonly StyledProperty<int> PortProperty =
            AvaloniaProperty.Register<ServerConfigurationControl, int>(nameof(Port), defaultValue: 2121, defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

        public static readonly StyledProperty<int> MaxConnectionsProperty =
            AvaloniaProperty.Register<ServerConfigurationControl, int>(nameof(MaxConnections), defaultValue: 10, defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

        public static readonly StyledProperty<CertificateSourceType> CertificateSourceProperty =
            AvaloniaProperty.Register<ServerConfigurationControl, CertificateSourceType>(nameof(CertificateSource), defaultValue: CertificateSourceType.SelfSigned, defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

        public static readonly StyledProperty<bool> CertificateUserProvidedVisibilityProperty =
            AvaloniaProperty.Register<ServerConfigurationControl, bool>(nameof(CertificateUserProvidedVisibility), defaultValue: false);

        public static readonly StyledProperty<string> CertificatePathProperty =
            AvaloniaProperty.Register<ServerConfigurationControl, string>(nameof(CertificatePath), defaultValue: string.Empty, defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

        public static readonly StyledProperty<string> CertificatePasswordProperty =
            AvaloniaProperty.Register<ServerConfigurationControl, string>(nameof(CertificatePassword), defaultValue: string.Empty, defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

        public int Port
        {
            get => GetValue(PortProperty);
            set => SetValue(PortProperty, value);
        }

        public int MaxConnections
        {
            get => GetValue(MaxConnectionsProperty);
            set => SetValue(MaxConnectionsProperty, value);
        }

        public CertificateSourceType CertificateSource
        {
            get => GetValue(CertificateSourceProperty);
            set
            {
                SetValue(CertificateSourceProperty, value);
                CertificateUserProvidedVisibility = value == CertificateSourceType.FromFile;
            }
        }

        public bool CertificateUserProvidedVisibility
        {
            get => GetValue(CertificateUserProvidedVisibilityProperty);
            set => SetValue(CertificateUserProvidedVisibilityProperty, value);
        }

        public string CertificatePath
        {
            get => GetValue(CertificatePathProperty);
            set => SetValue(CertificatePathProperty, value);
        }

        public string CertificatePassword
        {
            get => GetValue(CertificatePasswordProperty);
            set => SetValue(CertificatePasswordProperty, value);
        }

        public ServerConfigurationControl()
        {
            InitializeComponent();

            PCName.Text = Environment.MachineName;
            NetworkIpsControl.ItemsSource = NetworkHelper.GetMyLocalIps();

            PortControl.Value = Port;
            MaxConnectionsControl.Value = MaxConnections;

            this.AttachedToVisualTree += (s, e) =>
            {
                PortControl.Value = Port;
                MaxConnectionsControl.Value = MaxConnections;
            };
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == PortProperty && PortControl != null)
            {
                PortControl.Value = change.GetNewValue<int>();
            }
            else if (change.Property == MaxConnectionsProperty && MaxConnectionsControl != null)
            {
                MaxConnectionsControl.Value = change.GetNewValue<int>();
            }
            else if (change.Property == CertificateSourceProperty && SelfSignedCertButton != null)
            {
                var newValue = change.GetNewValue<CertificateSourceType>();
                SelfSignedCertButton.IsChecked = newValue == CertificateSourceType.SelfSigned;
            }
            else if (change.Property == CertificatePathProperty && CertPathTextBox != null)
            {
                CertPathTextBox.Text = change.GetNewValue<string>();
            }
        }

        private void CertificateSourceChanged(object? sender, RoutedEventArgs e)
        {
            if (SelfSignedCertButton.IsChecked == true)
                CertificateSource = CertificateSourceType.SelfSigned;
            else
                CertificateSource = CertificateSourceType.FromFile;
        }

        private async void BrowseCertificate_Click(object? sender, RoutedEventArgs e)
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) return;

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Select Certificate File",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("Certificate Files") { Patterns = new[] { "*.pfx", "*.pem", "*.der" } },
                    new FilePickerFileType("All Files") { Patterns = new[] { "*.*" } }
                }
            });

            if (files.Count > 0)
            {
                CertificatePath = files[0].Path.LocalPath;
                CertPathTextBox.Text = files[0].Path.LocalPath;
            }
        }
    }
}
