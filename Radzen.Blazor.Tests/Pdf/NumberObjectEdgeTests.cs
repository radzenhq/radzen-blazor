#nullable enable
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Radzen.Documents.Pdf.Objects;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// ISO 32000-1 7.3.3 defines only signed decimal numbers.
public class NumberObjectEdgeTests
{
    private static string Written(NumberObject number)
    {
        using var stream = new MemoryStream();
        number.Write(stream);
        return Encoding.Latin1.GetString(stream.ToArray());
    }

    private static NumberObject Parse(string text)
        => Assert.IsType<NumberObject>(ObjectParser.Parse(Encoding.Latin1.GetBytes(text), 0));

    [Theory]
    [InlineData("8589934592")]
    [InlineData("2147483648")]
    [InlineData("-8589934592")]
    public void ParsedInteger_WiderThan32Bits_IsNotTruncated(string text)
    {
        var number = Parse(text);

        Assert.Equal(double.Parse(text, CultureInfo.InvariantCulture), number.DoubleValue);
        Assert.Equal(text, Written(number));
    }

    [Fact]
    public void ParsedLargeInteger_InsideDictionary_RoundTrips()
    {
        var dictionary = Assert.IsType<DictionaryObject>(
            ObjectParser.Parse(Encoding.Latin1.GetBytes("<< /Prev 10000000000 >>"), 0));

        var prev = Assert.IsType<NumberObject>(dictionary["Prev"]);
        Assert.Equal(10000000000.0, prev.DoubleValue);
        Assert.Equal("10000000000", Written(prev));
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void NonFiniteReal_NeverSerializesAnInvalidToken(double value)
    {
        string? written = null;
        var exception = Record.Exception(() => written = Written(new NumberObject(value)));
        if (exception is not null)
        {
            return;
        }

        Assert.NotNull(written);
        Assert.Matches(new Regex(@"^[+-]?[0-9]*\.?[0-9]+$"), written!);
    }
}
