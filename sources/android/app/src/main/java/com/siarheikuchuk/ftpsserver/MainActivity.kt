package com.siarheikuchuk.ftpsserver

import android.Manifest
import android.os.Build
import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.activity.enableEdgeToEdge
import androidx.activity.viewModels
import androidx.core.app.ActivityCompat
import com.siarheikuchuk.ftpsserver.ui.FtpsTheme
import com.siarheikuchuk.ftpsserver.ui.MainScreen
import com.siarheikuchuk.ftpsserver.ui.MainViewModel

class MainActivity : ComponentActivity() {
    private val viewModel: MainViewModel by viewModels()

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        enableEdgeToEdge()
        if (Build.VERSION.SDK_INT >= 33) {
            ActivityCompat.requestPermissions(this, arrayOf(Manifest.permission.POST_NOTIFICATIONS), 1)
        }
        setContent {
            FtpsTheme {
                MainScreen(viewModel)
            }
        }
    }

    override fun onPause() {
        super.onPause()
        viewModel.save()
    }
}
