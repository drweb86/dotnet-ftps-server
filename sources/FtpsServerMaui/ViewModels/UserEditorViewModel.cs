using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FtpsServerMaui.Models;
using FtpsServerMaui.Helpers;
using FtpsServerMaui.Services;

namespace FtpsServerMaui.ViewModels;

[QueryProperty(nameof(EditingUser), "EditingUser")]
public partial class UserEditorViewModel(IUserEditorService userEditorService) : ObservableObject
{
    private readonly IUserEditorService _userEditorService = userEditorService;
    private UserConfiguration? _originalUser;

    [ObservableProperty]
    private UserConfiguration? _editingUser;

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

    [ObservableProperty]
    private bool _isEditMode;

    partial void OnEditingUserChanged(UserConfiguration? value)
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
    private async Task CancelAsync()
    {
        await Shell.Current.GoToAsync("..");
    }
}