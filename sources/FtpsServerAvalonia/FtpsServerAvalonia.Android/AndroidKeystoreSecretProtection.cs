using Android.App;
using Android.Content.PM;
using Android.Security.Keystore;
using FtpsServerLibrary;
using Java.Security;
using Javax.Crypto;
using Javax.Crypto.Spec;
using System;

namespace FtpsServerAvalonia.Android;

/// <summary>
/// AES-256-GCM in Android Keystore. The returned bytes are IV (12) followed by ciphertext and tag.
/// Callers add the enc:: prefix. The key stays in the TEE, or in StrongBox when the device has one.
/// </summary>
static class AndroidKeystoreSecretProtection
{
    private const string StoreName = "AndroidKeyStore";
    private const string Alias = "ftps_settings_v1";
    private const string Transformation = "AES/GCM/NoPadding";
    private const int IvLength = 12;
    private const int GcmTagBits = 128;

    private static readonly object Gate = new();

    public static void Register()
    {
        AndroidSecretProtection.Protect = Protect;
        AndroidSecretProtection.Unprotect = Unprotect;
    }

    private static byte[] Protect(byte[] plainBytes)
    {
        var key = SecretKey();
        using var cipher = Cipher.GetInstance(Transformation)
            ?? throw new InvalidOperationException("AES/GCM/NoPadding is not available.");
        cipher.Init(Javax.Crypto.CipherMode.EncryptMode, key);
        var ciphertext = cipher.DoFinal(plainBytes)
            ?? throw new InvalidOperationException("Android Keystore encryption returned no ciphertext.");
        var iv = cipher.GetIV()
            ?? throw new InvalidOperationException("Android Keystore encryption returned no IV.");
        if (iv.Length != IvLength)
            throw new InvalidOperationException($"Unexpected GCM IV length {iv.Length}.");

        var result = new byte[iv.Length + ciphertext.Length];
        Buffer.BlockCopy(iv, 0, result, 0, iv.Length);
        Buffer.BlockCopy(ciphertext, 0, result, iv.Length, ciphertext.Length);
        return result;
    }

    private static byte[] Unprotect(byte[] protectedBytes)
    {
        if (protectedBytes.Length <= IvLength)
            throw new System.Security.Cryptography.CryptographicException("Protected secret is too short.");

        var iv = new byte[IvLength];
        Buffer.BlockCopy(protectedBytes, 0, iv, 0, IvLength);
        var ciphertext = new byte[protectedBytes.Length - IvLength];
        Buffer.BlockCopy(protectedBytes, IvLength, ciphertext, 0, ciphertext.Length);

        var key = SecretKey();
        using var cipher = Cipher.GetInstance(Transformation)
            ?? throw new InvalidOperationException("AES/GCM/NoPadding is not available.");
        cipher.Init(Javax.Crypto.CipherMode.DecryptMode, key, new GCMParameterSpec(GcmTagBits, iv));
        return cipher.DoFinal(ciphertext)
            ?? throw new System.Security.Cryptography.CryptographicException("Android Keystore decryption returned no plaintext.");
    }

    private static IKey SecretKey()
    {
        EnsureKey();
        using var keyStore = KeyStore.GetInstance(StoreName)
            ?? throw new InvalidOperationException("AndroidKeyStore is not available.");
        keyStore.Load(null, null);
        return keyStore.GetKey(Alias, null)
            ?? throw new InvalidOperationException("Android Keystore did not return ftps_settings_v1.");
    }

    private static void EnsureKey()
    {
        lock (Gate)
        {
            using var keyStore = KeyStore.GetInstance(StoreName)
            ?? throw new InvalidOperationException("AndroidKeyStore is not available.");
            keyStore.Load(null, null);
            if (keyStore.ContainsAlias(Alias))
                return;
            if (!TryCreateStrongBoxKey())
                CreateKey(strongBox: false);
        }
    }

    private static bool TryCreateStrongBoxKey()
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(28))
            return false;

        var hasStrongBox = Application.Context?.PackageManager?
            .HasSystemFeature(PackageManager.FeatureStrongboxKeystore) == true;
        if (!hasStrongBox)
            return false;

        try
        {
            CreateKey(strongBox: true);
            return true;
        }
        catch (StrongBoxUnavailableException)
        {
            return false;
        }
    }

    private static void CreateKey(bool strongBox)
    {
        var builder = new KeyGenParameterSpec.Builder(
            Alias,
            KeyStorePurpose.Encrypt | KeyStorePurpose.Decrypt);
        builder.SetBlockModes(KeyProperties.BlockModeGcm);
        builder.SetEncryptionPaddings(KeyProperties.EncryptionPaddingNone);
        builder.SetKeySize(256);
        builder.SetRandomizedEncryptionRequired(true);
        builder.SetUserAuthenticationRequired(false);

        if (OperatingSystem.IsAndroidVersionAtLeast(28))
        {
            builder.SetUnlockedDeviceRequired(true);
            if (strongBox)
                builder.SetIsStrongBoxBacked(true);
        }

        var spec = builder.Build()
            ?? throw new InvalidOperationException("Android Keystore key spec was not created.");
        using var generator = KeyGenerator.GetInstance(KeyProperties.KeyAlgorithmAes, StoreName)
            ?? throw new InvalidOperationException("Android Keystore key generator is not available.");
        generator.Init(spec);
        generator.GenerateKey();
    }
}
