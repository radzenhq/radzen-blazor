#nullable enable
using System.Text;
using Radzen.Documents.Pdf.Fonts;
using Xunit;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

public class ToUnicodeBreakSetTests
{
    private static byte[] Bytes(string text) => Encoding.ASCII.GetBytes(text);

    [Theory]
    [InlineData("\0")]
    [InlineData("\t")]
    [InlineData("\n")]
    [InlineData("\f")]
    [InlineData("\r")]
    [InlineData(" ")]
    public void EveryPdfWhitespaceByte_SeparatesKeywordsAndCodes(string space)
    {
        var cmap = Bytes($"1{space}beginbfchar{space}<0041>{space}<0042>{space}endbfchar");

        var (map, _) = ToUnicodeCMap.Parse(cmap);

        Assert.Equal("B", map[0x41]);
    }

    [Fact]
    public void KeywordInsideAParentheticalRun_DoesNotStartASection()
    {
        var cmap = Bytes("/X (beginbfchar) <0041> <0042> endbfchar");

        var (map, _) = ToUnicodeCMap.Parse(cmap);

        Assert.Empty(map);
    }

    [Fact]
    public void SlashDoesNotEndAKeyword()
    {
        var (map, _) = ToUnicodeCMap.Parse(Bytes("/Foo/beginbfchar <0041> <0042> endbfchar"));

        Assert.Empty(map);
    }

    [Theory]
    [InlineData("<")]
    [InlineData("[")]
    [InlineData("]")]
    [InlineData("%")]
    public void DispatchedDelimiters_EndAKeywordWithoutWhitespace(string delimiter)
    {
        var cmap = Bytes($"1 beginbfchar <0041> <0042> endbfchar{delimiter}");

        var (map, _) = ToUnicodeCMap.Parse(cmap);

        Assert.Equal("B", map[0x41]);
    }
}
