using System.Globalization;
using System.Windows;
using FtpsServerWindows.Services;

namespace FtpsServerWindows
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("SCREENSHOTS")))
            {
                var culture = CultureInfo.GetCultureInfo("en-US");
                CultureInfo.DefaultThreadCurrentCulture = culture;
                CultureInfo.DefaultThreadCurrentUICulture = culture;
                CultureInfo.CurrentCulture = culture;
                CultureInfo.CurrentUICulture = culture;
            }

            WindowsFontScale.Start();
            base.OnStartup(e);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            WindowsFontScale.Stop();
            base.OnExit(e);
        }
    }
}
