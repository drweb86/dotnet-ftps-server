package com.siarheikuchuk.ftpsserver.ui

import androidx.activity.compose.BackHandler
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.AlertDialog
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.DropdownMenuItem
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.ExposedDropdownMenuBox
import androidx.compose.material3.ExposedDropdownMenuDefaults
import androidx.compose.material3.MenuAnchorType
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.OutlinedTextFieldDefaults
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.material3.TopAppBar
import androidx.compose.material3.TopAppBarDefaults
import androidx.compose.runtime.Composable
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.platform.LocalLayoutDirection
import androidx.compose.ui.res.stringResource
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.LayoutDirection
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.siarheikuchuk.ftpsserver.R
import com.siarheikuchuk.ftpsserver.privacy.PrivacyLanguages
import com.siarheikuchuk.ftpsserver.privacy.PrivacyStore
import com.siarheikuchuk.ftpsserver.privacy.loadPrivacyMarkdown

enum class PrivacyScreenMode {
    Info,
    ConsentGate,
    InfoWithWithdraw,
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun PrivacyPolicyScreen(
    mode: PrivacyScreenMode,
    onDismiss: () -> Unit,
    onAgree: () -> Unit = onDismiss,
    onDeclineConfirmed: () -> Unit = {},
) {
    val context = LocalContext.current
    val languages = PrivacyLanguages.all
    var selected by remember {
        mutableStateOf(
            PrivacyStore.languageCode(context)?.let { PrivacyLanguages.byCode(it) }
                ?: PrivacyLanguages.matchDevice(),
        )
    }
    var menuOpen by remember { mutableStateOf(false) }
    var confirmOpen by remember { mutableStateOf(false) }
    val body = remember(selected.code) { loadPrivacyMarkdown(context, selected.assetFile) }
    val direction = if (selected.rtl) LayoutDirection.Rtl else LayoutDirection.Ltr

    LaunchedEffect(Unit) {
        if (PrivacyStore.languageCode(context) == null) {
            PrivacyStore.setLanguageCode(context, selected.code)
        }
    }

    if (mode == PrivacyScreenMode.ConsentGate) {
        BackHandler { }
    } else {
        BackHandler { onDismiss() }
    }

    CompositionLocalProvider(LocalLayoutDirection provides direction) {
        Scaffold(
            containerColor = AppColors.background,
            topBar = {
                TopAppBar(
                    title = { Text(stringResource(R.string.menu_privacy)) },
                    colors = TopAppBarDefaults.topAppBarColors(
                        containerColor = AppColors.surface,
                        titleContentColor = AppColors.text,
                    ),
                )
            },
        ) { padding ->
            Column(
                modifier = Modifier
                    .padding(padding)
                    .fillMaxSize()
                    .padding(12.dp),
            ) {
                ExposedDropdownMenuBox(
                    expanded = menuOpen,
                    onExpandedChange = { menuOpen = it },
                    modifier = Modifier.fillMaxWidth(),
                ) {
                    OutlinedTextField(
                        value = selected.nativeName,
                        onValueChange = {},
                        readOnly = true,
                        label = { Text(stringResource(R.string.privacy_language)) },
                        trailingIcon = { ExposedDropdownMenuDefaults.TrailingIcon(expanded = menuOpen) },
                        modifier = Modifier
                            .fillMaxWidth()
                            .menuAnchor(MenuAnchorType.PrimaryNotEditable),
                        colors = OutlinedTextFieldDefaults.colors(
                            focusedTextColor = AppColors.text,
                            unfocusedTextColor = AppColors.text,
                            focusedBorderColor = AppColors.accent,
                            unfocusedBorderColor = AppColors.muted,
                            focusedLabelColor = AppColors.accent,
                            unfocusedLabelColor = AppColors.muted,
                            cursorColor = AppColors.accent,
                        ),
                    )
                    ExposedDropdownMenu(
                        expanded = menuOpen,
                        onDismissRequest = { menuOpen = false },
                    ) {
                        languages.forEach { lang ->
                            DropdownMenuItem(
                                text = { Text(lang.nativeName) },
                                onClick = {
                                    selected = lang
                                    PrivacyStore.setLanguageCode(context, lang.code)
                                    menuOpen = false
                                },
                            )
                        }
                    }
                }

                Text(
                    text = body,
                    color = AppColors.text,
                    fontSize = 14.sp,
                    modifier = Modifier
                        .weight(1f)
                        .fillMaxWidth()
                        .padding(top = 12.dp)
                        .verticalScroll(rememberScrollState()),
                )

                when (mode) {
                    PrivacyScreenMode.ConsentGate -> {
                        Row(
                            modifier = Modifier
                                .fillMaxWidth()
                                .padding(top = 12.dp),
                            horizontalArrangement = Arrangement.spacedBy(8.dp),
                        ) {
                            OutlinedButton(
                                onClick = { confirmOpen = true },
                                modifier = Modifier.weight(1f),
                            ) {
                                Text(stringResource(R.string.privacy_disagree), color = AppColors.muted)
                            }
                            Button(
                                onClick = onAgree,
                                modifier = Modifier.weight(1f),
                                colors = ButtonDefaults.buttonColors(
                                    containerColor = AppColors.accent,
                                    contentColor = AppColors.background,
                                ),
                            ) {
                                Text(stringResource(R.string.privacy_agree), fontWeight = FontWeight.Bold)
                            }
                        }
                    }
                    PrivacyScreenMode.Info, PrivacyScreenMode.InfoWithWithdraw -> {
                        Button(
                            onClick = onDismiss,
                            modifier = Modifier
                                .fillMaxWidth()
                                .padding(top = 12.dp),
                            colors = ButtonDefaults.buttonColors(
                                containerColor = AppColors.accent,
                                contentColor = AppColors.background,
                            ),
                        ) {
                            Text(stringResource(R.string.privacy_ok), fontWeight = FontWeight.Bold)
                        }
                        if (mode == PrivacyScreenMode.InfoWithWithdraw) {
                            TextButton(
                                onClick = { confirmOpen = true },
                                modifier = Modifier.fillMaxWidth(),
                            ) {
                                Text(
                                    stringResource(R.string.privacy_withdraw_consent),
                                    color = AppColors.muted,
                                    fontSize = 13.sp,
                                )
                            }
                        }
                    }
                }
            }
        }
    }

    if (confirmOpen) {
        AlertDialog(
            onDismissRequest = { confirmOpen = false },
            containerColor = AppColors.surface,
            titleContentColor = AppColors.text,
            textContentColor = AppColors.text,
            text = { Text(stringResource(R.string.privacy_decline_message)) },
            confirmButton = {
                TextButton(
                    onClick = {
                        confirmOpen = false
                        onDeclineConfirmed()
                    },
                ) {
                    Text(stringResource(R.string.privacy_delete_and_close), color = AppColors.error)
                }
            },
            dismissButton = {
                TextButton(onClick = { confirmOpen = false }) {
                    Text(stringResource(R.string.privacy_cancel), color = AppColors.accent)
                }
            },
        )
    }
}