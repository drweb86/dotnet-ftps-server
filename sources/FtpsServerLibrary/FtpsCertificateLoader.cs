using System;
using System.Diagnostics;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace FtpsServerLibrary;

class FtpsCertificateLoader(IFtpsServerLog log, IFtpsServerFileSystemProvider fileSystem)
{
    private readonly IFtpsServerLog _log = log;
    private readonly IFtpsServerFileSystemProvider _fileSystem = fileSystem;

    public async Task<X509Certificate2> LoadAsync(FtpsServerSettings settings)
    {
        if (settings.X509Certificate is not null)
        {
            _log.Info($"Loading certificate from X509Certificate.");
            return settings.X509Certificate;
        }

        if (settings.CertificateBytes is not null)
        {
            _log.Info($"Loading certificate from CertificateBytes.");
            return X509CertificateLoader.LoadCertificate(settings.CertificateBytes);
        }

        if (settings.CertificatePkcs12Bytes is not null)
        {
            _log.Info($"Loading certificate from CertificatePkcs12Bytes.");
            return X509CertificateLoader.LoadPkcs12(settings.CertificatePkcs12Bytes, settings.CertificatePassword);
        }

        if (!string.IsNullOrEmpty(settings.CertificatePath))
        {
            _log.Info($"Loading certificate from {settings.CertificatePath}");

            var fileName = await _fileSystem.GetFileName(settings.CertificatePath);
            var extension = System.IO.Path.GetExtension(fileName);
            if (extension is null)
            {
                var exception = new System.IO.InvalidDataException($"Certificate path extension {extension} is not recognizable! CertificatePath extension must end with .pem, .der, .pfx.");
                _log.Fatal(exception, exception.Message);
                throw exception;
            }

            switch (extension.ToLower())
            {
                case ".pem":
                case ".der":
                    return X509CertificateLoader.LoadCertificateFromFile(settings.CertificatePath);

                case ".pfx":
                    return X509CertificateLoader.LoadPkcs12FromFile(settings.CertificatePath, settings.CertificatePassword);

                default:
                    var exception = new System.IO.InvalidDataException($"Certificate path extension {extension} is not recognizable! CertificatePath extension must end with .pem, .der, .pfx.");
                    _log.Fatal(exception, exception.Message);
                    throw exception;
            }
        }

        if (settings.CertificateStoreLocation is not null &&
            settings.CertificateStoreName is not null &&
            settings.CertificateStoreSubject is not null)
        {
            _log.Info($"Loading certificate from Certificate Store StoreName={settings.CertificateStoreName} Location={settings.CertificateStoreLocation} Subject={settings.CertificateStoreSubject}");

            using var store = new X509Store(settings.CertificateStoreName.Value, settings.CertificateStoreLocation.Value);
            store.Open(OpenFlags.ReadOnly);
            var certs = store.Certificates.Find(
                X509FindType.FindBySubjectName,
                settings.CertificateStoreSubject,
                false);

            if (certs.Count == 1)
            {
                _log.Info($"Certificate was found.");
                return certs[0];
            }
            if (certs.Count > 1)
            {
                var exception = new System.IO.InvalidDataException($"More than 1 certificate found!");
                _log.Fatal(exception, exception.Message);
                throw exception;
            }
            else
            {
                var exception = new System.IO.InvalidDataException($"No certificates found!");
                _log.Fatal(exception, exception.Message);
                throw exception;
            }
        }

        _log.Info($"Getting or creating self-signed certificate.");
        return GetOrCreateCertificate(settings);
    }

