#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Radzen.Documents.Pdf;
using Xunit;

using Radzen.Documents.Pdf.Emit;
using Radzen.Documents;
using Radzen.Documents.Fonts;
using Radzen.Documents.Layout;
namespace Radzen.Blazor.Pdf.Tests;

public class HeaderFooterBandRegressionTests
{
    private const double Tol = 0.5;
    private const double PageWidth = 400;
    private const double PageHeight = 500;
    private const double Margin = 40;
    private const double HeaderFontSize = 30;
    private const double BodyFontSize = 12;

    private static (double Height, double Baseline) LineMetrics(double size)
    {
        var paragraph = new Paragraph();
        var run = paragraph.Inlines.Add("Xg");
        run.Font.Size = size;
        var box = LineBreaker.Break(paragraph, 100000, new FontCollection())[0];
        return (box.Height, box.Baseline);
    }

    private static Paragraph Text(string text, double size)
    {
        var paragraph = new Paragraph();
        var run = paragraph.Inlines.Add(text);
        run.Font.Size = size;
        return paragraph;
    }

    private static List<(string Text, double Y)> TextRuns(string content)
    {
        var runs = new List<(string, double)>();
        foreach (Match m in Regex.Matches(content, @"(-?[\d.]+)\s+(-?[\d.]+)\s+Td\s*\((.*?)\)\s*Tj", RegexOptions.Singleline))
        {
            runs.Add((m.Groups[3].Value, double.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture)));
        }

