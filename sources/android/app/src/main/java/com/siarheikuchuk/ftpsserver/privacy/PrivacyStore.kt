package com.siarheikuchuk.ftpsserver.privacy

import android.content.Context

object PrivacyStore {
    const val PREFS_NAME = "privacy_consent"
    const val CONSENT_KEY = "china_pipl_policy_privacy_consent_accepted"
    private const val LANGUAGE_KEY = "privacy_policy_language"

    @Volatile
    var skipSettingsSave: Boolean = false
        private set

    fun hasConsent(context: Context): Boolean =
        prefs(context).getBoolean(CONSENT_KEY, false)

    fun acceptConsent(context: Context) {
        prefs(context).edit().putBoolean(CONSENT_KEY, true).commit()
    }

    fun clearConsent(context: Context) {
        prefs(context).edit().putBoolean(CONSENT_KEY, false).commit()
    }

    fun languageCode(context: Context): String? =
        prefs(context).getString(LANGUAGE_KEY, null)?.takeIf { it.isNotBlank() }

    fun setLanguageCode(context: Context, code: String) {
        prefs(context).edit().putString(LANGUAGE_KEY, code).commit()
    }

    fun markWiping() {
        skipSettingsSave = true
    }

    private fun prefs(context: Context) =
        context.applicationContext.getSharedPreferences(PREFS_NAME, Context.MODE_PRIVATE)
}
