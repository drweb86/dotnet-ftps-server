using CommunityToolkit.Mvvm.ComponentModel;

namespace FtpsServerMaui.Models;

public partial class UserConfiguration : ObservableObject
{
    [ObservableProperty]
    private string _username = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private string _folder = string.Empty;

    [ObservableProperty]
    private bool _readPermission = true;

    [ObservableProperty]
    private bool _writePermission = true;

    public UserConfiguration()
    {
    }

    public UserConfiguration(string username, string password, string folder, bool read, bool write)
    {
        Username = username;
        Password = password;
        Folder = folder;
        ReadPermission = read;
        WritePermission = write;
    }
}
