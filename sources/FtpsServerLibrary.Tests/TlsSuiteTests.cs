using System.Net.Security;

namespace FtpsServerLibrary.Tests;

public class TlsSuiteTests
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

    [Fact]
    public void OnlyTheForwardSecretAeadAllowlistIsAccepted()
    {
        var allowed = new HashSet<TlsCipherSuite>(Allowed);
        foreach (var suite in Enum.GetValues<TlsCipherSuite>())
            Assert.Equal(allowed.Contains(suite), FtpsTlsSuites.IsAllowed(suite));
    }

    [Theory]
    [InlineData(TlsCipherSuite.TLS_RSA_WITH_AES_128_GCM_SHA256)]
    [InlineData(TlsCipherSuite.TLS_ECDHE_RSA_WITH_AES_128_CBC_SHA)]
    [InlineData(TlsCipherSuite.TLS_RSA_WITH_RC4_128_SHA)]
    [InlineData(TlsCipherSuite.TLS_RSA_WITH_3DES_EDE_CBC_SHA)]
    [InlineData(TlsCipherSuite.TLS_AES_128_CCM_SHA256)]
    public void TransportWithoutForwardSecrecyOrAeadIsRejected(TlsCipherSuite suite)
    {
        Assert.False(FtpsTlsSuites.IsAllowed(suite));
    }

    [Fact]
    public void SuiteListIsOfferedOnlyWhereThePlatformHonorsIt()
    {
        if (OperatingSystem.IsWindows() || OperatingSystem.IsAndroid())
        {
            Assert.Null(FtpsTlsSuites.Policy);
            return;
        }

        var policy = FtpsTlsSuites.Policy;
        Assert.NotNull(policy);
        Assert.Equal(Allowed.OrderBy(suite => suite), policy.AllowedCipherSuites.OrderBy(suite => suite));
    }
}
