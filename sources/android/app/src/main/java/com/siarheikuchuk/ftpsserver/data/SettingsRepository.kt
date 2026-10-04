package com.siarheikuchuk.ftpsserver.data

import android.content.Context
import com.siarheikuchuk.ftpsserver.security.KeystoreCipher
import org.json.JSONArray
import org.json.JSONObject
import java.io.File

data class UserAccount(
    var login: String,
    var password: String,
    var folderName: String,
    var folderUri: String,
    var readonly: Boolean,
)

data class AppSettings(
    var serverPort: Int = 2121,
    var maxConnections: Int = 10,
    var useSelfSigned: Boolean = true,
    var certificatePath: String = "",
    var certificatePassword: String = "",
    var users: MutableList<UserAccount> = mutableListOf(),
)

class SettingsRepository(context: Context) {
    private val file = File(context.filesDir, "settings.json")
    private val cipher = KeystoreCipher(context)

    fun load(): AppSettings {
        if (!file.exists()) return AppSettings()
        val json = JSONObject(file.readText())
        var migrate = false
        val storedCertificatePassword = json.optString("certificatePassword", "")
        if (isPlaintextSecret(storedCertificatePassword)) migrate = true
        val users = json.optJSONArray("users")?.let { arr ->
            MutableList(arr.length()) { i ->
                val user = arr.getJSONObject(i)
                val storedPassword = user.optString("password")
                if (isPlaintextSecret(storedPassword)) migrate = true
                UserAccount(
                    login = user.optString("login"),
                    password = cipher.decrypt(storedPassword),
                    folderName = user.optString("folderName"),
                    folderUri = user.optString("folderUri"),
                    readonly = user.optBoolean("readonly"),
                )
            }
        } ?: mutableListOf()
        val settings = AppSettings(
            serverPort = json.optInt("serverPort", 2121),
            maxConnections = json.optInt("maxConnections", 10),
            useSelfSigned = json.optBoolean("useSelfSigned", true),
            certificatePath = json.optString("certificatePath", ""),
            certificatePassword = cipher.decrypt(storedCertificatePassword),
            users = users,
        )
        if (migrate) save(settings)
        return settings
    }

    fun save(settings: AppSettings) {
        val users = JSONArray()
        for (u in settings.users) {
            users.put(
                JSONObject()
                    .put("login", u.login)
                    .put("password", cipher.encrypt(u.password))
                    .put("folderName", u.folderName)
                    .put("folderUri", u.folderUri)
                    .put("readonly", u.readonly),
            )
        }
        val json = JSONObject()
            .put("serverPort", settings.serverPort)
            .put("maxConnections", settings.maxConnections)
            .put("useSelfSigned", settings.useSelfSigned)
            .put("certificatePath", settings.certificatePath)
            .put("certificatePassword", cipher.encrypt(settings.certificatePassword))
            .put("users", users)
        file.writeText(json.toString())
    }

    private fun isPlaintextSecret(value: String): Boolean =
        value.isNotBlank() && !value.startsWith(KeystoreCipher.PREFIX)
}
