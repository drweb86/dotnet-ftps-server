package com.siarheikuchuk.ftpsserver.ui

import android.content.ClipData
import android.content.ClipboardManager
import android.content.Context
import android.content.Intent
import androidx.activity.compose.rememberLauncherForActivityResult
import androidx.activity.result.contract.ActivityResultContracts
import androidx.compose.animation.AnimatedVisibility
import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.ColumnScope
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.heightIn
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.widthIn
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.text.selection.SelectionContainer
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.Checkbox
import androidx.compose.material3.DropdownMenu
import androidx.compose.material3.DropdownMenuItem
import androidx.compose.material3.ExperimentalMaterial3Api
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

@Composable
private fun fieldColors() = OutlinedTextFieldDefaults.colors(
    focusedTextColor = AppColors.text,
    unfocusedTextColor = AppColors.text,
    focusedBorderColor = AppColors.accent,
    unfocusedBorderColor = AppColors.muted,
    focusedLabelColor = AppColors.accent,
    unfocusedLabelColor = AppColors.muted,
    cursorColor = AppColors.accent,
    errorBorderColor = AppColors.error,
    errorLabelColor = AppColors.error,
    errorSupportingTextColor = AppColors.error,
    errorCursorColor = AppColors.error,
)

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun MainScreen(viewModel: MainViewModel, onOpenPrivacy: () -> Unit) {
    val state by viewModel.state.collectAsState()
    val context = LocalContext.current
    var menuOpen by remember { mutableStateOf(false) }
    val scroll = rememberScrollState()
    val certPicker = rememberLauncherForActivityResult(ActivityResultContracts.GetContent()) { uri ->
        if (uri != null) {
            val dest = java.io.File(context.filesDir, "user-cert.pfx")
            context.contentResolver.openInputStream(uri)?.use { input -> dest.outputStream().use { input.copyTo(it) } }
            viewModel.setCertificateFile(dest.absolutePath)
        }
    }

    Scaffold(
        containerColor = AppColors.background,
        topBar = {
            TopAppBar(
                title = {
                    val version = BuildConfig.VERSION_NAME.substringBefore('-')
                    Text(
                        "${stringResource(R.string.app_title)} - ${stringResource(R.string.developer_name)} - V$version"
                    )
                },
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
        Box(
            modifier = Modifier
                .padding(padding)
                .fillMaxSize()
                .verticalScroll(scroll),
            contentAlignment = Alignment.TopCenter,
        ) {
            Column(
                modifier = Modifier
                    .widthIn(max = 720.dp)
                    .fillMaxWidth()
                    .padding(24.dp),
            ) {
                if (state.error != null) {
                    Row(
                        modifier = Modifier
                            .fillMaxWidth()
                            .padding(bottom = 8.dp)
                            .background(AppColors.error, RoundedCornerShape(4.dp))
                            .padding(12.dp),
                        verticalAlignment = Alignment.CenterVertically,
                    ) {
                        Text(state.error!!, color = AppColors.text, modifier = Modifier.weight(1f))
                    }
                }

                Button(
                    onClick = { viewModel.toggleServer() },
                    modifier = Modifier.fillMaxWidth().height(72.dp).padding(bottom = 16.dp),
                    colors = ButtonDefaults.buttonColors(
                        containerColor = AppColors.surface,
                        contentColor = if (state.running) AppColors.error else AppColors.success,
                    ),
                ) {
                    Text(
                        if (state.running) stringResource(R.string.menu_stop) else stringResource(R.string.menu_start),
                        fontWeight = FontWeight.Bold,
                        fontSize = 22.sp,
                    )
                }

                if (!state.running) {
                    ExpandableSection(
                        icon = "🔒",
                        title = stringResource(R.string.config_title),
                        expanded = state.configExpanded,
                        onToggle = { viewModel.toggleConfigExpanded() },
                    ) {
                        NumberField(
                            label = stringResource(R.string.config_port),
                            value = state.port,
                            error = state.portError,
                            help = stringResource(R.string.config_port_help),
                            onNudge = { viewModel.nudgePort(it) },
                            onValueChange = { viewModel.setPort(it) },
                        )
                        NumberField(
                            label = stringResource(R.string.config_max_connections),
                            value = state.maxConnections,
                            error = state.maxConnectionsError,
                            help = null,
                            onNudge = { viewModel.nudgeMaxConnections(it) },
                            onValueChange = { viewModel.setMaxConnections(it) },
                        )
                        Row(verticalAlignment = Alignment.CenterVertically) {
                            Checkbox(checked = state.useSelfSigned, onCheckedChange = { viewModel.setUseSelfSigned(it) })
                            Text(stringResource(R.string.config_use_self_signed), color = AppColors.text)
                        }
                        if (!state.useSelfSigned) {
                            OutlinedTextField(
                                value = state.certificatePath.ifBlank { "" },
                                onValueChange = {},
                                readOnly = true,
                                label = { Text(stringResource(R.string.config_cert_file)) },
                                isError = state.certificatePathError != null,
                                supportingText = {
                                    if (state.certificatePathError != null) {
                                        Text(state.certificatePathError!!)
                                    }
                                },
                                modifier = Modifier.fillMaxWidth().padding(top = 8.dp),
                                colors = fieldColors(),
                            )
                            OutlinedButton(onClick = { certPicker.launch("*/*") }) {
                                Text(stringResource(R.string.config_browse), color = AppColors.accent)
                            }
                            OutlinedTextField(
                                value = state.certificatePassword,
                                onValueChange = { viewModel.setCertificatePassword(it) },
                                label = { Text(stringResource(R.string.config_cert_password)) },
                                modifier = Modifier.fillMaxWidth().padding(top = 8.dp),
                                colors = fieldColors(),
                            )
                        }
                    }

                    ExpandableSection(
                        icon = "👤",
                        title = stringResource(R.string.users_tab),
                        expanded = state.usersExpanded,
                        onToggle = { viewModel.toggleUsersExpanded() },
                    ) {
                        OutlinedButton(
                            onClick = { viewModel.addUser() },
                            modifier = Modifier.padding(bottom = 8.dp),
                        ) { Text(stringResource(R.string.add_user_button), color = AppColors.accent) }
                        state.users.forEachIndexed { index, user ->
                            UserCard(index, user, state.userErrors.getOrNull(index) ?: UserFieldErrors(), viewModel)
                        }
                    }
                }

                if (state.running) {
                    ExpandableSection(
                        icon = "📋",
                        title = stringResource(R.string.connection_instruction_title),
                        expanded = state.connectionExpanded,
                        onToggle = { viewModel.toggleConnectionExpanded() },
                    ) {
                        val details = remember(state) { buildConnectionDetails(context, state) }
                        Row(
                            modifier = Modifier.fillMaxWidth(),
                            horizontalArrangement = Arrangement.spacedBy(8.dp),
                        ) {
                            var copied by remember { mutableStateOf(false) }
                            OutlinedButton(
                                onClick = {
                                    val cm = context.getSystemService(Context.CLIPBOARD_SERVICE) as ClipboardManager
                                    cm.setPrimaryClip(ClipData.newPlainText(context.getString(R.string.connection_details_share_subject), details))
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
                                    val send = Intent(Intent.ACTION_SEND).apply {
                                        type = "text/plain"
                                        putExtra(Intent.EXTRA_SUBJECT, context.getString(R.string.connection_details_share_subject))
                                        putExtra(Intent.EXTRA_TEXT, details)
                                    }
                                    context.startActivity(Intent.createChooser(send, context.getString(R.string.config_share_connection_details)))
                                },
                                modifier = Modifier.weight(1f),
                            ) {
                                Text(stringResource(R.string.config_share_connection_details), color = AppColors.accent)
                            }
                        }
                        SelectionContainer {
                            Text(
                                details,
                                color = AppColors.text,
                                fontSize = 13.sp,
                                modifier = Modifier.padding(top = 12.dp),
                            )
                        }
                    }

                    ExpandableSection(
                        icon = "📜",
                        title = stringResource(R.string.logs_tab),
                        expanded = state.logsExpanded,
                        onToggle = { viewModel.toggleLogsExpanded() },
                    ) {
                        OutlinedButton(
                            onClick = { viewModel.clearLogs() },
                            modifier = Modifier.padding(bottom = 8.dp),
                        ) { Text(stringResource(R.string.clear_logs), color = AppColors.accent) }
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
    }
}

@Composable
private fun ExpandableSection(
    icon: String,
    title: String,
    expanded: Boolean,
    onToggle: () -> Unit,
    content: @Composable ColumnScope.() -> Unit,
) {
    Column(
        modifier = Modifier
            .fillMaxWidth()
            .padding(bottom = 16.dp)
            .border(1.dp, AppColors.accent, RoundedCornerShape(8.dp))
            .background(AppColors.surface, RoundedCornerShape(8.dp)),
    ) {
        Row(
            modifier = Modifier
                .fillMaxWidth()
                .clickable(onClick = onToggle)
                .padding(12.dp),
            verticalAlignment = Alignment.CenterVertically,
        ) {
            Text(icon, fontSize = 18.sp)
            Text(
                title,
                color = AppColors.text,
                fontWeight = FontWeight.SemiBold,
                fontSize = 18.sp,
                modifier = Modifier.weight(1f).padding(start = 8.dp),
            )
            Text(if (expanded) "▼" else "▶", color = AppColors.muted)
        }
        AnimatedVisibility(visible = expanded) {
            Column(Modifier.padding(start = 16.dp, end = 16.dp, bottom = 16.dp), content = content)
        }
    }
}

@Composable
private fun NumberField(
    label: String,
    value: Int,
    error: String?,
    help: String?,
    onNudge: (Int) -> Unit,
    onValueChange: (Int) -> Unit,
) {
    val supporting = error ?: help
    OutlinedTextField(
        value = value.toString(),
        onValueChange = { raw -> raw.toIntOrNull()?.let(onValueChange) },
        label = { Text(label) },
        isError = error != null,
        supportingText = if (supporting != null) {
            { Text(supporting) }
        } else {
            null
        },
        trailingIcon = {
            Row {
                TextButton(onClick = { onNudge(-1) }) { Text(stringResource(R.string.numeric_down), color = AppColors.accent) }
                TextButton(onClick = { onNudge(1) }) { Text(stringResource(R.string.numeric_up), color = AppColors.accent) }
            }
        },
        modifier = Modifier.fillMaxWidth().padding(top = 8.dp),
        colors = fieldColors(),
        singleLine = true,
    )
}

@Composable
private fun UserCard(
    index: Int,
    user: com.siarheikuchuk.ftpsserver.data.UserAccount,
    errors: UserFieldErrors,
    viewModel: MainViewModel,
) {
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
            isError = errors.login != null,
            supportingText = { errors.login?.let { Text(it) } },
            modifier = Modifier.fillMaxWidth(),
            colors = fieldColors(),
        )
        OutlinedTextField(
            value = user.password,
            onValueChange = { v -> viewModel.updateUser(index) { it.copy(password = v) } },
            label = { Text(stringResource(R.string.user_password)) },
            isError = errors.password != null,
            supportingText = { errors.password?.let { Text(it) } },
            modifier = Modifier.fillMaxWidth().padding(top = 8.dp),
            colors = fieldColors(),
        )
        OutlinedTextField(
            value = user.folderName.ifBlank { "" },
            onValueChange = {},
            readOnly = true,
            label = { Text(stringResource(R.string.user_folder)) },
            isError = errors.folder != null,
            supportingText = { errors.folder?.let { Text(it) } },
            trailingIcon = {
                TextButton(onClick = { folderPicker.launch(null) }) {
                    Text(stringResource(R.string.config_browse), color = AppColors.accent)
                }
            },
            modifier = Modifier.fillMaxWidth().padding(top = 8.dp),
            colors = fieldColors(),
        )
        Row(verticalAlignment = Alignment.CenterVertically) {
            Checkbox(checked = user.readonly, onCheckedChange = { v -> viewModel.updateUser(index) { it.copy(readonly = v) } })
            Text(stringResource(R.string.user_readonly), color = AppColors.text, modifier = Modifier.weight(1f))
            TextButton(onClick = { viewModel.removeUser(index) }) { Text(stringResource(R.string.user_delete), color = AppColors.error) }
        }
    }
}
