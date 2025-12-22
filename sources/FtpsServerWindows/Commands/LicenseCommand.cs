using FtpsServerAppsShared.Services;
using System.Diagnostics;
using System.Windows.Input;

namespace FtpsServerWindows.Commands;

public class LicenseCommand : ICommand
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
        Process.Start(new ProcessStartInfo(ApplicationLinks.LicenseUrl) { UseShellExecute = true });
    }
}
