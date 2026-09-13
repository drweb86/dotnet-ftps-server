using FtpsServerWindows.Windows;
using System.Windows;
using System.Windows.Input;

namespace FtpsServerWindows.Commands;

public class PrivacyCommand : ICommand
{
    #pragma warning disable 67
    public event EventHandler? CanExecuteChanged;
    #pragma warning restore 67

    public bool CanExecute(object? parameter) => true;

    public void Execute(object? parameter)
    {
        var window = new PrivacyPolicyWindow
        {
            Owner = Application.Current.MainWindow,
        };
        window.ShowDialog();
    }
}
