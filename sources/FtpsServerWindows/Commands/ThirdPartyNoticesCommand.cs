using FtpsServerWindows.Windows;
using System.Windows;
using System.Windows.Input;

namespace FtpsServerWindows.Commands;

public class ThirdPartyNoticesCommand : ICommand
{
    #pragma warning disable 67
    public event EventHandler? CanExecuteChanged;
    #pragma warning restore 67

    public bool CanExecute(object? parameter) => true;

    public void Execute(object? parameter)
    {
        var window = new ThirdPartyNoticesWindow
        {
            Owner = Application.Current.MainWindow,
        };
        window.ShowDialog();
    }
}
