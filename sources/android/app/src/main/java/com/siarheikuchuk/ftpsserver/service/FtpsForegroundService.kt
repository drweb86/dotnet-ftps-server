package com.siarheikuchuk.ftpsserver.service

import android.app.Notification
import android.app.NotificationChannel
import android.app.NotificationManager
import android.app.PendingIntent
import android.app.Service
import android.content.Intent
import android.content.pm.ServiceInfo
import android.os.Build
import android.os.IBinder
import android.os.PowerManager
import androidx.core.app.NotificationCompat
import com.siarheikuchuk.ftpsserver.MainActivity
import com.siarheikuchuk.ftpsserver.R
import com.siarheikuchuk.ftpsserver.server.Certificates
import com.siarheikuchuk.ftpsserver.server.FtpsLog
import com.siarheikuchuk.ftpsserver.server.FtpsServer
import com.siarheikuchuk.ftpsserver.server.FtpsServerConfig
import com.siarheikuchuk.ftpsserver.server.FtpsServerSettings
import com.siarheikuchuk.ftpsserver.server.FtpsUserAccount
import com.siarheikuchuk.ftpsserver.server.LoadedCertificate
import com.siarheikuchuk.ftpsserver.storage.SafFileSystemProvider

class FtpsForegroundService : Service() {
    private var server: FtpsServer? = null
    private var wakeLock: PowerManager.WakeLock? = null

    override fun onBind(intent: Intent?): IBinder? = null

    override fun onStartCommand(intent: Intent?, flags: Int, startId: Int): Int {
        when (intent?.action) {
            ACTION_STOP -> {
                stopServer()
                stopSelf()
                return START_NOT_STICKY
            }
            else -> startServer(intent)
        }
        return START_STICKY
    }

    private fun startServer(intent: Intent?) {
        if (server != null) return
        createChannel()
        val notification = buildNotification()
        if (Build.VERSION.SDK_INT >= 34) {
            startForeground(NOTIFICATION_ID, notification, ServiceInfo.FOREGROUND_SERVICE_TYPE_SPECIAL_USE)
        } else {
            startForeground(NOTIFICATION_ID, notification)
        }

        val pm = getSystemService(POWER_SERVICE) as PowerManager
        wakeLock = pm.newWakeLock(PowerManager.PARTIAL_WAKE_LOCK, "ftpsserver:server").also { it.acquire() }

        val port = intent?.getIntExtra(EXTRA_PORT, 2121) ?: 2121
        val maxConn = intent?.getIntExtra(EXTRA_MAX, 10) ?: 10
        val selfSigned = intent?.getBooleanExtra(EXTRA_SELF_SIGNED, true) ?: true
        val certPath = intent?.getStringExtra(EXTRA_CERT_PATH)
        val certPassword = intent?.getStringExtra(EXTRA_CERT_PASSWORD)
        val logins = intent?.getStringArrayExtra(EXTRA_LOGINS) ?: emptyArray()
        val passwords = intent?.getStringArrayExtra(EXTRA_PASSWORDS) ?: emptyArray()
        val folders = intent?.getStringArrayExtra(EXTRA_FOLDERS) ?: emptyArray()
        val writes = intent?.getBooleanArrayExtra(EXTRA_WRITES) ?: BooleanArray(logins.size) { true }

        val users = logins.indices.map { i ->
            FtpsUserAccount(
                login = logins[i],
                password = passwords[i],
                folder = folders[i],
                canRead = true,
                canWrite = writes.getOrElse(i) { true },
            )
        }
        val settings = FtpsServerSettings(
            port = port,
            maxConnections = maxConn,
            certificatePath = if (selfSigned) null else certPath,
            certificatePassword = certPassword,
        )
        val log = object : FtpsLog {
            override fun debug(message: String) = ServerEvents.log("DEBUG", message)
            override fun info(message: String) = ServerEvents.log("INFO", message)
            override fun warn(message: String) = ServerEvents.log("WARN", message)
            override fun error(message: String, error: Throwable?) =
                ServerEvents.log("ERROR", if (error != null) "$message: ${error.message}" else message)
        }
        try {
            val cert: LoadedCertificate = Certificates.loadOrCreate(filesDir, settings, log)
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
        ServerEvents.certificate(null)
        ServerEvents.running(false)
        stopForeground(STOP_FOREGROUND_REMOVE)
    }

    override fun onDestroy() {
        stopServer()
        super.onDestroy()
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
            Intent(this, MainActivity::class.java),
            PendingIntent.FLAG_IMMUTABLE,
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
        const val EXTRA_PORT = "port"
        const val EXTRA_MAX = "max"
        const val EXTRA_SELF_SIGNED = "selfSigned"
        const val EXTRA_CERT_PATH = "certPath"
        const val EXTRA_CERT_PASSWORD = "certPassword"
        const val EXTRA_LOGINS = "logins"
        const val EXTRA_PASSWORDS = "passwords"
        const val EXTRA_FOLDERS = "folders"
        const val EXTRA_WRITES = "writes"
        private const val CHANNEL_ID = "ftps-server"
        private const val NOTIFICATION_ID = 1

        fun startIntent(context: android.content.Context): Intent =
            Intent(context, FtpsForegroundService::class.java)

        fun stopIntent(context: android.content.Context): Intent =
            Intent(context, FtpsForegroundService::class.java).setAction(ACTION_STOP)
    }
}

object ServerEvents {
    @Volatile var onLog: ((String, String) -> Unit)? = null
    @Volatile var onRunning: ((Boolean) -> Unit)? = null
    @Volatile var onFailed: ((String) -> Unit)? = null
    @Volatile var onCertificate: ((com.siarheikuchuk.ftpsserver.server.LoadedCertificate?) -> Unit)? = null

    fun log(level: String, message: String) {
        onLog?.invoke(level, message)
    }

    fun running(value: Boolean) {
        onRunning?.invoke(value)
    }

    fun failed(message: String) {
        onFailed?.invoke(message)
    }

    fun certificate(cert: com.siarheikuchuk.ftpsserver.server.LoadedCertificate?) {
        onCertificate?.invoke(cert)
    }
}
