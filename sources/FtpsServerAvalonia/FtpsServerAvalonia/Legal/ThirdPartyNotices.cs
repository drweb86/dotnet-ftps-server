using System;
using System.IO;

namespace FtpsServerAvalonia.Windows;

internal static class ThirdPartyNotices
{
    public static string Load()
    {
        using var stream = typeof(ThirdPartyNotices).Assembly.GetManifestResourceStream("FtpsServerAvalonia.ThirdPartyNotices.md")
            ?? throw new InvalidOperationException("Missing embedded third-party notices.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
