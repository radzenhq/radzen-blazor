#nullable enable
using System;
using System.Collections.Generic;
using Radzen.Documents;
using Radzen.Documents.Pdf.Objects;
using Xunit;
using Radzen.Documents.Core;

namespace Radzen.Blazor.Pdf.Tests;

public class LinkAnnotationGeometryTests
{
    private const string Url = "https://www.radzen.com/";

    private static List<(double X1, double Y1, double X2, double Y2)> LinkRects(DocumentReader reader, int pageIndex)
    {
        var page = ContentTestHelpers.Kid(reader, pageIndex);
        var rects = new List<(double, double, double, double)>();
        if (!page.TryGetValue("Annots", out var annotsObject) || reader.Resolve(annotsObject!) is not ArrayObject annots)
        {
            return rects;
        }

        for (var i = 0; i < annots.Count; i++)
        {
            if (reader.Resolve(annots[i]) is not DictionaryObject annot
                || !annot.TryGetValue("Subtype", out var subtype)
                || reader.Resolve(subtype!) is not NameObject { Value: "Link" }
                || !annot.TryGetValue("Rect", out var rectObject)
                || reader.Resolve(rectObject!) is not ArrayObject rect)
            {
                continue;
            }

            var n = new double[4];
            for (var j = 0; j < 4; j++)
            {
                n[j] = Assert.IsType<NumberObject>(reader.Resolve(rect[j])).DoubleValue;
            }

            rects.Add((Math.Min(n[0], n[2]), Math.Min(n[1], n[3]), Math.Max(n[0], n[2]), Math.Max(n[1], n[3])));
        }

        return rects;
    }

    private static Document LinkInContainer(double rotation)
    {
        var document = new Document();
        var section = document.Sections.Add();
        section.PageSize = new PageSize(Unit.FromPoint(400), Unit.FromPoint(300));
        section.Margins.SetAll(Unit.FromPoint(40));
        var container = section.Blocks.Add(new Container { Padding = Unit.FromPoint(5), Rotation = rotation });
        var paragraph = container.Blocks.Add(new Paragraph());
        paragraph.Inlines.Add("Radzen").Link = Url;
        return document;
    }

    [Fact]
    public void LinkRect_MatchesTheFaceAscentAndDescent()
    {
        const double size = 24;
        var document = new Document();
        BuildTestSupport.RegisterLatin(document);
        var section = document.Sections.Add();
        var paragraph = section.Blocks.Add(new Paragraph());
        var run = paragraph.Inlines.Add("Radzen");
        run.Link = Url;
        run.Font.Family = BuildTestSupport.Latin;
        run.Font.Size = Unit.FromPoint(size);

        var rect = Assert.Single(LinkRects(BuildTestSupport.Read(document), 0));

        var face = Radzen.Documents.Fonts.Sfnt.SfntFont.Parse(
            PdfTestResources.ReadAllBytes("Fonts/LiberationSans-Regular.ttf"));
        var expected = (face.Ascent - face.Descent) * size / face.UnitsPerEm;
        Assert.Equal(expected, rect.Y2 - rect.Y1, 3);
    }

    [Fact]
    public void LinkInsideAContainerInATableCell_CoversTheDrawnText()
    {
        var document = new Document();
        BuildTestSupport.RegisterLatin(document);
        var section = document.Sections.Add();
        section.PageSize = new PageSize(Unit.FromPoint(400), Unit.FromPoint(300));
        section.Margins.SetAll(Unit.FromPoint(40));

        var table = section.Blocks.Add(new Table());
        table.Columns.Add(Unit.FromPoint(120));
        table.Columns.Add(Unit.FromPoint(120));
        var row = table.Rows.Add();
        row.Cells[0].Blocks.Add(new Paragraph("First"));
        var container = row.Cells[1].Blocks.Add(new Container { Padding = Unit.FromPoint(7) });
        var paragraph = container.Blocks.Add(new Paragraph());
        var run = paragraph.Inlines.Add("Radzen");
        run.Link = Url;
        run.Font.Family = BuildTestSupport.Latin;
        run.Font.Size = Unit.FromPoint(14);

        var rect = Assert.Single(LinkRects(BuildTestSupport.Read(document), 0));
        var hit = Assert.Single(BuildTestSupport.Reload(document).Pages[0].FindText("Radzen"));

        Assert.False(hit.GeometryEstimated);
        Assert.True(
            Math.Abs(hit.Bounds.Left - rect.X1) < 0.05,
            $"the link rect {rect} must start where the drawn text {hit.Bounds} starts");
        Assert.True(
            Math.Abs(hit.Bounds.Right - rect.X2) < 0.05,
            $"the link rect {rect} must end where the drawn text {hit.Bounds} ends");
        var overlap = Math.Min(rect.Y2, hit.Bounds.Top) - Math.Max(rect.Y1, hit.Bounds.Bottom);
        Assert.True(
            overlap > 0.85 * (hit.Bounds.Top - hit.Bounds.Bottom),
            $"the link rect {rect} must sit on the drawn text at "
                + $"{hit.Bounds.Left},{hit.Bounds.Bottom},{hit.Bounds.Right},{hit.Bounds.Top}");
    }

    [Fact]
    public void LinkInsideRotatedContainer_AnnotationRectIsTransformed()
    {
        var upright = Assert.Single(LinkRects(BuildTestSupport.Read(LinkInContainer(0)), 0));
        var rotated = Assert.Single(LinkRects(BuildTestSupport.Read(LinkInContainer(90)), 0));

        Assert.True(upright.X2 - upright.X1 > upright.Y2 - upright.Y1, "an upright link rect is wider than it is tall");
        Assert.True(
            rotated.Y2 - rotated.Y1 > rotated.X2 - rotated.X1,
            $"a link inside a 90 degree container must carry a rotated rect, got {rotated}");
        Assert.Equal(upright.X2 - upright.X1, rotated.Y2 - rotated.Y1, 3);
        Assert.Equal(upright.Y2 - upright.Y1, rotated.X2 - rotated.X1, 3);
    }
}
