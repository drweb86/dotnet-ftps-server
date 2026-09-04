using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using FtpsServerAvalonia.Models;
using FtpsServerAvalonia.Resources;
using System.Runtime.InteropServices;

namespace FtpsServerAvalonia.Controls;

public partial class ServerConfigurationControl : UserControl
{
    public const int PortMinimum = 2121;
    public const int PortMaximum = 65535;
    public const int MaxConnectionsMinimum = 2;

    public static readonly StyledProperty<int> PortProperty =
        AvaloniaProperty.Register<ServerConfigurationControl, int>(nameof(Port), defaultValue: PortMinimum, defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public static readonly StyledProperty<int> MaxConnectionsProperty =
        AvaloniaProperty.Register<ServerConfigurationControl, int>(nameof(MaxConnections), defaultValue: 10, defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public static readonly StyledProperty<CertificateSourceType> CertificateSourceProperty =
        AvaloniaProperty.Register<ServerConfigurationControl, CertificateSourceType>(nameof(CertificateSource), defaultValue: CertificateSourceType.SelfSigned, defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public static readonly StyledProperty<bool> CertificateUserProvidedVisibilityProperty =
        AvaloniaProperty.Register<ServerConfigurationControl, bool>(nameof(CertificateUserProvidedVisibility), defaultValue: false);

    public static readonly StyledProperty<bool> CertificateIsSelfSignedProperty =
        AvaloniaProperty.Register<ServerConfigurationControl, bool>(nameof(CertificateIsSelfSigned), defaultValue: true, defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public static readonly StyledProperty<string> CertificatePathProperty =
        AvaloniaProperty.Register<ServerConfigurationControl, string>(nameof(CertificatePath), defaultValue: string.Empty, defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public static readonly StyledProperty<string> CertificatePasswordProperty =
        AvaloniaProperty.Register<ServerConfigurationControl, string>(nameof(CertificatePassword), defaultValue: string.Empty, defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public static readonly StyledProperty<string?> PortErrorProperty =
        AvaloniaProperty.Register<ServerConfigurationControl, string?>(nameof(PortError));

    public static readonly StyledProperty<string?> MaxConnectionsErrorProperty =
        AvaloniaProperty.Register<ServerConfigurationControl, string?>(nameof(MaxConnectionsError));

    public static readonly StyledProperty<string?> CertificatePathErrorProperty =
        AvaloniaProperty.Register<ServerConfigurationControl, string?>(nameof(CertificatePathError));

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
        set => SetValue(CertificateSourceProperty, value);
    }

    public bool CertificateUserProvidedVisibility
    {
        get => GetValue(CertificateUserProvidedVisibilityProperty);
        set => SetValue(CertificateUserProvidedVisibilityProperty, value);
    }

    public bool CertificateIsSelfSigned
    {
        get => GetValue(CertificateIsSelfSignedProperty);
        set => SetValue(CertificateIsSelfSignedProperty, value);
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

    public string? PortError
    {
        get => GetValue(PortErrorProperty);
        set => SetValue(PortErrorProperty, value);
    }

    public string? MaxConnectionsError
    {
        get => GetValue(MaxConnectionsErrorProperty);
        set => SetValue(MaxConnectionsErrorProperty, value);
    }

    public string? CertificatePathError
    {
        get => GetValue(CertificatePathErrorProperty);
        set => SetValue(CertificatePathErrorProperty, value);
    }

    public ServerConfigurationControl()
    {
        InitializeComponent();
        RefreshCertificateUserProvidedVisibility();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == PortProperty)
            PortError = null;
        else if (change.Property == MaxConnectionsProperty)
            MaxConnectionsError = null;
        else if (change.Property == CertificatePathProperty)
            CertificatePathError = null;
        else if (change.Property == CertificateSourceProperty)
        {
            CertificateIsSelfSigned = CertificateSource == CertificateSourceType.SelfSigned;
            RefreshCertificateUserProvidedVisibility();
            CertificatePathError = null;
        }
        else if (change.Property == CertificateIsSelfSignedProperty)
        {
            CertificateSource = CertificateIsSelfSigned ? CertificateSourceType.SelfSigned : CertificateSourceType.FromFile;
            RefreshCertificateUserProvidedVisibility();
            CertificatePathError = null;
        }
    }

    private void RefreshCertificateUserProvidedVisibility()
    {
        CertificateUserProvidedVisibility = CertificateSource == CertificateSourceType.FromFile;
        var canUseCustomCert = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ||
            RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
        if (!canUseCustomCert)
        {
            CertificateIsSelfSigned = true;
            CertificateSource = CertificateSourceType.SelfSigned;
            CertificateUserProvidedVisibility = false;
        }
    }

    private async void BrowseCertificate_Click(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = Strings.ConfigSelectCertTitle,
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("Certificate Files") { Patterns = ["*.pfx", "*.pem", "*.der"] },
                new FilePickerFileType("All Files") { Patterns = ["*.*"] }
            ]
        });

        if (files.Count > 0)
            CertificatePath = files[0].Path.LocalPath;
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
