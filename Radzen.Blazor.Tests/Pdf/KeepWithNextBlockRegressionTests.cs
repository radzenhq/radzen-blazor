#nullable enable
using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Radzen.Documents.Pdf;
using Xunit;

using Radzen.Documents.Pdf.Render;
using Radzen.Documents;
using Radzen.Documents.Fonts;
using Radzen.Documents.Layout;
using Radzen.Documents.Core;
namespace Radzen.Blazor.Pdf.Tests;

public class KeepWithNextBlockRegressionTests
{
    private const double PageWidth = 400;
    private const double PageHeight = 500;
    private const double Margin = 40;
    private const double BodyFontSize = 12;

    private static Paragraph Text(string text, double size)
    {
        var paragraph = new Paragraph();
        var run = paragraph.Inlines.Add(text);
        run.Font.Size = size;
        return paragraph;
    }

    private static string PageContent(Document document, int index, out int pageCount)
    {
        var reader = BuildTestSupport.Read(document);
        var leaves = BuildTestSupport.PageLeaves(reader);
        pageCount = leaves.Count;
        return Encoding.Latin1.GetString(BuildTestSupport.Content(reader, leaves[index].Page));
    }

    private static bool Shows(string content, string text)
        => Regex.IsMatch(content, Regex.Escape("(" + text + ")") + @"\s*Tj");

    private static (Document Builder, Section Section) AuthorNearlyFullPage()
    {
        var document = new Document();
        var section = document.Sections.Add();
        section.PageSize = new PageSize(Unit.FromPoint(PageWidth), Unit.FromPoint(PageHeight));
        section.Margins.SetAll(Unit.FromPoint(Margin));

        var lineHeight = PaginationSupport.BuiltInLineHeight(BodyFontSize);
        var contentHeight = PageHeight - 2 * Margin;
        var fillers = (int)(contentHeight / lineHeight) - 1;
        for (var i = 0; i < fillers; i++)
        {
            section.Blocks.Add(Text($"Filler{i}", BodyFontSize));
        }

        return (document, section);
    }

    [Fact]
    public void KeepWithNext_BeforeTable_MovesHeadingWithTable()
    {
        var (document, section) = AuthorNearlyFullPage();

        var heading = Text("LineItemsHeading", BodyFontSize);
        heading.KeepWithNext = true;
        section.Blocks.Add(heading);

        var table = section.Blocks.Add(new Table());
        table.Columns.Add().Width = Unit.FromPoint(200);
        var row = table.Rows.Add();
        var cellParagraph = row.Cells[0].Blocks.Add(new Paragraph());
        var run = cellParagraph.Inlines.Add("TallCellText");
        run.Font.Size = 30;

        var page1 = PageContent(document, 0, out var pageCount);
        Assert.Equal(2, pageCount);
        var page2 = PageContent(document, 1, out _);

        Assert.True(Shows(page1, "Filler0"), "fixture: filler text renders on page 1");
        Assert.True(Shows(page2, "TallCellText"), "fixture: the table row must break to page 2");
        Assert.False(
            Shows(page1, "LineItemsHeading"),
            "a KeepWithNext heading must not be stranded on page 1 when its table breaks to page 2");
        Assert.True(
            Shows(page2, "LineItemsHeading"),
            "the KeepWithNext heading must move to page 2 together with the table");
    }

    [Fact]
    public void KeepWithNext_BeforeTableWithHeaderRow_KeepsHeadingWithHeaderAndFirstRow()
    {
        var (document, section) = AuthorNearlyFullPage();

        var heading = Text("TableCaption", BodyFontSize);
        heading.KeepWithNext = true;
        section.Blocks.Add(heading);

        var table = section.Blocks.Add(new Table());
        table.Columns.Add().Width = Unit.FromPoint(200);
        var headerRow = table.Rows.Add();
        headerRow.RepeatOnEveryPage = true;
        headerRow.Cells[0].Blocks.Add(new Paragraph()).Inlines.Add("ColumnHead");
        var bodyRow = table.Rows.Add();
        bodyRow.Cells[0].Blocks.Add(new Paragraph()).Inlines.Add("FirstBodyRow");

        var page1 = PageContent(document, 0, out var pageCount);
        Assert.Equal(2, pageCount);
        var page2 = PageContent(document, 1, out _);

        Assert.True(
            Shows(page2, "ColumnHead") && Shows(page2, "FirstBodyRow"),
            "fixture: header row plus first body row must not fit under the heading and break to page 2");
        Assert.False(
            Shows(page1, "TableCaption"),
            "a KeepWithNext caption must not be stranded on page 1 when its table breaks to page 2");
        Assert.True(Shows(page2, "TableCaption"), "the caption must move to page 2 with the table");
    }

