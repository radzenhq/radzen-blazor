#nullable enable
using System;
using System.Collections.Generic;
using System.Text;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Radzen.Documents.Pdf.Objects.Filters;
using Xunit;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

public class ReaderHardeningTests
{

    [Fact]
    public void DeeplyNestedArray_Throws()
    {
        var bytes = Encoding.Latin1.GetBytes(new string('[', 5000));
        Assert.Throws<DocumentParseException>(() => ObjectParser.Parse(bytes, 0));
    }

    [Fact]
    public void DeeplyNestedDictionary_Throws()
    {
        var document = new StringBuilder();
        for (var i = 0; i < 5000; i++)
        {
            document.Append("<< /K ");
        }

        Assert.Throws<DocumentParseException>(() => ObjectParser.Parse(Encoding.Latin1.GetBytes(document.ToString()), 0));
    }

    [Fact]
    public void NormallyNestedObject_StillParses()
    {
        var root = Assert.IsType<DictionaryObject>(
            ObjectParser.Parse(Encoding.Latin1.GetBytes("<< /A [ 1 [ 2 [ 3 ] ] << /K (v) >> ] >>"), 0));
        var a = Assert.IsType<ArrayObject>(root["A"]);
        Assert.Equal(3, a.Count);
    }

    [Theory]
    [InlineData("+")]
    [InlineData("-")]
    public void StartXrefLoneSignThrowsDocumentParseException(string sign)
    {
        var bytes = Encoding.ASCII.GetBytes("%PDF-1.7\nstartxref\n" + sign + "\n%%EOF");

        Assert.Throws<DocumentParseException>(() => PdfBytes.FindStartXref(bytes));
    }

    [Theory]
    [InlineData("+12", 12L)]
    [InlineData("-12", -12L)]
    public void StartXrefSignedIntegerStillParses(string value, long expected)
    {
        var bytes = Encoding.ASCII.GetBytes("%PDF-1.7\nstartxref\n" + value + "\n%%EOF");

        Assert.Equal(expected, PdfBytes.FindStartXref(bytes));
    }


    [Fact]
    public void FlateBomb_ExceedsCap_Throws()
    {
        var bomb = FlateFilter.Encode(new byte[4 * 1024 * 1024]);
        Assert.Throws<DocumentParseException>(() => FlateFilter.Decode(bomb, 64 * 1024));
    }

    [Fact]
    public void NormalFlate_Decodes_PositiveControl()
    {
        var data = Encoding.ASCII.GetBytes("The quick brown fox jumps over the lazy dog.");
        Assert.Equal(data, FlateFilter.Decode(FlateFilter.Encode(data), 1 << 20));
    }

    [Fact]
    public void RunLengthBomb_ExceedsCap_Throws()
    {
        var bomb = new byte[128];
        for (var i = 0; i < bomb.Length; i += 2)
        {
            bomb[i] = 129;
            bomb[i + 1] = (byte)'A';
        }

        Assert.Throws<DocumentParseException>(() => RunLengthFilter.Decode(bomb, 1024));
    }

    [Fact]
    public void NormalRunLength_Decodes_PositiveControl()
    {
        var encoded = new byte[]
        {
            0xFB, 0x61, 0xFD, 0x62, 0xFF, 0x63, 0x02, 0x64, 0x65, 0x66, 0xFC, 0x67, 0x80,
        };

        Assert.Equal(
            Encoding.ASCII.GetBytes("aaaaaabbbbccdefggggg"),
            RunLengthFilter.Decode(encoded, 1 << 20));
    }

    [Fact]
    public void LzwBomb_ExceedsCap_Throws()
    {
        var codes = new List<int> { 256, 65 };
        for (var code = 258; code < 330; code++)
        {
            codes.Add(code);
        }

        var bomb = PackLzw(codes);
        Assert.Throws<DocumentParseException>(() => LzwFilter.Decode(bomb, 1, 1024));
    }

    [Fact]
    public void LzwOutOfBoundsCode_Throws()
    {
        var bomb = PackLzw(new List<int> { 300 });
        Assert.Throws<DocumentParseException>(() => LzwFilter.Decode(bomb, 1, 1 << 20));
    }

    [Fact]
    public void NormalLzw_Decodes_PositiveControl()
    {
        var stream = PackLzw(new List<int> { 256, 65, 66, 257 });
        Assert.Equal(new byte[] { 65, 66 }, LzwFilter.Decode(stream, 1, 1 << 20));
    }

    [Fact]
    public void Ascii85Bomb_ExceedsCap_Throws()
    {
        var bomb = Encoding.ASCII.GetBytes(new string('z', 16 * 1024) + "~>");
        Assert.Throws<DocumentParseException>(() => Ascii85Filter.Decode(bomb, 1024));
    }

    [Fact]
    public void NormalAscii85_Decodes_PositiveControl()
    {
        var encoded = Encoding.ASCII.GetBytes("87cURD]h>E6V0j/2'@*]Ebo7~>");

        Assert.Equal(
            Encoding.ASCII.GetBytes("Hello ASCII85 world"),
            Ascii85Filter.Decode(encoded, 1 << 20));
    }

    private static byte[] PackLzw(List<int> codes)
    {
        var bits = new List<bool>();
        var width = 9;
        var nextCode = 258;
        foreach (var code in codes)
        {
            for (var i = width - 1; i >= 0; i--)
            {
                bits.Add(((code >> i) & 1) != 0);
            }

            if (code == 256)
            {
                width = 9;
                nextCode = 258;
            }
            else if (code != 257)
            {
                nextCode++;
                if (nextCode + 1 == (1 << width) && width < 12)
                {
                    width++;
                }
            }
        }

        var bytes = new byte[(bits.Count + 7) / 8];
        for (var i = 0; i < bits.Count; i++)
        {
            if (bits[i])
            {
                bytes[i >> 3] |= (byte)(1 << (7 - (i & 7)));
            }
        }

        return bytes;
    }
}
