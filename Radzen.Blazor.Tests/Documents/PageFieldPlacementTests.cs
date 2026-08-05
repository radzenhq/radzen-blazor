#nullable enable
using System.Linq;
using Radzen.Documents.LaidOut;
using Radzen.Documents.Layout;
using Radzen.Documents;
using Xunit;
using Radzen.Documents.Core;

namespace Radzen.Blazor.Documents.Tests;

public class PageFieldPlacementTests
{
    private static Section Page(Document document)
    {
        var section = document.Sections.Add();
        section.PageSize = new PageSize(Unit.FromPoint(400), Unit.FromPoint(300));
        section.Margins.SetAll(Unit.FromPoint(20));
        return section;
    }

    private static void AddFields(Paragraph paragraph)
    {
        paragraph.Inlines.Add("Page ");
        paragraph.Inlines.Add(new PageNumberField());
        paragraph.Inlines.Add(" of ");
        paragraph.Inlines.Add(new PageCountField());
    }

    private static string Text(LaidOutLine line)
        => string.Concat(line.Line.Fragments.Select(fragment => fragment.Text));

    [Fact]
    public void PageFieldsInsideAContainer_ResolvePerPage()
    {
        var document = new Document();
        var section = Page(document);
        var container = section.Blocks.Add(new Container { Padding = Unit.FromPoint(4) });
        AddFields(container.Blocks.Add(new Paragraph()));
        section.Blocks.Add(new PageBreak());
        var second = section.Blocks.Add(new Container { Padding = Unit.FromPoint(4) });
        AddFields(second.Blocks.Add(new Paragraph()));

        var pages = DocumentLayouter.Layout(document).Pages;

        Assert.Equal(2, pages.Length);
        Assert.Equal("Page1of2", Text(Assert.Single(Assert.Single(pages[0].Body.Boxes).Content.Lines)));
        Assert.Equal("Page2of2", Text(Assert.Single(Assert.Single(pages[1].Body.Boxes).Content.Lines)));
    }

    [Fact]
    public void PageFieldsInsideANestedTableCell_ResolvePerPage()
    {
        var document = new Document();
        var section = Page(document);

        for (var page = 0; page < 2; page++)
        {
            if (page > 0)
            {
                section.Blocks.Add(new PageBreak());
            }

            var outer = section.Blocks.Add(new Table());
            outer.Columns.Add(Unit.FromPoint(300));
            var inner = outer.Rows.Add().Cells[0].Blocks.Add(new Table());
            inner.Columns.Add(Unit.FromPoint(200));
            AddFields(inner.Rows.Add().Cells[0].Blocks.Add(new Paragraph()));
        }

        var pages = DocumentLayouter.Layout(document).Pages;

        Assert.Equal(2, pages.Length);
        Assert.Equal("Page1of2", NestedCellText(pages[0]));
        Assert.Equal("Page2of2", NestedCellText(pages[1]));
    }

    private static string NestedCellText(LaidOutPage page)
    {
        var outer = Assert.Single(page.Body.Tables);
        var cell = Assert.Single(outer.Layout.Cells);
        var inner = Assert.Single(cell.Tables);

        return Text(Assert.Single(Assert.Single(inner.Layout.Cells).Lines));
    }
}
