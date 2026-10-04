package com.siarheikuchuk.ftpsserver.security

import android.content.Context
import android.content.pm.PackageManager
import android.os.Build
import android.security.keystore.KeyGenParameterSpec
import android.security.keystore.KeyProperties
import android.util.Base64
import java.security.KeyStore
import javax.crypto.Cipher
import javax.crypto.KeyGenerator
import javax.crypto.SecretKey
import javax.crypto.spec.GCMParameterSpec

/**
 * Encrypts persisted secrets with an AES-256-GCM key that stays in Android Keystore.
 * The key is not part of the ciphertext, so a copied settings file cannot be opened elsewhere.
 */
class KeystoreCipher(context: Context) {
    private val appContext = context.applicationContext

    fun encrypt(plain: String): String {
        val cipher = Cipher.getInstance(TRANSFORMATION)
        cipher.init(Cipher.ENCRYPT_MODE, secretKey())
        val ciphertext = cipher.doFinal(plain.toByteArray(Charsets.UTF_8))
        val iv = cipher.iv
        check(iv.size == IV_LENGTH) { "Unexpected GCM IV length ${iv.size}." }
        val blob = ByteArray(iv.size + ciphertext.size)
        System.arraycopy(iv, 0, blob, 0, iv.size)
        System.arraycopy(ciphertext, 0, blob, iv.size, ciphertext.size)
        return PREFIX + Base64.encodeToString(blob, Base64.NO_WRAP)
    }

    fun decrypt(stored: String): String {
        if (!stored.startsWith(PREFIX)) {
            // This release is the first to encrypt settings. A clear password is still
            // accepted until the keystore key exists, so an older file can be read once
            // and then encrypted. After that key exists, a clear value is refused.
            if (stored.isNotBlank() && hasKey()) {
                throw IllegalArgumentException("Settings secret is not protected.")
            }
            return stored
        }
        val blob = Base64.decode(stored.substring(PREFIX.length), Base64.DEFAULT)
        if (blob.size <= IV_LENGTH) {
            throw IllegalArgumentException("Protected secret is too short.")
        }
        val iv = blob.copyOfRange(0, IV_LENGTH)
        val ciphertext = blob.copyOfRange(IV_LENGTH, blob.size)
        val cipher = Cipher.getInstance(TRANSFORMATION)
        cipher.init(Cipher.DECRYPT_MODE, secretKey(), GCMParameterSpec(GCM_TAG_BITS, iv))
        return cipher.doFinal(ciphertext).toString(Charsets.UTF_8)
    }

    private fun hasKey(): Boolean {
        val keyStore = KeyStore.getInstance(ANDROID_KEYSTORE).apply { load(null) }
        return keyStore.containsAlias(ALIAS)
    }

    private fun secretKey(): SecretKey {
        val keyStore = KeyStore.getInstance(ANDROID_KEYSTORE).apply { load(null) }
        val existing = keyStore.getKey(ALIAS, null) as? SecretKey
        if (existing != null) return existing
        if (!createStrongBoxKey()) createKey(strongBox = false)
        keyStore.load(null)
        val created = keyStore.getKey(ALIAS, null) as? SecretKey
        return created ?: error("Android Keystore did not return $ALIAS.")
    }

    private fun createStrongBoxKey(): Boolean {
        if (Build.VERSION.SDK_INT < Build.VERSION_CODES.P) return false
        if (!appContext.packageManager.hasSystemFeature(PackageManager.FEATURE_STRONGBOX_KEYSTORE)) {
            return false
        }
        return try {
            createKey(strongBox = true)
            true
        } catch (e: Exception) {
            // Referenced by name so API 23 devices do not load a class added in API 28.
            if (e.javaClass.name != STRONGBOX_UNAVAILABLE) throw e
            false
        }
    }

    private fun createKey(strongBox: Boolean) {
        val builder = KeyGenParameterSpec.Builder(
            ALIAS,
            KeyProperties.PURPOSE_ENCRYPT or KeyProperties.PURPOSE_DECRYPT,
        )
            .setBlockModes(KeyProperties.BLOCK_MODE_GCM)
            .setEncryptionPaddings(KeyProperties.ENCRYPTION_PADDING_NONE)
            .setKeySize(256)
            .setRandomizedEncryptionRequired(true)
            .setUserAuthenticationRequired(false)
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.P) {
            builder.setUnlockedDeviceRequired(true)
            if (strongBox) builder.setIsStrongBoxBacked(true)
        }
        val generator = KeyGenerator.getInstance(KeyProperties.KEY_ALGORITHM_AES, ANDROID_KEYSTORE)
        generator.init(builder.build())
        generator.generateKey()
    }

    companion object {
        const val PREFIX = "enc::"
        private const val ANDROID_KEYSTORE = "AndroidKeyStore"
        private const val ALIAS = "ftps_settings_v1"
        private const val TRANSFORMATION = "AES/GCM/NoPadding"
        private const val IV_LENGTH = 12
        private const val GCM_TAG_BITS = 128
        private const val STRONGBOX_UNAVAILABLE = "android.security.keystore.StrongBoxUnavailableException"
    }
}
