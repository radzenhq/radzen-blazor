#nullable enable
using System.Linq;
using Xunit;
using Radzen.Documents;
using Radzen.Documents.Layout;

namespace Radzen.Blazor.Documents.Tests;

using Radzen.Blazor.Pdf.Tests;

public class HorizontalScaleMeasurementTests
{
    private static Paragraph ScaledRun(string text, double horizontalScale)
    {
        var paragraph = new Paragraph();
        var run = paragraph.Inlines.Add(text);
        run.Font.Family = LineLayoutSupport.Family;
        run.Font.Size = 12;
        run.HorizontalScale = horizontalScale;
        return paragraph;
    }

    [Fact]
    public void DoubledScale_DoublesMeasuredAdvance()
    {
        var fonts = LineLayoutSupport.Fonts();
        var plain = LineLayoutSupport.WordWidth(fonts, "Hello", 12);

        var fragment = LineBreaker.Break(ScaledRun("Hello", 2.0), 1000, fonts)[0].Fragments.Single();

        Assert.Equal(plain * 2.0, fragment.Advance, 6);
    }

    [Fact]
    public void DefaultScale_LeavesMeasuredAdvanceUnchanged()
    {
        var fonts = LineLayoutSupport.Fonts();
        var plain = LineLayoutSupport.WordWidth(fonts, "Hello", 12);

        var fragment = LineBreaker.Break(ScaledRun("Hello", 1.0), 1000, fonts)[0].Fragments.Single();

        Assert.Equal(plain, fragment.Advance, 6);
    }

    [Fact]
    public void DoubledScale_WrapsWhereUnscaledFits()
    {
        var fonts = LineLayoutSupport.Fonts();
        var wHello = LineLayoutSupport.WordWidth(fonts, "Hello", 12);
        var wWorld = LineLayoutSupport.WordWidth(fonts, "World", 12);
        var space = LineLayoutSupport.SpaceWidth(fonts, 12);
        var max = wHello + space + wWorld + 1.0;

        var unscaled = LineBreaker.Break(ScaledRun("Hello World", 1.0), max, fonts);
        var scaled = LineBreaker.Break(ScaledRun("Hello World", 2.0), max, fonts);

        Assert.Single(unscaled);
        Assert.Equal(2, scaled.Count);
    }
}
