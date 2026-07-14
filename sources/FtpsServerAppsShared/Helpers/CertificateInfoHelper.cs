using FtpsServerAppsShared.Models;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace FtpsServerAppsShared.Helpers;

public static class CertificateInfoHelper
{
    public static CertificateInfo GetInfo(X509Certificate2 cert)
    {
        return new CertificateInfo
        {
            IsSelfSigned = cert.Subject == cert.Issuer,
            Subject = cert.Subject,
            Issuer = cert.Issuer,
            ValidFrom = cert.NotBefore.ToLocalTime().ToString("yyyy-MM-dd"),
            ValidTo = cert.NotAfter.ToLocalTime().ToString("yyyy-MM-dd"),
            SerialNumber = cert.SerialNumber,
            Sha256Fingerprint = FormatFingerprint(cert.GetCertHashString(HashAlgorithmName.SHA256)),
            Sha1Fingerprint = FormatFingerprint(cert.GetCertHashString(HashAlgorithmName.SHA1)),
        };
    }

    private static string FormatFingerprint(string hex)
    {
        var upper = hex.ToUpperInvariant();
        return string.Join(":", Enumerable.Range(0, upper.Length / 2)
            .Select(i => upper.Substring(i * 2, 2)));
    }
}
