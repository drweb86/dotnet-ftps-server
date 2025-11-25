using FtpsServerMaui.Models;

namespace FtpsServerMaui.Services;

public interface IUserEditorService
{
    event EventHandler<UserConfiguration>? UserAdded;
    event EventHandler<(UserConfiguration Original, UserConfiguration Updated)>? UserUpdated;

    void NotifyUserAdded(UserConfiguration user);
    void NotifyUserUpdated(UserConfiguration original, UserConfiguration updated);
}

public class UserEditorService : IUserEditorService
{
    public event EventHandler<UserConfiguration>? UserAdded;
    public event EventHandler<(UserConfiguration Original, UserConfiguration Updated)>? UserUpdated;

    public void NotifyUserAdded(UserConfiguration user)
    {
        UserAdded?.Invoke(this, user);
    }

    public void NotifyUserUpdated(UserConfiguration original, UserConfiguration updated)
    {
        UserUpdated?.Invoke(this, (original, updated));
    }
}






