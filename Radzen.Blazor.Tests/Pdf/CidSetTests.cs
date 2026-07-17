#nullable enable
using System.Linq;
using Radzen.Documents.Pdf.Fonts.Cff;
using Radzen.Documents.Pdf.Fonts.Sfnt;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

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

        var stream = Type0EmbedSupport.Stream(e.Reader, e.Descriptor["CIDSet"]);
        var bits = Type0EmbedSupport.SetBits(Type0EmbedSupport.DecodeStream(e.Reader, stream));

        Assert.Equal(Enumerable.Range(0, n).ToHashSet(), bits);
    }
}
