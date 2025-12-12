using System.Collections.Generic;

namespace FtpsServerApp.Models
{
    public class AppSettings
    {
        public bool IsSimpleMode { get; set; } = true;
        
        // Simple Mode Settings
        public string SimpleRootFolder { get; set; } = "";
        public bool SimpleReadPermission { get; set; } = true;
        public bool SimpleWritePermission { get; set; } = true;
        
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

    public class UserAccount
    {
        public string Login { get; set; } = "";
        public string Password { get; set; } = "";
        public string Folder { get; set; } = "";
        public bool ReadPermission { get; set; } = true;
        public bool WritePermission { get; set; } = false;
    }

    public enum CertificateSourceType
    {
        SelfSigned,
        FromFile
    }
}