    [Fact]
    public void KeepWithNext_BeforeKeepTogetherParagraph_MovesHeadingWithTheWholeParagraph()
    {
        var (document, section) = AuthorNearlyFullPage();

        var heading = Text("ClosingHeading", BodyFontSize);
        heading.KeepWithNext = true;
        section.Blocks.Add(heading);

        var closing = Text(string.Join(" ", Enumerable.Repeat("closing words that wrap across several lines", 6)), BodyFontSize);
        closing.KeepTogether = true;
        section.Blocks.Add(closing);

        var page1 = PageContent(document, 0, out var pageCount);
        Assert.Equal(2, pageCount);
        var page2 = PageContent(document, 1, out _);

        Assert.Contains("closing", page2, StringComparison.Ordinal);
        Assert.DoesNotContain("closing", page1, StringComparison.Ordinal);
        Assert.False(
            Shows(page1, "ClosingHeading"),
            "a KeepWithNext heading must not be stranded on page 1 when its KeepTogether paragraph moves whole to page 2");
        Assert.True(Shows(page2, "ClosingHeading"), "the heading must move to page 2 with the paragraph");
    }

    [Fact]
    public void KeepWithNext_BeforeParagraphMovedWholeByOrphanControl_MovesHeadingWithIt()
    {
        var (document, section) = AuthorNearlyFullPage();

        var heading = Text("ClosingHeading", BodyFontSize);
        heading.KeepWithNext = true;
        section.Blocks.Add(heading);

        var closing = Text(string.Join(" ", Enumerable.Repeat("closing words that wrap across several lines", 6)), BodyFontSize);
        closing.Orphans = 4;
        closing.Widows = 4;
        section.Blocks.Add(closing);

        var page1 = PageContent(document, 0, out var pageCount);
        Assert.Equal(2, pageCount);
        var page2 = PageContent(document, 1, out _);

        Assert.Contains("closing", page2, StringComparison.Ordinal);
        Assert.DoesNotContain("closing", page1, StringComparison.Ordinal);
        Assert.False(
            Shows(page1, "ClosingHeading"),
            "a KeepWithNext heading must not be stranded on page 1 when orphan control moves its paragraph whole to page 2");
        Assert.True(Shows(page2, "ClosingHeading"), "the heading must move to page 2 with the paragraph");
    }

    [Fact]
    public void KeepWithNext_BeforeImage_MovesHeadingWithImage()
    {
        var (document, section) = AuthorNearlyFullPage();

        var heading = Text("FigureHeading", BodyFontSize);
        heading.KeepWithNext = true;
        section.Blocks.Add(heading);

        var image = section.Blocks.Add(new Image(PdfTestResources.Open("Images/rgb.jpg")));
        image.Width = Unit.FromPoint(60);
        image.Height = Unit.FromPoint(60);

        var page1 = PageContent(document, 0, out var pageCount);
        Assert.Equal(2, pageCount);
        var page2 = PageContent(document, 1, out _);

        Assert.False(
            Regex.IsMatch(page1, @"/\w+\s+Do\b"),
            "fixture: the image must not fit on page 1");
        Assert.True(
            Regex.IsMatch(page2, @"/\w+\s+Do\b"),
            "fixture: the image paints on page 2");
        Assert.False(
            Shows(page1, "FigureHeading"),
            "a KeepWithNext heading must not be stranded on page 1 when its image breaks to page 2");
        Assert.True(Shows(page2, "FigureHeading"), "the heading must move to page 2 with the image");
    }
}
