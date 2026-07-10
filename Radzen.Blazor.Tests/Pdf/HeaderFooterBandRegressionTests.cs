#nullable enable
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Radzen.Documents.Pdf;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// Regression tests for two header/footer band defects, asserted on the REAL emitted
// PDF (Build -> bytes -> content-stream Td positions / reload -> ExtractText):
//
// 1) A header band taller than the top margin must push the body content down (body
//    starts below the header's bottom edge), a footer band taller than the bottom
//    margin must pull the body's end up (body ends above the footer band, footer
//    stays on the page), and pagination must break pages on the REDUCED body height.
//
// 2) A Header/Footer whose Blocks contain a Table must render the table (cell text
//    present in the page output), exactly like a table in the body flow.
public class HeaderFooterBandRegressionTests
{
    private const double Tol = 0.5;
    private const double PageWidth = 400;
    private const double PageHeight = 500;
    private const double Margin = 40;
    private const double HeaderFontSize = 30;
    private const double BodyFontSize = 12;

    // Line height and top-to-baseline offset straight from the shared LineBreaker
    // (base-14 metrics, same path Build() uses when no font is registered).
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

    // (shown text, baseline Y in PDF space, i.e. origin at the page BOTTOM) pairs, in
    // emission order, from the base-14 literal-string "x y Td (text) Tj" pattern.
    private static List<(string Text, double Y)> TextRuns(string content)
    {
        var runs = new List<(string, double)>();
        foreach (Match m in Regex.Matches(content, @"(-?[\d.]+)\s+(-?[\d.]+)\s+Td\s*\((.*?)\)\s*Tj", RegexOptions.Singleline))
        {
            runs.Add((m.Groups[3].Value, double.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture)));
        }

        return runs;
    }

    private static (DocumentBuilder Builder, Section Section) Author()
    {
        var builder = new DocumentBuilder();
        var section = builder.Sections.Add();
        section.PageSize = new PageSize(Unit.FromPoint(PageWidth), Unit.FromPoint(PageHeight));
        section.Margin = Unit.FromPoint(Margin);
        return (builder, section);
    }

    [Fact]
    public void TallHeader_BodyFirstLineStartsBelowHeaderBand()
    {
        var (builder, section) = Author();
        for (var i = 0; i < 3; i++)
        {
            section.Header.Blocks.Add(Text($"HDR{i}", HeaderFontSize));
        }

        section.Blocks.Add(Text("BODY", BodyFontSize));

        var runs = TextRuns(CascadeTestSupport.FirstPageContent(builder));
        var headerYs = runs.Where(r => r.Text.StartsWith("HDR", System.StringComparison.Ordinal)).Select(r => r.Y).ToList();
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
        var (builder, section) = Author();
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

        var runs = TextRuns(CascadeTestSupport.FirstPageContent(builder));
        var footerYs = runs.Where(r => r.Text.StartsWith("FTR", System.StringComparison.Ordinal)).Select(r => r.Y).ToList();
        var bodyYs = runs.Where(r => r.Text.StartsWith("B", System.StringComparison.Ordinal)).Select(r => r.Y).ToList();

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

        var builder = new DocumentBuilder();
        var section = builder.Sections.Add();
        section.Margin = Unit.FromPoint(Margin);
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

        var reloaded = BuildTestSupport.Reload(builder);
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
        var builder = new DocumentBuilder();
        BuildTestSupport.RegisterLatin(builder);
        var section = builder.Sections.Add();
        BandTable(section.Header, "HeadLogo", "HeadTitle");
        BuildTestSupport.AddText(section, "BodyLine", BuildTestSupport.Latin);

        var text = BuildTestSupport.Reload(builder).ExtractText();

        Assert.Contains("HeadLogo", text, System.StringComparison.Ordinal);
        Assert.Contains("HeadTitle", text, System.StringComparison.Ordinal);
        Assert.Contains("BodyLine", text, System.StringComparison.Ordinal);
        Assert.True(
            text.IndexOf("HeadLogo", System.StringComparison.Ordinal)
                < text.IndexOf("BodyLine", System.StringComparison.Ordinal),
            "header table text reads above the body");
    }

    [Fact]
    public void FooterTable_RendersItsCellText()
    {
        var builder = new DocumentBuilder();
        BuildTestSupport.RegisterLatin(builder);
        var section = builder.Sections.Add();
        BandTable(section.Footer, "FootLeft", "FootRight");
        BuildTestSupport.AddText(section, "BodyLine", BuildTestSupport.Latin);

        var text = BuildTestSupport.Reload(builder).ExtractText();

        Assert.Contains("FootLeft", text, System.StringComparison.Ordinal);
        Assert.Contains("FootRight", text, System.StringComparison.Ordinal);
        Assert.Contains("BodyLine", text, System.StringComparison.Ordinal);
        Assert.True(
            text.IndexOf("BodyLine", System.StringComparison.Ordinal)
                < text.IndexOf("FootLeft", System.StringComparison.Ordinal),
            "footer table text reads below the body");
    }
}
