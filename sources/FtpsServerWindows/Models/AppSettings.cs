namespace FtpsServerApp.Models;

public class AppSettings
{
    public int ServerPort { get; set; } = 2121;
    public int MaxConnections { get; set; } = 10;
    public List<UserAccount> Users { get; set; } = new();
    public CertificateSourceType CertificateSource { get; set; } = CertificateSourceType.SelfSigned;
    public string CertificatePath { get; set; } = "";
    public string CertificatePassword { get; set; } = "";
}
