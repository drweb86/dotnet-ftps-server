namespace FtpsServerLibrary;

public class ServerConfiguration
{
    public ServerSettings ServerSettings { get; set; } = new ServerSettings();
    public List<UserAccount> Users { get; set; } = new List<UserAccount>();
}
