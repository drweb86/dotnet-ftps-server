using System;
using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Avalonia.Android;
using FtpsServerAvalonia.Services;

namespace FtpsServerAvalonia.Android;

[Activity(
    Label = "FTPS Server by Siarhei Kuchuk",
    Theme = "@style/MyTheme.NoActionBar",
    Icon = "@drawable/icon",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode,
    WindowSoftInputMode = SoftInput.AdjustResize)]
public class MainActivity : AvaloniaMainActivity, IAndroidKeepAwakeService
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        global::FtpsServerAvalonia.App.AndroidKeepAwakeService = this;

        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            if (e.ExceptionObject is Exception ex)
                RunOnUiThread(() => ShowErrorDialog(ex));
        };
        AndroidEnvironment.UnhandledExceptionRaiser += (_, e) =>
        {
            e.Handled = true;
            RunOnUiThread(() => ShowErrorDialog(e.Exception));
        };

        try
        {
            base.OnCreate(savedInstanceState);
        }
        catch (Exception ex)
        {
            ShowErrorDialog(ex);
        }
    }

    protected override void OnDestroy()
    {
        if (global::FtpsServerAvalonia.App.AndroidKeepAwakeService == this)
            global::FtpsServerAvalonia.App.AndroidKeepAwakeService = null;

        base.OnDestroy();
    }

    public void SetKeepScreenOn(bool enabled)
    {
        RunOnUiThread(() =>
        {
            if (enabled)
                Window?.AddFlags(WindowManagerFlags.KeepScreenOn);
            else
                Window?.ClearFlags(WindowManagerFlags.KeepScreenOn);
        });
    }

    private void ShowErrorDialog(Exception ex)
    {
        new AlertDialog.Builder(this)!
            .SetTitle("Crash")!
            .SetMessage(ex.ToString())!
            .SetPositiveButton("OK", (_, _) => { })!
            .Show();
    }
}
