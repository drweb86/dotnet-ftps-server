using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using FtpsServerAvalonia.Views;

namespace FtpsServerAvalonia;

public partial class App : Application
{
    public static Visual? Instance { get; private set; }
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            Instance = desktop.MainWindow = new MainWindow();
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleView)
        {
            Instance = singleView.MainView = new AndroidView();
        }
        base.OnFrameworkInitializationCompleted();
    }
}