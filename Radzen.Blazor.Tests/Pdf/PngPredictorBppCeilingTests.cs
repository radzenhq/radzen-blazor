#nullable enable
using Radzen.Documents.Pdf.Objects.Filters;
using Xunit;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

// ISO 32000-1 7.4.4.4: bytes-per-pixel is ceil(colors * bitsPerComponent / 8).
public class PngPredictorBppCeilingTests
{
    [Fact]
    public void Png_Sub_WidePixel_UsesCeilingBpp()
    {
        var input = new byte[] { 1, 10, 20, 20 };
        var expected = new byte[] { 10, 20, 30 };
        Assert.Equal(expected, PngPredictor.Decode(input, colors: 3, bitsPerComponent: 4, columns: 2));
    }

    [Fact]
    public void Png_Sub_ByteAligned_Unchanged()
    {
        var input = new byte[] { 1, 10, 10, 10, 5, 5, 5 };
        var expected = new byte[] { 10, 10, 10, 15, 15, 15 };
        Assert.Equal(expected, PngPredictor.Decode(input, colors: 3, bitsPerComponent: 8, columns: 2));
    }

    [Fact]
    public void Png_Paeth_WidePixel()
    {
        var input = new byte[] { 4, 0x28, 0x5A, 0xE4, 0x6E, 0xFB, 0xBA };
        var expected = new byte[] { 40, 90, 12, 200, 7, 130 };
        Assert.Equal(expected, PngPredictor.Decode(input, colors: 3, bitsPerComponent: 4, columns: 4));
    }
}
