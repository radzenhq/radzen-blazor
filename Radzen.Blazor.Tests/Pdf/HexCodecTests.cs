using System;
using System.Linq;
using System.Text;
using Radzen.Documents;
using Radzen.Documents.Crypto;
using Radzen.Documents.Pdf.Objects.Filters;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

public class HexCodecTests
{
    static byte[] AllBytes() => Enumerable.Range(0, 256).Select(i => (byte)i).ToArray();

    static string LegacyUpperString(byte[] digest)
    {
        const string hex = "0123456789ABCDEF";
        var chars = new char[digest.Length * 2];
        for (var i = 0; i < digest.Length; i++)
        {
            chars[i * 2] = hex[digest[i] >> 4];
            chars[i * 2 + 1] = hex[digest[i] & 0xF];
        }

        return new string(chars);
    }

    static string LegacyLowerString(byte[] digest)
    {
        const string hex = "0123456789abcdef";
        var chars = new char[digest.Length * 2];
        for (var i = 0; i < digest.Length; i++)
        {
            chars[i * 2] = hex[digest[i] >> 4];
            chars[i * 2 + 1] = hex[digest[i] & 0xF];
        }

        return new string(chars);
    }

    [Fact]
    public void EncodeToString_MatchesRetiredUppercaseCopy()
    {
        var data = AllBytes();
        Assert.Equal(LegacyUpperString(data), HexCodec.EncodeToString(data, HexCase.Upper));
    }

    [Fact]
    public void EncodeToString_MatchesRetiredLowercaseCopy()
    {
        var data = AllBytes();
        Assert.Equal(LegacyLowerString(data), HexCodec.EncodeToString(data, HexCase.Lower));
    }

    [Fact]
    public void Encode_MatchesRetiredSpanCopy()
    {
        var blob = AllBytes();

        var expected = new byte[blob.Length * 2];
        const string hex = "0123456789abcdef";
        for (var i = 0; i < blob.Length; i++)
        {
            expected[2 * i] = (byte)hex[blob[i] >> 4];
            expected[2 * i + 1] = (byte)hex[blob[i] & 0xF];
        }

        var actual = new byte[blob.Length * 2 + 7];
        HexCodec.Encode(blob, actual.AsSpan(3, blob.Length * 2), HexCase.Lower);

        Assert.Equal(expected, actual.Skip(3).Take(blob.Length * 2).ToArray());
        Assert.True(actual.Take(3).All(b => b == 0), "Encode wrote before the destination span.");
        Assert.True(actual.Skip(3 + blob.Length * 2).All(b => b == 0), "Encode wrote past the destination span.");
    }


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
    public void Sha2ComputeHashHex_StaysLowercase()
    {
        var hex = Sha2.ComputeHashHex256(Encoding.ASCII.GetBytes("abc"));

        Assert.Equal("ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad", hex);
    }

    [Fact]
    public void EncodeToString_InputTooLargeToEncode_ThrowsDiagnosable()
    {
        var data = new byte[1 << 30];

        var error = Assert.Throws<ArgumentException>(() => HexCodec.EncodeToString(data, HexCase.Lower));

        Assert.Contains("1073741824", error.Message, StringComparison.Ordinal);
    }
}
