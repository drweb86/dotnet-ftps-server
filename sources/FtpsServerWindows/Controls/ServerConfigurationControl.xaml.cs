using FtpsServerWindows.Models;
using FtpsServerWindows.Resources;
using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;

namespace FtpsServerWindows.Controls;

public partial class ServerConfigurationControl : UserControl
{
    public const int PortMinimum = 2121;
    public const int PortMaximum = 65535;
    public const int MaxConnectionsMinimum = 2;

    public static readonly DependencyProperty PortProperty =
        DependencyProperty.Register(nameof(Port), typeof(int), typeof(ServerConfigurationControl),
            new FrameworkPropertyMetadata(PortMinimum, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnPortChanged));

    public static readonly DependencyProperty MaxConnectionsProperty =
        DependencyProperty.Register(nameof(MaxConnections), typeof(int), typeof(ServerConfigurationControl),
            new FrameworkPropertyMetadata(10, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnMaxConnectionsChanged));

    public static readonly DependencyProperty CertificateSourceProperty =
        DependencyProperty.Register(nameof(CertificateSource), typeof(CertificateSourceType), typeof(ServerConfigurationControl),
            new FrameworkPropertyMetadata(CertificateSourceType.SelfSigned, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnCertificateSourceChanged));

    public static readonly DependencyProperty CertificateUserProvidedVisibilityProperty =
        DependencyProperty.Register(nameof(CertificateUserProvidedVisibility), typeof(Visibility), typeof(ServerConfigurationControl),
            new FrameworkPropertyMetadata(Visibility.Collapsed));

    public static readonly DependencyProperty CertificateIsSelfSignedProperty =
        DependencyProperty.Register(nameof(CertificateIsSelfSigned), typeof(bool), typeof(ServerConfigurationControl),
            new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnCertificateIsSelfSignedChanged));

    public static readonly DependencyProperty CertificatePathProperty =
        DependencyProperty.Register(nameof(CertificatePath), typeof(string), typeof(ServerConfigurationControl),
            new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnCertificatePathChanged));

    public static readonly DependencyProperty CertificatePasswordProperty =
        DependencyProperty.Register(nameof(CertificatePassword), typeof(string), typeof(ServerConfigurationControl),
            new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public static readonly DependencyProperty PortErrorProperty =
        DependencyProperty.Register(nameof(PortError), typeof(string), typeof(ServerConfigurationControl));

    public static readonly DependencyProperty MaxConnectionsErrorProperty =
        DependencyProperty.Register(nameof(MaxConnectionsError), typeof(string), typeof(ServerConfigurationControl));

    public static readonly DependencyProperty CertificatePathErrorProperty =
        DependencyProperty.Register(nameof(CertificatePathError), typeof(string), typeof(ServerConfigurationControl));

    public int Port { get => (int)GetValue(PortProperty); set => SetValue(PortProperty, value); }
    public int MaxConnections { get => (int)GetValue(MaxConnectionsProperty); set => SetValue(MaxConnectionsProperty, value); }
    public CertificateSourceType CertificateSource { get => (CertificateSourceType)GetValue(CertificateSourceProperty); set => SetValue(CertificateSourceProperty, value); }
    public Visibility CertificateUserProvidedVisibility { get => (Visibility)GetValue(CertificateUserProvidedVisibilityProperty); set => SetValue(CertificateUserProvidedVisibilityProperty, value); }
    public bool CertificateIsSelfSigned { get => (bool)GetValue(CertificateIsSelfSignedProperty); set => SetValue(CertificateIsSelfSignedProperty, value); }
    public string CertificatePath { get => (string)GetValue(CertificatePathProperty); set => SetValue(CertificatePathProperty, value); }
    public string CertificatePassword { get => (string)GetValue(CertificatePasswordProperty); set => SetValue(CertificatePasswordProperty, value); }
    public string? PortError { get => (string?)GetValue(PortErrorProperty); set => SetValue(PortErrorProperty, value); }
    public string? MaxConnectionsError { get => (string?)GetValue(MaxConnectionsErrorProperty); set => SetValue(MaxConnectionsErrorProperty, value); }
    public string? CertificatePathError { get => (string?)GetValue(CertificatePathErrorProperty); set => SetValue(CertificatePathErrorProperty, value); }

    public ServerConfigurationControl()
    {
        InitializeComponent();
        RefreshCertificateUserProvidedVisibility();
    }

    private static void OnPortChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((ServerConfigurationControl)d).PortError = null;

    private static void OnMaxConnectionsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((ServerConfigurationControl)d).MaxConnectionsError = null;

    private static void OnCertificatePathChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((ServerConfigurationControl)d).CertificatePathError = null;

    private static void OnCertificateSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (ServerConfigurationControl)d;
        control.CertificateIsSelfSigned = control.CertificateSource == CertificateSourceType.SelfSigned;
        control.RefreshCertificateUserProvidedVisibility();
        control.CertificatePathError = null;
    }

    private static void OnCertificateIsSelfSignedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (ServerConfigurationControl)d;
        control.CertificateSource = control.CertificateIsSelfSigned ? CertificateSourceType.SelfSigned : CertificateSourceType.FromFile;
        control.RefreshCertificateUserProvidedVisibility();
        control.CertificatePathError = null;
    }

    private void RefreshCertificateUserProvidedVisibility()
    {
        CertificateUserProvidedVisibility = CertificateSource == CertificateSourceType.FromFile ? Visibility.Visible : Visibility.Collapsed;
    }

    private void BrowseCertificate_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = Strings.ConfigCertFilter,
            Title = Strings.ConfigSelectCertTitle
        };

        if (dialog.ShowDialog() == true)
            CertificatePath = dialog.FileName;
    }

    public string? Validate()
    {
        PortError = Port < PortMinimum || Port > PortMaximum
            ? string.Format(Strings.ConfigPortValidation, PortMinimum, PortMaximum)
            : null;
        MaxConnectionsError = MaxConnections < MaxConnectionsMinimum
            ? string.Format(Strings.ConfigMaxConnectionsValidation, MaxConnectionsMinimum)
            : null;
        CertificatePathError = CertificateSource == CertificateSourceType.FromFile && string.IsNullOrWhiteSpace(CertificatePath)
            ? Strings.ErrorSelectCertificate
            : null;
        return PortError ?? MaxConnectionsError ?? CertificatePathError;
    }
}
