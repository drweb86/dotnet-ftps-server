using System.Diagnostics;
using System.Windows.Input;

namespace FtpsServerApp.Commands
{
    public class LicenseCommand : ICommand
    {
        private const string LicenseUrl = "https://github.com/drweb86/dotnet-ftps-server/blob/main/sources/FtpServerWpf/LICENSE.md";

        #pragma warning disable 67
        public event EventHandler? CanExecuteChanged;
        #pragma warning restore 67

        public bool CanExecute(object? parameter)
        {
            return true;
        }

        public void Execute(object? parameter)
        {
            Process.Start(new ProcessStartInfo(LicenseUrl) { UseShellExecute = true });
        }
    }
}
