using CommunityToolkit.Mvvm.ComponentModel;

namespace FtpsServerMaui.Models;

public partial class UserConfiguration : ObservableObject
{
    private string _username = string.Empty;
    public string Username
    {
        get => _username;
        set => SetProperty(ref _username, value);
    }

    private string _password = string.Empty;
    public string Password
    {
        get => _password;
        set => SetProperty(ref _password, value);
    }

    private string _folder = string.Empty;
    public string Folder
    {
        get => _folder;
        set => SetProperty(ref _folder, value);
    }

    private bool _readPermission = true;
    public bool ReadPermission
    {
        get => _readPermission;
        set => SetProperty(ref _readPermission, value);
    }

    private bool _writePermission = true;
    public bool WritePermission
    {
        get => _writePermission;
        set => SetProperty(ref _writePermission, value);
    }

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
