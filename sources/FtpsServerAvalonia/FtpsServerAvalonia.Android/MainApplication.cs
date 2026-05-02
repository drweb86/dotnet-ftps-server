using System;
using Android.App;
using Android.Runtime;
using Avalonia;
using Avalonia.Android;

namespace FtpsServerAvalonia.Android;

[Application]
public class MainApplication : AvaloniaAndroidApplication<App>
{
    public MainApplication(IntPtr javaReference, JniHandleOwnership transfer)
        : base(javaReference, transfer) { }

    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
        => base.CustomizeAppBuilder(builder).WithInterFont();
}
