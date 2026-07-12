#nullable enable
using System;
using System.Collections.Generic;
using System.Text;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Radzen.Documents.Pdf.Objects.Filters;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// Defensive-hardening contract for the pure-managed PDF reader. Each malicious
// input is crafted as a byte[] here and must raise a recoverable
// DocumentParseException quickly rather than hanging, exhausting memory, or
// overflowing the stack. Every guard is paired with a positive control proving a
// valid document or stream still parses/decodes unchanged.
public class ReaderHardeningTests
{
    // --- Item 1: ObjectParser recursive descent depth cap -------------------

    [Fact]
    public void DeeplyNestedArray_Throws()
    {
        var bytes = Encoding.Latin1.GetBytes(new string('[', 5000));
        Assert.Throws<DocumentParseException>(() => ObjectParser.Parse(bytes, 0));
    }

    [Fact]
    public void DeeplyNestedDictionary_Throws()
    {
        var builder = new StringBuilder();
        for (var i = 0; i < 5000; i++)
        {
            builder.Append("<< /K ");
        }

        Assert.Throws<DocumentParseException>(() => ObjectParser.Parse(Encoding.Latin1.GetBytes(builder.ToString()), 0));
    }

    [Fact]
    public void NormallyNestedObject_StillParses()
    {
        var root = Assert.IsType<DictionaryObject>(
            ObjectParser.Parse(Encoding.Latin1.GetBytes("<< /A [ 1 [ 2 [ 3 ] ] << /K (v) >> ] >>"), 0));
        var a = Assert.IsType<ArrayObject>(root["A"]);
        Assert.Equal(3, a.Count);
    }

    // --- Item 5 (filters): decoded-size caps, bombs, LZW bounds -------------

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
        // Each {129, X} pair emits 128 bytes (64x expansion); 64 pairs => 8192 bytes.
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
        var data = Encoding.ASCII.GetBytes("aaaaaabbbbccdefggggg");
        Assert.Equal(data, RunLengthFilter.Decode(RunLengthFilter.Encode(data), 1 << 20));
    }

    [Fact]
    public void LzwBomb_ExceedsCap_Throws()
    {
        // clear, literal 'A', then repeatedly reference the newest code (KwKwK) so
        // each step emits an ever-longer run. Output grows quadratically.
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
        // A first code of 300 references a table slot that does not exist yet.
        var bomb = PackLzw(new List<int> { 300 });
        Assert.Throws<DocumentParseException>(() => LzwFilter.Decode(bomb, 1, 1 << 20));
    }

    [Fact]
    public void NormalLzw_Decodes_PositiveControl()
    {
        // clear, 'A'(65), 'B'(66), Eod(257) -> "AB".
        var stream = PackLzw(new List<int> { 256, 65, 66, 257 });
        Assert.Equal(new byte[] { 65, 66 }, LzwFilter.Decode(stream, 1, 1 << 20));
    }

    [Fact]
    public void Ascii85Bomb_ExceedsCap_Throws()
    {
        var bomb = Ascii85Filter.Encode(new byte[64 * 1024]);
        Assert.Throws<DocumentParseException>(() => Ascii85Filter.Decode(bomb, 1024));
    }

    [Fact]
    public void NormalAscii85_Decodes_PositiveControl()
    {
        var data = Encoding.ASCII.GetBytes("Hello ASCII85 world");
        Assert.Equal(data, Ascii85Filter.Decode(Ascii85Filter.Encode(data), 1 << 20));
    }

    // Packs a code sequence into an LZW bit stream matching the filter's decoder:
    // 9-bit codes, MSB first, width grows to 10 when nextCode+1 hits 512.
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
