namespace FtpsServerLibrary;

public class ServerSettings
{
    public string IpAddress { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 21990;
    public string RootDirectory { get; set; } = "./ftproot";
    public string CertificatePath { get; set; } = "";
    public string CertificatePassword { get; set; } = "";
    public int MaxConnections { get; set; } = 10;
}
