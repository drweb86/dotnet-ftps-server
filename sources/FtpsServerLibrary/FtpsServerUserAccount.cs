namespace FtpsServerLibrary;

public class FtpsServerUserAccount
{
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public string RootFolder { get; set; } = "/";
    public FtpsServerUserPermissions Permissions { get; set; } = new FtpsServerUserPermissions();
}
