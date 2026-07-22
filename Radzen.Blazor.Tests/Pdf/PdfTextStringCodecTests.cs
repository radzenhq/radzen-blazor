using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

public class PdfTextStringCodecTests
{
    private static string RoundTrip(string value)
        => FormField.DecodeTextString(StringObject.FromText(value).Value);

    [Theory]
    [InlineData("Hello world")]
    [InlineData("caf\u00e9")]
    [InlineData("\u00a0nbsp\u00a0")]
    [InlineData("")]
    [InlineData("\u20ac100")]
    [InlineData("\u00a0diacritics \u0192")]
    [InlineData("mixed \u00a0 \u20ac \u65e5")]
    public void TextStringRoundTripsThroughEncodeAndDecode(string value)
    {
        Assert.Equal(value, RoundTrip(value));
    }

    [Fact]
    public void NonBreakingSpaceSurvivesInsteadOfBecomingEuro()
    {
        var decoded = RoundTrip("\u00a0");
        Assert.Equal("\u00a0", decoded);
        Assert.NotEqual("\u20ac", decoded);
    }

    [Fact]
    public void PlainAsciiEncodesAsRawSingleBytes()
    {
        Assert.Equal("Title", StringObject.FromText("Title").Value);
    }

    [Fact]
    public void EuroEncodesAsUtf16BigEndianWithByteOrderMark()
    {
        Assert.Equal("\u00fe\u00ff\u0020\u00ac", StringObject.FromText("\u20ac").Value);
    }
}