    private static X509Certificate2 CreateSelfSignedServerCertificate(string password)
    {
        SubjectAlternativeNameBuilder sanBuilder = new();
        sanBuilder.AddIpAddress(IPAddress.Loopback);
        sanBuilder.AddIpAddress(IPAddress.IPv6Loopback);
        sanBuilder.AddDnsName("localhost");
        sanBuilder.AddDnsName(Environment.MachineName);

        X500DistinguishedName distinguishedName = new($"CN=FtpsServerLibrary-SelfSigned-Certificates");

        using RSA rsa = RSA.Create(2048);
        var request = new CertificateRequest(distinguishedName, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        request.CertificateExtensions.Add(
            new X509KeyUsageExtension(X509KeyUsageFlags.DataEncipherment | X509KeyUsageFlags.KeyEncipherment | X509KeyUsageFlags.DigitalSignature, false));


        request.CertificateExtensions.Add(
           new X509EnhancedKeyUsageExtension(
               [new Oid("1.3.6.1.5.5.7.3.1")], false));

        request.CertificateExtensions.Add(sanBuilder.Build());

        var certificate = request.CreateSelfSigned(new DateTimeOffset(DateTime.UtcNow.AddDays(-1)), new DateTimeOffset(DateTime.UtcNow.AddDays(3650)));
        return X509CertificateLoader.LoadPkcs12(certificate.Export(X509ContentType.Pfx, password), password, CreateKeyStorageFlags);
    }

    private X509Certificate2 GetOrCreateCertificate(FtpsServerSettings ftpsServerSettings)
    {
        var directory = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FtpsServerLibrary",
            OperatingSystem.IsAndroid() ? "Certificates-Android-V1" : "Certificates");
        System.IO.Directory.CreateDirectory(directory);

        var certificateFile = System.IO.Path.Combine(directory, "Self-Signed.pfx")!;
        var passwordFile = System.IO.Path.Combine(directory, "Self-Signed.password");
        var callerPassword = ftpsServerSettings.CertificatePassword;
        var protectPassword = (OperatingSystem.IsWindows() || OperatingSystem.IsLinux() || OperatingSystem.IsAndroid()) &&
            string.IsNullOrEmpty(callerPassword);
        const string legacyPassword = "test";

        string? storedPassword = null;
        if (protectPassword && System.IO.File.Exists(passwordFile))
        {
            try
            {
                storedPassword = FtpsCertificatePasswordProtector.Unprotect(System.IO.File.ReadAllText(passwordFile));
            }
            catch (Exception e)
            {
                Trace.TraceError(e.Message);
            }
        }

        var password = protectPassword ? storedPassword : (callerPassword ?? legacyPassword);
        X509Certificate2? certificate = null;
        var migratedFromLegacy = false;

        if (System.IO.File.Exists(certificateFile))
        {
            _log.Info($"Loading self-signed certificate from file {certificateFile}.");
            if (password is not null)
                certificate = TryLoadPkcs12(certificateFile, password);

            if (certificate is null && protectPassword)
            {
                certificate = TryLoadPkcs12(certificateFile, legacyPassword);
                migratedFromLegacy = certificate is not null;
            }

            if (certificate is not null && IsCertificateCurrent(certificate) && !migratedFromLegacy)
                return certificate;

            if (certificate is not null && IsCertificateCurrent(certificate) && migratedFromLegacy)
            {
                password = CreateProtectedPassword(passwordFile);
                var exported = certificate.Export(X509ContentType.Pfx, password);
                System.IO.File.WriteAllBytes(certificateFile, exported);
                _log.Info($"Re-protected self-signed certificate {certificateFile}.");
                return certificate;
            }

            certificate?.Dispose();
            certificate = null;
        }

        if (protectPassword && string.IsNullOrEmpty(password))
            password = CreateProtectedPassword(passwordFile);

        _log.Info($"Creating self-signed certificate for file {certificateFile}.");
        certificate = CreateSelfSignedServerCertificate(password!);
        var pfxBytes = certificate.Export(X509ContentType.Pfx, password);
        System.IO.File.WriteAllBytes(certificateFile, pfxBytes);

        return certificate;
    }

    private static bool IsCertificateCurrent(X509Certificate2 certificate)
    {
        return certificate.NotAfter > DateTime.UtcNow.AddDays(7) &&
            certificate.NotBefore <= DateTime.UtcNow;
    }

    private static X509KeyStorageFlags CreateKeyStorageFlags =>
        OperatingSystem.IsWindows() || OperatingSystem.IsLinux()
            ? X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet
            : X509KeyStorageFlags.Exportable;

    private static X509KeyStorageFlags LoadKeyStorageFlags =>
        OperatingSystem.IsWindows() || OperatingSystem.IsLinux()
            ? X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet
            : X509KeyStorageFlags.Exportable;

    private X509Certificate2? TryLoadPkcs12(string certificateFile, string password)
    {
        try
        {
            return X509CertificateLoader.LoadPkcs12FromFile(certificateFile, password, LoadKeyStorageFlags);
        }
        catch (Exception e)
        {
            Trace.TraceError(e.Message);
            return null;
        }
    }

    private static string CreateProtectedPassword(string passwordFile)
    {
        var password = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        System.IO.File.WriteAllText(passwordFile, FtpsCertificatePasswordProtector.Protect(password));
        return password;
    }
}
