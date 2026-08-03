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
    public void AsciiHexFilterDecode_YieldsEveryByteValue()
    {
        const string Encoded =
            "000102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1E1F" +
            "202122232425262728292A2B2C2D2E2F303132333435363738393A3B3C3D3E3F" +
            "404142434445464748494A4B4C4D4E4F505152535455565758595A5B5C5D5E5F" +
            "606162636465666768696A6B6C6D6E6F707172737475767778797A7B7C7D7E7F" +
            "808182838485868788898A8B8C8D8E8F909192939495969798999A9B9C9D9E9F" +
            "A0A1A2A3A4A5A6A7A8A9AAABACADAEAFB0B1B2B3B4B5B6B7B8B9BABBBCBDBEBF" +
            "C0C1C2C3C4C5C6C7C8C9CACBCCCDCECFD0D1D2D3D4D5D6D7D8D9DADBDCDDDEDF" +
            "E0E1E2E3E4E5E6E7E8E9EAEBECEDEEEFF0F1F2F3F4F5F6F7F8F9FAFBFCFDFEFF>";

        Assert.Equal(AllBytes(), AsciiHexFilter.Decode(Encoding.ASCII.GetBytes(Encoded), 1 << 20));
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
