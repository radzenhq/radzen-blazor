#nullable enable
using System.Linq;
using Radzen.Documents.Pdf.Fonts.Cff;
using Radzen.Documents.Pdf.Fonts.Sfnt;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// Contract for the /CIDSet stream inside the FontDescriptor: a bitmap whose set
// bits mark the CIDs present in the subset.
//
// Both subsetters renumber into a COMPACT contiguous space 0..N-1 whose members
// are all present in the embedded font program (every glyf gid owns a loca entry;
// every CFF gid owns a charstring), so both cases pin CIDSet == exactly {0..N-1}
// where N is the embedded subset's glyph count (veraPDF 6.2.11.4.2).
public class CidSetTests
{
    private const string LiberationSample = "Radzen Привет";
    private const string NotoSample = "Ab Мир 中产";

    [Fact]
    public void Noto_CidSetMarksExactlyTheCompactCidSpace()
    {
        var font = Type0EmbedSupport.LoadNoto();
        var map = Type0EmbedSupport.BuildMap(font, NotoSample);
        var e = Type0EmbedSupport.Embed(font, map);

        var fontFile = Type0EmbedSupport.Stream(e.Reader, e.Descriptor["FontFile3"]);
        var subset = CffFont.Parse(Type0EmbedSupport.DecodeStream(e.Reader, fontFile));

        // CFF closure is used + notdef (no composite expansion), renumbered 0..N-1.
        var n = subset.GlyphCount;
        Assert.Equal(map.Count + 1, n);

        var stream = Type0EmbedSupport.Stream(e.Reader, e.Descriptor["CIDSet"]);
        var bits = Type0EmbedSupport.SetBits(Type0EmbedSupport.DecodeStream(e.Reader, stream));

        Assert.Equal(Enumerable.Range(0, n).ToHashSet(), bits);
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
