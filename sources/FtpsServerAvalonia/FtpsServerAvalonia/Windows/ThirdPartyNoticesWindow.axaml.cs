using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Interactivity;
using Avalonia.Media;
using FtpsServerAppsShared.Privacy;
using System;
using System.Collections.Generic;

namespace FtpsServerAvalonia.Windows;

public partial class ThirdPartyNoticesWindow : Window
{
    public ThirdPartyNoticesWindow()
    {
        InitializeComponent();
        Render(ThirdPartyNotices.Load());
    }

    private void OkButton_Click(object? sender, RoutedEventArgs e) => Close();

    private void Render(string markdown)
    {
        DocumentHost.Children.Clear();
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
                case PrivacyInline.Link link:
                    runs.Add(new Run
                    {
                        Text = link.Label == link.Url ? link.Url : $"{link.Label} ({link.Url})",
                    });
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
