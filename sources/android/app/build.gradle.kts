plugins {
    id("com.android.application")
    id("org.jetbrains.kotlin.android")
    id("org.jetbrains.kotlin.plugin.compose")
}

val screenshots = findProperty("screenshots")?.toString() == "true"

android {
    namespace = "com.siarheikuchuk.ftpsserver"
    compileSdk = 36
    buildToolsVersion = "36.0.0"

    defaultConfig {
        applicationId = "com.siarheikuchuk.ftpsserver"
        minSdk = 23
        targetSdk = 36
        val defaultVersionCode = 20260905
        val defaultVersionName = "2026.09.05"
        versionCode = findProperty("appVersionCode")?.toString()?.toIntOrNull() ?: defaultVersionCode
        versionName = findProperty("appVersionName")?.toString()?.takeIf { it.isNotBlank() } ?: defaultVersionName
        buildConfigField("boolean", "SCREENSHOTS", if (screenshots) "true" else "false")
        if (screenshots) {
            applicationIdSuffix = ".screenshots"
            resConfigs("en")
            resValue("string", "app_label", "FTPS Server (screenshots)")
        }
    }

    flavorDimensions += "policy"
    productFlavors {
        create("general") {
            dimension = "policy"
            isDefault = true
            buildConfigField("boolean", "CHINA_PIPL_POLICY", "false")
        }
        create("chinaPiplPolicy") {
            dimension = "policy"
            buildConfigField("boolean", "CHINA_PIPL_POLICY", "true")
        }
    }

    signingConfigs {
        val keystoreFile = System.getenv("ANDROID_KEYSTORE_FILE")
        if (!keystoreFile.isNullOrBlank()) {
            create("release") {
                storeFile = file(keystoreFile)
                storePassword = System.getenv("ANDROID_KEYSTORE_PASSWORD") ?: ""
                keyAlias = System.getenv("ANDROID_KEY_ALIAS") ?: ""
                keyPassword = System.getenv("ANDROID_KEY_PASSWORD") ?: ""
                enableV1Signing = true
                enableV2Signing = true
            }
        }
    }

    buildTypes {
        debug {
            if (!screenshots) {
                applicationIdSuffix = ".debug"
                versionNameSuffix = "-native"
                resValue("string", "app_label", "FTPS Server (native debug)")
            }
        }
        release {
            isMinifyEnabled = true
            isShrinkResources = true
            proguardFiles(
                getDefaultProguardFile("proguard-android-optimize.txt"),
                "proguard-rules.pro"
            )
            signingConfig = signingConfigs.findByName("release")
        }
    }

    compileOptions {
        sourceCompatibility = JavaVersion.VERSION_17
        targetCompatibility = JavaVersion.VERSION_17
    }
    kotlinOptions {
        jvmTarget = "17"
    }
    buildFeatures {
        compose = true
        buildConfig = true
    }
    packaging {
        resources {
            excludes += "/META-INF/{AL2.0,LGPL2.1}"
            excludes += "META-INF/versions/9/OSGI-INF/MANIFEST.MF"
            pickFirsts += "META-INF/versions/9/OSGI-INF/MANIFEST.MF"
        }
    }

    // F-Droid
    dependenciesInfo {
        includeInApk = false
        includeInBundle = false
    }
}

abstract class CopyPrivacyPoliciesTask : DefaultTask() {
    @get:InputDirectory
    @get:PathSensitive(PathSensitivity.RELATIVE)
    abstract val sourceDir: DirectoryProperty

    @get:OutputDirectory
    abstract val outputDir: DirectoryProperty

    @TaskAction
    fun copyPolicies() {
        val dest = outputDir.get().asFile.resolve("privacy")
        dest.deleteRecursively()
        dest.mkdirs()
        sourceDir.get().asFile.listFiles()
            ?.filter { it.isFile && it.extension == "md" && !it.name.equals("README.md", ignoreCase = true) }
            ?.forEach { it.copyTo(dest.resolve(it.name), overwrite = true) }
    }
}

androidComponents {
    onVariants { variant ->
        val copyPrivacyPolicies = tasks.register<CopyPrivacyPoliciesTask>(
            "copyPrivacyPolicies${variant.name.replaceFirstChar { it.uppercase() }}",
        ) {
            sourceDir.set(rootProject.projectDir.resolve("../../privacy/android"))
        }
        variant.sources.assets?.addGeneratedSourceDirectory(
            copyPrivacyPolicies,
            CopyPrivacyPoliciesTask::outputDir,
        )
        if (variant.flavorName == "chinaPiplPolicy" && variant.buildType == "debug") {
            variant.applicationId.set("com.siarheikuchuk.ftpsserver.chinapipl.debug")
        }
    }
}

dependencies {
    val composeBom = platform("androidx.compose:compose-bom:2024.10.01")
    implementation(composeBom)
    implementation("androidx.compose.ui:ui")
    implementation("androidx.compose.ui:ui-tooling-preview")
    implementation("androidx.compose.material3:material3")
    implementation("androidx.compose.material:material-icons-core")
    implementation("androidx.activity:activity-compose:1.9.3")
    implementation("androidx.lifecycle:lifecycle-runtime-ktx:2.8.7")
    implementation("androidx.lifecycle:lifecycle-viewmodel-ktx:2.8.7")
    implementation("androidx.lifecycle:lifecycle-viewmodel-compose:2.8.7")
    implementation("androidx.lifecycle:lifecycle-service:2.8.7")
    implementation("androidx.core:core-ktx:1.15.0")
    implementation("org.jetbrains.kotlinx:kotlinx-coroutines-android:1.9.0")
    implementation("androidx.documentfile:documentfile:1.0.1")
    implementation("org.bouncycastle:bcpkix-jdk18on:1.79")
    debugImplementation("androidx.compose.ui:ui-tooling")
}
