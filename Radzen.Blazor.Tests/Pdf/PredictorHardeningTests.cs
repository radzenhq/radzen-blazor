#nullable enable
using Radzen.Documents.Pdf.Objects;
using Radzen.Documents.Pdf.Objects.Filters;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// Hardening for the PNG predictor against /DecodeParms that drive unbounded
// allocation or int32 overflow: a giant /Columns, a /Columns that wraps the
// row-length product, and out-of-range colors/bit depths must all be rejected
// with DocumentParseException before any buffer is sized from them. A positive
// control proves an ordinary predictor row still decodes unchanged.
public class PredictorHardeningTests
{
    private static readonly byte[] SmallData = new byte[16];

    // /Columns 260000000 would allocate a ~260MB scratch row for 16 bytes of data.
    [Fact]
    public void GiantColumns_ThrowsFast()
    {
        Assert.Throws<DocumentParseException>(
            () => PngPredictor.Decode(SmallData, colors: 1, bitsPerComponent: 8, columns: 260000000));
    }

    // /Columns 268435456 * 8 bits wraps the 32-bit row-length product to negative.
    [Fact]
    public void ColumnsThatWrapInt32_ThrowsFast()
    {
        Assert.Throws<DocumentParseException>(
            () => PngPredictor.Decode(SmallData, colors: 1, bitsPerComponent: 8, columns: 268435456));
    }

    [Fact]
    public void InvalidBitDepth_Throws()
    {
        Assert.Throws<DocumentParseException>(
            () => PngPredictor.Decode(SmallData, colors: 1, bitsPerComponent: 3, columns: 4));
    }

    [Fact]
    public void NonPositiveColumns_Throws()
    {
        Assert.Throws<DocumentParseException>(
            () => PngPredictor.Decode(SmallData, colors: 1, bitsPerComponent: 8, columns: 0));
    }

    [Fact]
    public void ValidPredictorRow_StillDecodes()
    {
        var input = new byte[] { 1, 10, 10, 10, 10 };
        var expected = new byte[] { 10, 20, 30, 40 };
        Assert.Equal(expected, PngPredictor.Decode(input, colors: 1, bitsPerComponent: 8, columns: 4));
    }
}
