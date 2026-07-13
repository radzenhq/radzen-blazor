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

    [Theory]
    [InlineData(3)]
    [InlineData(9)]
    [InlineData(16)]
    public void UnsupportedPredictor_Throws(int predictor)
    {
        var parms = new DictionaryObject { ["Predictor"] = new NumberObject(predictor) };

        Assert.Throws<DocumentParseException>(() => StreamPredictor.Apply(SmallData, parms));
    }

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

    [Fact]
    public void Png_PartialTrailingRow_Throws()
    {
        Assert.Throws<DocumentParseException>(
            () => PngPredictor.Decode(new byte[6], colors: 1, bitsPerComponent: 8, columns: 4));
    }

    // TIFF: /Colors 65536 wraps the int32 rowLength=colors*columns product; the colors
    // bound rejects it before any row math (mirrors PngPredictor's MaxColors guard).
    [Fact]
    public void Tiff_ColorsOutOfRange_Throws()
    {
        Assert.Throws<DocumentParseException>(
            () => TiffPredictor.Decode(new byte[16], colors: 65536, bitsPerComponent: 8, columns: 65536));
    }

    [Fact]
    public void Tiff_NonPositiveColumns_Throws()
    {
        Assert.Throws<DocumentParseException>(
            () => TiffPredictor.Decode(new byte[16], colors: 1, bitsPerComponent: 8, columns: 0));
    }

    // A /Columns wider than the data leaves no whole row: reject rather than silently
    // returning the stream unmodified (predictor not applied).
    [Fact]
    public void Tiff_ColumnsWiderThanData_Throws()
    {
        Assert.Throws<DocumentParseException>(
            () => TiffPredictor.Decode(new byte[4], colors: 1, bitsPerComponent: 8, columns: 260000000));
    }

    // A trailing partial row (5 bytes over a 4-byte row) is corrupt input; fail loud
    // instead of passing the dangling bytes through untouched.
    [Fact]
    public void Tiff_PartialTrailingRow_Throws()
    {
        Assert.Throws<DocumentParseException>(
            () => TiffPredictor.Decode(new byte[5], colors: 1, bitsPerComponent: 8, columns: 4));
    }

    [Fact]
    public void Tiff_ValidRow_StillDecodes()
    {
        var input = new byte[] { 10, 1, 1, 1 };
        var expected = new byte[] { 10, 11, 12, 13 };
        Assert.Equal(expected, TiffPredictor.Decode(input, colors: 1, bitsPerComponent: 8, columns: 4));
    }
}
