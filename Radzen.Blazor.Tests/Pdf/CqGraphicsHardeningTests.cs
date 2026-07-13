#nullable enable

using System;
using System.Text;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;

using Radzen.Documents.Pdf.Content;
using Radzen.Documents.Pdf.Emit;
namespace Radzen.Blazor.Pdf.Tests;

// Fail-loud and bounded-work hardening for the graphics lane: names, blur kernels,
// gradient stops, watermark encoding, pattern dedup and rotated rounded geometry.
public class CqGraphicsHardeningTests
{
    private static double Num(DocumentObject o) => Assert.IsType<NumberObject>(o).DoubleValue;

    private static ArrayObject Arr(DocumentObject o) => Assert.IsType<ArrayObject>(o);

    // ---- #8: names above Latin-1 cannot be masked to a different resource ----

    [Fact]
    public void XObjectName_AboveLatin1_ThrowsOnSave()
    {
        var document = new Document();
        var page = document.Pages.Add();
        page.Content.Add(new XObjectContent("Ł1")); // U+0141 & 0xFF == 'A'

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

    // ---- #14: an extreme blur must not blow up the raster ----

    [Fact]
    public void HugeBlur_ProducesBoundedRaster()
    {
        var mask = GaussianBlur.Render(250, 250, 0, 200);

        // Before the kernel cap this was ~1712x1712 (~7e9 MACs); the cap keeps it small.
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

    // ---- #27: gradient stop offsets are validated and hard stops stay valid ----

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

    // ---- #34: base-14 watermark rejects non-WinAnsi text instead of blanking it ----

    [Fact]
    public void Watermark_NonWinAnsiText_ThrowsInsteadOfDropping()
    {
        var builder = new DocumentBuilder();
        var section = builder.Sections.Add();
        section.Watermark = new Watermark { Text = "机密" }; // "机密"
        var paragraph = new Paragraph();
        paragraph.Inlines.Add("Body");
        section.Blocks.Add(paragraph);

        Assert.Throws<NotSupportedException>(() => builder.ToArray());
    }

    // ---- #44: one gradient brush reused on the content writer emits one pattern ----

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

    // ---- #81: a rotated box cannot silently square its rounded corners ----

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
