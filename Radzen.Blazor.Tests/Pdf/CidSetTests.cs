#nullable enable
using System.Collections.Generic;
using Radzen.Documents.Pdf.Objects;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// Contract for the /CIDSet stream inside the FontDescriptor: a bitmap whose set
// bits mark the CIDs present in the subset. Under Identity-H CID == glyph id.
//
// The CID-keyed CFF subsetter closes over exactly (requested gids) + {0} with no
// component expansion, so the Noto case pins an exact bit set. The TrueType glyf
// subsetter also pulls in composite component glyphs, so the Liberation case only
// pins that every used gid is marked (and an unused gid is not).
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
    public void Liberation_CidSetMarksEveryUsedGlyph()
    {
        var font = Type0EmbedSupport.LoadLiberation();
        var map = Type0EmbedSupport.BuildMap(font, LiberationSample);
        var e = Type0EmbedSupport.Embed(font, map);

        var stream = Type0EmbedSupport.Stream(e.Reader, e.Descriptor["CIDSet"]);
        var cidSet = Type0EmbedSupport.DecodeStream(e.Reader, stream);

        foreach (var gid in map.Keys)
        {
            Assert.True(Type0EmbedSupport.CidBit(cidSet, gid), $"CIDSet missing gid {gid}");
        }

        Assert.False(Type0EmbedSupport.CidBit(cidSet, 2000));
    }
}
