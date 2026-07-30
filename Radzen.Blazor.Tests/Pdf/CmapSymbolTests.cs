#nullable enable
using System.Collections.Generic;
using Xunit;
using Radzen.Documents.Fonts.Sfnt;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

public class CmapSymbolTests
{
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

        U16(0);
        U16(1);
        U16(3);
        U16(encodingId);
        U32(12);

        const int segCount = 2;
        U16(4);
        U16(34);
        U16(0);
        U16(segCount * 2);
        U16(0);
        U16(0);
        U16(0);
        U16(charCode); U16(0xFFFF);
        U16(0);
        U16(charCode); U16(0xFFFF);
        U16(0); U16(1);
        U16(4); U16(0);
        U16(glyph);

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
