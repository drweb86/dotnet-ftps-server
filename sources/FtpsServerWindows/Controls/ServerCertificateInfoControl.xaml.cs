using FtpsServerAppsShared.Models;
using FtpsServerWindows.Resources;
using System.Windows;
using System.Windows.Controls;

namespace FtpsServerWindows.Controls
{
    public partial class ServerCertificateInfoControl : UserControl
    {
        public static readonly DependencyProperty CertInfoProperty =
            DependencyProperty.Register(nameof(CertInfo), typeof(CertificateInfo), typeof(ServerCertificateInfoControl),
                new PropertyMetadata(null, OnCertInfoChanged));

        public CertificateInfo? CertInfo
        {
            get => (CertificateInfo?)GetValue(CertInfoProperty);
            set => SetValue(CertInfoProperty, value);
        }

        public ServerCertificateInfoControl()
        {
            InitializeComponent();
        }

        private static void OnCertInfoChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((ServerCertificateInfoControl)d).UpdateDisplay();
        }

        private void UpdateDisplay()
        {
            var info = CertInfo;
            if (info == null || !info.IsSelfSigned)
            {
                Visibility = Visibility.Collapsed;
                return;
            }

            Visibility = Visibility.Visible;
            CertType.Text = Strings.CertTypeSelfSigned;
            CertSubject.Text = info.Subject;
            CertIssuer.Text = $"{info.Issuer}  {Strings.CertIssuerSelf}";
            CertValid.Text = $"{info.ValidFrom} — {info.ValidTo}";
            CertSerial.Text = info.SerialNumber;
            Sha256Text.Text = info.Sha256Fingerprint;
            Sha1Text.Text = info.Sha1Fingerprint;
        }

        private void CopySha256_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(CertInfo?.Sha256Fingerprint))
            {
                Clipboard.SetText(CertInfo.Sha256Fingerprint);
                CopySha256Button.Content = Strings.CertCopied;
                var timer = new System.Windows.Threading.DispatcherTimer { Interval = System.TimeSpan.FromSeconds(2) };
                timer.Tick += (s, _) => { CopySha256Button.Content = Strings.CertCopyButton; timer.Stop(); };
                timer.Start();
            }
        }

        private void CopySha1_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(CertInfo?.Sha1Fingerprint))
            {
                Clipboard.SetText(CertInfo.Sha1Fingerprint);
                CopySha1Button.Content = Strings.CertCopied;
                var timer = new System.Windows.Threading.DispatcherTimer { Interval = System.TimeSpan.FromSeconds(2) };
                timer.Tick += (s, _) => { CopySha1Button.Content = Strings.CertCopyButton; timer.Stop(); };
                timer.Start();
            }
        }
    }
}
