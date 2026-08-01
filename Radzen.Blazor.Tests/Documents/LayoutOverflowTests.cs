#nullable enable
using System;
using System.Linq;
using Radzen.Documents.Layout;
using Radzen.Documents;
using Radzen.Blazor.Pdf.Tests;
using Xunit;
using Radzen.Documents.Core;

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
    public void MarginsExceedingThePageHeight_ThrowNamingTheSectionAndTheComputedBox()
    {
        var document = new Document();
        var section = document.Sections.Add();
        section.PageSize = new PageSize(Unit.FromPoint(200), Unit.FromPoint(100));
        section.Margins.SetAll(Unit.FromPoint(10));
        section.Margins.Top = Unit.FromPoint(80);
        section.Margins.Bottom = Unit.FromPoint(80);
        section.Blocks.AddParagraph("Body");

        var error = Assert.Throws<InvalidOperationException>(() => DocumentLayouter.Layout(document));

        Assert.Contains("Section 0", error.Message);
        Assert.Contains("180 x -60 points", error.Message);
    }

    [Fact]
    public void MarginsExceedingThePageWidth_ThrowNamingTheSectionAndTheComputedBox()
    {
        var document = new Document();
        var section = document.Sections.Add();
        section.PageSize = new PageSize(Unit.FromPoint(100), Unit.FromPoint(200));
        section.Margins.SetAll(Unit.FromPoint(10));
        section.Margins.Left = Unit.FromPoint(80);
        section.Margins.Right = Unit.FromPoint(80);
        section.Blocks.AddParagraph("Body");

        var error = Assert.Throws<InvalidOperationException>(() => DocumentLayouter.Layout(document));

        Assert.Contains("Section 0", error.Message);
        Assert.Contains("-60 x 180 points", error.Message);
    }

    [Fact]
    public void HeaderAndFooterDistancesSwallowingTheContentBox_ThrowNamingTheSectionAndTheComputedBox()
    {
        var document = new Document();
        var section = document.Sections.Add();
        section.PageSize = new PageSize(Unit.FromPoint(200), Unit.FromPoint(100));
        section.Margins.SetAll(Unit.FromPoint(10));
        section.HeaderDistance = Unit.FromPoint(60);
        section.FooterDistance = Unit.FromPoint(60);
        section.Header.Blocks.AddParagraph("Header");
        section.Footer.Blocks.AddParagraph("Footer");
        section.Blocks.AddParagraph("Body");

        var error = Assert.Throws<InvalidOperationException>(() => DocumentLayouter.Layout(document));

        Assert.Contains("Section 0", error.Message);
        Assert.Contains("header distance and footer distance", error.Message);
    }

    [Fact]
    public void MarginsThatExactlyConsumeThePageHeight_Throw()
    {
        var document = new Document();
        var section = document.Sections.Add();
        section.PageSize = new PageSize(Unit.FromPoint(200), Unit.FromPoint(100));
        section.Margins.SetAll(Unit.FromPoint(10));
        section.Margins.Top = Unit.FromPoint(50);
        section.Margins.Bottom = Unit.FromPoint(50);
        section.Blocks.AddParagraph("Body");

        var error = Assert.Throws<InvalidOperationException>(() => DocumentLayouter.Layout(document));

        Assert.Contains("180 x 0 points", error.Message);
    }

    [Fact]
    public void ImpossibleMarginsInALaterSection_NameThatSection()
    {
        var document = new Document();
        var first = Page(document, 200, 100);
        first.Blocks.AddParagraph("Body");

        var second = document.Sections.Add();
        second.PageSize = new PageSize(Unit.FromPoint(200), Unit.FromPoint(100));
        second.Margins.SetAll(Unit.FromPoint(10));
        second.Margins.Top = Unit.FromPoint(80);
        second.Margins.Bottom = Unit.FromPoint(80);

        var error = Assert.Throws<InvalidOperationException>(() => DocumentLayouter.Layout(document));

        Assert.Contains("Section 1", error.Message);
    }

    [Fact]
    public void MarginsLeavingAThinContentBox_TerminateInsteadOfPaginatingForever()
    {
        var document = new Document();
        var section = document.Sections.Add();
        section.PageSize = new PageSize(Unit.FromPoint(200), Unit.FromPoint(100));
        section.Margins.SetAll(Unit.FromPoint(10));
        section.Margins.Top = Unit.FromPoint(49);
        section.Margins.Bottom = Unit.FromPoint(50);

        for (var line = 0; line < 10; line++)
        {
            section.Blocks.AddParagraph($"Line {line}");
        }

        var pages = DocumentLayouter.Layout(document).Pages;

        Assert.Equal(1, pages[0].ContentBox.Height, 6);
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
