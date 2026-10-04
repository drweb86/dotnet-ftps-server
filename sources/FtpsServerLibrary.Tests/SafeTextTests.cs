namespace FtpsServerLibrary.Tests;

public class SafeTextTests
{
    [Fact]
    public void PrintableTextIsReturnedUnchanged()
    {
        const string text = "Command 'FOO' not implemented: Привет/файл.txt";
        Assert.Same(text, FtpsSafeText.Sanitize(text));
    }

    [Theory]
    [InlineData("\u001b[2J", "?[2J")]
    [InlineData("A\u0007B", "A?B")]
    [InlineData("tab\there", "tab?here")]
    [InlineData("\u009b31m", "?31m")]
    [InlineData("del\u007f", "del?")]
    [InlineData("\0", "?")]
    public void ControlCharactersAreReplaced(string input, string expected)
    {
        Assert.Equal(expected, FtpsSafeText.Sanitize(input));
    }

    [Fact]
    public void SanitizingLogCleansEveryLevel()
    {
        var inner = new RecordingLog();
        var log = new FtpsSanitizingLog(inner);

        log.Debug("d\u001b");
        log.Info("i\u001b");
        log.Warn("w\u001b");
        log.Error(new Exception(), "e\u001b");
        log.Fatal(new Exception(), "f\u001b");

        Assert.Equal(["d?", "i?", "w?", "e?", "f?"], inner.Messages);
    }

    private sealed class RecordingLog : IFtpsServerLog
    {
        public List<string> Messages { get; } = [];
        public void Debug(string message) => Messages.Add(message);
        public void Error(Exception ex, string message) => Messages.Add(message);
        public void Fatal(Exception ex, string message) => Messages.Add(message);
        public void Info(string message) => Messages.Add(message);
        public void Warn(string message) => Messages.Add(message);
    }
}
