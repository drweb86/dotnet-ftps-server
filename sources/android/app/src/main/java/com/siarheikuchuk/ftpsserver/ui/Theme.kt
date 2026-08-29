package com.siarheikuchuk.ftpsserver.ui

import androidx.compose.material3.darkColorScheme
import androidx.compose.material3.MaterialTheme
import androidx.compose.runtime.Composable
import androidx.compose.ui.graphics.Color

object AppColors {
    val background = Color(0xFF0F1419)
    val surface = Color(0xFF252B37)
    val accent = Color(0xFF00D9FF)
    val text = Color(0xFFE8EAED)
    val muted = Color(0xFF9CA3AF)
    val error = Color(0xFFEF4444)
    val warning = Color(0xFFF59E0B)
    val success = Color(0xFF10B981)
}

@Composable
fun FtpsTheme(content: @Composable () -> Unit) {
    MaterialTheme(
        colorScheme = darkColorScheme(
            primary = AppColors.accent,
            onPrimary = AppColors.background,
            background = AppColors.background,
            surface = AppColors.surface,
            onBackground = AppColors.text,
            onSurface = AppColors.text,
            error = AppColors.error,
        ),
        content = content,
    )
}
