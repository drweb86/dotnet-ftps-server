namespace FtpsServerLibrary.Tests;

public class LoginDeadlineTests
{
    [Fact]
    public void FreshConnectionGetsTheIdleTimeout()
    {
        Assert.Equal(FtpsServerClientSession.UnauthenticatedIdleTimeout,
            FtpsServerClientSession.UnauthenticatedReadTimeout(TimeSpan.Zero));
    }

    [Fact]
    public void CommandsCannotExtendTheLoginDeadline()
    {
        var sinceConnect = FtpsServerClientSession.LoginTimeout - TimeSpan.FromSeconds(5);

        Assert.Equal(TimeSpan.FromSeconds(5), FtpsServerClientSession.UnauthenticatedReadTimeout(sinceConnect));
    }

    [Fact]
    public void PassedDeadlineLeavesNoTime()
    {
        Assert.True(FtpsServerClientSession.UnauthenticatedReadTimeout(FtpsServerClientSession.LoginTimeout) <= TimeSpan.Zero);
        Assert.True(FtpsServerClientSession.UnauthenticatedReadTimeout(TimeSpan.FromMinutes(10)) <= TimeSpan.Zero);
    }
}
