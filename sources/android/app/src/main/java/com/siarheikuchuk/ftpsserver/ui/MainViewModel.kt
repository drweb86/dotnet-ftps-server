package com.siarheikuchuk.ftpsserver.ui

import android.app.Application
import android.content.Intent
import android.net.Uri
import android.os.Build
import androidx.core.content.ContextCompat
import androidx.lifecycle.AndroidViewModel
import androidx.lifecycle.viewModelScope
import com.siarheikuchuk.ftpsserver.R
import com.siarheikuchuk.ftpsserver.data.AppSettings
import com.siarheikuchuk.ftpsserver.data.SettingsRepository
import com.siarheikuchuk.ftpsserver.data.UserAccount
import com.siarheikuchuk.ftpsserver.privacy.PrivacyStore
import com.siarheikuchuk.ftpsserver.server.LoadedCertificate
import com.siarheikuchuk.ftpsserver.service.FtpsForegroundService
import com.siarheikuchuk.ftpsserver.service.ServerEvents
import java.net.Inet4Address
import java.net.NetworkInterface
import java.text.SimpleDateFormat
import java.util.Date
import java.util.Locale
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch

data class LogLine(val timestamp: String, val level: String, val message: String)

data class NetworkRow(val name: String, val addresses: List<String>)

data class UserFieldErrors(
    val login: String? = null,
    val password: String? = null,
    val folder: String? = null,
) {
    val first: String? get() = login ?: password ?: folder
}

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
    val portError: String? = null,
    val maxConnectionsError: String? = null,
    val certificatePathError: String? = null,
    val userErrors: List<UserFieldErrors> = emptyList(),
    val configExpanded: Boolean = true,
    val usersExpanded: Boolean = true,
    val connectionExpanded: Boolean = true,
    val logsExpanded: Boolean = true,
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
            userErrors = List(loaded.users.size) { UserFieldErrors() },
            running = ServerEvents.isRunning.value,
            networks = localNetworks(),
            hostName = Build.MODEL ?: "Android",
            certificate = ServerEvents.loadedCertificate.value,
        )
        viewModelScope.launch {
            ServerEvents.isRunning.collect { running ->
                _state.update { it.copy(running = running, error = if (running) null else it.error) }
            }
        }
        viewModelScope.launch {
            ServerEvents.failures.collect { msg ->
                _state.update { it.copy(error = msg, running = false) }
            }
        }
        viewModelScope.launch {
            ServerEvents.loadedCertificate.collect { cert ->
                _state.update { it.copy(certificate = cert) }
            }
        }
        viewModelScope.launch {
            ServerEvents.logs.collect { (level, message) ->
                val time = SimpleDateFormat("HH:mm:ss", Locale.US).format(Date())
                _state.update { s ->
                    val next = (s.logs + LogLine(time, level, message)).takeLast(500)
                    s.copy(logs = next)
                }
            }
        }
    }

    fun setPort(value: Int) = _state.update { it.copy(port = value, portError = null) }
    fun nudgePort(delta: Int) = _state.update {
        it.copy(port = (it.port + delta).coerceIn(PORT_MIN, PORT_MAX), portError = null)
    }
    fun setMaxConnections(value: Int) = _state.update { it.copy(maxConnections = value, maxConnectionsError = null) }
    fun nudgeMaxConnections(delta: Int) = _state.update {
        it.copy(maxConnections = (it.maxConnections + delta).coerceAtLeast(MAX_CONNECTIONS_MIN), maxConnectionsError = null)
    }
    fun setUseSelfSigned(value: Boolean) = _state.update { it.copy(useSelfSigned = value, certificatePathError = null) }
    fun setCertificatePassword(value: String) = _state.update { it.copy(certificatePassword = value) }
    fun dismissError() = _state.update { it.copy(error = null) }
    fun clearLogs() = _state.update { it.copy(logs = emptyList()) }

    fun toggleConfigExpanded() = _state.update { it.copy(configExpanded = !it.configExpanded) }
    fun toggleUsersExpanded() = _state.update { it.copy(usersExpanded = !it.usersExpanded) }
    fun toggleConnectionExpanded() = _state.update { it.copy(connectionExpanded = !it.connectionExpanded) }
    fun toggleLogsExpanded() = _state.update { it.copy(logsExpanded = !it.logsExpanded) }

    fun addUser() {
        _state.update {
            val n = it.users.size + 1
            it.copy(
                users = it.users + UserAccount("user$n", "password$n", "", "", false),
                userErrors = it.userErrors + UserFieldErrors(),
            )
        }
    }

    fun removeUser(index: Int) {
        _state.update {
            it.copy(
                users = it.users.toMutableList().also { list -> list.removeAt(index) },
                userErrors = it.userErrors.toMutableList().also { list -> if (index < list.size) list.removeAt(index) },
            )
        }
    }

    fun updateUser(index: Int, transform: (UserAccount) -> UserAccount) {
        _state.update {
            val list = it.users.toMutableList()
            list[index] = transform(list[index])
            val errors = it.userErrors.toMutableList()
            while (errors.size < list.size) errors.add(UserFieldErrors())
            if (index < errors.size) errors[index] = UserFieldErrors()
            it.copy(users = list, userErrors = errors)
        }
    }

    fun setFolder(index: Int, uri: Uri, name: String) {
        val cr = getApplication<Application>().contentResolver
        cr.takePersistableUriPermission(uri, Intent.FLAG_GRANT_READ_URI_PERMISSION or Intent.FLAG_GRANT_WRITE_URI_PERMISSION)
        updateUser(index) { it.copy(folderName = name, folderUri = uri.toString()) }
    }

    fun setCertificateFile(path: String) {
        _state.update { it.copy(certificatePath = path, certificatePathError = null) }
    }

    fun toggleServer() {
        if (_state.value.running) stop() else start()
    }

    fun save() {
        if (PrivacyStore.skipSettingsSave) return
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
        if (!validateForStart()) return
        val s = _state.value
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

    private fun validateForStart(): Boolean {
        val app = getApplication<Application>()
        val s = _state.value
        val portError = if (s.port < PORT_MIN || s.port > PORT_MAX) {
            app.getString(R.string.config_port_validation, PORT_MIN, PORT_MAX)
        } else {
            null
        }
        val maxConnectionsError = if (s.maxConnections < MAX_CONNECTIONS_MIN) {
            app.getString(R.string.config_max_connections_validation, MAX_CONNECTIONS_MIN)
        } else {
            null
        }
        val certificatePathError = if (!s.useSelfSigned && s.certificatePath.isBlank()) {
            app.getString(R.string.error_select_certificate)
        } else {
            null
        }
        val userErrors = s.users.map { user ->
            UserFieldErrors(
                login = if (user.login.isBlank()) app.getString(R.string.user_username_validation) else null,
                password = if (user.password.isBlank()) app.getString(R.string.user_password_validation) else null,
                folder = if (user.folderUri.isBlank()) app.getString(R.string.user_folder_validation) else null,
            )
        }
        val usersBanner = when {
            s.users.isEmpty() -> app.getString(R.string.error_add_user)
            else -> userErrors.firstNotNullOfOrNull { it.first }
        }
        val configError = portError ?: maxConnectionsError ?: certificatePathError
        val banner = configError ?: usersBanner
        _state.update {
            it.copy(
                portError = portError,
                maxConnectionsError = maxConnectionsError,
                certificatePathError = certificatePathError,
                userErrors = userErrors,
                error = banner,
                configExpanded = it.configExpanded || configError != null,
                usersExpanded = it.usersExpanded || usersBanner != null,
            )
        }
        return banner == null
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

    override fun onCleared() {
        save()
        super.onCleared()
    }

    companion object {
        const val PORT_MIN = 2121
        const val PORT_MAX = 65535
        const val MAX_CONNECTIONS_MIN = 2
    }
}
