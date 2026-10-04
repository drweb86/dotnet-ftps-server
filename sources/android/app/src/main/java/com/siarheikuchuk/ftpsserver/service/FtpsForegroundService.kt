package com.siarheikuchuk.ftpsserver.service

import android.app.Notification
import android.app.NotificationChannel
import android.app.NotificationManager
import android.app.PendingIntent
import android.app.Service
import android.content.Intent
import android.content.pm.ServiceInfo
import android.net.wifi.WifiManager
import android.os.Build
import android.os.IBinder
import android.os.PowerManager
import androidx.core.app.NotificationCompat
import com.siarheikuchuk.ftpsserver.MainActivity
import com.siarheikuchuk.ftpsserver.R
import com.siarheikuchuk.ftpsserver.data.SettingsRepository
import com.siarheikuchuk.ftpsserver.server.Certificates
import com.siarheikuchuk.ftpsserver.server.FtpsLog
import com.siarheikuchuk.ftpsserver.server.FtpsServer
import com.siarheikuchuk.ftpsserver.server.FtpsServerConfig
import com.siarheikuchuk.ftpsserver.server.FtpsServerSettings
import com.siarheikuchuk.ftpsserver.server.FtpsUserAccount
import com.siarheikuchuk.ftpsserver.server.LoadedCertificate
import com.siarheikuchuk.ftpsserver.storage.SafFileSystemProvider
import kotlinx.coroutines.channels.BufferOverflow
import kotlinx.coroutines.flow.MutableSharedFlow
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.SharedFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asSharedFlow
import kotlinx.coroutines.flow.asStateFlow

class FtpsForegroundService : Service() {
    private var server: FtpsServer? = null
    private var wakeLock: PowerManager.WakeLock? = null
    private var wifiLock: WifiManager.WifiLock? = null

    override fun onBind(intent: Intent?): IBinder? = null

    override fun onStartCommand(intent: Intent?, flags: Int, startId: Int): Int {
        when (intent?.action) {
            ACTION_STOP -> {
                stopServer()
                stopSelf()
                return START_NOT_STICKY
            }
            else -> startServer()
        }
        return START_STICKY
    }

    private fun startServer() {
        // startForegroundService() requires startForeground() even when the
        // server is already running; otherwise Android shows "not responding".
        enterForeground()
        if (server != null) {
            ServerEvents.running(true)
            return
        }

        val pm = getSystemService(POWER_SERVICE) as PowerManager
        wakeLock = pm.newWakeLock(PowerManager.PARTIAL_WAKE_LOCK, "ftpsserver:server").apply {
            setReferenceCounted(false)
            acquire()
        }
        // HIGH_PERF is the lock that keeps Wi-Fi up with the screen off.
        // LOW_LATENCY only works while the activity is in the foreground.
        @Suppress("DEPRECATION")
        val wifi = applicationContext.getSystemService(WIFI_SERVICE) as WifiManager
        wifiLock = wifi.createWifiLock(WifiManager.WIFI_MODE_FULL_HIGH_PERF, "ftpsserver:wifi").apply {
            setReferenceCounted(false)
            acquire()
        }

        val log = object : FtpsLog {
            override fun debug(message: String) = ServerEvents.log("DEBUG", message)
            override fun info(message: String) = ServerEvents.log("INFO", message)
            override fun warn(message: String) = ServerEvents.log("WARN", message)
            override fun error(message: String, error: Throwable?) =
                ServerEvents.log("ERROR", if (error != null) "$message: ${error.message}" else message)
        }
        try {
            val saved = SettingsRepository(this).load()
            val users = saved.users.map { user ->
                FtpsUserAccount(
                    login = user.login,
                    password = user.password,
                    folder = user.folderUri,
                    canRead = true,
                    canWrite = !user.readonly,
                )
            }
            val settings = FtpsServerSettings(
                port = saved.serverPort,
                maxConnections = saved.maxConnections,
                certificatePath = if (saved.useSelfSigned) null else saved.certificatePath,
                certificatePassword = saved.certificatePassword,
            )
            val cert: LoadedCertificate = Certificates.loadOrCreate(this, filesDir, settings, log)
            ServerEvents.certificate(cert)
            val ftps = FtpsServer(log, FtpsServerConfig(settings, users), SafFileSystemProvider(this), cert)
            ftps.start()
            server = ftps
            ServerEvents.running(true)
        } catch (e: Exception) {
            ServerEvents.failed(e.message ?: e.toString())
            stopServer()
            stopSelf()
        }
    }