        return runs;
    }

    private static (Document Builder, Section Section) Author()
    {
        var document = new Document();
        var section = document.Sections.Add();
        section.PageSize = new PageSize(Unit.FromPoint(PageWidth), Unit.FromPoint(PageHeight));
        section.Margins.SetAll(Unit.FromPoint(Margin));
        return (document, section);
    }

    [Fact]
    public void TallHeader_BodyFirstLineStartsBelowHeaderBand()
    {
        var (document, section) = Author();
        for (var i = 0; i < 3; i++)
        {
            section.Header.Blocks.Add(Text($"HDR{i}", HeaderFontSize));
        }

        section.Blocks.Add(Text("BODY", BodyFontSize));

        var runs = TextRuns(CascadeTestSupport.FirstPageContent(document));
        var headerYs = runs.Where(r => r.Text.StartsWith("HDR", StringComparison.Ordinal)).Select(r => r.Y).ToList();
        var bodyY = Assert.Single(runs, r => r.Text == "BODY").Y;

        Assert.Equal(3, headerYs.Count);

        var (headerLineHeight, _) = LineMetrics(HeaderFontSize);
        var (_, bodyBaselineOffset) = LineMetrics(BodyFontSize);
        var headerBandHeight = 3 * headerLineHeight;
        Assert.True(headerBandHeight > Margin, "fixture: header band must be taller than the top margin");

        var headerBottomEdge = PageHeight - headerBandHeight;
        var bodyTopEdge = bodyY + bodyBaselineOffset;
        Assert.True(
            bodyTopEdge <= headerBottomEdge + Tol,
            $"body top edge {bodyTopEdge:F2} must be at or below the header band bottom {headerBottomEdge:F2}");
        Assert.All(headerYs, y => Assert.True(y > bodyY, "every header line sits above the body"));
    }

    [Fact]
    public void TallFooter_StaysOnPage_AndBodyEndsAboveFooterBand()
    {
        var (document, section) = Author();
        for (var i = 0; i < 3; i++)
        {
            section.Footer.Blocks.Add(Text($"FTR{i}", HeaderFontSize));
        }

        var (bodyLineHeight, _) = LineMetrics(BodyFontSize);
        var fullBodyLines = (int)((PageHeight - 2 * Margin) / bodyLineHeight);
        for (var i = 0; i < fullBodyLines; i++)
        {
            section.Blocks.Add(Text($"B{i}", BodyFontSize));
        }

        var runs = TextRuns(CascadeTestSupport.FirstPageContent(document));
        var footerYs = runs.Where(r => r.Text.StartsWith("FTR", StringComparison.Ordinal)).Select(r => r.Y).ToList();
        var bodyYs = runs.Where(r => r.Text.StartsWith("B", StringComparison.Ordinal)).Select(r => r.Y).ToList();

        Assert.Equal(3, footerYs.Count);
        Assert.NotEmpty(bodyYs);

        var (footerLineHeight, _) = LineMetrics(HeaderFontSize);
        var footerBandHeight = 3 * footerLineHeight;
        Assert.True(footerBandHeight > Margin, "fixture: footer band must be taller than the bottom margin");

        Assert.All(footerYs, y => Assert.True(y >= -Tol, $"footer baseline {y:F2} must stay on the page"));
        Assert.All(bodyYs, y => Assert.True(
            y >= footerBandHeight - Tol,
            $"body baseline {y:F2} must end above the footer band height {footerBandHeight:F2}"));
        Assert.True(footerYs.Max() < bodyYs.Min(), "footer sits strictly below all body lines");
    }

    [Fact]
    public void TallHeader_ReducedBodyHeight_ForcesPageBreak()
    {
        const int bodyLines = 10;
        var (bodyLineHeight, _) = LineMetrics(BodyFontSize);
        var (headerLineHeight, _) = LineMetrics(HeaderFontSize);

        var document = new Document();
        var section = document.Sections.Add();
        section.Margins.SetAll(Unit.FromPoint(Margin));
        section.PageSize = new PageSize(
            Unit.FromPoint(PageWidth),
            Unit.FromPoint(2 * Margin + bodyLines * bodyLineHeight + 0.01));

        for (var i = 0; i < 3; i++)
        {
            section.Header.Blocks.Add(Text($"HDR{i}", HeaderFontSize));
        }

        Assert.True(3 * headerLineHeight - Margin > bodyLineHeight, "fixture: header excess must exceed one body line");

        for (var i = 0; i < bodyLines; i++)
        {
            section.Blocks.Add(Text($"B{i}", BodyFontSize));
        }

        var reloaded = BuildTestSupport.Reload(document);
        Assert.True(
            reloaded.Pages.Count >= 2,
            $"expected the reduced body height to overflow onto a second page, got {reloaded.Pages.Count} page(s)");
    }

    private static Table BandTable(HeaderFooter band, string leftText, string rightText)
    {
        var table = band.Blocks.AddTable();
        table.Columns.Add();
        table.Columns.Add();
        var row = table.Rows.Add();
        TableLayoutSupport.Fill(row.Cells[0], leftText);
        TableLayoutSupport.Fill(row.Cells[1], rightText);
        return table;
    }

    [Fact]
    public void HeaderTable_RendersItsCellText()
    {
        var document = new Document();
        BuildTestSupport.RegisterLatin(document);
        var section = document.Sections.Add();
        BandTable(section.Header, "HeadLogo", "HeadTitle");
        BuildTestSupport.AddText(section, "BodyLine", BuildTestSupport.Latin);

        var text = BuildTestSupport.Reload(document).ExtractText();

        Assert.Contains("HeadLogo", text, StringComparison.Ordinal);
        Assert.Contains("HeadTitle", text, StringComparison.Ordinal);
        Assert.Contains("BodyLine", text, StringComparison.Ordinal);
        Assert.True(
            text.IndexOf("HeadLogo", StringComparison.Ordinal)
                < text.IndexOf("BodyLine", StringComparison.Ordinal),
            "header table text reads above the body");
    }

    [Fact]
    public void FooterTable_RendersItsCellText()
    {
        var document = new Document();
        BuildTestSupport.RegisterLatin(document);
        var section = document.Sections.Add();
        BandTable(section.Footer, "FootLeft", "FootRight");
        BuildTestSupport.AddText(section, "BodyLine", BuildTestSupport.Latin);

        var text = BuildTestSupport.Reload(document).ExtractText();

        Assert.Contains("FootLeft", text, StringComparison.Ordinal);
        Assert.Contains("FootRight", text, StringComparison.Ordinal);
        Assert.Contains("BodyLine", text, StringComparison.Ordinal);
        Assert.True(
            text.IndexOf("BodyLine", StringComparison.Ordinal)
                < text.IndexOf("FootLeft", StringComparison.Ordinal),
            "footer table text reads below the body");
    }
}
