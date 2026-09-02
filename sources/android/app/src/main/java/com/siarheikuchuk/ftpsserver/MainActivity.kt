package com.siarheikuchuk.ftpsserver

import android.Manifest
import android.content.Context
import android.content.Intent
import android.os.Build
import android.os.Bundle
import android.os.Process
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.activity.enableEdgeToEdge
import androidx.activity.viewModels
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.core.app.ActivityCompat
import com.siarheikuchuk.ftpsserver.privacy.AppPrivateDataWiper
import com.siarheikuchuk.ftpsserver.privacy.PrivacyStore
import com.siarheikuchuk.ftpsserver.ui.FtpsTheme
import com.siarheikuchuk.ftpsserver.ui.MainScreen
import com.siarheikuchuk.ftpsserver.ui.MainViewModel
import com.siarheikuchuk.ftpsserver.ui.PrivacyPolicyScreen
import com.siarheikuchuk.ftpsserver.ui.PrivacyScreenMode

class MainActivity : ComponentActivity() {
    private val viewModel: MainViewModel by viewModels()

    override fun attachBaseContext(newBase: Context) {
        super.attachBaseContext(
            if (BuildConfig.SCREENSHOTS) newBase.withEnglishLocale() else newBase,
        )
    }

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        enableEdgeToEdge()
        setContent {
            FtpsTheme {
                var consented by remember {
                    mutableStateOf(!BuildConfig.CHINA_PIPL_POLICY || PrivacyStore.hasConsent(this@MainActivity))
                }
                var privacyOpen by remember { mutableStateOf(false) }

                if (BuildConfig.CHINA_PIPL_POLICY && !consented) {
                    PrivacyPolicyScreen(
                        mode = PrivacyScreenMode.ConsentGate,
                        onDismiss = { },
                        onAgree = {
                            PrivacyStore.acceptConsent(this@MainActivity)
                            consented = true
                            requestNotificationPermission()
                        },
                        onDeclineConfirmed = { wipePrivateDataAndExit() },
                    )
                } else if (privacyOpen) {
                    PrivacyPolicyScreen(
                        mode = if (BuildConfig.CHINA_PIPL_POLICY) {
                            PrivacyScreenMode.InfoWithWithdraw
                        } else {
                            PrivacyScreenMode.Info
                        },
                        onDismiss = { privacyOpen = false },
                        onDeclineConfirmed = { wipePrivateDataAndExit() },
                    )
                } else {
                    MainScreen(
                        viewModel = viewModel,
                        onOpenPrivacy = { privacyOpen = true },
                    )
                }
            }
        }
        if (!BuildConfig.CHINA_PIPL_POLICY || PrivacyStore.hasConsent(this)) {
            requestNotificationPermission()
        }
    }

    private fun requestNotificationPermission() {
        if (Build.VERSION.SDK_INT >= 33) {
            ActivityCompat.requestPermissions(this, arrayOf(Manifest.permission.POST_NOTIFICATIONS), 1)
        }
    }

    private fun wipePrivateDataAndExit() {
        AppPrivateDataWiper.wipePrivateDataAndClearConsent(this)
        finishAffinity()
        Process.killProcess(Process.myPid())
    }

    override fun onNewIntent(intent: Intent) {
        super.onNewIntent(intent)
        setIntent(intent)
    }

    override fun onPause() {
        super.onPause()
        if (!PrivacyStore.skipSettingsSave) {
            viewModel.save()
        }
    }
}
