#nullable enable
using System.Linq;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Emit;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// /HorizontalScale (Tz) scales the drawn glyph advances, so line layout must measure runs at
// that scale or wrap and align a line at a width its glyphs never occupy.
public class HorizontalScaleMeasurementTests
{
    private static Paragraph ScaledRun(string text, double horizontalScale)
    {
        var paragraph = new Paragraph();
        var run = paragraph.Inlines.Add(text);
        run.Font.Name = LineLayoutSupport.Family;
        run.Font.Size = 12;
        run.HorizontalScale = horizontalScale;
        return paragraph;
    }

    [Fact]
    public void DoubledScale_DoublesMeasuredAdvance()
    {
        var fonts = LineLayoutSupport.Fonts();
        var plain = LineLayoutSupport.WordWidth(fonts, "Hello", 12);

        var fragment = LineBreaker.Break(ScaledRun("Hello", 200), 1000, fonts)[0].Fragments.Single();

        Assert.Equal(plain * 2.0, fragment.Advance, 6);
    }

    [Fact]
    public void DefaultScale_LeavesMeasuredAdvanceUnchanged()
    {
        var fonts = LineLayoutSupport.Fonts();
        var plain = LineLayoutSupport.WordWidth(fonts, "Hello", 12);

        var fragment = LineBreaker.Break(ScaledRun("Hello", 100), 1000, fonts)[0].Fragments.Single();

        Assert.Equal(plain, fragment.Advance, 6);
    }

    // A width that holds both words at 100% overflows once each word is drawn twice as wide,
    // so the scaled paragraph wraps where the unscaled one does not.
    [Fact]
    public void DoubledScale_WrapsWhereUnscaledFits()
    {
        var fonts = LineLayoutSupport.Fonts();
        var wHello = LineLayoutSupport.WordWidth(fonts, "Hello", 12);
        var wWorld = LineLayoutSupport.WordWidth(fonts, "World", 12);
        var space = LineLayoutSupport.SpaceWidth(fonts, 12);
        var max = wHello + space + wWorld + 1.0;

        var unscaled = LineBreaker.Break(ScaledRun("Hello World", 100), max, fonts);
        var scaled = LineBreaker.Break(ScaledRun("Hello World", 200), max, fonts);

        Assert.Single(unscaled);
        Assert.Equal(2, scaled.Count);
    }
}
