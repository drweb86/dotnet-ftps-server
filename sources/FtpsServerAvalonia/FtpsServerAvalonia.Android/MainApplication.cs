using System;
using Android.App;
using Android.Runtime;
using Avalonia;
using Avalonia.Android;

namespace FtpsServerAvalonia.Android;

[Application]
public class MainApplication : AvaloniaAndroidApplication<App>
{
    static MainApplication()
    {
        AndroidKeystoreSecretProtection.Register();
    }

    public MainApplication(IntPtr javaReference, JniHandleOwnership transfer)
        : base(javaReference, transfer)
    {
        AndroidKeystoreSecretProtection.Register();
    }

    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
        => base.CustomizeAppBuilder(builder).WithInterFont();
}
