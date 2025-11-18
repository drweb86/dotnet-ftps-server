namespace FtpsServerLibrary;

public class FtpsServerSettings
{
    public string IpAddress { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 21990;
    /// <summary>
    /// If CertificatePassword is empty, CertificatePath is PEM or DER encoding.
    /// Otherwise it should point to PKCS#12 PPX file.
    /// </summary>
    public string CertificatePath { get; set; } = "";
    public string CertificatePassword { get; set; } = "";
    public int MaxConnections { get; set; } = 10;
}
