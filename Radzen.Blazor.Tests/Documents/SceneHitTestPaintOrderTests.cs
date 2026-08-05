#nullable enable
using System.Collections.Immutable;
using Radzen.Blazor.Pdf.Tests;
using Radzen.Documents.Core;
using Radzen.Documents.LaidOut;
using Radzen.Documents.Layout;
using Radzen.Documents;
using Xunit;

namespace Radzen.Blazor.Documents.Tests;

public class SceneHitTestPaintOrderTests
{
    private static Section Page(Document document)
    {
        BuildTestSupport.RegisterLatin(document);
        document.Styles.Normal.Font.Family = BuildTestSupport.Latin;
        var section = document.Sections.Add();
        section.PageSize = new PageSize(Unit.FromPoint(400), Unit.FromPoint(300));
        section.Margins.SetAll(Unit.FromPoint(20));
        return section;
    }

    private static ImmutableArray<SceneGeometry> GeometryOf(LaidOutLayout laidOut, object authored)
    {
        var id = laidOut.Sources.Of(authored);
        Assert.NotNull(id);
        return SceneHitTest.Geometry(laidOut.Scene, id.Value);
    }

    private static Rect BoundsOf(LaidOutLayout laidOut, object authored)
    {
        var geometry = GeometryOf(laidOut, authored);
        Assert.NotEmpty(geometry);
        return geometry[0].Bounds;
    }

    [Fact]
    public void TwoOverlappingOverlayBoxes_ResolveToTheLaterPaintedOne()
    {
        var document = new Document();
        var section = Page(document);
        var overlay = section.Blocks.Add(new Container
        {
            Layout = ContainerLayout.Overlay,
            Width = Unit.FromPoint(240),
        });

        var under = new Container { Width = Unit.FromPoint(160), Padding = Unit.FromPoint(24) };
        under.Blocks.Add(new Paragraph()).Inlines.Add("under");
        var over = new Container { Width = Unit.FromPoint(160), Padding = Unit.FromPoint(24) };
        over.Blocks.Add(new Paragraph()).Inlines.Add("over");
        overlay.Blocks.Add(under);
        overlay.Blocks.Add(over);

        var laidOut = DocumentLayouter.LayoutWithSources(document);
        var page = Assert.Single(laidOut.Scene.Pages);
        var underBounds = BoundsOf(laidOut, under);
        var overBounds = BoundsOf(laidOut, over);

        Assert.Equal(underBounds.X, overBounds.X, 6);
        Assert.Equal(underBounds.Y, overBounds.Y, 6);

        var hit = SceneHitTest.At(page, overBounds.X + 2, overBounds.Y + overBounds.Height - 2);

        Assert.Equal(laidOut.Sources.Of(over), hit);
    }

    [Fact]
    public void TextStackedOverAnImage_ResolvesToTheText()
    {
        var document = new Document();
        var section = Page(document);
        var overlay = section.Blocks.Add(new Container
        {
            Layout = ContainerLayout.Overlay,
            Width = Unit.FromPoint(240),
        });

        var image = overlay.Blocks.Add(new Image(PdfTestResources.Open("Images/rgb.jpg")));
        image.Width = Unit.FromPoint(160);
        image.Height = Unit.FromPoint(80);
        var text = overlay.Blocks.Add(new Paragraph()).Inlines.Add("over the image");

        var laidOut = DocumentLayouter.LayoutWithSources(document);
        var page = Assert.Single(laidOut.Scene.Pages);
        var imageBounds = BoundsOf(laidOut, image);
        var textBounds = BoundsOf(laidOut, text);

        Assert.True(textBounds.X < imageBounds.X + imageBounds.Width);
        Assert.True(textBounds.Y < imageBounds.Y + imageBounds.Height);

        var hit = SceneHitTest.At(
            page,
            textBounds.X + (textBounds.Width / 2),
            textBounds.Y + (textBounds.Height / 2));

        Assert.Equal(laidOut.Sources.Of(text), hit);
    }

    [Fact]
    public void RotatedOverlayBox_IsHitThroughItsTransform()
    {
        var document = new Document();
        var section = Page(document);
        var rotated = section.Blocks.Add(new Container
        {
            Rotation = 90,
            Width = Unit.FromPoint(160),
            Padding = Unit.FromPoint(8),
        });
        var text = rotated.Blocks.Add(new Paragraph()).Inlines.Add("rotated run");

        var laidOut = DocumentLayouter.LayoutWithSources(document);
        var page = Assert.Single(laidOut.Scene.Pages);
        var bounds = BoundsOf(laidOut, text);

        Assert.True(bounds.Height > bounds.Width);

        var hit = SceneHitTest.At(
            page,
            bounds.X + (bounds.Width / 2),
            bounds.Y + (bounds.Height / 2));

        Assert.Equal(laidOut.Sources.Of(text), hit);
    }

    [Fact]
    public void ContentOfANestedBox_StaysInsideTheClippingAncestor()
    {
        var document = new Document();
        var section = Page(document);
        var outer = section.Blocks.Add(new Container { Width = Unit.FromPoint(40) });
        outer.Blocks.Add(new Paragraph()).Inlines.Add("WW").Font.Size = 60;
        var inner = new Container { Width = Unit.FromPoint(200) };
        var text = inner.Blocks.Add(new Paragraph()).Inlines.Add("inner text that is far wider than forty points");
        outer.Blocks.Add(inner);

        var laidOut = DocumentLayouter.LayoutWithSources(document);
        var page = Assert.Single(laidOut.Scene.Pages);
        var outerBounds = BoundsOf(laidOut, outer);
        var textBounds = BoundsOf(laidOut, text);

        foreach (var item in GeometryOf(laidOut, text))
        {
            Assert.True(
                item.Bounds.X + item.Bounds.Width <= outerBounds.X + outerBounds.Width + 0.01,
                "the ancestor clip must clamp the nested run");
        }

        Assert.Null(SceneHitTest.At(page, outerBounds.X + outerBounds.Width + 20, textBounds.Y + 1));
    }
}
