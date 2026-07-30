#nullable enable
using System;
using Radzen.Documents.Pdf;
using Xunit;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

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
        var document = new Document();
        BuildTestSupport.RegisterLatin(document);

        var section = document.Sections.Add();
        BuildTestSupport.AddText(section, "Intro", BuildTestSupport.Latin);
        SmallTable(section, "CellText");

        var reloaded = BuildTestSupport.Reload(document);
        Assert.Equal(1, reloaded.Pages.Count);

        var text = reloaded.Pages[0].ExtractText();
        Assert.Contains("Intro", text, StringComparison.Ordinal);
        Assert.Contains("CellText", text, StringComparison.Ordinal);
    }

    [Fact]
    public void TableBearingSection_RendersHeaderAndFooterOnEveryPage()
    {
        var document = new Document();
        BuildTestSupport.RegisterLatin(document);

        var section = document.Sections.Add();
        section.Margins.SetAll(Unit.FromPoint(72));
        section.Header.Blocks.Add(PaginationSupport.Text("HDRBAND"));
        section.Footer.Blocks.Add(PaginationSupport.Text("FTRBAND"));

        var table = section.Blocks.AddTable();
        table.Columns.Add(Unit.FromPoint(200));
        var head = table.Rows.Add();
        head.RepeatOnEveryPage = true;
        TableLayoutSupport.Fill(head.Cells[0], "H0");
        for (var i = 0; i < 80; i++)
        {
            TableLayoutSupport.Fill(table.Rows.Add().Cells[0], $"R{i}");
        }

        var reloaded = BuildTestSupport.Reload(document);
        Assert.True(reloaded.Pages.Count > 1, "tall table spans multiple pages");

        for (var i = 0; i < reloaded.Pages.Count; i++)
        {
            var text = reloaded.Pages[i].ExtractText();
            Assert.True(text.Contains("HDRBAND", StringComparison.Ordinal), $"header band on page {i + 1}");
            Assert.True(text.Contains("FTRBAND", StringComparison.Ordinal), $"footer band on page {i + 1}");
        }
    }

    [Fact]
    public void TableAfterParagraphs_FirstFragmentFillsRemainingHeight()
    {
        var document = new Document();
        BuildTestSupport.RegisterLatin(document);
        var lh = PaginationSupport.LineHeight(document.Fonts, 12);

        var section = document.Sections.Add();
        section.PageSize = new PageSize(Unit.FromPoint(320), Unit.FromPoint(PaginationSupport.HeightForLines(lh, 8)));
        section.Margins.SetAll(Unit.FromPoint(0));

        BuildTestSupport.AddText(section, "Fa", BuildTestSupport.Latin);
        BuildTestSupport.AddText(section, "Fb", BuildTestSupport.Latin);

        var table = section.Blocks.AddTable();
        table.Columns.Add(Unit.FromPoint(300));
        var head = table.Rows.Add();
        head.RepeatOnEveryPage = true;
        TableLayoutSupport.Fill(head.Cells[0], "H0");
        for (var i = 0; i < 12; i++)
        {
            TableLayoutSupport.Fill(table.Rows.Add().Cells[0], $"R{i}");
        }

        var reloaded = BuildTestSupport.Reload(document);
        Assert.True(reloaded.Pages.Count > 1, "table overflows the first page");

        var first = reloaded.Pages[0].ExtractText();
        Assert.Contains("Fa", first, StringComparison.Ordinal);
        Assert.True(first.Contains("H0", StringComparison.Ordinal),
            "table header starts in the space remaining after the paragraphs");
        Assert.True(first.Contains("R0", StringComparison.Ordinal),
            "first body row shares the page with the paragraphs");
        Assert.Contains("H0", reloaded.Pages[1].ExtractText(), StringComparison.Ordinal);
    }

    private static (Document Builder, Paragraph Tail) AuthorSplitSection(int fillerLines)
    {
        var document = new Document();
        BuildTestSupport.RegisterLatin(document);
        var fonts = document.Fonts;
        var lh = PaginationSupport.LineHeight(fonts, 12);

        var section = document.Sections.Add();
        section.PageSize = new PageSize(
            Unit.FromPoint(PaginationSupport.WidthForWordsPerLine(fonts, "Keep", 1, 12)),
            Unit.FromPoint(PaginationSupport.HeightForLines(lh, 5)));
        section.Margins.SetAll(Unit.FromPoint(0));

        for (var i = 0; i < fillerLines; i++)
        {
            BuildTestSupport.AddText(section, "Fa", BuildTestSupport.Latin);
        }

        var tail = section.Blocks.Add(PaginationSupport.Repeated("Keep", 3));
        SmallTable(section, "T");
        return (document, tail);
    }

    [Fact]
    public void KeepTogether_AppliesInTableBearingSection()
    {
        var (document, tail) = AuthorSplitSection(fillerLines: 3);
        tail.KeepTogether = true;

        var reloaded = BuildTestSupport.Reload(document);
        Assert.True(reloaded.Pages.Count > 1, "content overflows to a second page");

        var first = reloaded.Pages[0].ExtractText();
        Assert.Equal(0, BuildTestSupport.CountOccurrences(first, "Keep"));
        Assert.Equal(3, BuildTestSupport.CountOccurrences(reloaded.Pages[1].ExtractText(), "Keep"));
    }

    [Fact]
    public void OrphanControl_AppliesInTableBearingSection()
    {
        var (document, _) = AuthorSplitSection(fillerLines: 4);

        var reloaded = BuildTestSupport.Reload(document);
        Assert.True(reloaded.Pages.Count > 1, "content overflows to a second page");

        var first = reloaded.Pages[0].ExtractText();
        Assert.Equal(0, BuildTestSupport.CountOccurrences(first, "Keep"));
        Assert.Equal(3, BuildTestSupport.CountOccurrences(reloaded.Pages[1].ExtractText(), "Keep"));
    }
}
