using Avalonia.Controls;
using FtpsServerAppsShared.Services;
using FtpsServerAvalonia.Resources;
using System.Threading.Tasks;

namespace FtpsServerAvalonia.Controls;

public partial class UpdateCheckExpanderView : UserControl
{
    public UpdateCheckExpanderView()
    {
        InitializeComponent();
        IsVisible = false;

#if RELEASE
        _ = CheckForUpdates();
#endif
    }

    private async Task CheckForUpdates()
    {
        var update = await UpdateChecker.CheckForUpdateGithub();

        if (update.HasUpdate)
        {
            this.updateNews.Text = update.Changes;
            this.updateNewsTitle.Text = string.Format(Strings.UpdateAvailableFormat, update.Version);
            IsVisible = true;
        }
    }
}
