using System.Security.Authentication;

namespace FtpsServerLibrary.Tests;

public class TlsSessionTests
{
    private static readonly byte[] IdA = [1, 2, 3, 4];
    private static readonly byte[] IdB = [1, 2, 3, 5];

    [Fact]
    public void UnreadableSessionIsAcceptedOnlyOnThePlatformThatCannotExposeIt()
    {
        Assert.False(Match(sessionReadable: false, acceptUnreadableSession: false));
        Assert.True(Match(sessionReadable: false, acceptUnreadableSession: true));
    }

    [Fact]
    public void DataConnectionThatDidNotResumeIsRejectedWhereTheSessionIdLasts()
    {
        Assert.False(Match(dataResumed: false, controlId: IdA, dataId: IdA));
        Assert.False(Match(
            dataResumed: false,
            controlId: IdA,
            dataId: IdA,
            controlProtocol: SslProtocols.Tls13,
            dataProtocol: SslProtocols.Tls13,
            acceptAnyResumedTls13: false));
        Assert.False(Match(
            dataResumed: false,
            controlId: IdA,
            dataId: IdA,
            controlProtocol: SslProtocols.Tls12,
            dataProtocol: SslProtocols.Tls12,
            acceptAnyResumedTls13: true));
        Assert.True(Match(
            dataResumed: false,
            controlId: IdA,
            dataId: IdB,
            controlProtocol: SslProtocols.Tls13,
            dataProtocol: SslProtocols.Tls13,
            acceptAnyResumedTls13: true));
    }

    [Fact]
    public void MatchingSessionIdsAreTheSameSession()
    {
        Assert.True(Match(controlId: IdA, dataId: [1, 2, 3, 4], controlProtocol: SslProtocols.Tls12, dataProtocol: SslProtocols.Tls12));
        Assert.True(Match(
            controlId: IdA,
            dataId: [1, 2, 3, 4],
            controlProtocol: SslProtocols.Tls13,
            dataProtocol: SslProtocols.Tls13,
            acceptAnyResumedTls13: false));
    }

    [Fact]
    public void DifferentSessionIdsAreRejectedExceptResumedTls13WhereThePlatformHasNoId()
    {
        Assert.False(Match(controlId: IdA, dataId: IdB, controlProtocol: SslProtocols.Tls12, dataProtocol: SslProtocols.Tls12, acceptAnyResumedTls13: true));
        Assert.False(Match(controlId: IdA, dataId: IdB, controlProtocol: SslProtocols.Tls13, dataProtocol: SslProtocols.Tls13, acceptAnyResumedTls13: false));
        Assert.False(Match(controlId: IdA, dataId: IdB, controlProtocol: SslProtocols.Tls13, dataProtocol: SslProtocols.Tls12, acceptAnyResumedTls13: true));
        Assert.False(Match(controlId: IdA, dataId: IdB, controlProtocol: SslProtocols.Tls12, dataProtocol: SslProtocols.Tls13, acceptAnyResumedTls13: true));
        Assert.True(Match(controlId: IdA, dataId: IdB, controlProtocol: SslProtocols.Tls13, dataProtocol: SslProtocols.Tls13, acceptAnyResumedTls13: true));
    }

    [Fact]
    public void EmptyOrPrefixIdsDoNotCountAsAMatch()
    {
        Assert.False(Match(controlId: [], dataId: [], controlProtocol: SslProtocols.Tls12, dataProtocol: SslProtocols.Tls12, acceptAnyResumedTls13: true));
        Assert.False(Match(controlId: [1, 2], dataId: [1, 2, 3], controlProtocol: SslProtocols.Tls12, dataProtocol: SslProtocols.Tls12));
        Assert.True(Match(controlId: [], dataId: [], controlProtocol: SslProtocols.Tls13, dataProtocol: SslProtocols.Tls13, acceptAnyResumedTls13: true));
    }

    [Fact]
    public void ThisOperatingSystemUsesItsOwnSessionRule()
    {
        var resumedTls13 = FtpsTlsSession.SessionsMatch(
            true, IdA, IdB, true, SslProtocols.Tls13, SslProtocols.Tls13,
            OperatingSystem.IsAndroid(), OperatingSystem.IsWindows());
        Assert.Equal(OperatingSystem.IsWindows(), resumedTls13);

        var unreadable = FtpsTlsSession.SessionsMatch(
            false, [], [], false, SslProtocols.None, SslProtocols.None,
            OperatingSystem.IsAndroid(), OperatingSystem.IsWindows());
        Assert.Equal(OperatingSystem.IsAndroid(), unreadable);

        var fullHandshakeTls13 = FtpsTlsSession.SessionsMatch(
            true, IdA, IdB, false, SslProtocols.Tls13, SslProtocols.Tls13,
            OperatingSystem.IsAndroid(), OperatingSystem.IsWindows());
        Assert.Equal(OperatingSystem.IsWindows(), fullHandshakeTls13);
    }

    private static bool Match(
        bool sessionReadable = true,
        byte[]? controlId = null,
        byte[]? dataId = null,
        bool dataResumed = true,
        SslProtocols controlProtocol = SslProtocols.Tls12,
        SslProtocols dataProtocol = SslProtocols.Tls12,
        bool acceptUnreadableSession = false,
        bool acceptAnyResumedTls13 = false)
    {
        return FtpsTlsSession.SessionsMatch(
            sessionReadable,
            controlId ?? IdA,
            dataId ?? IdB,
            dataResumed,
            controlProtocol,
            dataProtocol,
            acceptUnreadableSession,
            acceptAnyResumedTls13);
    }
}
