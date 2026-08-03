using System;
using System.Linq;
using System.Text;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Crypto;
using Radzen.Documents.Pdf.Objects.Filters;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

public class HexCodecTests
{
    static byte[] AllBytes() => Enumerable.Range(0, 256).Select(i => (byte)i).ToArray();

    [Fact]
    public void EncodeToString_EmptyInputIsEmpty()
    {
        Assert.Equal(string.Empty, HexCodec.EncodeToString([], HexCase.Upper));
    }


    [Fact]
    public void AsciiHexFilterEncode_StaysUppercaseAndKeepsEod()
    {
        var encoded = AsciiHexFilter.Encode([0xAB, 0xCD, 0x0F]);

        Assert.Equal("ABCD0F>", Encoding.ASCII.GetString(encoded));
    }

    [Fact]
    public void AsciiHexFilterEncode_EmptyInputIsJustEod()
    {
        Assert.Equal(">", Encoding.ASCII.GetString(AsciiHexFilter.Encode([])));
    }

    [Fact]
    public void AsciiHexFilterEncode_RoundTripsThroughDecode()
    {
        var data = AllBytes();

        Assert.Equal(data, AsciiHexFilter.Decode(AsciiHexFilter.Encode(data)));
    }

    [Fact]
    public void Sha1ComputeHashHex_StaysUppercase()
    {
        var hex = Sha1.ComputeHashHex(Encoding.ASCII.GetBytes("abc"));

        Assert.Equal("A9993E364706816ABA3E25717850C26C9CD0D89D", hex);
    }

    [Fact]
    public void EncodeToString_InputTooLargeToEncode_ThrowsDiagnosable()
    {
        var data = new byte[1 << 30];

        var error = Assert.Throws<ArgumentException>(() => HexCodec.EncodeToString(data, HexCase.Lower));

        Assert.Contains("1073741824", error.Message, StringComparison.Ordinal);
    }
}
