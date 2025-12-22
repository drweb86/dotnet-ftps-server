using FtpsServerAppsShared.Services;
using System.Diagnostics;
using System.Windows.Input;

namespace FtpsServerApp.Commands;

public class AboutCommand : ICommand
{
    #pragma warning disable 67
    public event EventHandler? CanExecuteChanged;
    #pragma warning restore 67

    public bool CanExecute(object? parameter)
    {
        return true;
    }

    public void Execute(object? parameter)
    {
        Process.Start(new ProcessStartInfo(ApplicationLinks.AboutUrl) { UseShellExecute = true });
    }
}
