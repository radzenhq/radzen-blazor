#nullable enable
using System.Linq;
using Radzen.Documents.Layout;
using Radzen.Documents;
using Radzen.Blazor.Pdf.Tests;
using Xunit;

namespace Radzen.Blazor.Documents.Tests;

public class LayoutOverflowTests
{
    private static Section Page(Document document, double width, double height, double margin = 10)
    {
        var section = document.Sections.Add();
        section.PageSize = new PageSize(Unit.FromPoint(width), Unit.FromPoint(height));
        section.Margins.SetAll(Unit.FromPoint(margin));
        return section;
    }

    [Fact]
    public void ImageTallerThanThePage_StaysOnOnePageAndOverflowsTheContentBox()
    {
        var document = new Document();
        var section = Page(document, 200, 100);
        var image = section.Blocks.AddImage(PdfTestResources.Open("Images/rgb.jpg"));
        image.Width = Unit.FromPoint(150);
        image.Height = Unit.FromPoint(400);

        var page = Assert.Single(DocumentLayouter.Layout(document).Pages);
        var placed = Assert.Single(page.Body.Images);

        Assert.Equal(80, page.ContentBox.Height, 6);
        Assert.Equal(0, placed.Y, 6);
        Assert.Equal(400, placed.Height, 6);
        Assert.True(
            placed.Y + placed.Height > page.ContentBox.Height,
            "an atomic image taller than the content box is not split, scaled, or clipped - it overflows");
        Assert.True(
            placed.Height > page.Size.Height.Point,
            "the overflow reaches past the page edge and the layout emits no diagnostic");
    }

    [Fact]
    public void ContainerTallerThanThePage_StaysOnOnePageAndOverflowsTheContentBox()
    {
        var document = new Document();
        var section = Page(document, 300, 120);
        var container = section.Blocks.Add(new Container
        {
            Padding = Unit.FromPoint(4),
            Background = Color.FromRgb(200, 200, 200),
        });

        for (var line = 0; line < 20; line++)
        {
            container.Blocks.AddParagraph($"Line {line}");
        }

        var page = Assert.Single(DocumentLayouter.Layout(document).Pages);
        var box = Assert.Single(page.Body.Boxes);

        Assert.Equal(100, page.ContentBox.Height, 6);
        Assert.Equal(20, box.Content.Lines.Length);
        Assert.Equal(0, box.Bounds.Y, 6);
        Assert.True(
            box.Bounds.Height > page.ContentBox.Height,
            "a container taller than the content box is not split across pages - it overflows as one box");
        Assert.True(
            box.Bounds.Height > page.Size.Height.Point,
            "the overflowing box reaches past the page edge and no content is dropped");
    }

    [Fact]
    public void MarginsExceedingThePageHeight_ProduceANegativeContentBoxAndStillLayOutTheFirstLine()
    {
        var document = new Document();
        var section = document.Sections.Add();
        section.PageSize = new PageSize(Unit.FromPoint(200), Unit.FromPoint(100));
        section.Margins.Top = Unit.FromPoint(80);
        section.Margins.Bottom = Unit.FromPoint(80);
        section.Blocks.AddParagraph("Body");

        var page = Assert.Single(DocumentLayouter.Layout(document).Pages);

        Assert.Equal(-60, page.ContentBox.Height, 6);
        Assert.Equal(80, page.ContentBox.Y, 6);
        Assert.Equal(0, Assert.Single(page.Body.Lines).Y, 6);
    }

    [Fact]
    public void MarginsExceedingThePageHeight_TerminateInsteadOfPaginatingForever()
    {
        var document = new Document();
        var section = document.Sections.Add();
        section.PageSize = new PageSize(Unit.FromPoint(200), Unit.FromPoint(100));
        section.Margins.Top = Unit.FromPoint(80);
        section.Margins.Bottom = Unit.FromPoint(80);

        for (var line = 0; line < 10; line++)
        {
            section.Blocks.AddParagraph($"Line {line}");
        }

        var pages = DocumentLayouter.Layout(document).Pages;

        Assert.Equal(10, pages.Sum(page => page.Body.Lines.Length));
        Assert.InRange(pages.Length, 1, 10);
    }

    [Fact]
    public void EmptySection_EmitsExactlyOneEmptyPage()
    {
        var document = new Document();
        document.Sections.Add();

        var page = Assert.Single(DocumentLayouter.Layout(document).Pages);

        Assert.Empty(page.Body.Lines);
        Assert.Empty(page.Body.Boxes);
        Assert.Empty(page.Body.Tables);
        Assert.Empty(page.Body.Images);
        Assert.Empty(page.Body.CodeSymbols);
        Assert.Equal(1, page.Number);
    }

    [Fact]
    public void EmptyDocumentWithNoSections_EmitsNoPages()
        => Assert.Empty(DocumentLayouter.Layout(new Document()).Pages);
}
