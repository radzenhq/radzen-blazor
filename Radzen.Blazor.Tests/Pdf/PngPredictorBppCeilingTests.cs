#nullable enable
using Radzen.Documents.Pdf.Objects.Filters;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// bytes-per-pixel is ceil(colors * bitsPerComponent / 8) (ISO 32000-1 7.4.4.4). A wide
// sub-byte pixel (RGB at 4 bpc = 12 bits) is two bytes wide; floor picks one and reads
// the wrong left neighbour for the Sub/Up/Paeth filters.
public class PngPredictorBppCeilingTests
{
    // colors=3, bpc=4, columns=2 -> 12 bits/pixel -> bpp = ceil(12/8) = 2, rowLength = 3.
    // Sub-encoded [10,20,20] with bpp=2 reconstructs to [10,20,30]:
    //   out[0]=10, out[1]=20, out[2]=20+out[0]=30.
    // With the floor bug (bpp=1) it wrongly yields [10,30,50].
    [Fact]
    public void Png_Sub_WidePixel_UsesCeilingBpp()
    {
        var input = new byte[] { 1, 10, 20, 20 };
        var expected = new byte[] { 10, 20, 30 };
        Assert.Equal(expected, PngPredictor.Decode(input, colors: 3, bitsPerComponent: 4, columns: 2));
    }

    // Byte-aligned pixels (bpc=8) are unaffected: colors*bpc is a multiple of 8 so floor
    // and ceiling agree. RGB Sub row deltas [10,10,10 | 5,5,5] -> [10,20,30 | 15,25,35].
    [Fact]
    public void Png_Sub_ByteAligned_Unchanged()
    {
        var input = new byte[] { 1, 10, 10, 10, 5, 5, 5 };
        var expected = new byte[] { 10, 10, 10, 15, 15, 15 };
        Assert.Equal(expected, PngPredictor.Decode(input, colors: 3, bitsPerComponent: 8, columns: 2));
    }

    // Encode/Decode round trip over a wide sub-byte pixel recovers the original, which it
    // can only do when both sides agree on the ceiling bpp.
    [Fact]
    public void Png_RoundTrip_WidePixel()
    {
        var original = new byte[] { 40, 90, 12, 200, 7, 130 };
        var encoded = PngPredictor.Encode(original, predictor: 14, colors: 3, bitsPerComponent: 4, columns: 4);
        var decoded = PngPredictor.Decode(encoded, colors: 3, bitsPerComponent: 4, columns: 4);
        Assert.Equal(original, decoded);
    }
}
