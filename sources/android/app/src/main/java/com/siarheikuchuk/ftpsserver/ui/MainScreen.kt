package com.siarheikuchuk.ftpsserver.ui

import android.content.ClipData
import android.content.ClipboardManager
import android.content.Context
import android.content.Intent
import androidx.activity.compose.rememberLauncherForActivityResult
import androidx.activity.result.contract.ActivityResultContracts
import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.heightIn
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Close
import androidx.compose.material.icons.filled.PlayArrow
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.Checkbox
import androidx.compose.material3.DropdownMenu
import androidx.compose.material3.DropdownMenuItem
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.Icon
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.OutlinedTextFieldDefaults
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.material3.TopAppBar
import androidx.compose.material3.TopAppBarDefaults
import androidx.compose.runtime.Composable
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.res.stringResource
import androidx.compose.ui.text.font.FontFamily
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.siarheikuchuk.ftpsserver.BuildConfig
import com.siarheikuchuk.ftpsserver.R
import com.siarheikuchuk.ftpsserver.server.LoadedCertificate
import java.text.SimpleDateFormat
import java.util.Date
import java.util.Locale

@Composable
private fun fieldColors() = OutlinedTextFieldDefaults.colors(
    focusedTextColor = AppColors.text,
    unfocusedTextColor = AppColors.text,
    focusedBorderColor = AppColors.accent,
    unfocusedBorderColor = AppColors.muted,
    focusedLabelColor = AppColors.accent,
    unfocusedLabelColor = AppColors.muted,
    cursorColor = AppColors.accent,
)

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun MainScreen(viewModel: MainViewModel, onOpenPrivacy: () -> Unit) {
    val state by viewModel.state.collectAsState()
    val context = LocalContext.current
    var menuOpen by remember { mutableStateOf(false) }
    val scroll = rememberScrollState()

    Scaffold(
        containerColor = AppColors.background,
        topBar = {
            TopAppBar(
                title = { Text(stringResource(R.string.menu_about_format, BuildConfig.VERSION_NAME.substringBefore('-'))) },
                colors = TopAppBarDefaults.topAppBarColors(
                    containerColor = AppColors.surface,
                    titleContentColor = AppColors.text,
                ),
                actions = {
                    TextButton(onClick = { menuOpen = true }) {
                        Text("?", color = AppColors.accent, fontSize = 22.sp, fontWeight = FontWeight.Bold)
                    }
                    DropdownMenu(expanded = menuOpen, onDismissRequest = { menuOpen = false }) {
                        DropdownMenuItem(
                            text = { Text(stringResource(R.string.menu_privacy)) },
                            onClick = {
                                menuOpen = false
                                onOpenPrivacy()
                            },
                        )
                    }
                },
            )
        },
    ) { padding ->
        Column(
            modifier = Modifier
                .padding(padding)
                .fillMaxSize()
                .verticalScroll(scroll)
                .padding(12.dp),
        ) {
            Button(
                onClick = { viewModel.toggleServer() },
                modifier = Modifier.fillMaxWidth().height(56.dp),
                colors = ButtonDefaults.buttonColors(
                    containerColor = AppColors.surface,
                    contentColor = if (state.running) AppColors.error else AppColors.success,
                ),
            ) {
                Icon(if (state.running) Icons.Filled.Close else Icons.Filled.PlayArrow, contentDescription = null)
                Spacer(Modifier.width(8.dp))
                Text(
                    if (state.running) stringResource(R.string.menu_stop) else stringResource(R.string.menu_start),
                    fontWeight = FontWeight.Bold,
                    fontSize = 18.sp,
                )
            }

            if (state.error != null) {
                Row(
                    modifier = Modifier
                        .fillMaxWidth()
                        .padding(top = 8.dp)
                        .background(AppColors.error, RoundedCornerShape(4.dp))
                        .padding(8.dp),
                    verticalAlignment = Alignment.CenterVertically,
                ) {
                    Text(state.error!!, color = AppColors.text, modifier = Modifier.weight(1f))
                    TextButton(onClick = { viewModel.dismissError() }) { Text("X", color = AppColors.text) }
                }
            }

            SectionCard {
                Text(stringResource(R.string.config_title), color = AppColors.text, fontWeight = FontWeight.SemiBold, fontSize = 18.sp)
                Text(stringResource(R.string.config_access_info_android), color = AppColors.muted, fontSize = 13.sp, modifier = Modifier.padding(top = 8.dp))
                Text(stringResource(R.string.config_name) + " " + state.hostName, color = AppColors.text, fontSize = 13.sp, modifier = Modifier.padding(top = 4.dp))
                for (net in state.networks) {
                    Text("${net.name}: ${net.addresses.joinToString()}", color = AppColors.muted, fontSize = 13.sp, modifier = Modifier.padding(top = 2.dp))
                }
                NumberRow(stringResource(R.string.config_port), state.port, 2121) { viewModel.setPort(it) }
                NumberRow(stringResource(R.string.config_max_connections), state.maxConnections, 2) { viewModel.setMaxConnections(it) }
                Row(verticalAlignment = Alignment.CenterVertically) {
                    Checkbox(checked = state.useSelfSigned, onCheckedChange = { viewModel.setUseSelfSigned(it) })
                    Text(stringResource(R.string.config_use_self_signed), color = AppColors.text)
                }
                if (!state.useSelfSigned) {
                    val certPicker = rememberLauncherForActivityResult(ActivityResultContracts.GetContent()) { uri ->
                        if (uri != null) {
                            val dest = java.io.File(context.filesDir, "user-cert.pfx")
                            context.contentResolver.openInputStream(uri)?.use { input -> dest.outputStream().use { input.copyTo(it) } }
                            viewModel.setCertificateFile(dest.absolutePath)
                        }
                    }
                    Text(stringResource(R.string.config_cert_file), color = AppColors.muted, fontSize = 13.sp)
                    Text(state.certificatePath.ifBlank { "—" }, color = AppColors.text, fontSize = 13.sp)
                    OutlinedButton(onClick = { certPicker.launch("*/*") }) { Text(stringResource(R.string.config_browse), color = AppColors.accent) }
                    OutlinedTextField(
                        value = state.certificatePassword,
                        onValueChange = { viewModel.setCertificatePassword(it) },
                        label = { Text(stringResource(R.string.config_cert_password)) },
                        modifier = Modifier.fillMaxWidth().padding(top = 8.dp),
                        colors = fieldColors(),
                    )
                }
                Row(
                    modifier = Modifier.fillMaxWidth().padding(top = 12.dp),
                    horizontalArrangement = Arrangement.spacedBy(8.dp),
                ) {
                    var copied by remember { mutableStateOf(false) }
                    OutlinedButton(
                        onClick = {
                            val text = buildConnectionDetails(context, state)
                            val cm = context.getSystemService(Context.CLIPBOARD_SERVICE) as ClipboardManager
                            cm.setPrimaryClip(ClipData.newPlainText(context.getString(R.string.connection_details_share_subject), text))
                            copied = true
                        },
                        modifier = Modifier.weight(1f),
                    ) {
                        Text(
                            if (copied) stringResource(R.string.cert_copied) else stringResource(R.string.config_copy_connection_details),
                            color = AppColors.accent,
                        )
                    }
                    OutlinedButton(
                        onClick = {
                            val text = buildConnectionDetails(context, state)
                            val send = Intent(Intent.ACTION_SEND).apply {
                                type = "text/plain"
                                putExtra(Intent.EXTRA_SUBJECT, context.getString(R.string.connection_details_share_subject))
                                putExtra(Intent.EXTRA_TEXT, text)
                            }
                            context.startActivity(Intent.createChooser(send, context.getString(R.string.config_share_connection_details)))
                        },
                        modifier = Modifier.weight(1f),
                    ) {
                        Text(stringResource(R.string.config_share_connection_details), color = AppColors.accent)
                    }
                }
            }

            state.certificate?.let { CertificateCard(it) }

            SectionCard {
                Row(Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.SpaceBetween, verticalAlignment = Alignment.CenterVertically) {
                    Text(stringResource(R.string.users_tab), color = AppColors.text, fontWeight = FontWeight.SemiBold, fontSize = 18.sp)
                    OutlinedButton(onClick = { viewModel.addUser() }) { Text(stringResource(R.string.add_user_button), color = AppColors.accent) }
                }
                state.users.forEachIndexed { index, user ->
                    UserCard(index, user, viewModel)
                }
            }

            SectionCard {
                Row(Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.SpaceBetween, verticalAlignment = Alignment.CenterVertically) {
                    Text(stringResource(R.string.logs_tab), color = AppColors.text, fontWeight = FontWeight.SemiBold, fontSize = 18.sp)
                    OutlinedButton(onClick = { viewModel.clearLogs() }) { Text(stringResource(R.string.clear_logs), color = AppColors.accent) }
                }
                Column(
                    modifier = Modifier
                        .fillMaxWidth()
                        .heightIn(min = 160.dp, max = 320.dp)
                        .background(AppColors.background, RoundedCornerShape(4.dp))
                        .padding(8.dp)
                        .verticalScroll(rememberScrollState()),
                ) {
                    for (line in state.logs) {
                        val color = when (line.level) {
                            "ERROR", "FATAL" -> AppColors.error
                            "WARN" -> AppColors.warning
                            "DEBUG" -> AppColors.muted
                            else -> AppColors.accent
                        }
                        Text("[${line.timestamp}] ${line.level}: ${line.message}", color = color, fontSize = 12.sp, fontFamily = FontFamily.Monospace)
                    }
                }
            }
        }
    }
}

