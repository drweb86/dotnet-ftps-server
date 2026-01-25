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

    /// <summary>
    /// Bookmark string for Android SAF (Storage Access Framework) folder access.
    /// This is used to persist folder access permissions across app restarts.
    /// </summary>
    public string FolderBookmark
    {
        get => _folderBookmark;
        set => SetField(ref _folderBookmark, value);
    }

    public bool ReadonlyPermission
    {
        get => _readonlyPermission;
        set => SetField(ref _readonlyPermission, value);
    }
}
