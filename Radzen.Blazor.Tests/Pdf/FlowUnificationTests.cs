#nullable enable
using Radzen.Documents.Pdf;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// P1 regression tests: sections mixing paragraphs and tables must run through ONE
// unified flow. Pinned behavior:
//   * a Table starts at the current cursor (no forced page break before it); its first
//     fragment gets the REMAINING page height and breaks early only when the header +
//     first body row cannot fit,
//   * section Header/Footer bands render on EVERY page of EVERY section, including
//     table-bearing ones,
//   * KeepTogether / widow / orphan rules apply across the unified flow.
// All asserts reload the produced bytes and inspect extracted per-page text.
public class FlowUnificationTests
{
    private static Table SmallTable(Section section, string text)
    {
        var table = section.Blocks.AddTable();
        table.Columns.Add(Unit.FromPoint(100));
        var row = table.Rows.Add();
        TableLayoutSupport.Fill(row.Cells[0], text);
        return table;
    }

    [Fact]
    public void ParagraphThenTable_FlowsInlineOnSamePage()
    {
        var builder = new DocumentBuilder();
        BuildTestSupport.RegisterLatin(builder);

        var section = builder.Sections.Add();
        BuildTestSupport.AddText(section, "Intro", BuildTestSupport.Latin);
        SmallTable(section, "CellText");

        var reloaded = BuildTestSupport.Reload(builder);
        Assert.Equal(1, reloaded.Pages.Count);

        var text = reloaded.Pages[0].ExtractText();
        Assert.Contains("Intro", text, System.StringComparison.Ordinal);
        Assert.Contains("CellText", text, System.StringComparison.Ordinal);
    }

    [Fact]
    public void TableBearingSection_RendersHeaderAndFooterOnEveryPage()
    {
        var builder = new DocumentBuilder();
        BuildTestSupport.RegisterLatin(builder);

        var section = builder.Sections.Add();
        section.Margin = Unit.FromPoint(72);
        section.Header.Blocks.Add(PaginationSupport.Text("HDRBAND"));
        section.Footer.Blocks.Add(PaginationSupport.Text("FTRBAND"));

        var table = section.Blocks.AddTable();
        table.Columns.Add(Unit.FromPoint(200));
        var head = table.Rows.Add();
        head.IsHeader = true;
        TableLayoutSupport.Fill(head.Cells[0], "H0");
        for (var i = 0; i < 80; i++)
        {
            TableLayoutSupport.Fill(table.Rows.Add().Cells[0], $"R{i}");
        }

        var reloaded = BuildTestSupport.Reload(builder);
        Assert.True(reloaded.Pages.Count > 1, "tall table spans multiple pages");

        for (var i = 0; i < reloaded.Pages.Count; i++)
        {
            var text = reloaded.Pages[i].ExtractText();
            Assert.True(text.Contains("HDRBAND", System.StringComparison.Ordinal), $"header band on page {i + 1}");
            Assert.True(text.Contains("FTRBAND", System.StringComparison.Ordinal), $"footer band on page {i + 1}");
        }
    }

    [Fact]
    public void TableAfterParagraphs_FirstFragmentFillsRemainingHeight()
    {
        var builder = new DocumentBuilder();
        BuildTestSupport.RegisterLatin(builder);
        var lh = PaginationSupport.LineHeight(builder.Fonts, 12);

        var section = builder.Sections.Add();
        section.PageSize = new PageSize(Unit.FromPoint(320), Unit.FromPoint(PaginationSupport.HeightForLines(lh, 8)));
        section.Margin = Unit.FromPoint(0);

        BuildTestSupport.AddText(section, "Fa", BuildTestSupport.Latin);
        BuildTestSupport.AddText(section, "Fb", BuildTestSupport.Latin);

        var table = section.Blocks.AddTable();
        table.Columns.Add(Unit.FromPoint(300));
        var head = table.Rows.Add();
        head.IsHeader = true;
        TableLayoutSupport.Fill(head.Cells[0], "H0");
        for (var i = 0; i < 12; i++)
        {
            TableLayoutSupport.Fill(table.Rows.Add().Cells[0], $"R{i}");
        }

        var reloaded = BuildTestSupport.Reload(builder);
        Assert.True(reloaded.Pages.Count > 1, "table overflows the first page");

        var first = reloaded.Pages[0].ExtractText();
        Assert.Contains("Fa", first, System.StringComparison.Ordinal);
        Assert.True(first.Contains("H0", System.StringComparison.Ordinal),
            "table header starts in the space remaining after the paragraphs");
        Assert.True(first.Contains("R0", System.StringComparison.Ordinal),
            "first body row shares the page with the paragraphs");
        Assert.Contains("H0", reloaded.Pages[1].ExtractText(), System.StringComparison.Ordinal);
    }

    private static (DocumentBuilder Builder, Paragraph Tail) AuthorSplitSection(int fillerLines)
    {
        var builder = new DocumentBuilder();
        BuildTestSupport.RegisterLatin(builder);
        var fonts = builder.Fonts;
        var lh = PaginationSupport.LineHeight(fonts, 12);

        var section = builder.Sections.Add();
        section.PageSize = new PageSize(
            Unit.FromPoint(PaginationSupport.WidthForWordsPerLine(fonts, "Keep", 1, 12)),
            Unit.FromPoint(PaginationSupport.HeightForLines(lh, 5)));
        section.Margin = Unit.FromPoint(0);

        for (var i = 0; i < fillerLines; i++)
        {
            BuildTestSupport.AddText(section, "Fa", BuildTestSupport.Latin);
        }

        var tail = section.Blocks.Add(PaginationSupport.Repeated("Keep", 3));
        SmallTable(section, "T");
        return (builder, tail);
    }

    [Fact]
    public void KeepTogether_AppliesInTableBearingSection()
    {
        var (builder, tail) = AuthorSplitSection(fillerLines: 3);
        tail.KeepTogether = true;

        var reloaded = BuildTestSupport.Reload(builder);
        Assert.True(reloaded.Pages.Count > 1, "content overflows to a second page");

        var first = reloaded.Pages[0].ExtractText();
        Assert.Equal(0, BuildTestSupport.CountOccurrences(first, "Keep"));
        Assert.Equal(3, BuildTestSupport.CountOccurrences(reloaded.Pages[1].ExtractText(), "Keep"));
    }

    [Fact]
    public void OrphanControl_AppliesInTableBearingSection()
    {
        // 4 filler lines leave room for only 1 line of the 3-line paragraph; the
        // default Orphans = 2 must push the whole paragraph to the next page.
        var (builder, _) = AuthorSplitSection(fillerLines: 4);

        var reloaded = BuildTestSupport.Reload(builder);
        Assert.True(reloaded.Pages.Count > 1, "content overflows to a second page");

        var first = reloaded.Pages[0].ExtractText();
        Assert.Equal(0, BuildTestSupport.CountOccurrences(first, "Keep"));
        Assert.Equal(3, BuildTestSupport.CountOccurrences(reloaded.Pages[1].ExtractText(), "Keep"));
    }
}
