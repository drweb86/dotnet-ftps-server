using System.Security.Cryptography.X509Certificates;

namespace FtpsServerLibrary;

public class FtpsServerSettings
{
    /// <summary>
    /// The IP address server will be listening to.
    /// Optional parameter.
    /// Default value: 0.0.0.0.
    /// 0.0.0.0 - listen on every available network interface.
    /// </summary>
    public string? Ip { get; set; } = "0.0.0.0";

    /// <summary>
    /// The Port for server to listen to.
    /// Optional parameter.
    /// Default value: 2121.
    /// </summary>
    public int? Port { get; set; } = 2121;




    public int MaxConnections { get; set; } = 10;

    /// <summary>
    /// PEM, DER or PKCS#12 PFX file.
    /// PFX file is opened with CertificatePassword (if specified).
    /// </summary>
    public string? CertificatePath { get; set; }
    public string? CertificatePassword { get; set; }

    /// <summary>
    /// Single X.509 certificate in either the PEM or DER encoding.
    /// </summary>
    public byte[]? CertificateBytes { get; set; }

    /// <summary>
    /// PKCS#12 PFX content. Opened with CertificatePassword (if specified)
    /// </summary>
    public byte[]? CertificatePkcs12Bytes { get; set; }
    public X509Certificate2? X509Certificate { get; set; }


    public StoreName? CertificateStoreName { get; set; }
    public StoreLocation? CertificateStoreLocation { get; set; }
    public string? CertificateStoreSubject { get; set; }
}
