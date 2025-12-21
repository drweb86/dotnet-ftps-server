using FtpsServerApp.Helpers;
using FtpsServerApp.Models;
using Microsoft.Win32;
using System;
using System.Windows;
using System.Windows.Controls;

namespace FtpsServerApp.Controls
{
    public partial class ServerConfigurationControl : UserControl
    {
        public static readonly DependencyProperty PortProperty =
            DependencyProperty.Register(nameof(Port), typeof(int), typeof(ServerConfigurationControl),
                new FrameworkPropertyMetadata(2121, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnPortChanged));

        public static readonly DependencyProperty MaxConnectionsProperty =
            DependencyProperty.Register(nameof(MaxConnections), typeof(int), typeof(ServerConfigurationControl),
                new FrameworkPropertyMetadata(10, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnMaxConnectionsChanged));

        public static readonly DependencyProperty CertificateSourceProperty =
            DependencyProperty.Register(nameof(CertificateSource), typeof(CertificateSourceType), typeof(ServerConfigurationControl),
                new FrameworkPropertyMetadata(CertificateSourceType.SelfSigned, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnCertificateSourceChanged));

        public static readonly DependencyProperty CertificateUserProvidedVisibilityProperty =
            DependencyProperty.Register(nameof(CertificateUserProvidedVisibility), typeof(Visibility), typeof(ServerConfigurationControl),
                new FrameworkPropertyMetadata(Visibility.Hidden));

        public static readonly DependencyProperty CertificatePathProperty =
            DependencyProperty.Register(nameof(CertificatePath), typeof(string), typeof(ServerConfigurationControl),
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnCertificatePathChanged));

        public static readonly DependencyProperty CertificatePasswordProperty =
            DependencyProperty.Register(nameof(CertificatePassword), typeof(string), typeof(ServerConfigurationControl),
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

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
            set
            {
                SetValue(CertificateSourceProperty, value);
                CertificateUserProvidedVisibility = value == CertificateSourceType.FromFile ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public Visibility CertificateUserProvidedVisibility
        {
            get => (Visibility)GetValue(CertificateUserProvidedVisibilityProperty);
            set => SetValue(CertificateUserProvidedVisibilityProperty, value);
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

        public ServerConfigurationControl()
        {
            InitializeComponent();

            PCName.Text = Environment.MachineName;
            NetworkIpsControl.ItemsSource = NetworkHelper.GetMyLocalIps();

            PortControl.Value = Port;
            MaxConnectionsControl.Value = MaxConnections;

            PortControl.Loaded += (s, e) => PortControl.Value = Port;
            MaxConnectionsControl.Loaded += (s, e) => MaxConnectionsControl.Value = MaxConnections;
        }

        private static void OnPortChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (ServerConfigurationControl)d;
            if (control.PortControl != null)
                control.PortControl.Value = (int)e.NewValue;
        }

        private static void OnMaxConnectionsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (ServerConfigurationControl)d;
            if (control.MaxConnectionsControl != null)
                control.MaxConnectionsControl.Value = (int)e.NewValue;
        }

        private static void OnCertificateSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (ServerConfigurationControl)d;
            var newValue = (CertificateSourceType)e.NewValue;

            if (control.SelfSignedCertButton != null && control.SelfSignedCertButton != null)
            {
                if (newValue == CertificateSourceType.SelfSigned)
                    control.SelfSignedCertButton.IsChecked = true;
                else
                    control.SelfSignedCertButton.IsChecked = false;
            }
        }

        private static void OnCertificatePathChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (ServerConfigurationControl)d;
            if (control.CertPathTextBox != null)
                control.CertPathTextBox.Text = (string)e.NewValue;
        }

        private void CertificateSourceChanged(object sender, RoutedEventArgs e)
        {
            if (SelfSignedCertButton.IsChecked == true)
                CertificateSource = CertificateSourceType.SelfSigned;
            else
                CertificateSource = CertificateSourceType.FromFile;
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
                CertificatePath = dialog.FileName;
                CertPathTextBox.Text = dialog.FileName;
            }
        }
    }
}
