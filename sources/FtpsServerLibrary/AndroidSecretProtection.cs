using System;

namespace FtpsServerLibrary;

/// <summary>
/// Android Keystore hooks registered by the Android app before secrets are read.
/// The shared libraries target net10.0 and cannot reference the Android SDK.
/// </summary>
public static class AndroidSecretProtection
{
    public static Func<byte[], byte[]>? Protect { get; set; }

    public static Func<byte[], byte[]>? Unprotect { get; set; }

    public static byte[] ProtectBytes(byte[] plainBytes)
    {
        var protect = Protect ?? throw new PlatformNotSupportedException(
            "Android secret protection is not registered.");
        return protect(plainBytes);
    }

    public static byte[] UnprotectBytes(byte[] protectedBytes)
    {
        var unprotect = Unprotect ?? throw new PlatformNotSupportedException(
            "Android secret protection is not registered.");
        return unprotect(protectedBytes);
    }
}
