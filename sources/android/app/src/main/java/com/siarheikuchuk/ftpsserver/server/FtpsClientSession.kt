package com.siarheikuchuk.ftpsserver.server

import java.io.BufferedReader
import java.io.InputStreamReader
import java.io.OutputStreamWriter
import java.net.Inet4Address
import java.net.ServerSocket
import java.net.Socket
import java.nio.charset.Charset
import java.nio.charset.StandardCharsets
import java.text.SimpleDateFormat
import java.util.Calendar
import java.util.Date
import java.util.Locale
import java.util.TimeZone
import javax.net.ssl.SSLContext
import javax.net.ssl.SSLSocket
import javax.net.ssl.SSLSocketFactory

class FtpsClientSession(
    private val log: FtpsLog,
    private var socket: Socket,
    private val users: List<FtpsUserAccount>,
    private val sslContext: SSLContext?,
    private val fileSystem: FileSystemProvider,
) {
    private var reader: BufferedReader = BufferedReader(InputStreamReader(socket.getInputStream(), LATIN1))
    private var writer: OutputStreamWriter = OutputStreamWriter(socket.getOutputStream(), LATIN1)
    private var encoding: Charset = LATIN1
    private var username: String? = null
    private var user: FtpsUserAccount? = null
    private var authenticated = false
    private var path = VirtualPath()
    private var renameFrom: VirtualPath? = null
    private var dataListener: ServerSocket? = null
    private var passive = false
    private var dataProtection = DataProtection.Clear
    private val clientAddress = socket.remoteSocketAddress?.toString() ?: "unknown"
    private val mlsSelected = mutableSetOf("type", "size", "modify", "perm")

    fun handle() {
        try {
            send(220, "FTPS Server Ready")
            while (true) {
                val line = reader.readLine() ?: break
                val logLine = if (line.startsWith("PASS ", ignoreCase = true)) "PASS ****" else line
                log.debug("[$clientAddress] >> $logLine")
                val space = line.indexOf(' ')
                val command = (if (space < 0) line else line.substring(0, space)).uppercase(Locale.ROOT)
                val argument = if (space < 0) "" else line.substring(space + 1)
                process(command, argument)
                if (command == "QUIT") break
            }
        } catch (e: Exception) {
            log.error("[$clientAddress] Session error", e)
        } finally {
            try { socket.close() } catch (_: Exception) {}
            try { dataListener?.close() } catch (_: Exception) {}
        }
    }

    private fun process(command: String, argument: String) {
        try {
            when (command) {
                "USER" -> handleUser(argument)
                "PASS" -> handlePass(argument)
                "AUTH" -> handleAuth(argument)
                "PBSZ" -> send(200, "PBSZ=0")
                "PROT" -> handleProt(argument)
                "PWD", "XPWD" -> handlePwd()
                "CWD", "XCWD" -> handleCwd(argument)
                "CDUP", "XCUP" -> handleCdup()
                "MKD", "XMKD" -> handleMkd(argument)
                "RMD", "XRMD" -> handleRmd(argument)
                "DELE" -> handleDele(argument)
                "RNFR" -> handleRnfr(argument)
                "RNTO" -> handleRnto(argument)
                "TYPE" -> {
                    if (!checkAuth()) send(530, "Not logged in")
                    else send(200, "Type set to ${argument.uppercase(Locale.ROOT)}")
                }
                "PASV" -> handlePasv()
                "LIST" -> handleList(argument)
                "MLSD" -> handleMlsd(argument)
                "MLST" -> handleMlst(argument)
                "NLST" -> handleNlst(argument)
                "RETR" -> handleRetr(argument)
                "STOR" -> handleStor(argument)
                "SIZE" -> handleSize(argument)
                "MDTM" -> handleMdtm(argument)
                "SYST" -> send(215, "UNIX Type: L8")
                "FEAT" -> handleFeat()
                "OPTS" -> handleOpts(argument)
                "NOOP" -> send(200, "OK")
                "QUIT" -> {
                    send(221, "Goodbye")
                    log.info("[$clientAddress] User ${username ?: "anonymous"} logged out")
                }
                else -> send(502, "Command '$command' not implemented")
            }
        } catch (e: SecurityException) {
            log.warn("[$clientAddress] Access denied: ${e.message}")
            send(550, "Permission denied")
        } catch (e: Exception) {
            log.error("[$clientAddress] Command error: $command", e)
            send(550, "Error: ${e.message}")
        }
    }

    private fun handleOpts(argument: String) {
        val args = argument.split(Regex("\\s+")).filter { it.isNotEmpty() }
        if (args.isNotEmpty() && args[0].equals("MLST", true)) {
            mlsSelected.clear()
            if (args.size >= 2) {
                val names = args.drop(1).joinToString(" ").split(';').map { it.trim() }.filter { it.isNotEmpty() }
                for (name in names) {
                    val match = MLS_FACTS.firstOrNull { it.equals(name, true) }
                    if (match != null) mlsSelected += match
                }
            }
            var selected = MLS_FACTS.filter { it in mlsSelected }.joinToString(";")
            if (selected.isNotEmpty()) selected += ";"
            send(200, "MLST OPTS $selected")
            return
        }
        if (args.isNotEmpty() && args[0].equals("UTF8", true)) {
            if (args.size == 1 || args[1].equals("ON", true)) {
                encoding = StandardCharsets.UTF_8
                recreateStreams()
                send(200, "UTF8 mode enabled")
                return
            }
            if (args[1].equals("OFF", true)) {
                encoding = LATIN1
                recreateStreams()
                send(200, "UTF8 mode disabled")
                return
            }
        }
        send(501, "Invalid OPTS command")
    }

    private fun recreateStreams() {
        writer.flush()
        writer = OutputStreamWriter(socket.getOutputStream(), encoding)
        reader = BufferedReader(InputStreamReader(socket.getInputStream(), encoding))
    }

    private fun handleUser(name: String) {
        username = name
        log.info("[$clientAddress] User login attempt: $name")
        send(331, "Password required")
    }

    private fun handlePass(password: String) {
        val name = username
        if (name.isNullOrEmpty()) {
            send(503, "Login with USER first")
            return
        }
        val found = users.firstOrNull { it.login == name }
        if (found != null && found.password == password) {
            user = found
            authenticated = true
            path = VirtualPath()
            log.info("[$clientAddress] User logged in: $name")
            send(230, "User logged in")
        } else {
            log.warn("[$clientAddress] Failed login attempt for user: $name")
            username = null
            send(530, "Login incorrect")
        }
    }

    private fun handleAuth(argument: String) {
        val ctx = sslContext
        if (ctx == null) {
            send(502, "TLS not available")
            return
        }
        if (!argument.equals("TLS", true) && !argument.equals("SSL", true)) {
            send(504, "AUTH type not supported")
            return
        }
        send(234, "AUTH command ok. Expecting TLS Negotiation.")
        try {
            val factory = ctx.socketFactory as SSLSocketFactory
            val ssl = factory.createSocket(socket, socket.inetAddress.hostAddress, socket.port, true) as SSLSocket
            ssl.useClientMode = false
            ssl.startHandshake()
            socket = ssl
            recreateStreams()
            log.info("[$clientAddress] TLS enabled on control connection")
        } catch (e: Exception) {
            log.error("[$clientAddress] TLS negotiation failed", e)
        }
    }

    private fun handleProt(argument: String) {
        when (argument.uppercase(Locale.ROOT)) {
            "P" -> {
                dataProtection = DataProtection.Protected
                send(200, "Protection level set to Private")
            }
            "C" -> {
                dataProtection = DataProtection.Clear
                send(200, "Protection level set to Clear")
            }
            else -> send(504, "PROT type not supported")
        }
    }

    private fun handlePwd() {
        if (!checkAuth()) {
            send(530, "Not logged in")
            return
        }
        val current = path.toFtpsPath()
        info("get current dir", current)
        send(257, "\"$current\" is current directory")
    }

    private fun handleCwd(directory: String) {
        if (!checkAuth() || !checkPerm(read = true, write = false)) {
            send(550, "Permission denied")
            return
        }
        val result = path.append(directory)
        info("change directory", result.toFtpsPath())
        try {
            if (fileSystem.directoryExists(user!!.folder, result.parts)) {
                path = result
                send(250, "Directory changed")
            } else {
                send(550, "Directory not found")
            }
        } catch (e: SecurityException) {
            log.error("[$clientAddress] Attempt to change directory to: $directory", e)
            send(550, "Directory not found")
        }
    }

    private fun handleCdup() {
        if (!checkAuth() || !checkPerm(read = true, write = false)) {
            send(550, "Permission denied")
            return
        }
        path = path.goUp()
        info("change directory up", path.toFtpsPath())
        send(250, "Directory changed")
    }

    private fun handleMkd(directory: String) {
        if (!checkAuth() || !checkPerm(read = false, write = true)) {
            send(550, "Permission denied")
            return
        }
        val p = path.append(directory)
        info("create directory", p.toFtpsPath())
        try {
            fileSystem.createDirectory(user!!.folder, p.parts)
            send(257, "\"${p.toFtpsPath()}\" created")
        } catch (e: Exception) {
            log.error("Failed to create directory: ${p.toFtpsPath()}", e)
            send(550, "Cannot create directory: ${e.message}")
        }
    }

    private fun handleRmd(directory: String) {
        if (!checkAuth() || !checkPerm(read = false, write = true)) {
            send(550, "Permission denied")
            return
        }
        val p = path.append(directory)
        info("delete directory", p.toFtpsPath())
        try {
            if (fileSystem.directoryExists(user!!.folder, p.parts)) {
                fileSystem.directoryDelete(user!!.folder, p.parts)
                send(250, "Directory removed")
            } else {
                send(550, "Directory not found")
            }
        } catch (e: Exception) {
            log.error("Failed to delete directory: ${p.toFtpsPath()}", e)
            send(550, "Cannot remove directory: ${e.message}")
        }
    }

    private fun handleDele(filename: String) {
        if (!checkAuth() || !checkPerm(read = false, write = true)) {
            send(550, "Permission denied")
            return
        }
        val p = path.append(filename)
        info("delete file", p.toFtpsPath())
        try {
            if (fileSystem.fileExists(user!!.folder, p.parts)) {
                fileSystem.fileDelete(user!!.folder, p.parts)
                send(250, "File deleted")
            } else {
                send(550, "File not found")
            }
        } catch (e: Exception) {
            log.error("Failed to delete file: ${p.toFtpsPath()}", e)
            send(550, "Cannot delete file: ${e.message}")
        }
    }

    private fun handleRnfr(filename: String) {
        if (!checkAuth() || !checkPerm(read = false, write = true)) {
            send(550, "Permission denied")
            return
        }
        val p = path.append(filename)
        info("rename from", p.toFtpsPath())
        renameFrom = p
        if (fileSystem.fileExists(user!!.folder, p.parts) || fileSystem.directoryExists(user!!.folder, p.parts)) {
            send(350, "Ready for RNTO")
        } else {
            renameFrom = null
            send(550, "File/directory not found")
        }
    }

    private fun handleRnto(filename: String) {
        if (!checkAuth() || !checkPerm(read = false, write = true)) {
            send(550, "Permission denied")
            return
        }
        val from = renameFrom
        if (from == null) {
            send(503, "RNFR required first")
            return
        }
        val p = path.append(filename)
        info("rename to", p.toFtpsPath())
        try {
            when {
                fileSystem.fileExists(user!!.folder, from.parts) -> {
                    fileSystem.fileMove(user!!.folder, from.parts, p.parts)
                    send(250, "File renamed")
                }
                fileSystem.directoryExists(user!!.folder, from.parts) -> {
                    fileSystem.directoryMove(user!!.folder, from.parts, p.parts)
                    send(250, "Directory renamed")
                }
                else -> send(550, "Rename failed")
            }
        } catch (e: Exception) {
            log.error("rename failed", e)
            send(550, "Rename failed: ${e.message}")
        } finally {
            renameFrom = null
        }
    }

    private fun handlePasv() {
        if (!checkAuth()) {
            send(530, "Not logged in")
            return
        }
        try { dataListener?.close() } catch (_: Exception) {}
        val listener = ServerSocket(0)
        dataListener = listener
        passive = true
        val local = socket.localAddress
        val ip = if (local is Inet4Address) local.address else byteArrayOf(127, 0, 0, 1)
        val port = listener.localPort
        val response = "Entering Passive Mode (${ip[0].toUByte()},${ip[1].toUByte()},${ip[2].toUByte()},${ip[3].toUByte()},${port / 256},${port % 256})"
        log.debug("[$clientAddress] $response")
        send(227, response)
    }

    private fun wrapData(client: Socket): Socket {
        if (dataProtection != DataProtection.Protected || sslContext == null) return client
        val factory = sslContext.socketFactory as SSLSocketFactory
        val ssl = factory.createSocket(client, client.inetAddress.hostAddress, client.port, true) as SSLSocket
        ssl.useClientMode = false
        ssl.startHandshake()
        return ssl
    }

    private fun acceptData(): Socket? {
        val listener = dataListener
        if (!passive || listener == null) {
            send(425, "Use PASV first")
            return null
        }
        if (!dataEncryptedOk()) {
            send(534, "Data connection must be encrypted; use AUTH TLS and PROT P")
            return null
        }
        return try {
            listener.accept()
        } catch (_: Exception) {
            send(425, "Can't open data connection")
            null
        }
    }

    private fun closePasv() {
        try { dataListener?.close() } catch (_: Exception) {}
        dataListener = null
        passive = false
    }

    private fun handleList(directory: String) {
        if (!checkAuth() || !checkPerm(read = true, write = false)) {
            send(550, "Permission denied")
            return
        }
        val dataClient = acceptData() ?: return
        val p = path.append(directory.ifBlank { "." })
        info("get directory contents", p.toFtpsPath())
        send(150, "Opening data connection")
        try {
            wrapData(dataClient).use { sock ->
                OutputStreamWriter(sock.getOutputStream(), StandardCharsets.UTF_8).use { out ->
                    if (fileSystem.directoryExists(user!!.folder, p.parts)) {
                        for (entry in fileSystem.directoryEntries(user!!.folder, p.parts)) {
                            val permissions = if (entry.isDirectory) "drwxr-xr-x" else "-rw-r--r--"
                            val size = if (entry.isDirectory) "0" else entry.length.toString()
                            val modified = formatUnixListDate(entry.lastWriteTimeMillis)
                            out.write("$permissions 1 owner group ${size.padStart(15)} $modified ${entry.fileName}\r\n")
                        }
                        out.flush()
                    }
                }
            }
            send(226, "Transfer complete")
        } catch (e: Exception) {
            log.error("List failed", e)
            send(550, "List failed: ${e.message}")
        } finally {
            closePasv()
        }
    }

    private fun handleMlsd(directory: String) {
        if (!checkAuth() || !checkPerm(read = true, write = false)) {
            send(550, "Permission denied")
            return
        }
        val dataClient = acceptData() ?: return
        val p = path.append(directory.ifBlank { "." })
        info("mlsd", p.toFtpsPath())
        try {
            if (!fileSystem.directoryExists(user!!.folder, p.parts)) {
                dataClient.close()
                send(550, "Directory not found")
                return
            }
            send(150, "Opening data connection")
            wrapData(dataClient).use { sock ->
                OutputStreamWriter(sock.getOutputStream(), StandardCharsets.UTF_8).use { out ->
                    for (entry in fileSystem.directoryEntries(user!!.folder, p.parts)) {
                        out.write(formatMlsRecord(mlsType(entry.fileName, entry.isDirectory), entry.lastWriteTimeMillis, entry.length, entry.isDirectory, entry.fileName) + "\r\n")
                    }
                    out.flush()
                }
            }
            send(226, "Transfer complete")
        } catch (e: Exception) {
            log.error("MLSD failed", e)
            send(550, "MLSD failed: ${e.message}")
        } finally {
            closePasv()
        }
    }

    private fun handleMlst(pathname: String) {
        if (!checkAuth() || !checkPerm(read = true, write = false)) {
            send(550, "Permission denied")
            return
        }
        val p = if (pathname.isBlank()) path else path.append(pathname)
        val ftpsPath = p.toFtpsPath()
        info("mlst", ftpsPath)
        when {
            fileSystem.directoryExists(user!!.folder, p.parts) -> {
                val self = fileSystem.directoryEntries(user!!.folder, p.parts).firstOrNull { it.fileName == "." }
                val isCurrent = pathname.isBlank() || pathname == "." || pathname == "./"
                val type = if (isCurrent) "cdir" else "dir"
                val display = if (isCurrent) ftpsPath else pathname
                sendLine("250-Listing $ftpsPath")
                sendLine(" " + formatMlsRecord(type, self?.lastWriteTimeMillis ?: System.currentTimeMillis(), 0, true, display))
                send(250, "End")
            }
            fileSystem.fileExists(user!!.folder, p.parts) -> {
                val display = if (pathname.isBlank()) ftpsPath else pathname
                sendLine("250-Listing $ftpsPath")
                sendLine(
                    " " + formatMlsRecord(
                        "file",
                        fileSystem.getFileLastWriteTimeUtcMillis(user!!.folder, p.parts),
                        fileSystem.getFileLength(user!!.folder, p.parts),
                        false,
                        display,
                    )
                )
                send(250, "End")
            }
            else -> send(550, "File not found")
        }
    }

    private fun handleNlst(directory: String) {
        if (!checkAuth() || !checkPerm(read = true, write = false)) {
            send(550, "Permission denied")
            return
        }
        val dataClient = acceptData() ?: return
        val p = path.append(directory.ifBlank { "." })
        info("get directory contents 2", p.toFtpsPath())
        send(150, "Opening data connection")
        try {
            wrapData(dataClient).use { sock ->
                OutputStreamWriter(sock.getOutputStream(), StandardCharsets.UTF_8).use { out ->
                    if (fileSystem.directoryExists(user!!.folder, p.parts)) {
                        for (entry in fileSystem.directoryEntries(user!!.folder, p.parts)) {
                            out.write(entry.fileName + "\r\n")
                        }
                        out.flush()
                    }
                }
            }
            send(226, "Transfer complete")
        } catch (e: Exception) {
            log.error("List failed", e)
            send(550, "List failed: ${e.message}")
        } finally {
            closePasv()
        }
    }

    private fun handleRetr(filename: String) {
        if (!checkAuth() || !checkPerm(read = true, write = false)) {
            send(550, "Permission denied")
            return
        }
        val dataClient = acceptData() ?: return
        val p = path.append(filename)
        info("download", p.toFtpsPath())
        if (!fileSystem.fileExists(user!!.folder, p.parts)) {
            dataClient.close()
            send(550, "File not found")
            return
        }
        send(150, "Opening data connection")
        try {
            wrapData(dataClient).use { sock ->
                fileSystem.fileOpenRead(user!!.folder, p.parts).use { input ->
                    input.copyTo(sock.getOutputStream())
                    sock.getOutputStream().flush()
                }
            }
            log.info("[$clientAddress] Download complete: $p")
            send(226, "Transfer complete")
        } catch (e: Exception) {
            log.error("Download failed: ${p.toFtpsPath()}", e)
            send(550, "Transfer failed: ${e.message}")
        } finally {
            closePasv()
        }
    }

    private fun handleStor(filename: String) {
        if (!checkAuth() || !checkPerm(read = false, write = true)) {
            send(550, "Permission denied")
            return
        }
        val dataClient = acceptData() ?: return
        val p = path.append(filename)
        info("upload", p.toFtpsPath())
        send(150, "Opening data connection for ${p.toFtpsPath()}")
        try {
            wrapData(dataClient).use { sock ->
                fileSystem.fileCreate(user!!.folder, p.parts).use { output ->
                    sock.getInputStream().copyTo(output)
                    output.flush()
                }
            }
            send(226, "Transfer complete")
        } catch (e: Exception) {
            log.error("Upload failed: ${p.toFtpsPath()}", e)
            send(550, "Transfer failed: ${e.message}")
        } finally {
            closePasv()
        }
    }

    private fun handleSize(filename: String) {
        if (!checkAuth() || !checkPerm(read = true, write = false)) {
            send(550, "Permission denied")
            return
        }
        val p = path.append(filename)
        info("get size", p.toFtpsPath())
        if (fileSystem.fileExists(user!!.folder, p.parts)) {
            send(213, fileSystem.getFileLength(user!!.folder, p.parts).toString())
        } else {
            send(550, "File not found")
        }
    }

    private fun handleMdtm(filename: String) {
        if (!checkAuth() || !checkPerm(read = true, write = false)) {
            send(550, "Permission denied")
            return
        }
        val p = path.append(filename)
        info("get modified time", p.toFtpsPath())
        if (fileSystem.fileExists(user!!.folder, p.parts)) {
            val utc = fileSystem.getFileLastWriteTimeUtcMillis(user!!.folder, p.parts)
            val fmt = SimpleDateFormat("yyyyMMddHHmmss", Locale.US).apply { timeZone = TimeZone.getTimeZone("UTC") }
            send(213, fmt.format(Date(utc)))
        } else {
            send(550, "File not found")
        }
    }

    private fun handleFeat() {
        val mlst = MLS_FACTS.joinToString(";") { if (it in mlsSelected) "$it*" else it } + ";"
        listOf(
            "211-Features:",
            " AUTH TLS",
            " PBSZ",
            " PROT",
            " SIZE",
            " MDTM",
            " MLST $mlst",
            " UTF8",
            "211 End",
        ).forEach { sendLine(it) }
    }

    private fun formatMlsRecord(type: String, lastWriteMillis: Long, size: Long, isDirectory: Boolean, pathname: String): String {
        val facts = mutableListOf<String>()
        if ("type" in mlsSelected) facts += "type=$type"
        if ("size" in mlsSelected && !isDirectory) facts += "size=$size"
        if ("modify" in mlsSelected) {
            val fmt = SimpleDateFormat("yyyyMMddHHmmss", Locale.US).apply { timeZone = TimeZone.getTimeZone("UTC") }
            facts += "modify=${fmt.format(Date(lastWriteMillis))}"
        }
        if ("perm" in mlsSelected) facts += "perm=${mlsPerm(isDirectory)}"
        val factString = if (facts.isEmpty()) "" else facts.joinToString(";") + ";"
        return "$factString $pathname"
    }

    private fun mlsPerm(isDirectory: Boolean): String {
        val read = user?.canRead == true
        val write = user?.canWrite == true
        return if (isDirectory) {
            buildString {
                if (write) append("c")
                if (write) append("d")
                if (read) append("e")
                if (write) append("f")
                if (read) append("l")
                if (write) append("m")
                if (write) append("p")
            }
        } else {
            buildString {
                if (write) append("a")
                if (write) append("d")
                if (write) append("f")
                if (read) append("r")
                if (write) append("w")
            }
        }
    }

    private fun formatUnixListDate(millis: Long): String {
        val date = Date(millis)
        val now = Calendar.getInstance()
        val then = Calendar.getInstance().apply { time = date }
        val sixMonthsMs = 31556952L / 2 * 1000
        val age = now.timeInMillis - millis
        val recent = age >= 0 && age < sixMonthsMs
        val fmt = SimpleDateFormat(if (recent) "MMM dd HH:mm" else "MMM dd  yyyy", Locale.US)
        return fmt.format(date)
    }

    private fun checkAuth() = authenticated && user != null
    private fun dataEncryptedOk() = sslContext == null || dataProtection == DataProtection.Protected
    private fun checkPerm(read: Boolean, write: Boolean): Boolean {
        val u = user ?: return false
        return (!read || u.canRead) && (!write || u.canWrite)
    }

    private fun info(command: String, text: String) {
        log.info("[${user?.login}] $command: $text")
    }

    private fun send(code: Int, message: String) = sendLine("$code $message")

    private fun sendLine(line: String) {
        log.debug("[$clientAddress] << $line")
        val bytes = (line + "\r\n").toByteArray(encoding)
        socket.getOutputStream().write(bytes)
        socket.getOutputStream().flush()
        writer = OutputStreamWriter(socket.getOutputStream(), encoding)
    }

    companion object {
        private val LATIN1 = Charset.forName("ISO-8859-1")
        private val MLS_FACTS = listOf("type", "size", "modify", "perm")
        private fun mlsType(name: String, isDir: Boolean) = when (name) {
            "." -> "cdir"
            ".." -> "pdir"
            else -> if (isDir) "dir" else "file"
        }
    }
}
