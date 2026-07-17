#nullable enable
using Radzen.Documents.Pdf.Objects;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

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

        Assert.True(
            wArray.Count < 2 * map.Count,
            $"W array has {wArray.Count} elements; the per-pair form would have {2 * map.Count}");
    }
}
