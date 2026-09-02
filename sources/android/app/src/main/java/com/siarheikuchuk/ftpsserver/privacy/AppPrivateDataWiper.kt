package com.siarheikuchuk.ftpsserver.privacy

import android.content.Context
import android.content.Intent
import com.siarheikuchuk.ftpsserver.service.FtpsForegroundService
import java.io.File

object AppPrivateDataWiper {
    /**
     * Deletes app-private FTPS settings and SAF grants. Does not touch files in
     * user-shared folders. Clears the China PIPL consent flag. Keeps the privacy
     * language preference so the next launch can show the same language.
     */
    fun wipePrivateDataAndClearConsent(context: Context) {
        PrivacyStore.markWiping()
        val app = context.applicationContext
        try {
            app.startService(FtpsForegroundService.stopIntent(app))
        } catch (_: Exception) {
        }
        revokeSafGrants(app)
        deleteChildren(app.filesDir)
        deleteChildren(app.cacheDir)
        app.noBackupFilesDir?.let { deleteChildren(it) }
        PrivacyStore.clearConsent(app)
    }

    private fun revokeSafGrants(context: Context) {
        val cr = context.contentResolver
        for (perm in cr.persistedUriPermissions.toList()) {
            var flags = 0
            if (perm.isReadPermission) flags = flags or Intent.FLAG_GRANT_READ_URI_PERMISSION
            if (perm.isWritePermission) flags = flags or Intent.FLAG_GRANT_WRITE_URI_PERMISSION
            try {
                cr.releasePersistableUriPermission(perm.uri, flags)
            } catch (_: Exception) {
            }
        }
    }

    private fun deleteChildren(dir: File?) {
        if (dir == null || !dir.exists()) return
        dir.listFiles()?.forEach { child ->
            child.deleteRecursively()
        }
    }
}

fun loadPrivacyMarkdown(context: Context, fileName: String): String {
    fun read(name: String): String? = try {
        context.assets.open("privacy/$name").bufferedReader().use { it.readText() }
            .lineSequence()
            .filterNot { it.trim() == "[Languages](README.md)" }
            .joinToString("\n")
            .trim()
            .ifBlank { null }
    } catch (_: Exception) {
        null
    }
    if (fileName == "en.md") return read("en.md").orEmpty()
    return read(fileName) ?: read("en.md").orEmpty()
}
