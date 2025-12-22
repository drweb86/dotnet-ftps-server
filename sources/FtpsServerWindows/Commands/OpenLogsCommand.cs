using System.Diagnostics;
using System.IO;
using System.Windows.Input;

namespace FtpsServerApp.Commands;

public class OpenLogsCommand : ICommand
{
    private string Folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ftps-server", "logs");

    #pragma warning disable 67
    public event EventHandler? CanExecuteChanged;
    #pragma warning restore 67

    public bool CanExecute(object? parameter)
    {
        return true;
    }

    public void Execute(object? parameter)
    {
        Directory.CreateDirectory(Folder);
        Process.Start(new ProcessStartInfo(Folder) { UseShellExecute = true });
    }
}
