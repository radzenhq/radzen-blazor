#nullable enable
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Radzen.Documents.Pdf.Objects;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// NumberObject edge cases: integers wider than 32 bits (offsets in files larger
// than 2 GiB, /Prev values, IDs) must survive parse and re-serialization without
// truncation, and NaN/Infinity must never leak into the output as an invalid
// token (ISO 32000-1 7.3.3 defines only signed decimal numbers).
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
    [InlineData("8589934592")]      // 2^33
    [InlineData("2147483648")]      // 2^31, one past int.MaxValue
    [InlineData("-8589934592")]
    public void ParsedInteger_WiderThan32Bits_IsNotTruncated(string text)
    {
        var number = Parse(text);

        Assert.Equal(double.Parse(text, System.Globalization.CultureInfo.InvariantCulture), number.DoubleValue);
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

    // A PDF numeric token is [+-]?digits[.digits]. NaN/Infinity have no valid
    // representation; writing them must either throw or emit a valid number.
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
            return; // throwing is an accepted guard
        }

        Assert.NotNull(written);
        Assert.Matches(new Regex(@"^[+-]?[0-9]*\.?[0-9]+$"), written!);
    }
}
