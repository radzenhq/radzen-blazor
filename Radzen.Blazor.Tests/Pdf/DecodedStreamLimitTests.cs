#nullable enable
using System.Text;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Radzen.Documents.Pdf.Objects.Filters;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

public class DecodedStreamLimitTests
{
    private static StreamDecoder DecoderWithCap(long cap)
        => new(new ReaderLimits { MaxDecodedStreamBytes = cap }, x => x);

    [Fact]
    public void UnfilteredStreamExceedingTheCap_Throws()
    {
        var payload = Encoding.ASCII.GetBytes("this is much larger than the cap");
        Assert.Throws<DocumentParseException>(() => DecoderWithCap(4).Decode(new DictionaryObject(), payload));
    }

    [Fact]
    public void AsciiHexStreamExceedingTheCap_Throws()
    {
        var dictionary = new DictionaryObject { ["Filter"] = new NameObject("ASCIIHexDecode") };
        var payload = Encoding.ASCII.GetBytes("48656C6C6F>");
        Assert.Throws<DocumentParseException>(() => DecoderWithCap(2).Decode(dictionary, payload));
    }

    [Fact]
    public void AsciiHexDecodeWithCap_ThrowsBeforeAllocatingOversizedOutput()
    {
        var payload = Encoding.ASCII.GetBytes("48656C6C6F>");
        Assert.Throws<DocumentParseException>(() => AsciiHexFilter.Decode(payload, 2));
    }
}
