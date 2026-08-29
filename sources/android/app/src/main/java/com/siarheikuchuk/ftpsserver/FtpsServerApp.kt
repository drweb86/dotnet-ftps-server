package com.siarheikuchuk.ftpsserver

import android.app.Application
import android.content.Context

class FtpsServerApp : Application() {
    override fun attachBaseContext(base: Context) {
        super.attachBaseContext(
            if (BuildConfig.SCREENSHOTS) base.withEnglishLocale() else base,
        )
    }
}
