buildscript {
    dependencies {
        // AGP's built-in Kotlin is 2.2.10. A higher classpath wins, and the Compose
        // compiler plugin must use that same Kotlin version.
        classpath("org.jetbrains.kotlin:kotlin-gradle-plugin:2.4.21")
    }
}

plugins {
    id("com.android.application") version "9.5.0" apply false
    id("org.jetbrains.kotlin.plugin.compose") version "2.4.21" apply false
}
