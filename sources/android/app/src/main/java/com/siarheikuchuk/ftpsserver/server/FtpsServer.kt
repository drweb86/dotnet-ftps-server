package com.siarheikuchuk.ftpsserver.server

import java.net.InetAddress
import java.net.ServerSocket
import java.util.concurrent.Executors
import java.util.concurrent.atomic.AtomicBoolean
import java.util.concurrent.atomic.AtomicInteger
import javax.net.ssl.SSLContext

class FtpsServer(
    private val log: FtpsLog,
    private val config: FtpsServerConfig,
    private val fileSystem: FileSystemProvider,
    private val certificate: LoadedCertificate?,
) {
    private val running = AtomicBoolean(false)
    private val active = AtomicInteger(0)
    private val pool = Executors.newCachedThreadPool()
    private val loginThrottle = LoginThrottle()
    private val gate = Any()
    private val sessions = mutableSetOf<FtpsClientSession>()
    private var listener: ServerSocket? = null

    val loadedCertificate: LoadedCertificate? get() = certificate

    fun start() {
        val port = config.settings.port
        val bind = InetAddress.getByName(config.settings.ip)
        listener = ServerSocket(port, 50, bind)
        running.set(true)
        log.info("FTPS Server started successfully on ${config.settings.ip}:$port (Explicit encryption)")
        pool.execute { acceptLoop() }
    }

    fun stop() {
        val open: List<FtpsClientSession>
        synchronized(gate) {
            running.set(false)
            open = sessions.toList()
        }
        try {
            listener?.close()
        } catch (_: Exception) {
        }
        listener = null
        for (session in open) session.close()
        log.info("Server stopped")
    }

    private fun acceptLoop() {
        val server = listener ?: return
        while (running.get()) {
            try {
                val client = server.accept()
                client.keepAlive = true
                client.tcpNoDelay = true
                val ipKey = client.inetAddress?.let { LoginThrottle.key(it) }
                if (ipKey != null && loginThrottle.isLocked(ipKey, System.currentTimeMillis())) {
                    log.warn("Connection rejected from ${client.remoteSocketAddress}: too many failed logins")
                    client.close()
                    continue
                }
                val max = config.settings.maxConnections
                if (active.get() >= max) {
                    log.warn("Connection rejected from ${client.inetAddress}: Max connections reached")
                    client.close()
                    continue
                }
                active.incrementAndGet()
                log.info("Client connected: ${client.remoteSocketAddress} (Active: ${active.get()})")
                val session = FtpsClientSession(
                    log = log,
                    socket = client,
                    users = config.users,
                    sslContext = certificate?.sslContext,
                    fileSystem = fileSystem,
                    loginThrottle = loginThrottle,
                )
                val started = synchronized(gate) {
                    if (!running.get()) {
                        false
                    } else {
                        sessions.add(session)
                        true
                    }
                }
                if (!started) {
                    active.decrementAndGet()
                    try { client.close() } catch (_: Exception) {}
                    log.info("Client disconnected: ${client.remoteSocketAddress} (Active: ${active.get()})")
                    continue
                }
                pool.execute {
                    try {
                        session.handle()
                    } finally {
                        synchronized(gate) { sessions.remove(session) }
                        active.decrementAndGet()
                        log.info("Client disconnected: ${client.remoteSocketAddress} (Active: ${active.get()})")
                    }
                }
            } catch (e: Exception) {
                if (running.get()) log.error("Error accepting client", e)
            }
        }
    }
}
