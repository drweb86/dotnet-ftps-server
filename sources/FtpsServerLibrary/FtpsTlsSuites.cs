using System;
using System.Net.Security;

namespace FtpsServerLibrary;

static class FtpsTlsSuites
{
    private static readonly TlsCipherSuite[] Allowed =
    [
        TlsCipherSuite.TLS_AES_128_GCM_SHA256,
        TlsCipherSuite.TLS_AES_256_GCM_SHA384,
        TlsCipherSuite.TLS_CHACHA20_POLY1305_SHA256,
        TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_128_GCM_SHA256,
        TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_256_GCM_SHA384,
        TlsCipherSuite.TLS_ECDHE_RSA_WITH_AES_128_GCM_SHA256,
        TlsCipherSuite.TLS_ECDHE_RSA_WITH_AES_256_GCM_SHA384,
        TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_CHACHA20_POLY1305_SHA256,
        TlsCipherSuite.TLS_ECDHE_RSA_WITH_CHACHA20_POLY1305_SHA256,
        TlsCipherSuite.TLS_DHE_RSA_WITH_AES_128_GCM_SHA256,
        TlsCipherSuite.TLS_DHE_RSA_WITH_AES_256_GCM_SHA384,
        TlsCipherSuite.TLS_DHE_RSA_WITH_CHACHA20_POLY1305_SHA256,
    ];

    // Windows and Android cannot apply a per-connection suite list. Those platforms
    // still refuse a negotiated suite that is not in Allowed.
    public static CipherSuitesPolicy? Policy { get; } = CreatePolicy();

    public static bool IsAllowed(TlsCipherSuite suite)
    {
        foreach (var allowed in Allowed)
        {
            if (allowed == suite)
                return true;
        }

        return false;
    }

    private static CipherSuitesPolicy? CreatePolicy()
    {
        if (OperatingSystem.IsWindows() || OperatingSystem.IsAndroid())
            return null;
        if (!OperatingSystem.IsLinux() && !OperatingSystem.IsMacOS())
            return null;

        try
        {
            return new CipherSuitesPolicy(Allowed);
        }
        catch (PlatformNotSupportedException)
        {
            return null;
        }
    }
}
