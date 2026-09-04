using System.Text.Json.Serialization;

namespace FtpsServerAvalonia.Models;

public class UserAccount : ObservableObject
{
    private string _login = "";
    private string _password = "";
    private string _folder = "";
    private string _folderBookmark = "";
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

    /// <summary>
    /// Bookmark string for Android SAF (Storage Access Framework) folder access.
    /// This is used to persist folder access permissions across app restarts.
    /// </summary>
    public string FolderBookmark
    {
        get => _folderBookmark;
        set
        {
            if (SetField(ref _folderBookmark, value))
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
