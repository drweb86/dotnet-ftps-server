using System.Globalization;
using System.Windows;
using System.Windows.Markup;

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

            base.OnStartup(e);
        }
    }
}
