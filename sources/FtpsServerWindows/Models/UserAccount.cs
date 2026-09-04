using System.Text.Json.Serialization;

namespace FtpsServerWindows.Models;

public class UserAccount : ObservableObject
{
    private string _login = "";
    private string _password = "";
    private string _folder = "";
    private bool _readonlyPermission = false;

    public string Login
    {
        get => _login;
        set
        {
            if (SetField(ref _login, value))
                LoginError = null;
        }
    }

    public string Password
    {
        get => _password;
        set
        {
            if (SetField(ref _password, value))
                PasswordError = null;
        }
    }

    public string Folder
    {
        get => _folder;
        set
        {
            if (SetField(ref _folder, value))
                FolderError = null;
        }
    }

    public bool ReadonlyPermission
    {
        get => _readonlyPermission;
        set => SetField(ref _readonlyPermission, value);
    }

    private string? _loginError;
    private string? _passwordError;
    private string? _folderError;

    [JsonIgnore]
    public string? LoginError
    {
        get => _loginError;
        set => SetField(ref _loginError, value);
    }

    [JsonIgnore]
    public string? PasswordError
    {
        get => _passwordError;
        set => SetField(ref _passwordError, value);
    }

    [JsonIgnore]
    public string? FolderError
    {
        get => _folderError;
        set => SetField(ref _folderError, value);
    }
}
