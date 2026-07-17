using System;
using System.IO;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Content;
using Radzen.Documents.Pdf.Objects;
using Radzen.Documents.Pdf.Objects.Filters;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

public class SharedHelperExtractionTests
{
    [Theory]
    [InlineData(-1.0, 0.0)]
    [InlineData(-0.0001, 0.0)]
    [InlineData(0.0, 0.0)]
    [InlineData(0.25, 0.25)]
    [InlineData(1.0, 1.0)]
    [InlineData(1.0001, 1.0)]
    [InlineData(double.PositiveInfinity, 1.0)]
    [InlineData(double.NegativeInfinity, 0.0)]
    public void UnitIntervalClampMatchesTheOriginalExpression(double value, double expected)
    {
        Assert.Equal(expected, UnitInterval.Clamp(value));
    }

    [Fact]
    public void UnitIntervalClampPropagatesNaN()
    {
        Assert.True(double.IsNaN(UnitInterval.Clamp(double.NaN)));
    }

    [Fact]
    public void RunAndPathContentClampFillCmykIdentically()
    {
        var run = new Run("x");
        run.SetFillCmyk(-1, 0.5, 2, double.NaN);

        var path = new PathContent();
        path.SetFillCmyk(-1, 0.5, 2, double.NaN);

        var runPaint = run.FillPaint!.Value;
        var pathPaint = path.FillPaint!.Value;

        Assert.Equal(runPaint.Operands[0], pathPaint.Operands[0]);
        Assert.Equal(runPaint.Operands[1], pathPaint.Operands[1]);
        Assert.Equal(runPaint.Operands[2], pathPaint.Operands[2]);
        Assert.True(double.IsNaN(runPaint.Operands[3]) && double.IsNaN(pathPaint.Operands[3]));
        Assert.Equal(0.0, runPaint.Operands[0]);
        Assert.Equal(1.0, runPaint.Operands[2]);
    }

    // Every ISO 32000-1 7.2.2 delimiter (plus '#') forces the escaped form.
    [Theory]
    [InlineData("(")]
    [InlineData(")")]
    [InlineData("<")]
    [InlineData(">")]
    [InlineData("[")]
    [InlineData("]")]
    [InlineData("{")]
    [InlineData("}")]
    [InlineData("/")]
    [InlineData("%")]
    [InlineData("#")]
    [InlineData(" ")]
    [InlineData("\t")]
    [InlineData("")]
    [InlineData("ÿ")]
    public void NameEscapingAgreesBetweenNameObjectAndContentWriter(string name)
    {
        Assert.True(NameObject.NeedsEscaping(name));
        Assert.Equal(WriteViaStream(name), WriteViaContentWriter(name));
    }

    [Theory]
    [InlineData("Helvetica")]
    [InlineData("F1")]
    [InlineData("GS0")]
    [InlineData("Pattern")]
    [InlineData("a~z")]
    [InlineData("!")]
    public void UnescapedNamesTakeTheAllocationFreePathInBothWriters(string name)
    {
        Assert.False(NameObject.NeedsEscaping(name));
        Assert.Equal("/" + name, WriteViaContentWriter(name));
        Assert.Equal("/" + name, WriteViaStream(name));
    }

    [Fact]
    public void BothNameWritersRejectTheSameUnencodableCodePointWithTheSameMessage()
    {
        const string name = "AŁB";

        var fromStream = Assert.Throws<NotSupportedException>(() => WriteViaStream(name));
        var fromWriter = Assert.Throws<NotSupportedException>(() => WriteViaContentWriter(name));

        Assert.Equal(fromStream.Message, fromWriter.Message);
        Assert.Contains("U+0141", fromWriter.Message);
    }

    [Fact]
    public void UnencodableCodePointAfterAnEscapeTriggerStillThrows()
    {
        const string name = "a bŁ";

        Assert.Throws<NotSupportedException>(() => WriteViaStream(name));
        Assert.Throws<NotSupportedException>(() => WriteViaContentWriter(name));
    }

    [Theory]
    [InlineData("PNG")]
    [InlineData("TIFF")]
    public void PredictorsRejectOutOfRangeColorsWithTheirOwnPrefix(string predictor)
    {
        var error = Assert.Throws<DocumentParseException>(
            () => Decode(predictor, new byte[8], colors: PredictorParameters.MaxColors + 1, columns: 4));

        Assert.Equal($"{predictor} predictor colors/columns are out of range.", error.Message);
    }

    [Theory]
    [InlineData("PNG", 0, 4)]
    [InlineData("PNG", -1, 4)]
    [InlineData("PNG", 1, 0)]
    [InlineData("PNG", 1, -1)]
    [InlineData("TIFF", 0, 4)]
    [InlineData("TIFF", -1, 4)]
    [InlineData("TIFF", 1, 0)]
    [InlineData("TIFF", 1, -1)]
    public void PredictorsRejectTheSameColorsAndColumnsRange(string predictor, int colors, int columns)
    {
        Assert.Throws<DocumentParseException>(() => Decode(predictor, new byte[8], colors, columns));
    }

    [Theory]
    [InlineData("PNG")]
    [InlineData("TIFF")]
    public void PredictorsAcceptExactlyMaxColors(string predictor)
    {
        var data = new byte[predictor == "PNG" ? (PredictorParameters.MaxColors * 4) + 1 : PredictorParameters.MaxColors * 4];

        Decode(predictor, data, colors: PredictorParameters.MaxColors, columns: 4);
    }

    private static byte[] Decode(string predictor, byte[] data, int colors, int columns)
        => predictor == "PNG"
            ? PngPredictor.Decode(data, colors, 8, columns)
            : TiffPredictor.Decode(data, colors, 8, columns);

    private static string WriteViaStream(string name)
    {
        using var stream = new MemoryStream();
        NameObject.WriteEscaped(stream, name);
        return System.Text.Encoding.Latin1.GetString(stream.ToArray());
    }

    private static string WriteViaContentWriter(string name)
    {
        using var writer = new ContentWriter();
        writer.WriteName(name);
        return System.Text.Encoding.Latin1.GetString(writer.ToArray());
    }
}
