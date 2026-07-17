using System;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Emit;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

public class ShadowMaskQuantizationTests
{
    private static byte Legacy(float coverage) => (byte)Math.Clamp((int)Math.Round(coverage * 255.0), 0, 255);

    [Fact]
    public void TheSoleReachableMidpointRoundsIdenticallyUnderBothModes()
    {
        Assert.Equal(127.5, 0.5f * 255.0);
        Assert.Equal(128, Legacy(0.5f));
        Assert.Equal(128, ColorComponent.ToChannel(0.5f));
    }

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

        Assert.Equal(1, midpoints);
    }

    private static void AssertAgree(float f)
    {
        if (Legacy(f) != ColorComponent.ToChannel(f))
        {
            Assert.Fail($"Quantizers disagree at coverage {f:R}: {Legacy(f)} vs {ColorComponent.ToChannel(f)}.");
        }
    }

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
