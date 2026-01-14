using Avalonia.Controls;
using FtpsServerAppsShared.Services;
using System;
using System.Diagnostics;
using System.Windows.Input;

namespace FtpsServerAvalonia.Commands;

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
        var launcher = TopLevel.GetTopLevel(App.Instance)?.Launcher;
        if (launcher is not null)
            launcher.LaunchUriAsync(new Uri(ApplicationLinks.AboutUrl));
    }
}
