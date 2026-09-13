using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using FtpsServerAppsShared.Privacy;
using System;
using System.Collections.Generic;

namespace FtpsServerAvalonia.Windows;

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

    private void LanguageCombo_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_suppressLanguageChange || LanguageCombo.SelectedItem is not PrivacyLanguage language)
            return;

        PrivacyStore.SetLanguageCode(language.Code);
        LoadDocument(language);
    }

    private void OkButton_Click(object? sender, RoutedEventArgs e) => Close();

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
                        FontWeight = FontWeight.Bold,
                        FontSize = heading.Level == 1 ? 22 : heading.Level == 2 ? 18 : 16,
                        TextWrapping = TextWrapping.Wrap,
                        Margin = new Thickness(0, heading.Level == 1 ? 4 : 16, 0, 8),
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

    private Control CreateRichText(
        IReadOnlyList<PrivacyInline> inlines,
        double leftMargin,
        double bottomMargin,
        string? prefix = null)
    {
        var block = new TextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(leftMargin, 0, 0, bottomMargin),
            FontSize = 14,
        };
        var runs = block.Inlines ?? [];
        block.Inlines = runs;
        if (!string.IsNullOrEmpty(prefix))
            runs.Add(new Run { Text = prefix });

        foreach (var inline in inlines)
        {
            switch (inline)
            {
                case PrivacyInline.Text text:
                    runs.Add(new Run
                    {
                        Text = text.Value,
                        FontWeight = text.Bold ? FontWeight.Bold : FontWeight.Normal,
                    });
                    break;
                case PrivacyInline.Link link when Uri.TryCreate(link.Url, UriKind.Absolute, out var uri):
                    runs.Add(new InlineUIContainer
                    {
                        Child = new HyperlinkButton
                        {
                            Content = link.Label,
                            NavigateUri = uri,
                            Padding = new Thickness(0),
                            FontSize = 14,
                            VerticalAlignment = VerticalAlignment.Center,
                        },
                    });
                    break;
                case PrivacyInline.Link link:
                    runs.Add(new Run { Text = link.Label });
                    break;
                case PrivacyInline.Code code:
                    runs.Add(new Run
                    {
                        Text = code.Value,
                        FontFamily = new FontFamily("Consolas, Menlo, monospace"),
                    });
                    break;
            }
        }

        return block;
    }
}
