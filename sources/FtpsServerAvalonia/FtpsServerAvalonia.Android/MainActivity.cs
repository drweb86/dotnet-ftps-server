using System;
using Android.App;
using Android.Content;
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
public class MainActivity : AvaloniaMainActivity, IAndroidKeepAwakeService, IAndroidShareService
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        global::FtpsServerAvalonia.App.AndroidKeepAwakeService = this;
        global::FtpsServerAvalonia.App.AndroidShareService = this;

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
        if (global::FtpsServerAvalonia.App.AndroidShareService == this)
            global::FtpsServerAvalonia.App.AndroidShareService = null;

        base.OnDestroy();
    }

    public void ShareText(string title, string text)
    {
        var intent = new Intent(Intent.ActionSend);
        intent.SetType("text/plain");
        intent.PutExtra(Intent.ExtraSubject, title);
        intent.PutExtra(Intent.ExtraText, text);
        StartActivity(Intent.CreateChooser(intent, title));
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
