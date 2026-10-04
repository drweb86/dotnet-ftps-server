using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace FtpsServerLibrary;

/// <summary>
/// Protects the auto-generated PFX password with an OS-backed key.
/// Windows uses DPAPI scoped to the current user. Linux uses AES-256 with a key
/// derived from the machine id, so a copied certificate file cannot be opened elsewhere.
/// Android uses the same Keystore hook as settings secrets.
/// </summary>
static class FtpsCertificatePasswordProtector
{
    private const string Prefix = "enc::";

    public static string Protect(string value)
    {
        var protectedBytes = ProtectBytes(Encoding.UTF8.GetBytes(value));
        return Prefix + Convert.ToBase64String(protectedBytes);
    }

    public static string Unprotect(string value)
    {
        if (!value.StartsWith(Prefix, StringComparison.Ordinal))
            return value;

        var protectedBytes = Convert.FromBase64String(value[Prefix.Length..]);
        return Encoding.UTF8.GetString(UnprotectBytes(protectedBytes));
    }

    private static byte[] ProtectBytes(byte[] plainBytes)
    {
        if (OperatingSystem.IsAndroid())
            return AndroidSecretProtection.ProtectBytes(plainBytes);

        if (OperatingSystem.IsWindows())
            return FtpsDpapi.Protect(plainBytes);

        if (OperatingSystem.IsLinux())
            return ProtectWithMachineKey(plainBytes);

        throw new PlatformNotSupportedException("Certificate password protection is available on Windows, Linux, and Android.");
    }

    private static byte[] UnprotectBytes(byte[] protectedBytes)
    {
        if (OperatingSystem.IsAndroid())
            return AndroidSecretProtection.UnprotectBytes(protectedBytes);

        if (OperatingSystem.IsWindows())
            return FtpsDpapi.Unprotect(protectedBytes);

        if (OperatingSystem.IsLinux())
            return UnprotectWithMachineKey(protectedBytes);

        throw new PlatformNotSupportedException("Certificate password protection is available on Windows, Linux, and Android.");
    }

    private static byte[] ProtectWithMachineKey(byte[] plainBytes)
    {
        using var aes = Aes.Create();
        aes.Key = LinuxKey.Value;
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor();
        var encrypted = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

        var result = new byte[aes.IV.Length + encrypted.Length];
        Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
        Buffer.BlockCopy(encrypted, 0, result, aes.IV.Length, encrypted.Length);
        return result;
    }

    private static byte[] UnprotectWithMachineKey(byte[] protectedBytes)
    {
        using var aes = Aes.Create();
        aes.Key = LinuxKey.Value;

        var ivLength = aes.BlockSize / 8;
        if (protectedBytes.Length <= ivLength)
            throw new CryptographicException("Protected secret is too short.");

        var iv = new byte[ivLength];
        Buffer.BlockCopy(protectedBytes, 0, iv, 0, iv.Length);
        aes.IV = iv;

        var ciphertext = new byte[protectedBytes.Length - iv.Length];
        Buffer.BlockCopy(protectedBytes, iv.Length, ciphertext, 0, ciphertext.Length);

        using var decryptor = aes.CreateDecryptor();
        return decryptor.TransformFinalBlock(ciphertext, 0, ciphertext.Length);
    }

    private static readonly Lazy<byte[]> LinuxKey = new(DeriveLinuxKey);

    private static byte[] DeriveLinuxKey()
    {
        var machineId = GetMachineId();
        var salt = Encoding.UTF8.GetBytes("SiarheiKuchuk.FtpsServerLibrary.v1");
        return Rfc2898DeriveBytes.Pbkdf2(machineId, salt, 100_000, HashAlgorithmName.SHA256, 32);
    }

    private static byte[] GetMachineId()
    {
        const string machineIdPath = "/etc/machine-id";
        if (File.Exists(machineIdPath))
        {
            var content = File.ReadAllText(machineIdPath).Trim();
            if (content.Length > 0)
                return Encoding.UTF8.GetBytes(content);
        }

        const string dbusIdPath = "/var/lib/dbus/machine-id";
        if (File.Exists(dbusIdPath))
        {
            var content = File.ReadAllText(dbusIdPath).Trim();
            if (content.Length > 0)
                return Encoding.UTF8.GetBytes(content);
        }

        throw new InvalidOperationException(
            "Cannot find machine-id. Ensure /etc/machine-id or /var/lib/dbus/machine-id exists.");
    }
}
