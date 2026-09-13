namespace FtpsServerAppsShared.Privacy;

public sealed record PrivacyLanguage(
    string Code,
    string AssetFile,
    string NativeName,
    bool Rtl = false);
