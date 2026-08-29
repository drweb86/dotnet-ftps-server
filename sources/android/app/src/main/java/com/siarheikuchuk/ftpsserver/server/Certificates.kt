package com.siarheikuchuk.ftpsserver.server

import java.io.File
import java.security.KeyPairGenerator
import java.security.KeyStore
import java.security.cert.X509Certificate
import java.util.Date
import javax.net.ssl.KeyManagerFactory
import javax.net.ssl.SSLContext
import org.bouncycastle.asn1.x500.X500Name
import org.bouncycastle.asn1.x509.BasicConstraints
import org.bouncycastle.asn1.x509.Extension
import org.bouncycastle.asn1.x509.GeneralName
import org.bouncycastle.asn1.x509.GeneralNames
import org.bouncycastle.asn1.x509.KeyUsage
import org.bouncycastle.cert.jcajce.JcaX509CertificateConverter
import org.bouncycastle.cert.jcajce.JcaX509v3CertificateBuilder
import org.bouncycastle.jce.provider.BouncyCastleProvider
import org.bouncycastle.operator.jcajce.JcaContentSignerBuilder
import java.math.BigInteger

data class LoadedCertificate(
    val sslContext: SSLContext,
    val x509: X509Certificate,
    val fingerprintSha256: String,
    val fingerprintSha1: String,
    val isSelfSigned: Boolean,
)

object Certificates {
    private const val PFX_PASSWORD = "test"

    // Android already registers a truncated provider named "BC". Looking up "BC"
    // by name hits that stub (no SHA256withRSA). Use our bundled instance instead.
    private val bc = BouncyCastleProvider()

    fun loadOrCreate(filesDir: File, settings: FtpsServerSettings, log: FtpsLog): LoadedCertificate {
        if (!settings.certificatePath.isNullOrBlank()) {
            return loadFromFile(File(settings.certificatePath), settings.certificatePassword, log)
        }
        return getOrCreateSelfSigned(filesDir, log)
    }

    private fun getOrCreateSelfSigned(filesDir: File, log: FtpsLog): LoadedCertificate {
        val dir = File(filesDir, "Certificates-Android-V1")
        dir.mkdirs()
        val pfx = File(dir, "Self-Signed.pfx")
        if (pfx.exists()) {
            try {
                val loaded = loadFromKeyStore(pfx, PFX_PASSWORD)
                if (loaded.x509.notAfter.time > System.currentTimeMillis() + 7L * 24 * 60 * 60 * 1000) {
                    log.info("Loading self-signed certificate from file ${pfx.absolutePath}.")
                    return loaded
                }
            } catch (e: Exception) {
                log.warn("Stored certificate could not be loaded: ${e.message}")
            }
        }
        log.info("Creating self-signed certificate for file ${pfx.absolutePath}.")
        val created = createSelfSigned()
        pfx.outputStream().use { created.keyStore.store(it, PFX_PASSWORD.toCharArray()) }
        return created.toLoaded()
    }

    private fun loadFromFile(file: File, password: String?, log: FtpsLog): LoadedCertificate {
        log.info("Loading certificate from ${file.absolutePath}")
        val ext = file.extension.lowercase()
        return when (ext) {
            "pfx", "p12" -> loadFromKeyStore(file, password ?: "")
            "pem", "der", "crt", "cer" -> error("PEM/DER certificates without a private key are not supported. Use a .pfx file.")
            else -> error("Certificate path extension $ext is not recognizable. Use .pfx")
        }
    }

    private fun loadFromKeyStore(file: File, password: String): LoadedCertificate {
        val ks = KeyStore.getInstance("PKCS12")
        file.inputStream().use { ks.load(it, password.toCharArray()) }
        return fromKeyStore(ks, password)
    }

    private class CreatedCert(val keyStore: KeyStore, val password: String, val cert: X509Certificate) {
        fun toLoaded() = fromKeyStore(keyStore, password)
    }

    private fun createSelfSigned(): CreatedCert {
        val keyPair = KeyPairGenerator.getInstance("RSA").apply { initialize(2048) }.generateKeyPair()
        val now = System.currentTimeMillis()
        val notBefore = Date(now - 24L * 60 * 60 * 1000)
        val notAfter = Date(now + 3650L * 24 * 60 * 60 * 1000)
        val name = X500Name("CN=FtpsServerLibrary-SelfSigned-Certificates")
        val builder = JcaX509v3CertificateBuilder(
            name,
            BigInteger.valueOf(now),
            notBefore,
            notAfter,
            name,
            keyPair.public,
        )
        builder.addExtension(Extension.basicConstraints, true, BasicConstraints(false))
        builder.addExtension(
            Extension.keyUsage,
            false,
            KeyUsage(KeyUsage.digitalSignature or KeyUsage.keyEncipherment or KeyUsage.dataEncipherment),
        )
        val names = GeneralNames(
            arrayOf(
                GeneralName(GeneralName.iPAddress, "127.0.0.1"),
                GeneralName(GeneralName.dNSName, "localhost"),
            )
        )
        builder.addExtension(Extension.subjectAlternativeName, false, names)
        val signer = JcaContentSignerBuilder("SHA256withRSA").setProvider(bc).build(keyPair.private)
        val cert = JcaX509CertificateConverter().setProvider(bc).getCertificate(builder.build(signer))
        val ks = KeyStore.getInstance("PKCS12")
        ks.load(null, PFX_PASSWORD.toCharArray())
        ks.setKeyEntry("ftpsserver", keyPair.private, PFX_PASSWORD.toCharArray(), arrayOf(cert))
        return CreatedCert(ks, PFX_PASSWORD, cert)
    }

    private fun fromKeyStore(ks: KeyStore, password: String): LoadedCertificate {
        val alias = ks.aliases().toList().first { ks.isKeyEntry(it) }
        val cert = ks.getCertificate(alias) as X509Certificate
        val kmf = KeyManagerFactory.getInstance(KeyManagerFactory.getDefaultAlgorithm())
        kmf.init(ks, password.toCharArray())
        val ctx = SSLContext.getInstance("TLS")
        ctx.init(kmf.keyManagers, null, null)
        return LoadedCertificate(
            sslContext = ctx,
            x509 = cert,
            fingerprintSha256 = fingerprint(cert, "SHA-256"),
            fingerprintSha1 = fingerprint(cert, "SHA-1"),
            isSelfSigned = cert.subjectDN == cert.issuerDN || cert.subjectX500Principal == cert.issuerX500Principal,
        )
    }

    private fun fingerprint(cert: X509Certificate, alg: String): String {
        val md = java.security.MessageDigest.getInstance(alg)
        val hex = md.digest(cert.encoded).joinToString("") { "%02X".format(it) }
        return hex.chunked(2).joinToString(":")
    }
}