    private fun stopServer() {
        try {
            server?.stop()
        } catch (_: Exception) {
        }
        server = null
        wakeLock?.let { if (it.isHeld) it.release() }
        wakeLock = null
        wifiLock?.let { if (it.isHeld) it.release() }
        wifiLock = null
        ServerEvents.certificate(null)
        ServerEvents.running(false)
        stopForeground(STOP_FOREGROUND_REMOVE)
    }

    override fun onDestroy() {
        stopServer()
        super.onDestroy()
    }

    private fun enterForeground() {
        createChannel()
        val notification = buildNotification()
        if (Build.VERSION.SDK_INT >= 34) {
            startForeground(NOTIFICATION_ID, notification, ServiceInfo.FOREGROUND_SERVICE_TYPE_SPECIAL_USE)
        } else {
            startForeground(NOTIFICATION_ID, notification)
        }
    }

    private fun createChannel() {
        if (Build.VERSION.SDK_INT >= 26) {
            val mgr = getSystemService(NotificationManager::class.java)
            mgr.createNotificationChannel(
                NotificationChannel(CHANNEL_ID, getString(R.string.notification_channel), NotificationManager.IMPORTANCE_LOW)
            )
        }
    }

    private fun buildNotification(): Notification {
        val launch = PendingIntent.getActivity(
            this,
            0,
            Intent(this, MainActivity::class.java).apply {
                flags = Intent.FLAG_ACTIVITY_SINGLE_TOP or Intent.FLAG_ACTIVITY_CLEAR_TOP
            },
            PendingIntent.FLAG_IMMUTABLE or PendingIntent.FLAG_UPDATE_CURRENT,
        )
        return NotificationCompat.Builder(this, CHANNEL_ID)
            .setSmallIcon(R.drawable.ic_stat_server)
            .setContentTitle(getString(R.string.notification_title))
            .setContentText(getString(R.string.notification_text))
            .setContentIntent(launch)
            .setOngoing(true)
            .build()
    }

    companion object {
        const val ACTION_STOP = "com.siarheikuchuk.ftpsserver.STOP"
        private const val CHANNEL_ID = "ftps-server"
        private const val NOTIFICATION_ID = 1

        fun startIntent(context: android.content.Context): Intent =
            Intent(context, FtpsForegroundService::class.java)

        fun stopIntent(context: android.content.Context): Intent =
            Intent(context, FtpsForegroundService::class.java).setAction(ACTION_STOP)
    }
}

object ServerEvents {
    private val _isRunning = MutableStateFlow(false)
    val isRunning: StateFlow<Boolean> = _isRunning.asStateFlow()

    private val _loadedCertificate = MutableStateFlow<LoadedCertificate?>(null)
    val loadedCertificate: StateFlow<LoadedCertificate?> = _loadedCertificate.asStateFlow()

    private val _logs = MutableSharedFlow<Pair<String, String>>(
        extraBufferCapacity = 64,
        onBufferOverflow = BufferOverflow.DROP_OLDEST,
    )
    val logs: SharedFlow<Pair<String, String>> = _logs.asSharedFlow()

    private val _failures = MutableSharedFlow<String>(
        extraBufferCapacity = 1,
        onBufferOverflow = BufferOverflow.DROP_OLDEST,
    )
    val failures: SharedFlow<String> = _failures.asSharedFlow()

    fun log(level: String, message: String) {
        _logs.tryEmit(level to message)
    }

    fun running(value: Boolean) {
        _isRunning.value = value
    }

    fun failed(message: String) {
        _isRunning.value = false
        _failures.tryEmit(message)
    }

    fun certificate(cert: LoadedCertificate?) {
        _loadedCertificate.value = cert
    }
}
