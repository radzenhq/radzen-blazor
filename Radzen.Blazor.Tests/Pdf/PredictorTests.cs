#nullable enable
using Radzen.Documents.Pdf.Objects.Filters;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// All expected vectors hand-computed per PNG spec and cross-checked with a python3
// reference implementation of the PNG unfilter and TIFF horizontal-differencing decode.
public class PredictorTests
{
    // PNG filter type 0 (None): stored bytes pass through unchanged.
    [Fact]
    public void Png_None_Row()
    {
        var input = new byte[] { 0, 10, 20, 30, 40 };
        var expected = new byte[] { 10, 20, 30, 40 };
        Assert.Equal(expected, PngPredictor.Decode(input, colors: 1, bitsPerComponent: 8, columns: 4));
    }

    // PNG filter type 1 (Sub): each byte adds the reconstructed byte bpp positions to the left.
    // colors=1,bpc=8 -> bpp=1. deltas 10,10,10,10 -> 10,20,30,40.
    [Fact]
    public void Png_Sub_Row()
    {
        var input = new byte[] { 1, 10, 10, 10, 10 };
        var expected = new byte[] { 10, 20, 30, 40 };
        Assert.Equal(expected, PngPredictor.Decode(input, colors: 1, bitsPerComponent: 8, columns: 4));
    }

    // PNG filter type 2 (Up) single row: prior row is all zero, so stored bytes pass through.
    [Fact]
    public void Png_Up_SingleRow_PriorIsZero()
    {
        var input = new byte[] { 2, 5, 6, 7, 8 };
        var expected = new byte[] { 5, 6, 7, 8 };
        Assert.Equal(expected, PngPredictor.Decode(input, colors: 1, bitsPerComponent: 8, columns: 4));
    }

    // Two-row Up proves prior-row dependency: row1 None [10,20,30,40];
    // row2 Up deltas [1,1,1,1] add the row above -> [11,21,31,41].
    [Fact]
    public void Png_Up_TwoRows_UsesPriorRow()
    {
        var input = new byte[] { 0, 10, 20, 30, 40, 2, 1, 1, 1, 1 };
        var expected = new byte[] { 10, 20, 30, 40, 11, 21, 31, 41 };
        Assert.Equal(expected, PngPredictor.Decode(input, colors: 1, bitsPerComponent: 8, columns: 4));
    }

    // PNG filter type 3 (Average) single row: prior=0, average = floor((left+above)/2).
    // deltas 10,10,10,10: 10, 10+5=15, 10+7=17, 10+8=18.
    [Fact]
    public void Png_Average_Row()
    {
        var input = new byte[] { 3, 10, 10, 10, 10 };
        var expected = new byte[] { 10, 15, 17, 18 };
        Assert.Equal(expected, PngPredictor.Decode(input, colors: 1, bitsPerComponent: 8, columns: 4));
    }

    // Two-row Average uses left and above: row1 [10,20,30,40]; row2 deltas 2,2,2,2:
    // x0 avg(0,10)=5 ->7; x1 avg(7,20)=13 ->15; x2 avg(15,30)=22 ->24; x3 avg(24,40)=32 ->34.
    [Fact]
    public void Png_Average_TwoRows()
    {
        var input = new byte[] { 0, 10, 20, 30, 40, 3, 2, 2, 2, 2 };
        var expected = new byte[] { 10, 20, 30, 40, 7, 15, 24, 34 };
        Assert.Equal(expected, PngPredictor.Decode(input, colors: 1, bitsPerComponent: 8, columns: 4));
    }

    // PNG filter type 4 (Paeth) single row: prior=0 so predictor collapses to Sub-like (paeth(a,0,0)=a).
    [Fact]
    public void Png_Paeth_SingleRow()
    {
        var input = new byte[] { 4, 10, 10, 10, 10 };
        var expected = new byte[] { 10, 20, 30, 40 };
        Assert.Equal(expected, PngPredictor.Decode(input, colors: 1, bitsPerComponent: 8, columns: 4));
    }

    // Two-row Paeth exercises the full predictor. row1 [10,20,30,40]; row2 deltas 1,2,3,4:
    // x0 paeth(0,10,0)=10 ->11; x1 paeth(11,20,10)=20 ->22; x2 paeth(22,30,20)=30 ->33; x3 paeth(33,40,30)=40 ->44.
    [Fact]
    public void Png_Paeth_TwoRows()
    {
        var input = new byte[] { 0, 10, 20, 30, 40, 4, 1, 2, 3, 4 };
        var expected = new byte[] { 10, 20, 30, 40, 11, 22, 33, 44 };
        Assert.Equal(expected, PngPredictor.Decode(input, colors: 1, bitsPerComponent: 8, columns: 4));
    }

    // colors=3 proves bytes-per-pixel offset: bpp=3, columns=2, rowlen=6.
    // Sub deltas: pixel1 [1,2,3] verbatim; pixel2 [10,20,30] += pixel1 -> [11,22,33].
    [Fact]
    public void Png_Sub_ThreeColors()
    {
        var input = new byte[] { 1, 1, 2, 3, 10, 20, 30 };
        var expected = new byte[] { 1, 2, 3, 11, 22, 33 };
        Assert.Equal(expected, PngPredictor.Decode(input, colors: 3, bitsPerComponent: 8, columns: 2));
    }

    // Encode with PDF predictor 12 (Up) then Decode reconstructs the original raw rows.
    [Fact]
    public void Png_Encode12_Decode_RoundTrip()
    {
        var raw = new byte[] { 10, 20, 30, 40, 11, 21, 31, 41, 12, 22, 32, 42 };
        var encoded = PngPredictor.Encode(raw, predictor: 12, colors: 1, bitsPerComponent: 8, columns: 4);
        var decoded = PngPredictor.Decode(encoded, colors: 1, bitsPerComponent: 8, columns: 4);
        Assert.Equal(raw, decoded);
    }

    // TIFF predictor 2 (horizontal differencing) decode: each component adds the previous same-component value.
    // colors=1,columns=4: diffs [10,1,1,1] -> [10,11,12,13].
    [Fact]
    public void Tiff_Predictor2_Decode()
    {
        var input = new byte[] { 10, 1, 1, 1 };
        var expected = new byte[] { 10, 11, 12, 13 };
        Assert.Equal(expected, TiffPredictor.Decode(input, colors: 1, bitsPerComponent: 8, columns: 4));
    }

    // TIFF colors=3, columns=2: pixel2 components add pixel1 components.
    [Fact]
    public void Tiff_Predictor2_ThreeColors()
    {
        var input = new byte[] { 10, 20, 30, 1, 2, 3 };
        var expected = new byte[] { 10, 20, 30, 11, 22, 33 };
        Assert.Equal(expected, TiffPredictor.Decode(input, colors: 3, bitsPerComponent: 8, columns: 2));
    }
}
