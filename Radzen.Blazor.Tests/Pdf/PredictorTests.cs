#nullable enable
using Radzen.Documents.Pdf.Objects.Filters;
using Xunit;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

public class PredictorTests
{
    [Fact]
    public void Png_None_Row()
    {
        var input = new byte[] { 0, 10, 20, 30, 40 };
        var expected = new byte[] { 10, 20, 30, 40 };
        Assert.Equal(expected, PngPredictor.Decode(input, colors: 1, bitsPerComponent: 8, columns: 4));
    }

    [Fact]
    public void Png_Sub_Row()
    {
        var input = new byte[] { 1, 10, 10, 10, 10 };
        var expected = new byte[] { 10, 20, 30, 40 };
        Assert.Equal(expected, PngPredictor.Decode(input, colors: 1, bitsPerComponent: 8, columns: 4));
    }

    [Fact]
    public void Png_Up_SingleRow_PriorIsZero()
    {
        var input = new byte[] { 2, 5, 6, 7, 8 };
        var expected = new byte[] { 5, 6, 7, 8 };
        Assert.Equal(expected, PngPredictor.Decode(input, colors: 1, bitsPerComponent: 8, columns: 4));
    }

    [Fact]
    public void Png_Up_TwoRows_UsesPriorRow()
    {
        var input = new byte[] { 0, 10, 20, 30, 40, 2, 1, 1, 1, 1 };
        var expected = new byte[] { 10, 20, 30, 40, 11, 21, 31, 41 };
        Assert.Equal(expected, PngPredictor.Decode(input, colors: 1, bitsPerComponent: 8, columns: 4));
    }

    [Fact]
    public void Png_Average_Row()
    {
        var input = new byte[] { 3, 10, 10, 10, 10 };
        var expected = new byte[] { 10, 15, 17, 18 };
        Assert.Equal(expected, PngPredictor.Decode(input, colors: 1, bitsPerComponent: 8, columns: 4));
    }

    [Fact]
    public void Png_Average_TwoRows()
    {
        var input = new byte[] { 0, 10, 20, 30, 40, 3, 2, 2, 2, 2 };
        var expected = new byte[] { 10, 20, 30, 40, 7, 15, 24, 34 };
        Assert.Equal(expected, PngPredictor.Decode(input, colors: 1, bitsPerComponent: 8, columns: 4));
    }

    [Fact]
    public void Png_Paeth_SingleRow()
    {
        var input = new byte[] { 4, 10, 10, 10, 10 };
        var expected = new byte[] { 10, 20, 30, 40 };
        Assert.Equal(expected, PngPredictor.Decode(input, colors: 1, bitsPerComponent: 8, columns: 4));
    }

    [Fact]
    public void Png_Paeth_TwoRows()
    {
        var input = new byte[] { 0, 10, 20, 30, 40, 4, 1, 2, 3, 4 };
        var expected = new byte[] { 10, 20, 30, 40, 11, 22, 33, 44 };
        Assert.Equal(expected, PngPredictor.Decode(input, colors: 1, bitsPerComponent: 8, columns: 4));
    }

    [Fact]
    public void Png_Sub_ThreeColors()
    {
        var input = new byte[] { 1, 1, 2, 3, 10, 20, 30 };
        var expected = new byte[] { 1, 2, 3, 11, 22, 33 };
        Assert.Equal(expected, PngPredictor.Decode(input, colors: 3, bitsPerComponent: 8, columns: 2));
    }

    [Fact]
    public void Png_Up_ThreeRows()
    {
        var input = new byte[] { 2, 10, 20, 30, 40, 2, 1, 1, 1, 1, 2, 1, 1, 1, 1 };
        var expected = new byte[] { 10, 20, 30, 40, 11, 21, 31, 41, 12, 22, 32, 42 };
        Assert.Equal(expected, PngPredictor.Decode(input, colors: 1, bitsPerComponent: 8, columns: 4));
    }

    [Fact]
    public void Tiff_Predictor2_Decode()
    {
        var input = new byte[] { 10, 1, 1, 1 };
        var expected = new byte[] { 10, 11, 12, 13 };
        Assert.Equal(expected, TiffPredictor.Decode(input, colors: 1, bitsPerComponent: 8, columns: 4));
    }

    [Fact]
    public void Tiff_Predictor2_ThreeColors()
    {
        var input = new byte[] { 10, 20, 30, 1, 2, 3 };
        var expected = new byte[] { 10, 20, 30, 11, 22, 33 };
        Assert.Equal(expected, TiffPredictor.Decode(input, colors: 3, bitsPerComponent: 8, columns: 2));
    }
}
