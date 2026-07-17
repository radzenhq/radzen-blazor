#nullable enable

using System;
using System.Text;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;

using Radzen.Documents.Pdf.Content;
using Radzen.Documents.Pdf.Emit;
namespace Radzen.Blazor.Pdf.Tests;

public class CqGraphicsHardeningTests
{
    private static double Num(DocumentObject o) => Assert.IsType<NumberObject>(o).DoubleValue;

    private static ArrayObject Arr(DocumentObject o) => Assert.IsType<ArrayObject>(o);


    [Fact]
    public void XObjectName_AboveLatin1_ThrowsOnSave()
    {
        var document = new Document();
        var page = document.Pages.Add();
        page.Content.Add(new XObjectContent("Ł1"));

        Assert.Throws<NotSupportedException>(() => document.ToArray());
    }

    [Fact]
    public void XObjectName_HighLatin1_StillEncodesAsEscape()
    {
        var document = new Document();
        var page = document.Pages.Add();
        page.Content.Add(new XObjectContent("©x"));

        var bytes = document.ToArray();
        Assert.Contains("/#A9x Do", Encoding.Latin1.GetString(bytes), StringComparison.Ordinal);
    }


    [Fact]
    public void HugeBlur_ProducesBoundedRaster()
    {
        var mask = GaussianBlur.Render(250, 250, 0, 200);

        Assert.True(mask.Width < 400, $"width {mask.Width} should be bounded");
        Assert.True(mask.Height < 400, $"height {mask.Height} should be bounded");

        var soft = false;
        foreach (var p in mask.Pixels)
        {
            if (p is > 0 and < 255)
            {
                soft = true;
                break;
            }
        }

        Assert.True(soft, "a blurred shape still has a soft edge");
    }


    [Fact]
    public void GradientStop_OffsetOutOfRange_Throws()
    {
        Assert.Throws<ArgumentException>(() => new LinearGradient(0, 0, 1, 0,
            new GradientStop(0, Color.Red),
            new GradientStop(1.5, Color.Blue)));
    }

    [Fact]
    public void GradientStop_DecreasingOffsets_Throw()
    {
        Assert.Throws<ArgumentException>(() => new LinearGradient(0, 0, 1, 0,
            new GradientStop(0.7, Color.Red),
            new GradientStop(0.3, Color.Blue)));
    }

    [Fact]
    public void GradientHardStop_EmitsStrictlyIncreasingBounds()
    {
        var brush = new LinearGradient(0, 0, 100, 0,
            new GradientStop(0, Color.Red),
            new GradientStop(0.5, Color.Red),
            new GradientStop(0.5, Color.Blue),
            new GradientStop(1, Color.Blue));

        var func = Assert.IsType<DictionaryObject>(ShadingBuilder.BuildShading(brush)["Function"]!);
        var bounds = Arr(func["Bounds"]!);

        for (var i = 1; i < bounds.Count; i++)
        {
            Assert.True(Num(bounds[i]) > Num(bounds[i - 1]),
                $"bound {i} ({Num(bounds[i])}) must exceed bound {i - 1} ({Num(bounds[i - 1])})");
        }
    }


    [Fact]
    public void Watermark_NonWinAnsiText_ThrowsInsteadOfDropping()
    {
        var builder = new DocumentBuilder();
        var section = builder.Sections.Add();
        section.Watermark = new Watermark { Text = "机密" };
        var paragraph = new Paragraph();
        paragraph.Inlines.Add("Body");
        section.Blocks.Add(paragraph);

        Assert.Throws<NotSupportedException>(() => builder.ToArray());
    }


    [Fact]
    public void ContentWriter_SameBrush_RegistersOnePattern()
    {
        using var writer = new ContentWriter();
        var brush = new LinearGradient(0, 0, 10, 0,
            new GradientStop(0, Color.Red),
            new GradientStop(1, Color.Blue));

        var first = writer.RegisterPattern(brush);
        var second = writer.RegisterPattern(brush);

        Assert.Equal(first, second);
        Assert.Single(writer.Patterns);
    }

    [Fact]
    public void ContentWriter_DistinctBrushes_RegisterSeparatePatterns()
    {
        using var writer = new ContentWriter();
        var a = new LinearGradient(0, 0, 10, 0, new GradientStop(0, Color.Red), new GradientStop(1, Color.Blue));
        var b = new LinearGradient(0, 0, 10, 0, new GradientStop(0, Color.Red), new GradientStop(1, Color.Blue));

        Assert.NotEqual(writer.RegisterPattern(a), writer.RegisterPattern(b));
        Assert.Equal(2, writer.Patterns.Count);
    }


