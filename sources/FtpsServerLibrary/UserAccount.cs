namespace FtpsServerLibrary;

public class UserAccount
{
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public string RootFolder { get; set; } = "/";
    public UserPermissions Permissions { get; set; } = new UserPermissions();
}
