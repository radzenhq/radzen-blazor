#nullable enable
using Xunit;
using Radzen.Documents.Pdf.Fonts;

namespace Radzen.Blazor.Pdf.Tests;

// WinAnsiEncoding per ISO 32000-1 Annex D.2. Spot-check values cross-checked against
// python3 "chr(u).encode('cp1252')" for the printable/high-range code points.
public class WinAnsiEncodingTests
{
    [Theory]
    [InlineData('A', 65)]
    [InlineData('a', 97)]
    [InlineData('0', 48)]
    [InlineData(' ', 32)]
    [InlineData('~', 126)]
    [InlineData('€', 128)]   // euro
    [InlineData('™', 153)]   // trademark
    [InlineData('“', 147)]   // left double quote
    [InlineData('”', 148)]   // right double quote
    [InlineData('é', 233)]   // e acute
    [InlineData(' ', 160)]   // nbsp -> space slot in Annex D
    [InlineData('ÿ', 255)]   // y dieresis
    public void TryGetCode_MapsKnownChars(char c, int expected)
    {
        Assert.True(WinAnsiEncoding.TryGetCode(c, out var code));
        Assert.Equal(expected, code);
    }

    [Theory]
    [InlineData('Б')]   // Cyrillic Be
    [InlineData('α')]   // Greek alpha
    [InlineData('中')]   // CJK
    public void TryGetCode_ReturnsFalseForUnencodable(char c)
    {
        Assert.False(WinAnsiEncoding.TryGetCode(c, out var code));
        Assert.Equal(0, code);
    }

    [Theory]
    [InlineData(65, "A")]
    [InlineData(97, "a")]
    [InlineData(32, "space")]
    [InlineData(128, "Euro")]
    [InlineData(153, "trademark")]
    [InlineData(147, "quotedblleft")]
    [InlineData(233, "eacute")]
    [InlineData(160, "space")]
    public void GetGlyphName_ReturnsAdobeName(int code, string expected)
    {
        Assert.Equal(expected, WinAnsiEncoding.GetGlyphName((byte)code));
    }

    [Theory]
    [InlineData(129)]
    [InlineData(141)]
    [InlineData(143)]
    [InlineData(144)]
    [InlineData(157)]
    [InlineData(0)]
    [InlineData(31)]
    public void GetGlyphName_ReturnsNotdefForUndefinedCodes(int code)
    {
        Assert.Equal(".notdef", WinAnsiEncoding.GetGlyphName((byte)code));
    }

    [Theory]
    [InlineData('A', true)]
    [InlineData('é', true)]
    [InlineData('€', true)]
    [InlineData('Б', false)]
    [InlineData('α', false)]
    public void CanEncode_MatchesTryGetCode(char c, bool expected)
    {
        Assert.Equal(expected, WinAnsiEncoding.CanEncode(c));
    }
}
