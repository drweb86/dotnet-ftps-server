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

    /// <summary>
    /// Maximum number of simultaneous server connections.
    /// Optional parameter.
    /// Default value: 10.
    /// </summary>
    public int? MaxConnections { get; set; } = 10;

    #region Certificate Source

    /// <summary>
    /// PEM, DER or PKCS#12 PFX file.
    /// Optional parameter.
    /// PFX file is opened with CertificatePassword (if specified).
    /// </summary>
    public string? CertificatePath { get; set; }

    /// <summary>
    /// Certificate password.
    /// Optional parameter.
    /// When specified, will be used for opening certificate from CertificatePath, CertificatePkcs12Bytes.
    /// </summary>
    public string? CertificatePassword { get; set; }

    /// <summary>
    /// Single X.509 certificate in either the PEM or DER encoding.
    /// Optional parameter.
    /// </summary>
    public byte[]? CertificateBytes { get; set; }

    /// <summary>
    /// PKCS#12 PFX content.
    /// Optional parameter.
    /// Opened with CertificatePassword (if specified).
    /// </summary>
    public byte[]? CertificatePkcs12Bytes { get; set; }

    /// <summary>
    /// Certificate.
    /// Optional parameter.
    /// </summary>
    public X509Certificate2? X509Certificate { get; set; }

    /// <summary>
    /// Certificate store name. Possible values: AuthRoot, CertificateAuthority, My, Root, TrustedPublisher.
    /// Used when CertificateStoreName, CertificateStoreLocation and CertificateStoreSubject are together specified.
    /// Optional parameter.
    /// </summary>
    public StoreName? CertificateStoreName { get; set; }

    /// <summary>
    /// Certificate store location. Possible values: CurrentUser, LocalMachine.
    /// Used when CertificateStoreName, CertificateStoreLocation and CertificateStoreSubject are together specified.
    /// Optional parameter.
    /// </summary>
    public StoreLocation? CertificateStoreLocation { get; set; }

    /// <summary>
    /// Certificate store subject by which certificate will be searched in certificate store and location.
    /// Used when CertificateStoreName, CertificateStoreLocation and CertificateStoreSubject are together specified.
    /// Optional parameter.
    /// </summary>
    public string? CertificateStoreSubject { get; set; }

    #endregion
}
