namespace FtpsServerApp.Models;

public class UserAccount : ObservableObject
{
    private string _login = "";
    private string _password = "";
    private string _folder = "";
    private bool _readonlyPermission = false;

    public string Login
    {
        get => _login;
        set => SetField(ref _login, value);
    }

    public string Password
    {
        get => _password;
        set => SetField(ref _password, value);
    }

    public string Folder
    {
        get => _folder;
        set => SetField(ref _folder, value);
    }

    public bool ReadonlyPermission
    {
        get => _readonlyPermission;
        set => SetField(ref _readonlyPermission, value);
    }
}