@Composable
private fun SectionCard(content: @Composable () -> Unit) {
    Column(
        modifier = Modifier
            .fillMaxWidth()
            .padding(top = 16.dp)
            .border(1.dp, AppColors.accent, RoundedCornerShape(8.dp))
            .background(AppColors.surface, RoundedCornerShape(8.dp))
            .padding(16.dp),
    ) { content() }
}

@Composable
private fun NumberRow(label: String, value: Int, min: Int, onChange: (Int) -> Unit) {
    Row(Modifier.fillMaxWidth().padding(top = 8.dp), verticalAlignment = Alignment.CenterVertically) {
        Text(label, color = AppColors.muted, modifier = Modifier.weight(1f))
        OutlinedButton(onClick = { onChange((value - 1).coerceAtLeast(min)) }) { Text(stringResource(R.string.numeric_down), color = AppColors.accent) }
        Text("$value", color = AppColors.text, modifier = Modifier.padding(horizontal = 12.dp))
        OutlinedButton(onClick = { onChange(value + 1) }) { Text(stringResource(R.string.numeric_up), color = AppColors.accent) }
    }
}

@Composable
private fun UserCard(index: Int, user: com.siarheikuchuk.ftpsserver.data.UserAccount, viewModel: MainViewModel) {
    val folderPicker = rememberLauncherForActivityResult(ActivityResultContracts.OpenDocumentTree()) { uri ->
        if (uri != null) {
            val name = uri.lastPathSegment?.substringAfterLast(':') ?: uri.toString()
            viewModel.setFolder(index, uri, name)
        }
    }
    Column(
        modifier = Modifier
            .fillMaxWidth()
            .padding(top = 12.dp)
            .border(1.dp, AppColors.muted, RoundedCornerShape(8.dp))
            .padding(12.dp),
    ) {
        OutlinedTextField(
            value = user.login,
            onValueChange = { v -> viewModel.updateUser(index) { it.copy(login = v) } },
            label = { Text(stringResource(R.string.user_username)) },
            modifier = Modifier.fillMaxWidth(),
            colors = fieldColors(),
        )
        OutlinedTextField(
            value = user.password,
            onValueChange = { v -> viewModel.updateUser(index) { it.copy(password = v) } },
            label = { Text(stringResource(R.string.user_password)) },
            modifier = Modifier.fillMaxWidth().padding(top = 8.dp),
            colors = fieldColors(),
        )
        Row(Modifier.fillMaxWidth().padding(top = 8.dp), verticalAlignment = Alignment.CenterVertically) {
            Text(user.folderName.ifBlank { stringResource(R.string.user_folder) }, color = AppColors.text, modifier = Modifier.weight(1f))
            OutlinedButton(onClick = { folderPicker.launch(null) }) { Text(stringResource(R.string.config_browse), color = AppColors.accent) }
        }
        Row(verticalAlignment = Alignment.CenterVertically) {
            Checkbox(checked = user.readonly, onCheckedChange = { v -> viewModel.updateUser(index) { it.copy(readonly = v) } })
            Text(stringResource(R.string.user_readonly), color = AppColors.text, modifier = Modifier.weight(1f))
            TextButton(onClick = { viewModel.removeUser(index) }) { Text(stringResource(R.string.user_delete), color = AppColors.error) }
        }
    }
}

