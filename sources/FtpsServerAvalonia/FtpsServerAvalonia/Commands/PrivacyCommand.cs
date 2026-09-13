using Avalonia.Controls;
using FtpsServerAvalonia.Windows;
using System;
using System.Windows.Input;

namespace FtpsServerAvalonia.Commands;

public class PrivacyCommand : ICommand
{
    #pragma warning disable 67
    public event EventHandler? CanExecuteChanged;
    #pragma warning restore 67

    public bool CanExecute(object? parameter) => true;

    public async void Execute(object? parameter)
    {
        var window = new PrivacyPolicyWindow();
        if (App.Instance is Window owner)
            await window.ShowDialog(owner);
        else
            window.Show();
    }
}
