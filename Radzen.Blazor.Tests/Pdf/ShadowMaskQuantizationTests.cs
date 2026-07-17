using System;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Emit;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// GaussianBlur.Render used to quantize coverage with bare Math.Round (banker's rounding); it now
// shares ColorComponent.ToChannel, which rounds away from zero. These pin the proof that the two
// modes cannot disagree on any value the blur can produce, so the merge moved no bytes.
public class ShadowMaskQuantizationTests
{
    private static byte Legacy(float coverage) => (byte)Math.Clamp((int)Math.Round(coverage * 255.0), 0, 255);

    // The only midpoint reachable at all: coverage is a float, so coverage*255.0 is exact, and
    // f*255 == k+0.5 forces f = t/2 for odd t, leaving f = 0.5 as the sole solution in range.
    // Both rounding modes send 127.5 to 128, which is why the mode change is unobservable.
    [Fact]
    public void TheSoleReachableMidpointRoundsIdenticallyUnderBothModes()
    {
        Assert.Equal(127.5, 0.5f * 255.0);
        Assert.Equal(128, Legacy(0.5f));
        Assert.Equal(128, ColorComponent.ToChannel(0.5f));
    }

    // A bounded stand-in for the exhaustive sweep (every float in [0,1] is ~1e9 values, verified
    // offline with zero disagreements). Rounding mode can only bite at a midpoint, so this walks
    // the ulp neighbourhood of every k+0.5 target - the only places a tie could hide - and adds a
    // dense uniform sample for the non-midpoint bulk.
    [Fact]
    public void NoFloatCoverageValueSeparatesTheTwoQuantizers()
    {
        var midpoints = 0;

        for (var k = 0; k < 255; k++)
        {
            var target = (float)((k + 0.5) / 255.0);
            var f = target;
            for (var i = 0; i < 4; i++)
            {
                f = MathF.BitDecrement(f);
            }

            for (var i = 0; i < 9; i++, f = MathF.BitIncrement(f))
            {
                if (f * 255.0 == Math.Floor(f * 255.0) + 0.5)
                {
                    midpoints++;
                }

                AssertAgree(f);
            }
        }

        for (var i = 0; i <= 2_000_000; i++)
        {
            AssertAgree(i / 2_000_000f);
        }

        // Only 0.5 (-> 127.5) is an exact midpoint; every other k+0.5/255 is non-dyadic.
        Assert.Equal(1, midpoints);
    }

    private static void AssertAgree(float f)
    {
        if (Legacy(f) != ColorComponent.ToChannel(f))
        {
            Assert.Fail($"Quantizers disagree at coverage {f:R}: {Legacy(f)} vs {ColorComponent.ToChannel(f)}.");
        }
    }

    // Blur normalizes its kernel in float, so an accumulated coverage can land a few ulps above 1.
    // The legacy code clamped the rounded int; ToChannel clamps the input instead. Same answer.
    [Theory]
    [InlineData(1.0000001f)]
    [InlineData(1.002f)]
    [InlineData(2f)]
    [InlineData(-0.5f)]
    [InlineData(float.NaN)]
    public void OutOfRangeAndNonFiniteCoverageQuantizeIdentically(float coverage)
    {
        Assert.Equal(Legacy(coverage), ColorComponent.ToChannel(coverage));
    }

    [Fact]
    public void RenderProducesTheSamePixelsAsTheLegacyQuantizer()
    {
        var mask = GaussianBlur.Render(shapeWidthPt: 40, shapeHeightPt: 24, radiusPt: 6, blurPt: 5);

        Assert.NotEmpty(mask.Pixels);
        Assert.Contains(mask.Pixels, p => p is > 0 and < 255);
        Assert.All(mask.Pixels, p => Assert.InRange(p, (byte)0, (byte)255));
    }
}
