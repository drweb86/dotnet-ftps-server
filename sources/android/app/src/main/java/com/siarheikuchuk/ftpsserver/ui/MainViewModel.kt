package com.siarheikuchuk.ftpsserver.ui

import android.app.Application
import android.content.Intent
import android.net.Uri
import android.os.Build
import androidx.core.content.ContextCompat
import androidx.lifecycle.AndroidViewModel
import com.siarheikuchuk.ftpsserver.BuildConfig
import com.siarheikuchuk.ftpsserver.data.AppSettings
import com.siarheikuchuk.ftpsserver.data.SettingsRepository
import com.siarheikuchuk.ftpsserver.data.UserAccount
import com.siarheikuchuk.ftpsserver.server.LoadedCertificate
import com.siarheikuchuk.ftpsserver.service.FtpsForegroundService
import com.siarheikuchuk.ftpsserver.service.ServerEvents
import java.net.HttpURLConnection
import java.net.Inet4Address
import java.net.NetworkInterface
import java.net.URL
import java.text.SimpleDateFormat
import java.util.Date
import java.util.Locale
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.update
import org.json.JSONObject

data class LogLine(val timestamp: String, val level: String, val message: String)

data class NetworkRow(val name: String, val addresses: List<String>)

data class UpdateInfo(val version: String, val changes: String)

data class UiState(
    val port: Int = 2121,
    val maxConnections: Int = 10,
    val useSelfSigned: Boolean = true,
    val certificatePath: String = "",
    val certificatePassword: String = "",
    val users: List<UserAccount> = emptyList(),
    val running: Boolean = false,
    val error: String? = null,
    val logs: List<LogLine> = emptyList(),
    val networks: List<NetworkRow> = emptyList(),
    val hostName: String = Build.MODEL ?: "Android",
    val certificate: LoadedCertificate? = null,
    val update: UpdateInfo? = null,
)

class MainViewModel(application: Application) : AndroidViewModel(application) {
    private val repo = SettingsRepository(application)
    private val _state = MutableStateFlow(UiState())
    val state: StateFlow<UiState> = _state

    init {
        val loaded = repo.load()
        _state.value = UiState(
            port = loaded.serverPort,
            maxConnections = loaded.maxConnections,
            useSelfSigned = loaded.useSelfSigned,
            certificatePath = loaded.certificatePath,
            certificatePassword = loaded.certificatePassword,
            users = loaded.users.toList(),
            networks = localNetworks(),
            hostName = Build.MODEL ?: "Android",
        )
        ServerEvents.onLog = { level, message ->
            val time = SimpleDateFormat("HH:mm:ss", Locale.US).format(Date())
            _state.update { s ->
                val next = (s.logs + LogLine(time, level, message)).takeLast(500)
                s.copy(logs = next)
            }
        }
        ServerEvents.onRunning = { running -> _state.update { it.copy(running = running) } }
        ServerEvents.onFailed = { msg -> _state.update { it.copy(error = msg, running = false) } }
        ServerEvents.onCertificate = { cert -> _state.update { it.copy(certificate = cert) } }
        if (!BuildConfig.SCREENSHOTS) {
            Thread { checkUpdate() }.start()
        }
    }

    fun setPort(value: Int) = _state.update { it.copy(port = value.coerceAtLeast(2121)) }
    fun setMaxConnections(value: Int) = _state.update { it.copy(maxConnections = value.coerceAtLeast(2)) }
    fun setUseSelfSigned(value: Boolean) = _state.update { it.copy(useSelfSigned = value) }
    fun setCertificatePassword(value: String) = _state.update { it.copy(certificatePassword = value) }
    fun dismissError() = _state.update { it.copy(error = null) }
    fun clearLogs() = _state.update { it.copy(logs = emptyList()) }

    fun addUser() {
        _state.update {
            val n = it.users.size + 1
            it.copy(users = it.users + UserAccount("user$n", "password$n", "", "", false))
        }
    }

    fun removeUser(index: Int) {
        _state.update { it.copy(users = it.users.toMutableList().also { list -> list.removeAt(index) }) }
    }

    fun updateUser(index: Int, transform: (UserAccount) -> UserAccount) {
        _state.update {
            val list = it.users.toMutableList()
            list[index] = transform(list[index])
            it.copy(users = list)
        }
    }

    fun setFolder(index: Int, uri: Uri, name: String) {
        val cr = getApplication<Application>().contentResolver
        cr.takePersistableUriPermission(uri, Intent.FLAG_GRANT_READ_URI_PERMISSION or Intent.FLAG_GRANT_WRITE_URI_PERMISSION)
        updateUser(index) { it.copy(folderName = name, folderUri = uri.toString()) }
    }

