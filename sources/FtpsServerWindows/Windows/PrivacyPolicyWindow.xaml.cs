using FtpsServerAppsShared.Privacy;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Navigation;

namespace FtpsServerWindows.Windows;

public partial class PrivacyPolicyWindow : Window
{
    private bool _suppressLanguageChange;

    public PrivacyPolicyWindow()
    {
        InitializeComponent();

        LanguageCombo.ItemsSource = PrivacyLanguages.All;
        var selected = PrivacyStore.LanguageCode() is { } code
            ? PrivacyLanguages.ByCode(code)
            : PrivacyLanguages.MatchDevice();

        _suppressLanguageChange = true;
        LanguageCombo.SelectedItem = selected;
        _suppressLanguageChange = false;

        if (PrivacyStore.LanguageCode() == null)
            PrivacyStore.SetLanguageCode(selected.Code);

        LoadDocument(selected);
    }

    private void LanguageCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressLanguageChange || LanguageCombo.SelectedItem is not PrivacyLanguage language)
            return;

        PrivacyStore.SetLanguageCode(language.Code);
        LoadDocument(language);
    }

    private void OkButton_Click(object sender, RoutedEventArgs e) => Close();

    private void LoadDocument(PrivacyLanguage language)
    {
        FlowDirection = language.Rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        DocumentHost.Children.Clear();

        var markdown = PrivacyDocuments.LoadMarkdown(language.AssetFile);
        foreach (var block in PrivacyMarkdown.Parse(markdown))
        {
            switch (block)
            {
                case PrivacyMdBlock.Heading heading:
                    DocumentHost.Children.Add(new TextBlock
                    {
                        Text = heading.Text,
                        FontWeight = FontWeights.Bold,
                        FontSize = heading.Level == 1 ? 22 : heading.Level == 2 ? 18 : 16,
                        TextWrapping = TextWrapping.Wrap,
                        Margin = new Thickness(0, heading.Level == 1 ? 4 : 16, 0, 8),
                        Foreground = (Brush)FindResource("WindowForegroundBrush"),
                    });
                    break;
                case PrivacyMdBlock.Paragraph paragraph:
                    DocumentHost.Children.Add(CreateRichText(paragraph.Inlines, 0, 10));
                    break;
                case PrivacyMdBlock.Bullet bullet:
                    DocumentHost.Children.Add(CreateRichText(bullet.Inlines, 12, 6, "•  "));
                    break;
            }
        }
    }

    private TextBlock CreateRichText(
        IReadOnlyList<PrivacyInline> inlines,
        double leftMargin,
        double bottomMargin,
        string? prefix = null)
    {
        var block = new TextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(leftMargin, 0, 0, bottomMargin),
            Foreground = (Brush)FindResource("WindowForegroundBrush"),
            FontSize = 14,
        };
        if (!string.IsNullOrEmpty(prefix))
            block.Inlines.Add(new Run(prefix));

        foreach (var inline in inlines)
        {
            switch (inline)
            {
                case PrivacyInline.Text text:
                    block.Inlines.Add(new Run(text.Value)
                    {
                        FontWeight = text.Bold ? FontWeights.Bold : FontWeights.Normal,
                    });
                    break;
                case PrivacyInline.Link link when Uri.TryCreate(link.Url, UriKind.Absolute, out var uri):
                    var hyperlink = new Hyperlink(new Run(link.Label))
                    {
                        NavigateUri = uri,
                        Foreground = (Brush)FindResource("AccentCyanBrush"),
                    };
                    hyperlink.RequestNavigate += OpenLink;
                    block.Inlines.Add(hyperlink);
                    break;
                case PrivacyInline.Link link:
                    block.Inlines.Add(new Run(link.Label));
                    break;
                case PrivacyInline.Code code:
                    block.Inlines.Add(new Run(code.Value)
                    {
                        FontFamily = new FontFamily("Consolas"),
                    });
                    break;
            }
        }

        return block;
    }

    private static void OpenLink(object sender, RequestNavigateEventArgs e)
    {
        Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
        e.Handled = true;
    }
}
