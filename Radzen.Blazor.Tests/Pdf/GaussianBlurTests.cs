#nullable enable

using Radzen.Documents.Pdf;
using Xunit;

using Radzen.Documents.Pdf.Emit;
namespace Radzen.Blazor.Pdf.Tests;

public class GaussianBlurTests
{
    private static byte At(ShadowMask mask, int x, int y) => mask.Pixels[(y * mask.Width) + x];

    [Fact]
    public void SharpShape_NoBlur_IsOpaqueInsideAndTransparentOutside()
    {
        var mask = GaussianBlur.Render(40, 24, 10, 0);
        Assert.Equal(mask.Width * mask.Height, mask.Pixels.Length);

        Assert.True(At(mask, mask.Width / 2, mask.Height / 2) > 250);
        Assert.Equal(0, At(mask, 0, 0));
    }

    [Fact]
    public void Blur_SpreadsCoverageBeyondTheSharpEdge()
    {
        var sharp = GaussianBlur.Render(40, 24, 0, 0);
        var blurred = GaussianBlur.Render(40, 24, 0, 10);

        Assert.True(blurred.Width > sharp.Width);
        Assert.True(blurred.MarginPoints > 0);

        var soft = false;
        foreach (var p in blurred.Pixels)
        {
            if (p > 0 && p < 255)
            {
                soft = true;
                break;
            }
        }

        Assert.True(soft, "a blurred coverage buffer must contain partial-coverage pixels");
    }

    [Fact]
    public void Coverage_IsSymmetric()
    {
        var mask = GaussianBlur.Render(30, 30, 8, 6);
        for (var y = 0; y < mask.Height; y++)
        {
            for (var x = 0; x < mask.Width; x++)
            {
                Assert.Equal(At(mask, x, y), At(mask, mask.Width - 1 - x, y));
                Assert.Equal(At(mask, x, y), At(mask, x, mask.Height - 1 - y));
            }
        }
    }
}
