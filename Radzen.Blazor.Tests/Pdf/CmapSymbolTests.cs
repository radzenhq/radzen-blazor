#nullable enable
using System.Collections.Generic;
using Xunit;
using Radzen.Documents.Pdf.Fonts.Sfnt;

namespace Radzen.Blazor.Pdf.Tests;

// Microsoft symbol fonts expose a (3,0) format 4 subtable whose entries live in the
// F000-F0FF private-use area; consumers must retry charCode | 0xF000 on a .notdef miss.
public class CmapSymbolTests
{
    // A whole cmap table with a single encoding record (platform 3 / given encodingId)
    // pointing at one format 4 subtable that maps exactly charCode -> glyph.
    private static byte[] BuildFormat4Cmap(int encodingId, int charCode, int glyph)
    {
        var bytes = new List<byte>();

        void U16(int v) { bytes.Add((byte)(v >> 8)); bytes.Add((byte)v); }
        void U32(long v)
        {
            bytes.Add((byte)(v >> 24));
            bytes.Add((byte)(v >> 16));
            bytes.Add((byte)(v >> 8));
            bytes.Add((byte)v);
        }

        // cmap header
        U16(0);           // version
        U16(1);           // numTables
        U16(3);           // platformID (Windows)
        U16(encodingId);  // encodingID
        U32(12);          // offset to subtable (4 + 8 header bytes)

        // format 4 subtable at offset 12; two segments (mapping + 0xFFFF terminator)
        const int segCount = 2;
        U16(4);               // format
        U16(34);              // length
        U16(0);               // language
        U16(segCount * 2);    // segCountX2
        U16(0);               // searchRange
        U16(0);               // entrySelector
        U16(0);               // rangeShift
        U16(charCode); U16(0xFFFF);   // endCode[]
        U16(0);               // reservedPad
        U16(charCode); U16(0xFFFF);   // startCode[]
        U16(0); U16(1);       // idDelta[] (terminator delta 1 => glyph 0)
        U16(4); U16(0);       // idRangeOffset[] (seg0 points at glyphIdArray[0])
        U16(glyph);           // glyphIdArray[0]

        return bytes.ToArray();
    }

    [Fact]
    public void SymbolSubtable_ResolvesViaPuaRetry()
    {
        var mapper = Cmap.Parse(BuildFormat4Cmap(encodingId: 0, charCode: 0xF041, glyph: 7));
        Assert.Equal(7, (int)mapper.GetGlyphId(0x41));
        Assert.Equal(7, (int)mapper.GetGlyphId(0xF041));
    }

    [Fact]
    public void UnicodeSubtable_NoPuaRetry()
    {
        var mapper = Cmap.Parse(BuildFormat4Cmap(encodingId: 1, charCode: 0xF041, glyph: 7));
        Assert.Equal(0, (int)mapper.GetGlyphId(0x41));
        Assert.Equal(7, (int)mapper.GetGlyphId(0xF041));
    }
}
