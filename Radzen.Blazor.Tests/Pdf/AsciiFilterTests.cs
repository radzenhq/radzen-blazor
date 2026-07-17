#nullable enable
using System;
using System.IO;
using System.Text;
using Radzen.Documents.Pdf.Objects.Filters;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

public class AsciiHexFilterTests
{
    [Fact]
    public void Decode_Hello()
    {
        var result = AsciiHexFilter.Decode(Encoding.ASCII.GetBytes("48656C6C6F>"));
        Assert.Equal("Hello", Encoding.ASCII.GetString(result));
    }

    [Fact]
    public void Decode_IgnoresWhitespace()
    {
        var result = AsciiHexFilter.Decode(Encoding.ASCII.GetBytes("48 65\n6C>"));
        Assert.Equal(new byte[] { 0x48, 0x65, 0x6C }, result);
    }

    [Fact]
    public void Decode_OddFinalDigit_PadsZero()
    {
        var result = AsciiHexFilter.Decode(Encoding.ASCII.GetBytes("7>"));
        Assert.Equal(new byte[] { 0x70 }, result);
    }

    [Fact]
    public void Decode_IgnoresDataAfterEod()
    {
        var result = AsciiHexFilter.Decode(Encoding.ASCII.GetBytes("48>6566"));
        Assert.Equal(new byte[] { 0x48 }, result);
    }

    [Fact]
    public void Decode_ImmediateEod_ReturnsEmpty()
    {
        var result = AsciiHexFilter.Decode(Encoding.ASCII.GetBytes(">"));
        Assert.Empty(result);
    }

    [Fact]
    public void Decode_InvalidChar_Throws()
    {
        Assert.ThrowsAny<Exception>(() => AsciiHexFilter.Decode(Encoding.ASCII.GetBytes("48ZZ>")));
    }

    [Fact]
    public void Encode_ProducesUppercaseHexWithEod()
    {
        var result = Encoding.ASCII.GetString(AsciiHexFilter.Encode(Encoding.ASCII.GetBytes("Hello")));
        Assert.Equal("48656C6C6F>", result);
    }

    [Fact]
    public void Encode_Decode_RoundTrip()
    {
        var data = new byte[] { 0x00, 0x7F, 0x80, 0xFF, 0x10, 0xAB };
        var decoded = AsciiHexFilter.Decode(AsciiHexFilter.Encode(data));
        Assert.Equal(data, decoded);
    }
}

public class Ascii85FilterTests
{
    [Fact]
    public void Decode_KnownVector_WithEod()
    {
        var result = Ascii85Filter.Decode(Encoding.ASCII.GetBytes("9jqo^~>"));
        Assert.Equal("Man ", Encoding.ASCII.GetString(result));
    }

    [Fact]
    public void Encode_KnownVector()
    {
        var result = Encoding.ASCII.GetString(Ascii85Filter.Encode(Encoding.ASCII.GetBytes("Man ")));
        Assert.StartsWith("9jqo^", result);
        Assert.EndsWith("~>", result);
    }

    [Fact]
    public void Decode_ZShortcut_FourZeroBytes()
    {
        var result = Ascii85Filter.Decode(Encoding.ASCII.GetBytes("z~>"));
        Assert.Equal(new byte[] { 0, 0, 0, 0 }, result);
    }

    [Fact]
    public void Decode_PartialGroup_OneByte()
    {
        var result = Ascii85Filter.Decode(Encoding.ASCII.GetBytes("9`~>"));
        Assert.Equal(Encoding.ASCII.GetBytes("M"), result);
    }

    [Fact]
    public void Decode_PartialGroup_TwoBytes()
    {
        var result = Ascii85Filter.Decode(Encoding.ASCII.GetBytes("9jn~>"));
        Assert.Equal(Encoding.ASCII.GetBytes("Ma"), result);
    }

    [Fact]
    public void Decode_PartialGroup_ThreeBytes()
    {
        var result = Ascii85Filter.Decode(Encoding.ASCII.GetBytes("9jqo~>"));
        Assert.Equal(Encoding.ASCII.GetBytes("Man"), result);
    }

    [Fact]
    public void Decode_IgnoresWhitespace()
    {
        var result = Ascii85Filter.Decode(Encoding.ASCII.GetBytes("9j qo\n^~>"));
        Assert.Equal("Man ", Encoding.ASCII.GetString(result));
    }

    [Fact]
    public void Decode_ImmediateEod_ReturnsEmpty()
    {
        var result = Ascii85Filter.Decode(Encoding.ASCII.GetBytes("~>"));
        Assert.Empty(result);
    }

    [Fact]
    public void Decode_CharOutOfRange_Throws()
    {
        Assert.ThrowsAny<Exception>(() => Ascii85Filter.Decode(Encoding.ASCII.GetBytes("9jqv^~>")));
    }

    [Fact]
    public void Decode_DanglingSingleChar_Throws()
    {
        Assert.ThrowsAny<Exception>(() => Ascii85Filter.Decode(Encoding.ASCII.GetBytes("9jqo^9~>")));
    }

    [Fact]
    public void Encode_Decode_RoundTrip()
    {
        var data = Encoding.ASCII.GetBytes("Hello, World!");
        var decoded = Ascii85Filter.Decode(Ascii85Filter.Encode(data));
        Assert.Equal(data, decoded);
    }

    [Fact]
    public void Encode_Decode_RoundTrip_WithZeroRun()
    {
        var data = new byte[] { 0, 0, 0, 0, 1, 2, 3, 0, 0, 0, 0 };
        var decoded = Ascii85Filter.Decode(Ascii85Filter.Encode(data));
        Assert.Equal(data, decoded);
    }

    [Fact]
    public void AsciiHex_BadDigit_MessageNamesTheOffendingByte()
    {
        var e = Assert.Throws<InvalidDataException>(() => AsciiHexFilter.Decode(Encoding.ASCII.GetBytes("48ZZ>")));

        Assert.Contains("0x5A", e.Message);
    }

    [Fact]
    public void Encode_InputTooLargeToEncode_ThrowsDiagnosable()
    {
        var data = new byte[1 << 30];

        var e = Assert.Throws<ArgumentException>(() => AsciiHexFilter.Encode(data));

        Assert.Contains("ASCIIHex", e.Message, StringComparison.Ordinal);
    }
}