@Composable
private fun CertificateCard(cert: LoadedCertificate) {
    val dateFmt = remember { SimpleDateFormat("yyyy-MM-dd", Locale.US) }
    SectionCard {
        Text(stringResource(R.string.cert_notice_title), color = AppColors.warning, fontWeight = FontWeight.Bold)
        Text(stringResource(R.string.cert_notice_message), color = AppColors.muted, fontSize = 12.sp, modifier = Modifier.padding(top = 6.dp, bottom = 8.dp))
        Text(stringResource(R.string.cert_section_title), color = AppColors.text, fontWeight = FontWeight.SemiBold)
        Meta(stringResource(R.string.cert_type_label), if (cert.isSelfSigned) stringResource(R.string.cert_type_self_signed) else cert.x509.type)
        Meta(stringResource(R.string.cert_subject_label), cert.x509.subjectX500Principal.name)
        Meta(
            stringResource(R.string.cert_issuer_label),
            if (cert.isSelfSigned) stringResource(R.string.cert_issuer_self) else cert.x509.issuerX500Principal.name,
        )
        Meta(stringResource(R.string.cert_valid_label), "${dateFmt.format(cert.x509.notBefore)} — ${dateFmt.format(cert.x509.notAfter)}")
        Meta(stringResource(R.string.cert_serial_label), cert.x509.serialNumber.toString(16).uppercase(Locale.US))
        Text(stringResource(R.string.cert_sha256_label), color = AppColors.text, fontWeight = FontWeight.Bold, modifier = Modifier.padding(top = 8.dp))
        FingerprintRow(cert.fingerprintSha256)
        Text(stringResource(R.string.cert_sha1_label), color = AppColors.muted, modifier = Modifier.padding(top = 8.dp))
        FingerprintRow(cert.fingerprintSha1)
    }
}

@Composable
private fun Meta(label: String, value: String) {
    Text("$label $value", color = AppColors.text, fontSize = 13.sp, modifier = Modifier.padding(top = 2.dp))
}

@Composable
private fun FingerprintRow(value: String) {
    val context = LocalContext.current
    var copied by remember { mutableStateOf(false) }
    Row(verticalAlignment = Alignment.CenterVertically) {
        Text(value, color = AppColors.text, fontFamily = FontFamily.Monospace, fontSize = 12.sp, modifier = Modifier.weight(1f))
        TextButton(onClick = {
            val cm = context.getSystemService(Context.CLIPBOARD_SERVICE) as ClipboardManager
            cm.setPrimaryClip(ClipData.newPlainText("fingerprint", value))
            copied = true
        }) { Text(if (copied) stringResource(R.string.cert_copied) else stringResource(R.string.cert_copy_button), color = AppColors.accent) }
    }
}
