using System.Globalization;

namespace FtpsServerAppsShared.Privacy;

public static class PrivacyLanguages
{
    public static IReadOnlyList<PrivacyLanguage> All { get; } =
    [
        new("en", "en.md", "English"),
        new("am", "am.md", "አማርኛ"),
        new("ar", "ar.md", "العربية", Rtl: true),
        new("bn", "bn.md", "বাংলা"),
        new("my", "my.md", "မြန်မာ"),
        new("yue", "yue.md", "粵語"),
        new("zh-Hans", "zh-Hans.md", "简体中文"),
        new("fr", "fr.md", "Français"),
        new("de", "de.md", "Deutsch"),
        new("ha", "ha.md", "Hausa"),
        new("hi", "hi.md", "हिन्दी"),
        new("ig", "ig.md", "Igbo"),
        new("id", "id.md", "Bahasa Indonesia"),
        new("it", "it.md", "Italiano"),
        new("ja", "ja.md", "日本語"),
        new("kk", "kk.md", "Қазақша"),
        new("ko", "ko.md", "한국어"),
        new("mr", "mr.md", "मराठी"),
        new("ne", "ne.md", "नेपाली"),
        new("pcm", "pcm.md", "Nigerian Pidgin"),
        new("om", "om.md", "Afaan Oromoo"),
        new("ps", "ps.md", "پښتو", Rtl: true),
        new("fa", "fa.md", "فارسی", Rtl: true),
        new("pl", "pl.md", "Polski"),
        new("pt-BR", "pt-BR.md", "Português (Brasil)"),
        new("pa", "pa.md", "ਪੰਜਾਬੀ"),
        new("ru", "ru.md", "Русский"),
        new("es", "es.md", "Español"),
        new("sw", "sw.md", "Kiswahili"),
        new("ta", "ta.md", "தமிழ்"),
        new("te", "te.md", "తెలుగు"),
        new("th", "th.md", "ไทย"),
        new("tr", "tr.md", "Türkçe"),
        new("uk", "uk.md", "Українська"),
        new("ur", "ur.md", "اردو", Rtl: true),
        new("uz", "uz.md", "Oʻzbekcha"),
        new("vi", "vi.md", "Tiếng Việt"),
        new("yo", "yo.md", "Yorùbá"),
    ];

    public static PrivacyLanguage ByCode(string? code) =>
        All.FirstOrDefault(l => string.Equals(l.Code, code, StringComparison.OrdinalIgnoreCase))
        ?? All.First(l => l.Code == "en");

    public static PrivacyLanguage MatchDevice(CultureInfo? culture = null)
    {
        culture ??= CultureInfo.CurrentUICulture;
        var lang = culture.TwoLetterISOLanguageName.ToLowerInvariant();
        var country = culture.Name.Contains('-')
            ? culture.Name[(culture.Name.IndexOf('-') + 1)..].ToUpperInvariant()
            : culture.Name.ToUpperInvariant();
        var script = culture.Name;

        if (lang == "zh" && (script.Contains("Hant", StringComparison.OrdinalIgnoreCase)
            || country is "HK" or "MO" or "TW" or "HANT"))
            return ByCode("yue");
        if (lang == "zh")
            return ByCode("zh-Hans");
        if (lang == "pt")
            return ByCode("pt-BR");
        if (lang is "in" or "id")
            return ByCode("id");
        if (lang == "yue")
            return ByCode("yue");
        if (lang == "pcm")
            return ByCode("pcm");

        return All.FirstOrDefault(l => l.Code.Equals(lang, StringComparison.OrdinalIgnoreCase))
            ?? ByCode("en");
    }
}
