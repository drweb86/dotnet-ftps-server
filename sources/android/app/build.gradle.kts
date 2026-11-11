plugins {
    id("com.android.application")
    id("org.jetbrains.kotlin.plugin.compose")
}

val screenshots = findProperty("screenshots")?.toString() == "true"

android {
    namespace = "com.siarheikuchuk.ftpsserver"
    compileSdk = 37
    buildToolsVersion = "36.0.0"

    defaultConfig {
        applicationId = "com.siarheikuchuk.ftpsserver"
        minSdk = 23
        targetSdk = 36
        val defaultVersionCode = 20261113
        val defaultVersionName = "2026.11.13"
        versionCode = findProperty("appVersionCode")?.toString()?.toIntOrNull() ?: defaultVersionCode
        versionName = findProperty("appVersionName")?.toString()?.takeIf { it.isNotBlank() } ?: defaultVersionName
        buildConfigField("boolean", "SCREENSHOTS", if (screenshots) "true" else "false")
        if (screenshots) {
            applicationIdSuffix = ".screenshots"
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
    if (screenshots) {
        androidResources {
            localeFilters += "en"
        }
    }
    buildFeatures {
        compose = true
        buildConfig = true
        resValues = true
    }
    packaging {
        resources {
            excludes += "/META-INF/{AL2.0,LGPL2.1}"
            excludes += "META-INF/LICENSE.md"
            excludes += "META-INF/NOTICE.md"
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

// F-Droid: AGP ArtProfile output (assets/dexopt/baseline.prof{,m}) is not
// bit-reproducible across builders, so signature copying fails.
tasks.configureEach {
    if (name.contains("ArtProfile")) {
        enabled = false
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

abstract class CopyThirdPartyNoticesTask : DefaultTask() {
    @get:InputFile
    @get:PathSensitive(PathSensitivity.RELATIVE)
    abstract val sourceFile: RegularFileProperty

    @get:OutputDirectory
    abstract val outputDir: DirectoryProperty

    @TaskAction
    fun copyNotice() {
        val dest = outputDir.get().asFile
        dest.mkdirs()
        sourceFile.get().asFile.copyTo(dest.resolve("THIRD-PARTY-NOTICES.md"), overwrite = true)
    }
}

abstract class CopyLicenseDocumentsTask : DefaultTask() {
    @get:InputDirectory
    @get:PathSensitive(PathSensitivity.RELATIVE)
    abstract val sourceDir: DirectoryProperty

    @get:InputDirectory
    @get:PathSensitive(PathSensitivity.RELATIVE)
    abstract val appendixDir: DirectoryProperty

    @get:OutputDirectory
    abstract val outputDir: DirectoryProperty

    @TaskAction
    fun copyLicenses() {
        copyMdFolder(sourceDir.get().asFile, outputDir.get().asFile.resolve("license"))
        copyMdFolder(appendixDir.get().asFile, outputDir.get().asFile.resolve("license/china-appendix"))
    }

    private fun copyMdFolder(source: java.io.File, dest: java.io.File) {
        dest.deleteRecursively()
        dest.mkdirs()
        source.listFiles()
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
        val copyThirdPartyNotices = tasks.register<CopyThirdPartyNoticesTask>(
            "copyThirdPartyNotices${variant.name.replaceFirstChar { it.uppercase() }}",
        ) {
            sourceFile.set(rootProject.projectDir.resolve("THIRD-PARTY-NOTICES.md"))
        }
        val copyLicenses = tasks.register<CopyLicenseDocumentsTask>(
            "copyLicenses${variant.name.replaceFirstChar { it.uppercase() }}",
        ) {
            sourceDir.set(rootProject.projectDir.resolve("../../licenses"))
            appendixDir.set(rootProject.projectDir.resolve("../../licenses/china-appendix"))
        }
        variant.sources.assets?.addGeneratedSourceDirectory(
            copyPrivacyPolicies,
            CopyPrivacyPoliciesTask::outputDir,
        )
        variant.sources.assets?.addGeneratedSourceDirectory(
            copyThirdPartyNotices,
            CopyThirdPartyNoticesTask::outputDir,
        )
        variant.sources.assets?.addGeneratedSourceDirectory(
            copyLicenses,
            CopyLicenseDocumentsTask::outputDir,
        )
        if (variant.flavorName == "chinaPiplPolicy" && variant.buildType == "debug") {
            variant.applicationId.set("com.siarheikuchuk.ftpsserver.chinapipl.debug")
        }
    }
}

dependencies {
    val composeBom = platform("androidx.compose:compose-bom:2026.09.00")
    implementation(composeBom)
    implementation("androidx.compose.ui:ui")
    implementation("androidx.compose.ui:ui-tooling-preview")
    implementation("androidx.compose.material3:material3")
    implementation("androidx.compose.material:material-icons-core")
    implementation("androidx.activity:activity-compose:1.13.0")
    implementation("androidx.lifecycle:lifecycle-runtime-ktx:2.11.0")
    implementation("androidx.lifecycle:lifecycle-viewmodel-ktx:2.11.0")
    implementation("androidx.lifecycle:lifecycle-viewmodel-compose:2.11.0")
    implementation("androidx.lifecycle:lifecycle-service:2.11.0")
    implementation("androidx.core:core-ktx:1.19.1")
    implementation("org.jetbrains.kotlinx:kotlinx-coroutines-android:1.11.0")
    implementation("androidx.documentfile:documentfile:1.1.0")
    implementation("org.bouncycastle:bcpkix-jdk18on:1.87")
    debugImplementation("androidx.compose.ui:ui-tooling")
}
