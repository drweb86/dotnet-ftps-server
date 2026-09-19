using FtpsServerAppsShared.License;
using FtpsServerAppsShared.Privacy;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace FtpsServerWindows.Windows;

public partial class LicenseWindow : Window
{
    private bool _suppressLanguageChange;

    public LicenseWindow()
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

        var markdown = LicenseDocuments.LoadMarkdown(language.AssetFile);
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
                case PrivacyInline.Link:
                    // License text is shown in-app with no outbound links.
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
}
