using System.Collections.Generic;

namespace FtpsServer
{
    // Configuration Models
    public class ServerConfiguration
    {
        public ServerSettings ServerSettings { get; set; } = new ServerSettings();
        public List<UserAccount> Users { get; set; } = new List<UserAccount>();
        public LoggingSettings Logging { get; set; } = new LoggingSettings();
    }
}
