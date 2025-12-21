using System.Reflection;
using System.Windows.Controls;

namespace FtpsServerApp
{
    public partial class MainMenuControl : UserControl
    {
        public MainMenuControl()
        {
            InitializeComponent();

            // Set menu item header with version
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            AboutMenuItem.Header = $"FTPS Server - {version}";
        }
    }
}
