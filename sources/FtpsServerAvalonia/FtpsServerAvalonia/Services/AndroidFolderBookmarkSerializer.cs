using System.IO;
using System.Text.Json;

namespace FtpsServerAvalonia.Services;

public static class AndroidFolderBookmarkSerializer
{
    public static string Serialise(AndroidFolderBookmark bookmark)
    {
        if (bookmark == null)
            return string.Empty;

        return JsonSerializer.Serialize(bookmark);
    }

    public static AndroidFolderBookmark Deserialise(string serializedBookmark)
    {
        return JsonSerializer.Deserialize<AndroidFolderBookmark>(serializedBookmark)
            ?? throw new InvalidDataException();
    }
}
