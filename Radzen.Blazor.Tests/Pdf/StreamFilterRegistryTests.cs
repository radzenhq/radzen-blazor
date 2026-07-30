#nullable enable
using System.Text;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Radzen.Documents.Pdf.Objects.Filters;
using Xunit;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

public class StreamFilterRegistryTests
{
    [Theory]
    [InlineData("FlateDecode")]
    [InlineData("Fl")]
    [InlineData("LZWDecode")]
    [InlineData("LZW")]
    [InlineData("RunLengthDecode")]
    [InlineData("RL")]
    [InlineData("ASCIIHexDecode")]
    [InlineData("AHx")]
    [InlineData("ASCII85Decode")]
    [InlineData("A85")]
    public void Get_ResolvesEveryBuiltInFilterAndAbbreviation(string name)
    {
        var filter = StreamFilterRegistry.Get(name);

        Assert.NotNull(filter);
    }

    [Fact]
    public void Get_UnknownFilter_ThrowsFailLoud()
    {
        var exception = Assert.Throws<DocumentParseException>(() => StreamFilterRegistry.Get("Bogus"));

        Assert.Contains("Bogus", exception.Message);
    }

    [Fact]
    public void AsciiHexFilter_DecodesThroughRegistry()
    {
        var encoded = Encoding.ASCII.GetBytes("48656C6C6F>");

        var decoded = StreamFilterRegistry.Get("ASCIIHexDecode")
            .Decode(encoded, null, ReaderLimits.Default.MaxDecodedStreamBytes);

        Assert.Equal("Hello", Encoding.ASCII.GetString(decoded));
    }

    [Fact]
    public void FlateFilter_RoundTripsThroughRegistry()
    {
        var plain = Encoding.ASCII.GetBytes("BT /F1 12 Tf (registry) Tj ET");
        var encoded = FlateFilter.Encode(plain);

        var decoded = StreamFilterRegistry.Get("FlateDecode")
            .Decode(encoded, null, ReaderLimits.Default.MaxDecodedStreamBytes);

        Assert.Equal(plain, decoded);
    }
}
