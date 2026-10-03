package com.siarheikuchuk.ftpsserver.server

import java.net.Inet6Address
import java.net.InetAddress

class LoginThrottle {
    private val byIp = HashMap<String, Entry>()

    private class Entry(var failures: Int = 0, var lockedUntilMs: Long = 0L)

    fun isLocked(ip: String, nowMs: Long): Boolean = synchronized(byIp) {
        val entry = byIp[ip] ?: return false
        if (entry.lockedUntilMs > nowMs) return true
        if (entry.lockedUntilMs != 0L) byIp.remove(ip)
        false
    }

    fun registerSuccess(ip: String) = synchronized(byIp) {
        byIp.remove(ip)
    }

    fun registerFailure(ip: String, nowMs: Long): Boolean = synchronized(byIp) {
        var entry = byIp[ip]
        if (entry != null && entry.lockedUntilMs > nowMs) return true
        if (entry == null || entry.lockedUntilMs != 0L) entry = Entry()
        entry.failures += 1
        if (entry.failures >= FAILURE_LIMIT) {
            entry.lockedUntilMs = nowMs + LOCKOUT_MS
            byIp[ip] = entry
            return true
        }
        entry.lockedUntilMs = 0L
        byIp[ip] = entry
        false
    }

    companion object {
        const val FAILURE_LIMIT = 5
        const val FAILURE_DELAY_MS = 1_000L
        const val LOCKOUT_MS = 60_000L

        fun key(address: InetAddress): String {
            val bytes = canonical(address)
            return bytes.joinToString(":") { "%02x".format(it.toInt() and 0xff) }
        }

        private fun canonical(address: InetAddress): ByteArray {
            val bytes = address.address
            if (address is Inet6Address && bytes.size == 16) {
                val mapped = (0..9).all { bytes[it].toInt() == 0 } &&
                    bytes[10] == 0xff.toByte() && bytes[11] == 0xff.toByte()
                if (mapped) return bytes.copyOfRange(12, 16)
            }
            return bytes
        }
    }
}
