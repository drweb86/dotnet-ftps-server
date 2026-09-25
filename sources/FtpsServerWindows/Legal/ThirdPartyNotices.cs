using System.IO;

namespace FtpsServerWindows.Windows;

internal static class ThirdPartyNotices
{
    public static string Load()
    {
        using var stream = typeof(ThirdPartyNotices).Assembly.GetManifestResourceStream("FtpsServerWindows.ThirdPartyNotices.md")
            ?? throw new InvalidOperationException("Missing embedded third-party notices.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
