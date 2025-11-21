namespace FtpsServerLibrary;

public class FtpsServerUserAccount
{
    public required string Login { get; set; }
    public required string Password { get; set; }
    public required string Folder { get; set; }
    public bool Read { get; set; }
    public bool Write { get; set; }
}
