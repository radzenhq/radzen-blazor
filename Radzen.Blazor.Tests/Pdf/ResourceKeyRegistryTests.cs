#nullable enable
using Radzen.Documents.LaidOut;
using Radzen.Documents.Layout;
using Radzen.Documents.Pdf.Content;
using Radzen.Documents.Pdf.Emission;
using Radzen.Documents.Pdf.Render;
using Radzen.Documents.Pdf;
using Radzen.Documents;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

public class ResourceKeyRegistryTests
{
    private static readonly LayoutCaptureContext Capture = new();

    private static PagePlan Plan() => new() { Size = PageSizes.A4 };

    private static GradientPaint Paint(GradientBrush brush)
        => GeometryCapture.Gradient(
            brush,
            GradientReference.Box(100, 100),
            Capture)!.Value;

    private static EmissionSoftMask Mask() => new(
        EmissionSoftMaskType.Luminosity,
        new EmissionTransparencyGroup([], [0, 0, 1, 1], null, null, null, []),
        null);

    [Fact]
    public void PlainAndSoftMaskStates_ShareOneGsCounter()
    {
        var plan = Plan();

        Assert.Equal("GS0", plan.RegisterExtGState(0.5, 0.5));
        Assert.Equal("GS1", plan.RegisterSoftMaskExtGState(1, 1, Mask(), "a"));
        Assert.Equal("GS2", plan.RegisterExtGState(0.25, 0.25));

        Assert.Equal("GS1", plan.RegisterSoftMaskExtGState(1, 1, Mask(), "a"));
        Assert.Equal("GS0", plan.RegisterExtGState(0.5, 0.5));

        Assert.Equal("GS3", plan.RegisterSoftMaskExtGState(1, 1, Mask(), "b"));
        Assert.Equal(4, plan.ExtGStates.Count);
    }

    [Fact]
    public void SoftMaskWithoutContentKey_AppendsAFreshStatePerCall()
    {
        var plan = Plan();

        Assert.Equal("GS0", plan.RegisterSoftMaskExtGState(1, 1, Mask(), null));
        Assert.Equal("GS1", plan.RegisterSoftMaskExtGState(1, 1, Mask(), null));
        Assert.Equal(2, plan.ExtGStates.Count);
    }

    [Fact]
    public void PlainState_NeverReusesASoftMaskStateWithEqualAlpha()
    {
        var plan = Plan();
        var mask = plan.RegisterSoftMaskExtGState(0.5, 0.5, Mask(), "a");

        Assert.NotEqual(mask, plan.RegisterExtGState(0.5, 0.5));
    }

    [Fact]
    public void EqualGradientInstances_StayDistinctPatterns()
    {
        var plan = Plan();
        var first = Paint(new LinearGradient(0, 0, 1, 1, new GradientStop(0, Color.Red), new GradientStop(1, Color.Blue)));
        var second = Paint(new LinearGradient(0, 0, 1, 1, new GradientStop(0, Color.Red), new GradientStop(1, Color.Blue)));

        Assert.Equal("P0", plan.RegisterPattern(first, Matrix.Identity));
        Assert.Equal("P0", plan.RegisterPattern(first, Matrix.Identity));
        Assert.Equal("P1", plan.RegisterPattern(second, Matrix.Identity));
    }

    [Fact]
    public void NegativeZeroOpacity_SharesTheZeroEntry()
    {
        using var writer = new ContentWriter();

        Assert.Equal(writer.RegisterOpacity(0.0), writer.RegisterOpacity(-0.0));
    }

    [Fact]
    public void NanOpacity_NeverDedupsAgainstItself()
    {
        using var writer = new ContentWriter();

        Assert.NotEqual(writer.RegisterOpacity(double.NaN), writer.RegisterOpacity(double.NaN));
    }
}
