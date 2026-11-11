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

    // A CipherSuitesPolicy is not installed on any platform. .NET's OpenSSL backend
    // turns the TLS session cache off when one is set, and a protected data connection
    // has to resume the control connection. The negotiated suite is still checked
    // against Allowed after the handshake, on Linux and macOS as well as Windows.
    public static bool IsAllowed(TlsCipherSuite suite)
    {
        foreach (var allowed in Allowed)
        {
            if (allowed == suite)
                return true;
        }

        return false;
    }
}
