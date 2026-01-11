using FtpsServerAppsShared.Services;
using FtpsServerWindows.Resources;
using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace FtpsServerWindows.Controls;

public partial class UpdateCheckExpanderView : UserControl
{
    public UpdateCheckExpanderView()
    {
        InitializeComponent();
        Visibility = Visibility.Collapsed;

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
            Visibility = Visibility.Visible;
        }
    }
}
