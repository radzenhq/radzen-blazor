#nullable enable
using System;
using Radzen.Documents.Pdf;
using Xunit;
using Radzen.Documents;
using Document = Radzen.Documents.Document;

namespace Radzen.Blazor.Pdf.Tests;

public class BuildTableTests
{
    private static int Index(string text, string needle)
        => text.IndexOf(needle, StringComparison.Ordinal);

    [Fact]
    public void SinglePageTable_RoundTripsHeaderAndCellsInOrder()
    {
        var document = new Document();
        BuildTestSupport.RegisterLatin(document);

        var section = document.Sections.Add();
        var table = section.Blocks.AddTable();
        table.Columns.Add();
        table.Columns.Add();

        var header = table.Rows.Add();
        header.RepeatOnEveryPage = true;
        header.IsHeaderRow = true;
        TableLayoutSupport.Fill(header.Cells[0], "Item");
        TableLayoutSupport.Fill(header.Cells[1], "Price");

        var apple = table.Rows.Add();
        TableLayoutSupport.Fill(apple.Cells[0], "Apple");
        TableLayoutSupport.Fill(apple.Cells[1], "111");

        var bread = table.Rows.Add();
        TableLayoutSupport.Fill(bread.Cells[0], "Bread");
        TableLayoutSupport.Fill(bread.Cells[1], "222");

        var reloaded = BuildTestSupport.Reload(document);
        Assert.Equal(1, reloaded.Pages.Count);

        var text = reloaded.ExtractText();
        foreach (var cell in new[] { "Item", "Price", "Apple", "111", "Bread", "222" })
        {
            Assert.True(Index(text, cell) >= 0, $"cell '{cell}' present");
        }

        Assert.True(Index(text, "Item") < Index(text, "Apple"), "header row above first body row");
        Assert.True(Index(text, "Apple") < Index(text, "Bread"), "body rows in source order");
    }

    private static Document AuthorTallTable()
    {
        var document = new Document();
        BuildTestSupport.RegisterLatin(document);
        var lh = PaginationSupport.LineHeight(document.Fonts, 12);

        var section = document.Sections.Add();
        section.PageSize = new PageSize(Unit.FromPoint(320), Unit.FromPoint(PaginationSupport.HeightForLines(lh, 5)));
        section.Margins.SetAll(Unit.FromPoint(0));

        var table = section.Blocks.AddTable();
        table.Columns.Add(Unit.FromPoint(300));

        var header = table.Rows.Add();
        header.RepeatOnEveryPage = true;
        TableLayoutSupport.Fill(header.Cells[0], "H0");

        for (var i = 0; i < 12; i++)
        {
            var row = table.Rows.Add();
            TableLayoutSupport.Fill(row.Cells[0], $"R{i}");
        }

        return document;
    }

    [Fact]
    public void TallTable_PaginatesToMultiplePages()
    {
        var reloaded = BuildTestSupport.Reload(AuthorTallTable());
        Assert.True(reloaded.Pages.Count > 1, "table taller than one page splits");
    }

    [Fact]
    public void TallTable_RepeatsHeaderOnEveryPage()
    {
        var reloaded = BuildTestSupport.Reload(AuthorTallTable());

        foreach (var page in reloaded.Pages)
        {
            Assert.Contains("H0", page.ExtractText(), StringComparison.Ordinal);
        }

        var occurrences = BuildTestSupport.CountOccurrences(reloaded.ExtractText(), "H0");
        Assert.True(occurrences >= reloaded.Pages.Count, "header repeats on each page");
    }

    [Fact]
    public void TallTable_BodyRowsSurviveInOrder()
    {
        var text = BuildTestSupport.Reload(AuthorTallTable()).ExtractText();

        var previous = -1;
        for (var i = 0; i < 12; i++)
        {
            var at = Index(text, $"R{i}");
            Assert.True(at >= 0, $"body row R{i} present");
            Assert.True(at > previous, $"body row R{i} follows R{i - 1}");
            previous = at;
        }
    }
}
