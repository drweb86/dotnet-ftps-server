using System;
using System.Text;

namespace FtpsServerLibrary;

// Client-supplied text (command names, paths, user names) ends up in replies and in the log.
// ESC, BEL and other control characters there can drive a terminal that shows the log, so
// they are replaced before the text leaves the session.
static class FtpsSafeText
{
    internal const char Replacement = '?';

    internal static string Sanitize(string text)
    {
        var firstControl = -1;
        for (var i = 0; i < text.Length; i++)
        {
            if (char.IsControl(text[i]))
            {
                firstControl = i;
                break;
            }
        }

        if (firstControl < 0)
            return text;

        var result = new StringBuilder(text.Length);
        result.Append(text, 0, firstControl);
        for (var i = firstControl; i < text.Length; i++)
            result.Append(char.IsControl(text[i]) ? Replacement : text[i]);
        return result.ToString();
    }
}

sealed class FtpsSanitizingLog(IFtpsServerLog inner) : IFtpsServerLog
{
    public void Debug(string message) => inner.Debug(FtpsSafeText.Sanitize(message));
    public void Error(Exception ex, string message) => inner.Error(ex, FtpsSafeText.Sanitize(message));
    public void Fatal(Exception ex, string message) => inner.Fatal(ex, FtpsSafeText.Sanitize(message));
    public void Info(string message) => inner.Info(FtpsSafeText.Sanitize(message));
    public void Warn(string message) => inner.Warn(FtpsSafeText.Sanitize(message));
}
