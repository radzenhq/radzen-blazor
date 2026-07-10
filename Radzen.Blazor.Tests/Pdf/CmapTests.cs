#nullable enable
using System.Collections.Generic;
using Xunit;
using Radzen.Documents.Pdf.Fonts.Sfnt;

namespace Radzen.Blazor.Pdf.Tests;

// No committed fixture contains a cmap format 12 (segmented coverage) subtable:
// both Liberation (.ttf/.ttc) and NotoSansSC-Subset.otf expose only format 4
// (verified via fonttools: every subtable's .format is 4 or 6). Format 12 is
// therefore pinned against a hand-built cmap table fed to the lower-level
// Cmap.Parse(byte[]) mapper. Magic offsets/lengths below were computed by hand
// from the OpenType cmap spec, not from a fixture.
public class CmapTests
{
    // A whole cmap table (starting at the version field) with a single
    // platform 3 / encoding 10 record pointing at one format 12 subtable:
    //   U+1F600            -> glyph 5
    //   U+1F601..U+1F603   -> glyphs 6, 7, 8 (sequential)
    private static byte[] BuildFormat12Cmap()
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
        U16(0);   // version
        U16(1);   // numTables
        // encoding record
        U16(3);   // platformID (Windows)
        U16(10);  // encodingID (Unicode full repertoire)
        U32(12);  // offset to subtable (4 + 8 header bytes)

        // format 12 subtable at offset 12
        const int numGroups = 2;
        U16(12);              // format
        U16(0);               // reserved
        U32(16 + numGroups * 12); // length: 16-byte header + groups
        U32(0);               // language
        U32(numGroups);       // numGroups
        // group 1: single mapping
        U32(0x1F600); U32(0x1F600); U32(5);
        // group 2: sequential range
        U32(0x1F601); U32(0x1F603); U32(6);

        return bytes.ToArray();
    }

    [Fact]
    public void Format12_SingleMapping()
    {
        var mapper = Cmap.Parse(BuildFormat12Cmap());
        Assert.Equal(5, (int)mapper.GetGlyphId(0x1F600));
    }

    [Fact]
    public void Format12_SequentialRange()
    {
        var mapper = Cmap.Parse(BuildFormat12Cmap());
        Assert.Equal(6, (int)mapper.GetGlyphId(0x1F601));
        Assert.Equal(7, (int)mapper.GetGlyphId(0x1F602));
        Assert.Equal(8, (int)mapper.GetGlyphId(0x1F603));
    }

    [Fact]
    public void Format12_UnmappedReturnsNotdef()
    {
        var mapper = Cmap.Parse(BuildFormat12Cmap());
        Assert.Equal(0, (int)mapper.GetGlyphId(0x1F610));
        Assert.Equal(0, (int)mapper.GetGlyphId('A'));
    }
}
