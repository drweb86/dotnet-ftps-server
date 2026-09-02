package com.siarheikuchuk.ftpsserver.privacy

import java.util.Locale

data class PrivacyLanguage(
    val code: String,
    val assetFile: String,
    val nativeName: String,
    val rtl: Boolean = false,
)

object PrivacyLanguages {
    val all: List<PrivacyLanguage> = listOf(
        PrivacyLanguage("en", "en.md", "English"),
        PrivacyLanguage("am", "am.md", "አማርኛ"),
        PrivacyLanguage("ar", "ar.md", "العربية", rtl = true),
        PrivacyLanguage("bn", "bn.md", "বাংলা"),
        PrivacyLanguage("my", "my.md", "မြန်မာ"),
        PrivacyLanguage("yue", "yue.md", "粵語"),
        PrivacyLanguage("zh-Hans", "zh-Hans.md", "简体中文"),
        PrivacyLanguage("fr", "fr.md", "Français"),
        PrivacyLanguage("de", "de.md", "Deutsch"),
        PrivacyLanguage("ha", "ha.md", "Hausa"),
        PrivacyLanguage("hi", "hi.md", "हिन्दी"),
        PrivacyLanguage("ig", "ig.md", "Igbo"),
        PrivacyLanguage("id", "id.md", "Bahasa Indonesia"),
        PrivacyLanguage("it", "it.md", "Italiano"),
        PrivacyLanguage("ja", "ja.md", "日本語"),
        PrivacyLanguage("kk", "kk.md", "Қазақша"),
        PrivacyLanguage("ko", "ko.md", "한국어"),
        PrivacyLanguage("mr", "mr.md", "मराठी"),
        PrivacyLanguage("ne", "ne.md", "नेपाली"),
        PrivacyLanguage("pcm", "pcm.md", "Nigerian Pidgin"),
        PrivacyLanguage("om", "om.md", "Afaan Oromoo"),
        PrivacyLanguage("ps", "ps.md", "پښتو", rtl = true),
        PrivacyLanguage("fa", "fa.md", "فارسی", rtl = true),
        PrivacyLanguage("pl", "pl.md", "Polski"),
        PrivacyLanguage("pt-BR", "pt-BR.md", "Português (Brasil)"),
        PrivacyLanguage("pa", "pa.md", "ਪੰਜਾਬੀ"),
        PrivacyLanguage("ru", "ru.md", "Русский"),
        PrivacyLanguage("es", "es.md", "Español"),
        PrivacyLanguage("sw", "sw.md", "Kiswahili"),
        PrivacyLanguage("ta", "ta.md", "தமிழ்"),
        PrivacyLanguage("te", "te.md", "తెలుగు"),
        PrivacyLanguage("th", "th.md", "ไทย"),
        PrivacyLanguage("tr", "tr.md", "Türkçe"),
        PrivacyLanguage("uk", "uk.md", "Українська"),
        PrivacyLanguage("ur", "ur.md", "اردو", rtl = true),
        PrivacyLanguage("uz", "uz.md", "Oʻzbekcha"),
        PrivacyLanguage("vi", "vi.md", "Tiếng Việt"),
        PrivacyLanguage("yo", "yo.md", "Yorùbá"),
    )

    fun byCode(code: String): PrivacyLanguage =
        all.firstOrNull { it.code.equals(code, ignoreCase = true) } ?: all.first { it.code == "en" }

    fun matchDevice(locale: Locale = Locale.getDefault()): PrivacyLanguage {
        val lang = locale.language.lowercase(Locale.ROOT)
        val country = locale.country.uppercase(Locale.ROOT)
        val script = locale.script
        when {
            lang == "zh" && (script.equals("Hant", true) || country in setOf("HK", "MO", "TW")) ->
                return byCode("yue")
            lang == "zh" -> return byCode("zh-Hans")
            lang == "pt" -> return byCode("pt-BR")
            lang == "in" || lang == "id" -> return byCode("id")
            lang == "yue" -> return byCode("yue")
            lang == "pcm" -> return byCode("pcm")
        }
        return all.firstOrNull { it.code.equals(lang, ignoreCase = true) } ?: byCode("en")
    }
}
