#nullable enable
using System.Collections.Generic;
using System.Linq;
using Radzen.Documents.Pdf.Fonts.Sfnt;
using Radzen.Documents.Pdf.Objects;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// Contract for the /CIDSet stream inside the FontDescriptor: a bitmap whose set
// bits mark the CIDs present in the subset.
//
// The CID-keyed CFF subsetter closes over exactly (requested gids) + {0} with no
// component expansion and keeps CID == original gid, so the Noto case pins an
// exact bit set. The TrueType glyf subsetter renumbers into a COMPACT contiguous
// glyph space 0..N-1 (used + composite closure + notdef), so the Liberation case
// pins CIDSet == exactly {0..N-1} where N is the embedded subset's numGlyphs.
public class CidSetTests
{
    private const string LiberationSample = "Radzen Привет";
    private const string NotoSample = "Ab Мир 中产";

    [Fact]
    public void Noto_CidSetMarksExactlyUsedGlyphs()
    {
        var font = Type0EmbedSupport.LoadNoto();
        var map = Type0EmbedSupport.BuildMap(font, NotoSample);
        var e = Type0EmbedSupport.Embed(font, map);

        var stream = Type0EmbedSupport.Stream(e.Reader, e.Descriptor["CIDSet"]);
        var bits = Type0EmbedSupport.SetBits(Type0EmbedSupport.DecodeStream(e.Reader, stream));

        foreach (var gid in map.Keys)
        {
            Assert.Contains(gid, bits);
        }

        // The subset closure is the used gids plus notdef; notdef is optional.
        var allowed = new HashSet<int> { 0 };
        foreach (var gid in map.Keys)
        {
            allowed.Add(gid);
        }

        Assert.Subset(allowed, bits);

        // A glyph well outside the sample must not be marked.
        Assert.DoesNotContain(500, bits);
    }

    [Fact]
    public void Liberation_CidSetMarksExactlyTheCompactGlyphSpace()
    {
        var font = Type0EmbedSupport.LoadLiberation();
        var map = Type0EmbedSupport.BuildMap(font, LiberationSample + " ");
        var e = Type0EmbedSupport.Embed(font, map);

        var fontFile = Type0EmbedSupport.Stream(e.Reader, e.Descriptor["FontFile2"]);
        var subset = SfntFont.Parse(Type0EmbedSupport.DecodeStream(e.Reader, fontFile));

        var expected = Type0EmbedSupport.GlyfClosure(font, map.Keys.Select(gid => (int)gid));
        var n = (int)subset.GlyphCount;
        Assert.Equal(expected.Count, n);
        Assert.True(n < font.GlyphCount, "subset must be compact");

        // Every compact gid 0..N-1 is present in the font program (including the
        // empty-outline space glyph, which owns a loca entry), so the CIDSet marks
        // exactly the compact space with no gaps and no extra bits.
        var stream = Type0EmbedSupport.Stream(e.Reader, e.Descriptor["CIDSet"]);
        var bits = Type0EmbedSupport.SetBits(Type0EmbedSupport.DecodeStream(e.Reader, stream));

        Assert.Equal(Enumerable.Range(0, n).ToHashSet(), bits);
    }
}
