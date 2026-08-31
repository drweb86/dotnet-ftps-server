package com.siarheikuchuk.ftpsserver.ui

import android.content.Context
import com.siarheikuchuk.ftpsserver.R

fun buildConnectionDetails(context: Context, state: UiState): String {
    fun s(id: Int) = context.getString(id)
    val hosts = buildList {
        for (net in state.networks) {
            addAll(net.addresses)
        }
        if (state.hostName.isNotBlank()) add(state.hostName)
    }.distinct()

    val b = StringBuilder()
    fun line(text: String? = null) {
        if (text != null) b.append(text)
        b.append('\n')
    }

    line(s(R.string.connection_details_title))
    line()
    line(s(R.string.connection_details_intro_android))
    line()
    line(s(R.string.connection_details_clients))
    line()
    line(s(R.string.connection_details_fill_fields))
    line()
    line(s(R.string.connection_details_host_title))
    line()
    line(s(R.string.connection_details_host_body))
    line()
    for (host in hosts) line("  $host")
    line()
    line(s(R.string.connection_details_port_title))
    line()
    line(s(R.string.connection_details_port_body))
    line()
    line("  ${state.port}")
    line()
    line(s(R.string.connection_details_encryption_title))
    line()
    line(s(R.string.connection_details_encryption_body))
    line()
    line(s(R.string.connection_details_accounts_title))
    line()
    line(s(R.string.connection_details_accounts_body))
    line()
    state.users.forEachIndexed { index, user ->
        val letter = if (index < 26) ('a' + index).toString() else (index + 1).toString()
        val access = s(if (user.readonly) R.string.connection_details_access_read_only else R.string.connection_details_access_read_write)
        val folder = user.folderName.ifBlank { s(R.string.connection_details_folder_unspecified) }
        line(
            "  " + context.getString(
                R.string.connection_details_account_format,
                letter,
                user.login,
                access,
                folder,
            ),
        )
        line()
        line("     ${s(R.string.connection_details_login_label)}     ${user.login}")
        line("     ${s(R.string.connection_details_password_label)}  ${user.password}")
        line()
    }

    val cert = state.certificate
    if (state.useSelfSigned && cert != null && cert.isSelfSigned) {
        line(s(R.string.connection_details_cert_title))
        line()
        line(s(R.string.connection_details_cert_body))
        line()
        line("  " + context.getString(R.string.connection_details_cert_sha256_format, cert.fingerprintSha256))
        line()
        line("  " + context.getString(R.string.connection_details_cert_sha1_format, cert.fingerprintSha1))
        line()
        line(s(R.string.connection_details_cert_fingerprint_hint))
    }

    return b.toString().trimEnd() + "\n"
}
