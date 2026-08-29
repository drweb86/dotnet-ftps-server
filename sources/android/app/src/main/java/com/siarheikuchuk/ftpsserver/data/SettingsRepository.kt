package com.siarheikuchuk.ftpsserver.data

import android.content.Context
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

    fun load(): AppSettings {
        return try {
            if (!file.exists()) return AppSettings()
            val json = JSONObject(file.readText())
            AppSettings(
                serverPort = json.optInt("serverPort", 2121),
                maxConnections = json.optInt("maxConnections", 10),
                useSelfSigned = json.optBoolean("useSelfSigned", true),
                certificatePath = json.optString("certificatePath", ""),
                certificatePassword = json.optString("certificatePassword", ""),
                users = json.optJSONArray("users")?.let { arr ->
                    MutableList(arr.length()) { i ->
                        val u = arr.getJSONObject(i)
                        UserAccount(
                            login = u.optString("login"),
                            password = u.optString("password"),
                            folderName = u.optString("folderName"),
                            folderUri = u.optString("folderUri"),
                            readonly = u.optBoolean("readonly"),
                        )
                    }
                } ?: mutableListOf(),
            )
        } catch (_: Exception) {
            AppSettings()
        }
    }

    fun save(settings: AppSettings) {
        val users = JSONArray()
        for (u in settings.users) {
            users.put(
                JSONObject()
                    .put("login", u.login)
                    .put("password", u.password)
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
            .put("certificatePassword", settings.certificatePassword)
            .put("users", users)
        file.writeText(json.toString())
    }
}
