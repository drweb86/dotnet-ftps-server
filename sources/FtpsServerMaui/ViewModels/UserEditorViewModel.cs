using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FtpsServerMaui.Helpers;
using FtpsServerMaui.Models;

namespace FtpsServerMaui.ViewModels;

[QueryProperty(nameof(EditingUser), "EditingUser")]
public partial class UserEditorViewModel(IUserEditorService userEditorService) : ObservableObject
{
    private readonly IUserEditorService _userEditorService = userEditorService;
    private UserConfiguration? _originalUser;

    private UserConfiguration? _editingUser;
    public UserConfiguration? EditingUser
    {
        get => _editingUser;
        set
        {
            SetProperty(ref _editingUser, value);
            OnEditingUserChanged(value);
        }
    }

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

    private bool _isEditMode;
    public bool IsEditMode
    {
        get => _isEditMode;
        set => SetProperty(ref _isEditMode, value);
    }

    private void OnEditingUserChanged(UserConfiguration? value)
    {
        if (value != null)
        {
            _originalUser = value;
            IsEditMode = true;
            Username = value.Username;
            Password = value.Password;
            Folder = value.Folder;
            ReadPermission = value.ReadPermission;
            WritePermission = value.WritePermission;
        }
        else
        {
            IsEditMode = false;
            _originalUser = null;
        }
    }

    [RelayCommand]
    private async Task BrowseFolderAsync()
    {
        var selectedPath = await FolderPickerHelper.PickFolderAsync();
        if (!string.IsNullOrEmpty(selectedPath))
        {
            Folder = selectedPath;
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(Username))
        {
            await Shell.Current.DisplayAlertAsync("Error", "Username is required", "OK");
            return;
        }

        if (string.IsNullOrWhiteSpace(Password))
        {
            await Shell.Current.DisplayAlertAsync("Error", "Password is required", "OK");
            return;
        }

        if (string.IsNullOrWhiteSpace(Folder))
        {
            await Shell.Current.DisplayAlertAsync("Error", "Folder is required", "OK");
            return;
        }

        // Create directory if it doesn't exist
        if (!Directory.Exists(Folder))
        {
            try
            {
                Directory.CreateDirectory(Folder);
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlertAsync("Error", $"Failed to create directory: {ex.Message}", "OK");
                return;
            }
        }

        var newUser = new UserConfiguration
        {
            Username = Username,
            Password = Password,
            Folder = Folder,
            ReadPermission = ReadPermission,
            WritePermission = WritePermission
        };

        if (IsEditMode && _originalUser != null)
        {
            _userEditorService.NotifyUserUpdated(_originalUser, newUser);
        }
        else
        {
            _userEditorService.NotifyUserAdded(newUser);
        }

        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    private static async Task CancelAsync()
    {
        await Shell.Current.GoToAsync("..");
    }
}