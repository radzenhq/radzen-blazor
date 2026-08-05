#nullable enable
using System.Linq;
using Radzen.Documents.Core;
using Radzen.Documents.LaidOut;
using Radzen.Documents.Layout;
using Radzen.Documents;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

public class DesignerHitTestTests
{
    private static Section Page(Document document, double width = 400, double height = 300)
    {
        var section = document.Sections.Add();
        section.PageSize = new PageSize(Unit.FromPoint(width), Unit.FromPoint(height));
        section.Margins.SetAll(Unit.FromPoint(20));
        return section;
    }

    [Fact]
    public void HitTestingTextOfTheSecondParagraph_ResolvesToTheInlineThatAuthoredIt()
    {
        var document = new Document();
        var section = Page(document);
        BuildTestSupport.RegisterLatin(document);
        var first = section.Blocks.Add(new Paragraph());
        first.Inlines.Add("First paragraph").Font.Family = BuildTestSupport.Latin;
        var second = section.Blocks.Add(new Paragraph());
        var target = second.Inlines.Add("Second paragraph");
        target.Font.Family = BuildTestSupport.Latin;

        var laidOut = DocumentLayouter.LayoutWithSources(document);
        var page = Assert.Single(laidOut.Scene.Pages);
        var line = page.Body.Lines[1];
        var fragment = line.Line.Fragments[0];
        var x = page.ContentBox.X + line.X + fragment.XOffset + (fragment.Advance / 2);
        var y = page.ContentBox.Y + line.Y + (line.Line.Height / 2);

        var hit = SceneHitTest.At(page, x, y);

        Assert.NotNull(hit);
        Assert.Same(target, laidOut.Sources.Resolve(hit.Value));
        Assert.Same(second, laidOut.Sources.Resolve(line.Source));
        Assert.Equal(hit, laidOut.Sources.Of(target));
    }

    [Fact]
    public void HitTestingAnEmptyAreaOfThePage_ResolvesToNothing()
    {
        var document = new Document();
        var section = Page(document);
        BuildTestSupport.RegisterLatin(document);
        section.Blocks.Add(new Paragraph()).Inlines.Add("Only line").Font.Family = BuildTestSupport.Latin;

        var laidOut = DocumentLayouter.LayoutWithSources(document);
        var page = Assert.Single(laidOut.Scene.Pages);

        Assert.Null(SceneHitTest.At(page, 5, 5));
        Assert.Null(SceneHitTest.At(page, page.Size.Width.Point - 5, page.Size.Height.Point - 5));
    }

    [Fact]
    public void GeometryOfTheModelImage_MatchesItsPlacementInTheScene()
    {
        var document = new Document();
        var section = Page(document);
        BuildTestSupport.RegisterLatin(document);
        section.Blocks.Add(new Paragraph()).Inlines.Add("Above").Font.Family = BuildTestSupport.Latin;
        var image = section.Blocks.Add(new Image(PdfTestResources.Open("Images/rgb.jpg")));
        image.Width = Unit.FromPoint(80);
        image.Height = Unit.FromPoint(40);

        var laidOut = DocumentLayouter.LayoutWithSources(document);
        var page = Assert.Single(laidOut.Scene.Pages);
        var placed = Assert.Single(page.Body.Images);
        var id = laidOut.Sources.Of(image);

        Assert.NotNull(id);
        var geometry = Assert.Single(SceneHitTest.Geometry(laidOut.Scene, id.Value));

        Assert.Equal(0, geometry.PageIndex);
        Assert.Equal(page.ContentBox.X + placed.X, geometry.Bounds.X, 6);
        Assert.Equal(page.ContentBox.Y + placed.Y, geometry.Bounds.Y, 6);
        Assert.Equal(placed.Width, geometry.Bounds.Width, 6);
        Assert.Equal(placed.Height, geometry.Bounds.Height, 6);
        Assert.Equal(
            id,
            SceneHitTest.At(
                page,
                geometry.Bounds.X + (geometry.Bounds.Width / 2),
                geometry.Bounds.Y + (geometry.Bounds.Height / 2)));
    }

    [Fact]
    public void GeometryOfAParagraphSplitByAPageBreak_CoversBothPagesWithinTheirBounds()
    {
        var document = new Document();
        var section = Page(document, height: 120);
        BuildTestSupport.RegisterLatin(document);
        var paragraph = section.Blocks.Add(new Paragraph());
        var run = paragraph.Inlines.Add(string.Join(' ', Enumerable.Repeat("flowing text", 40)));
        run.Font.Family = BuildTestSupport.Latin;

        var laidOut = DocumentLayouter.LayoutWithSources(document);
        Assert.True(laidOut.Scene.Pages.Length > 1);
        var id = laidOut.Sources.Of(paragraph);

        Assert.NotNull(id);
        var geometry = SceneHitTest.Geometry(laidOut.Scene, id.Value);

        Assert.Contains(geometry, item => item.PageIndex == 0);
        Assert.Contains(geometry, item => item.PageIndex == 1);
        foreach (var item in geometry)
        {
            var page = laidOut.Scene.Pages[item.PageIndex];
            Assert.InRange(item.Bounds.X, 0, page.Size.Width.Point);
            Assert.InRange(item.Bounds.Y, 0, page.Size.Height.Point);
            Assert.InRange(item.Bounds.X + item.Bounds.Width, 0, page.Size.Width.Point);
            Assert.InRange(item.Bounds.Y + item.Bounds.Height, 0, page.Size.Height.Point);
        }
    }

    [Fact]
    public void ResolvingAnUnknownSourceId_ThrowsAndOfAnUnregisteredObject_ReturnsNothing()
    {
        var document = new Document();
        var section = Page(document);
        BuildTestSupport.RegisterLatin(document);
        section.Blocks.Add(new Paragraph()).Inlines.Add("Text").Font.Family = BuildTestSupport.Latin;

        var sources = DocumentLayouter.LayoutWithSources(document).Sources;

        Assert.Null(sources.Of(new Paragraph()));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => sources.Resolve(new SourceId(sources.Count)));
    }

    [Fact]
    public void LayoutWithSources_ProducesTheSameSceneAsLayout()
    {
        var document = new Document();
        var section = Page(document);
        BuildTestSupport.RegisterLatin(document);
        section.Blocks.Add(new Paragraph()).Inlines.Add("Text").Font.Family = BuildTestSupport.Latin;

        Assert.Equal(
            LayoutGeometry.Serialize(DocumentLayouter.Layout(document)),
            LayoutGeometry.Serialize(DocumentLayouter.LayoutWithSources(document).Scene));
    }
}
