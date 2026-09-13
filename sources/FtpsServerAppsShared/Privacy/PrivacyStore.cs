namespace FtpsServerAppsShared.Privacy;

public static class PrivacyStore
{
    private static readonly string DirectoryPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "FtpsServerApp");

    private static readonly string LanguageFile = Path.Combine(DirectoryPath, "privacy-language.txt");

    public static string? LanguageCode()
    {
        try
        {
            if (!File.Exists(LanguageFile))
                return null;
            var code = File.ReadAllText(LanguageFile).Trim();
            return string.IsNullOrEmpty(code) ? null : code;
        }
        catch
        {
            return null;
        }
    }

    public static void SetLanguageCode(string code)
    {
        try
        {
            Directory.CreateDirectory(DirectoryPath);
            File.WriteAllText(LanguageFile, code);
        }
        catch
        {
            // Preference is optional; the document still opens.
        }
    }
}
