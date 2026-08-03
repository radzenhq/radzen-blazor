#nullable enable
using System;
using System.IO;
using System.Text;
using Radzen.Documents.Pdf.Objects.Filters;
using Xunit;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

public class AsciiHexFilterTests
{
    const long MaxOutput = 1 << 20;

    [Fact]
    public void Decode_Hello()
    {
        var result = AsciiHexFilter.Decode(Encoding.ASCII.GetBytes("48656C6C6F>"), MaxOutput);
        Assert.Equal("Hello", Encoding.ASCII.GetString(result));
    }

    [Fact]
    public void Decode_IgnoresWhitespace()
    {
        var result = AsciiHexFilter.Decode(Encoding.ASCII.GetBytes("48 65\n6C>"), MaxOutput);
        Assert.Equal(new byte[] { 0x48, 0x65, 0x6C }, result);
    }

    [Fact]
    public void Decode_OddFinalDigit_PadsZero()
    {
        var result = AsciiHexFilter.Decode(Encoding.ASCII.GetBytes("7>"), MaxOutput);
        Assert.Equal(new byte[] { 0x70 }, result);
    }

    [Fact]
    public void Decode_IgnoresDataAfterEod()
    {
        var result = AsciiHexFilter.Decode(Encoding.ASCII.GetBytes("48>6566"), MaxOutput);
        Assert.Equal(new byte[] { 0x48 }, result);
    }

    [Fact]
    public void Decode_ImmediateEod_ReturnsEmpty()
    {
        var result = AsciiHexFilter.Decode(Encoding.ASCII.GetBytes(">"), MaxOutput);
        Assert.Empty(result);
    }

    [Fact]
    public void Decode_InvalidChar_Throws()
    {
        Assert.ThrowsAny<Exception>(() => AsciiHexFilter.Decode(Encoding.ASCII.GetBytes("48ZZ>"), MaxOutput));
    }
}

public class Ascii85FilterTests
{
    const long MaxOutput = 1 << 20;

    [Fact]
    public void Decode_KnownVector_WithEod()
    {
        var result = Ascii85Filter.Decode(Encoding.ASCII.GetBytes("9jqo^~>"), MaxOutput);
        Assert.Equal("Man ", Encoding.ASCII.GetString(result));
    }

    [Fact]
    public void Decode_ZShortcut_FourZeroBytes()
    {
        var result = Ascii85Filter.Decode(Encoding.ASCII.GetBytes("z~>"), MaxOutput);
        Assert.Equal(new byte[] { 0, 0, 0, 0 }, result);
    }

    [Fact]
    public void Decode_PartialGroup_OneByte()
    {
        var result = Ascii85Filter.Decode(Encoding.ASCII.GetBytes("9`~>"), MaxOutput);
        Assert.Equal(Encoding.ASCII.GetBytes("M"), result);
    }

    [Fact]
    public void Decode_PartialGroup_TwoBytes()
    {
        var result = Ascii85Filter.Decode(Encoding.ASCII.GetBytes("9jn~>"), MaxOutput);
        Assert.Equal(Encoding.ASCII.GetBytes("Ma"), result);
    }

    [Fact]
    public void Decode_PartialGroup_ThreeBytes()
    {
        var result = Ascii85Filter.Decode(Encoding.ASCII.GetBytes("9jqo~>"), MaxOutput);
        Assert.Equal(Encoding.ASCII.GetBytes("Man"), result);
    }

    [Fact]
    public void Decode_IgnoresWhitespace()
    {
        var result = Ascii85Filter.Decode(Encoding.ASCII.GetBytes("9j qo\n^~>"), MaxOutput);
        Assert.Equal("Man ", Encoding.ASCII.GetString(result));
    }

    [Fact]
    public void Decode_ImmediateEod_ReturnsEmpty()
    {
        var result = Ascii85Filter.Decode(Encoding.ASCII.GetBytes("~>"), MaxOutput);
        Assert.Empty(result);
    }

    [Fact]
    public void Decode_CharOutOfRange_Throws()
    {
        Assert.ThrowsAny<Exception>(() => Ascii85Filter.Decode(Encoding.ASCII.GetBytes("9jqv^~>"), MaxOutput));
    }

    [Fact]
    public void Decode_DanglingSingleChar_Throws()
    {
        Assert.ThrowsAny<Exception>(() => Ascii85Filter.Decode(Encoding.ASCII.GetBytes("9jqo^9~>"), MaxOutput));
    }

    [Fact]
    public void AsciiHex_BadDigit_MessageNamesTheOffendingByte()
    {
        var e = Assert.Throws<InvalidDataException>(
            () => AsciiHexFilter.Decode(Encoding.ASCII.GetBytes("48ZZ>"), MaxOutput));

        Assert.Contains("0x5A", e.Message);
    }
}
