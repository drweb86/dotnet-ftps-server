using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Threading;
using FtpsServerAppsShared.Models;
using FtpsServerAvalonia.Resources;
using System;
using System.Threading.Tasks;

namespace FtpsServerAvalonia.Controls
{
    public partial class ServerCertificateInfoControl : UserControl
    {
        public static readonly StyledProperty<CertificateInfo?> CertInfoProperty =
            AvaloniaProperty.Register<ServerCertificateInfoControl, CertificateInfo?>(nameof(CertInfo));

        public CertificateInfo? CertInfo
        {
            get => GetValue(CertInfoProperty);
            set => SetValue(CertInfoProperty, value);
        }

        public ServerCertificateInfoControl()
        {
            InitializeComponent();
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);
            if (change.Property == CertInfoProperty)
                UpdateDisplay();
        }

        private void UpdateDisplay()
        {
            var info = CertInfo;
            if (info == null || !info.IsSelfSigned)
            {
                IsVisible = false;
                return;
            }

            IsVisible = true;
            CertType.Text = Strings.CertTypeSelfSigned;
            CertSubject.Text = info.Subject;
            CertIssuer.Text = $"{info.Issuer}  {Strings.CertIssuerSelf}";
            CertValid.Text = $"{info.ValidFrom} — {info.ValidTo}";
            CertSerial.Text = info.SerialNumber;
            Sha256Text.Text = info.Sha256Fingerprint;
            Sha1Text.Text = info.Sha1Fingerprint;
        }

        private async void CopySha256_Click(object? sender, RoutedEventArgs e)
        {
            var text = CertInfo?.Sha256Fingerprint;
            if (string.IsNullOrEmpty(text)) return;

            await (TopLevel.GetTopLevel(this)?.Clipboard?.SetTextAsync(text) ?? Task.CompletedTask);

            CopySha256Button.Content = Strings.CertCopied;
            DispatcherTimer.RunOnce(() => CopySha256Button.Content = Strings.CertCopyButton, TimeSpan.FromSeconds(2));
        }

        private async void CopySha1_Click(object? sender, RoutedEventArgs e)
        {
            var text = CertInfo?.Sha1Fingerprint;
            if (string.IsNullOrEmpty(text)) return;

            await (TopLevel.GetTopLevel(this)?.Clipboard?.SetTextAsync(text) ?? Task.CompletedTask);

            CopySha1Button.Content = Strings.CertCopied;
            DispatcherTimer.RunOnce(() => CopySha1Button.Content = Strings.CertCopyButton, TimeSpan.FromSeconds(2));
        }
    }
}
