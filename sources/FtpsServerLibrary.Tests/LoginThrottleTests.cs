using System.Net;

namespace FtpsServerLibrary.Tests;

public class LoginThrottleTests
{
    private static readonly DateTime Now = new(2026, 10, 4, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void LockoutWindowIsFiveFailuresThenSixtySeconds()
    {
        Assert.Equal(5, FtpsLoginThrottle.FailureLimit);
        Assert.Equal(TimeSpan.FromSeconds(1), FtpsLoginThrottle.FailureDelay);
        Assert.Equal(TimeSpan.FromSeconds(60), FtpsLoginThrottle.Lockout);
    }

    [Fact]
    public void FifthFailureLocksUntilTheWindowEnds()
    {
        var throttle = new FtpsLoginThrottle();
        var ip = IPAddress.Parse("203.0.113.5");

        Assert.False(throttle.IsLocked(ip, Now));
        for (var i = 1; i < FtpsLoginThrottle.FailureLimit; i++)
            Assert.False(throttle.RegisterFailure(ip, Now));

        Assert.True(throttle.RegisterFailure(ip, Now));
        Assert.True(throttle.IsLocked(ip, Now));
        Assert.True(throttle.IsLocked(ip, Now.Add(FtpsLoginThrottle.Lockout).AddTicks(-1)));
        Assert.False(throttle.IsLocked(ip, Now.Add(FtpsLoginThrottle.Lockout)));
    }

    [Fact]
    public void FailureDuringLockoutDoesNotExtendIt()
    {
        var throttle = Locked("203.0.113.6");

        Assert.True(throttle.RegisterFailure(IPAddress.Parse("203.0.113.6"), Now.AddSeconds(10)));
        Assert.False(throttle.IsLocked(IPAddress.Parse("203.0.113.6"), Now.Add(FtpsLoginThrottle.Lockout)));
    }

    [Fact]
    public void SuccessClearsTheLockAndTheFailureCount()
    {
        var throttle = Locked("203.0.113.7");
        var ip = IPAddress.Parse("203.0.113.7");

        throttle.RegisterSuccess(ip);

        Assert.False(throttle.IsLocked(ip, Now));
        Assert.False(throttle.RegisterFailure(ip, Now));
    }

    [Fact]
    public void AnotherAddressIsNotLocked()
    {
        var throttle = Locked("203.0.113.8");

        Assert.False(throttle.IsLocked(IPAddress.Parse("203.0.113.9"), Now));
        Assert.False(throttle.RegisterFailure(IPAddress.Parse("2001:db8::1"), Now));
    }

    [Fact]
    public void IPv4MappedIPv6SharesTheCounter()
    {
        var throttle = new FtpsLoginThrottle();
        var v4 = IPAddress.Parse("203.0.113.10");
        var mapped = IPAddress.Parse("::ffff:203.0.113.10");

        for (var i = 1; i < FtpsLoginThrottle.FailureLimit; i++)
            Assert.False(throttle.RegisterFailure(v4, Now));

        Assert.True(throttle.RegisterFailure(mapped, Now));
        Assert.True(throttle.IsLocked(v4, Now));
        Assert.True(throttle.IsLocked(mapped, Now));
    }

    [Fact]
    public void ExpiredLockoutStartsTheCountOver()
    {
        var throttle = Locked("203.0.113.11");
        var ip = IPAddress.Parse("203.0.113.11");
        var after = Now.Add(FtpsLoginThrottle.Lockout);

        Assert.False(throttle.IsLocked(ip, after));
        Assert.False(throttle.RegisterFailure(ip, after));
        Assert.False(throttle.IsLocked(ip, after));
    }

    [Fact]
    public void ConcurrentFailuresLockOnceTheLimitIsReached()
    {
        var throttle = new FtpsLoginThrottle();
        var ip = IPAddress.Parse("198.51.100.10");
        const int attempts = 32;
        var lockedReports = 0;

        Parallel.For(0, attempts, _ =>
        {
            if (throttle.RegisterFailure(ip, Now))
                Interlocked.Increment(ref lockedReports);
        });

        Assert.Equal(attempts - (FtpsLoginThrottle.FailureLimit - 1), lockedReports);
        Assert.True(throttle.IsLocked(ip, Now.AddSeconds(59)));
        Assert.False(throttle.IsLocked(ip, Now.Add(FtpsLoginThrottle.Lockout)));
    }

    private static FtpsLoginThrottle Locked(string address)
    {
        var throttle = new FtpsLoginThrottle();
        var ip = IPAddress.Parse(address);
        for (var i = 0; i < FtpsLoginThrottle.FailureLimit; i++)
            throttle.RegisterFailure(ip, Now);
        return throttle;
    }
}