    fun setCertificateFile(path: String) {
        _state.update { it.copy(certificatePath = path) }
    }

    fun toggleServer() {
        if (_state.value.running) stop() else start()
    }

    fun save() {
        val s = _state.value
        repo.save(
            AppSettings(
                serverPort = s.port,
                maxConnections = s.maxConnections,
                useSelfSigned = s.useSelfSigned,
                certificatePath = s.certificatePath,
                certificatePassword = s.certificatePassword,
                users = s.users.toMutableList(),
            )
        )
    }

    private fun start() {
        save()
        val s = _state.value
        if (s.users.isEmpty()) {
            _state.update { it.copy(error = getApplication<Application>().getString(com.siarheikuchuk.ftpsserver.R.string.error_add_user)) }
            return
        }
        for (u in s.users) {
            if (u.login.isBlank() || u.password.isBlank() || u.folderUri.isBlank()) {
                _state.update {
                    it.copy(error = getApplication<Application>().getString(com.siarheikuchuk.ftpsserver.R.string.error_incomplete_user_format, u.login))
                }
                return
            }
        }
        if (!s.useSelfSigned && s.certificatePath.isBlank()) {
            _state.update { it.copy(error = getApplication<Application>().getString(com.siarheikuchuk.ftpsserver.R.string.error_select_certificate)) }
            return
        }
        val ctx = getApplication<Application>()
        val intent = FtpsForegroundService.startIntent(ctx).apply {
            putExtra(FtpsForegroundService.EXTRA_PORT, s.port)
            putExtra(FtpsForegroundService.EXTRA_MAX, s.maxConnections)
            putExtra(FtpsForegroundService.EXTRA_SELF_SIGNED, s.useSelfSigned)
            putExtra(FtpsForegroundService.EXTRA_CERT_PATH, s.certificatePath)
            putExtra(FtpsForegroundService.EXTRA_CERT_PASSWORD, s.certificatePassword)
            putExtra(FtpsForegroundService.EXTRA_LOGINS, s.users.map { it.login }.toTypedArray())
            putExtra(FtpsForegroundService.EXTRA_PASSWORDS, s.users.map { it.password }.toTypedArray())
            putExtra(FtpsForegroundService.EXTRA_FOLDERS, s.users.map { it.folderUri }.toTypedArray())
            putExtra(FtpsForegroundService.EXTRA_WRITES, s.users.map { !it.readonly }.toBooleanArray())
        }
        ContextCompat.startForegroundService(ctx, intent)
    }

    private fun stop() {
        val ctx = getApplication<Application>()
        ctx.startService(FtpsForegroundService.stopIntent(ctx))
    }

    private fun localNetworks(): List<NetworkRow> {
        val rows = mutableListOf<NetworkRow>()
        val ifaces = NetworkInterface.getNetworkInterfaces() ?: return rows
        for (nic in ifaces) {
            if (!nic.isUp || nic.isLoopback) continue
            val addrs = nic.inetAddresses.toList()
                .filter { !it.isLoopbackAddress && it is Inet4Address }
                .map { it.hostAddress ?: it.toString() }
            if (addrs.isNotEmpty()) rows += NetworkRow(nic.displayName ?: nic.name, addrs)
        }
        return rows
    }

    private fun checkUpdate() {
        try {
            val conn = URL("https://api.github.com/repos/drweb86/dotnet-ftps-server/releases/latest").openConnection() as HttpURLConnection
            conn.setRequestProperty("User-Agent", "FtpsServer-Android")
            conn.setRequestProperty("Accept", "application/vnd.github+json")
            conn.connectTimeout = 8000
            conn.readTimeout = 8000
            val body = conn.inputStream.bufferedReader().readText()
            val json = JSONObject(body)
            val tag = json.optString("tag_name").trimStart('v')
            val notes = json.optString("body")
            val remote = tag.split('.').mapNotNull { it.toIntOrNull() }
            val local = BuildConfig.VERSION_NAME.substringBefore('-').split('.').mapNotNull { it.toIntOrNull() }
            var newer = false
            for (i in 0 until maxOf(remote.size, local.size)) {
                val r = remote.getOrElse(i) { 0 }
                val l = local.getOrElse(i) { 0 }
                if (r != l) {
                    newer = r > l
                    break
                }
            }
            if (newer) {
                _state.update { it.copy(update = UpdateInfo(tag, notes.take(800))) }
            }
        } catch (_: Exception) {
        }
    }

    override fun onCleared() {
        save()
        ServerEvents.onLog = null
        ServerEvents.onRunning = null
        ServerEvents.onFailed = null
        ServerEvents.onCertificate = null
        super.onCleared()
    }
}
