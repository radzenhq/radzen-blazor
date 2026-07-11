#nullable enable
using Radzen.Documents.Pdf.Objects;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// The Type0 /W array packs the contiguous compact-CID space into "startCid [w0 w1 ...]"
// runs instead of one "cid [w]" pair per glyph. Every CID must still report the correct
// width, and the run form must be strictly smaller than the per-pair form.
public class WidthArrayCompactionTests
{
    private const string Sample = "Radzen Привет";

    [Fact]
    public void Widths_AreCorrectPerCidAndMoreCompactThanPerPairForm()
    {
        var font = Type0EmbedSupport.LoadLiberation();
        var map = Type0EmbedSupport.BuildMap(font, Sample);
        var e = Type0EmbedSupport.Embed(font, map);

        var toUnicode = Type0EmbedSupport.ParseToUnicode(
            Type0EmbedSupport.DecodeStream(e.Reader, Type0EmbedSupport.Stream(e.Reader, e.Top["ToUnicode"])));

        var wArray = (ArrayObject)e.Reader.Resolve(e.Descendant["W"]);
        var widths = Type0EmbedSupport.ParseWidths(e.Reader, wArray);

        foreach (var (gid, cp) in map)
        {
            var cid = Type0EmbedSupport.NewGid(toUnicode, (char)cp);
            Assert.True(widths.ContainsKey(cid), $"W missing compact CID {cid}");
            Assert.Equal(Type0EmbedSupport.ScaleWidth(font, gid), widths[cid]);
        }

        // Per-pair form would be exactly two array elements per described glyph.
        Assert.True(
            wArray.Count < 2 * map.Count,
            $"W array has {wArray.Count} elements; the per-pair form would have {2 * map.Count}");
    }
}