    [Fact]
    public void ApplyTransform_RotatedRoundedFill_Throws()
    {
        var plan = new PagePlan { Size = PageSizes.A4 };
        var mark = plan.Mark();
        plan.Fills.Add(new FillDraw
        {
            X = 10,
            Y = 20,
            Width = 100,
            Height = 40,
            Color = Color.FromRgb(200, 200, 200),
            Radius = 8,
        });

        Assert.Throws<NotSupportedException>(() => plan.ApplyTransform(Matrix.Rotate(30), mark));
    }

    [Fact]
    public void ApplyTransform_RotatedRoundedBorder_Throws()
    {
        var plan = new PagePlan { Size = PageSizes.A4 };
        var mark = plan.Mark();
        plan.RoundedStrokes.Add(new RoundedStrokeDraw
        {
            X = 10,
            Y = 20,
            Width = 100,
            Height = 40,
            Radius = 6,
            LineWidth = 1,
            Color = Color.Black,
            Style = BorderStyle.Solid,
        });

        Assert.Throws<NotSupportedException>(() => plan.ApplyTransform(Matrix.Rotate(30), mark));
    }

    [Fact]
    public void ApplyTransform_RotatedEdgeWithRoundedClip_ThrowsNamingTheEdge()
    {
        var plan = new PagePlan { Size = PageSizes.A4 };
        var mark = plan.Mark();
        plan.Edges.Add(Edge());
        plan.ApplyRoundedClip(new PdfRect(10, 20, 100, 40), 6, mark);

        var error = Assert.Throws<NotSupportedException>(() => plan.ApplyTransform(Matrix.Rotate(30), mark));
        Assert.Contains("edge", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ApplyTransform_RotatedEdgeWithoutClip_KeepsRotatingEndpoints()
    {
        var plan = new PagePlan { Size = PageSizes.A4 };
        var mark = plan.Mark();
        plan.Edges.Add(Edge());

        plan.ApplyTransform(Matrix.Rotate(90), mark);

        var edge = Assert.Single(plan.Edges);
        Assert.Equal(-20, edge.X1, 6);
        Assert.Equal(10, edge.Y1, 6);
        Assert.Null(edge.Clip);
    }

    [Fact]
    public void ApplyTransform_RotatedEdgeWithSquareClip_DoesNotThrow()
    {
        var plan = new PagePlan { Size = PageSizes.A4 };
        var mark = plan.Mark();
        plan.Edges.Add(Edge());
        plan.ApplyRoundedClip(new PdfRect(10, 20, 100, 40), 0, mark);

        plan.ApplyTransform(Matrix.Rotate(30), mark);
        Assert.Single(plan.Edges);
    }

    [Fact]
    public void RotatedContainer_RoundedTableWithBorderEdgesAndNoFills_Throws()
    {
        var builder = new DocumentBuilder();
        BuildTestSupport.RegisterLatin(builder);
        var section = builder.Sections.Add();
        var container = section.Blocks.Add(new Container { Rotation = 30 });
        var table = container.Blocks.AddTable();
        table.Columns.Add(Unit.FromPoint(150));
        table.Borders.Top.Width = 1;
        table.CornerRadius = Unit.FromPoint(8);
        var run = table.Rows.Add().Cells[0].Blocks.AddParagraph().Inlines.Add("NoFill");
        run.Font.Name = BuildTestSupport.Latin;

        Assert.Throws<NotSupportedException>(() => builder.Build());
    }

    private static EdgeDraw Edge() => new()
    {
        X1 = 10,
        Y1 = 20,
        X2 = 110,
        Y2 = 20,
        LineWidth = 2,
        Color = Color.Black,
        Style = BorderStyle.Solid,
    };

    [Fact]
    public void ApplyTransform_RotatedSquareFill_DoesNotThrow()
    {
        var plan = new PagePlan { Size = PageSizes.A4 };
        var mark = plan.Mark();
        plan.Fills.Add(new FillDraw
        {
            X = 10,
            Y = 20,
            Width = 100,
            Height = 40,
            Color = Color.FromRgb(200, 200, 200),
        });

        plan.ApplyTransform(Matrix.Rotate(30), mark);
        Assert.Single(plan.Edges);
    }
}
