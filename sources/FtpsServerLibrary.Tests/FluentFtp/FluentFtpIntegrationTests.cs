using System.Security.Authentication;

namespace FtpsServerLibrary.Tests.FluentFtp;

// Schannel caches a resumed TLS session by host name. These servers are all reached
// as loopback, so one collection keeps their FluentFTP clients from resuming each
// other's sessions. A resumed foreign session fails the server's TLS 1.2 check.
[CollectionDefinition("FluentFTP integration")]
public sealed class FluentFtpIntegrationCollection :
    ICollectionFixture<WindowsConsoleFixture>,
    ICollectionFixture<LinuxConsoleFixture>,
    ICollectionFixture<AndroidEmulatorFixture>;

[Collection("FluentFTP integration")]
public sealed class WindowsConsoleFluentFtpTests(WindowsConsoleFixture server)
{
    [Fact(Timeout = 300000)]
    public Task Tls12() => FluentFtpProtocolSuite.RunAsync(server.Endpoint, SslProtocols.Tls12);

    [Fact(Timeout = 300000)]
    public Task Tls13() => FluentFtpProtocolSuite.RunAsync(server.Endpoint, SslProtocols.Tls13);
}

[Collection("FluentFTP integration")]
public sealed class LinuxConsoleFluentFtpTests(LinuxConsoleFixture server)
{
    [WslFact]
    public Task Tls12() => FluentFtpProtocolSuite.RunAsync(server.Endpoint, SslProtocols.Tls12);

    [WslFact]
    public Task Tls13() => FluentFtpProtocolSuite.RunAsync(server.Endpoint, SslProtocols.Tls13);
}

[Collection("FluentFTP integration")]
public sealed class AndroidEmulatorFluentFtpTests(AndroidEmulatorFixture server)
{
    [AndroidEmulatorFact]
    public Task Tls12() => FluentFtpProtocolSuite.RunAsync(server.Endpoint, SslProtocols.Tls12);

    [AndroidEmulatorFact]
    public Task Tls13() => FluentFtpProtocolSuite.RunAsync(server.Endpoint, SslProtocols.Tls13);
}
