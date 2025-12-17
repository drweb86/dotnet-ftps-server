using System.Collections.Generic;

namespace FtpsServerApp.Models
{
    public class AppSettings
    {
        public bool IsSimpleMode { get; set; } = true;
        public UserAccount? SimpleModeUser { get; set; }

        // Advanced Mode Settings
        public string ServerIp { get; set; } = "0.0.0.0";
        public int ServerPort { get; set; } = 2121;
        public int MaxConnections { get; set; } = 10;
        public List<UserAccount> Users { get; set; } = new();
        
        // Certificate Settings
        public CertificateSourceType CertificateSource { get; set; } = CertificateSourceType.SelfSigned;
        public string CertificatePath { get; set; } = "";
        public string CertificatePassword { get; set; } = "";
    }
}
