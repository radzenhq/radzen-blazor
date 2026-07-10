#nullable enable
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Radzen.Documents.Pdf.Fonts.Cff;
using Radzen.Documents.Pdf.Fonts.Sfnt;
using Radzen.Documents.Pdf.Objects;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// Contract for Type0FontEmbedder: given a loaded face and a used-glyph -> Unicode
// map it writes a Type0 font object graph via the merged object model. The graph is
// serialized with DocumentWriter, reloaded with DocumentReader, and every dictionary
// shape (ISO 32000-1 9.7) is asserted. Widths are derived from the fixture and the
// embedded FontFile2/FontFile3 is re-parsed with F2/F4a to prove it is a valid subset
// that preserves the used glyphs and their advances.
//
// glyf fixture: LiberationSans-Regular.ttf, upem 2048, sample "Radzen Привет".
//   'R' gid 53 adv 1479 -> 722 ; ' ' gid 3 adv 569 -> 278 ; 'П' gid 976 -> 719.
// CFF  fixture: NotoSansSC-Subset.otf (ROS Adobe-Identity-0), upem 1000,
//   sample "Ab Мир 中产". '中' gid 395 adv 1000 ; 'М' gid 202 adv 812.
public class Type0EmbedTests
{
    private const string LiberationSample = "Radzen Привет";
    private const string NotoSample = "Ab Мир 中产";

    private static readonly Regex SubsetTag = new("^[A-Z]{6}\\+.+$");

    [Fact]
    public void Liberation_TopLevelType0Dictionary()
    {
        var font = Type0EmbedSupport.LoadLiberation();
        var e = Type0EmbedSupport.Embed(font, Type0EmbedSupport.BuildMap(font, LiberationSample));

        Assert.Equal("Font", Type0EmbedSupport.Name(e.Reader, e.Top, "Type"));
        Assert.Equal("Type0", Type0EmbedSupport.Name(e.Reader, e.Top, "Subtype"));
        Assert.Equal("Identity-H", Type0EmbedSupport.Name(e.Reader, e.Top, "Encoding"));

        var baseFont = Type0EmbedSupport.Name(e.Reader, e.Top, "BaseFont");
        Assert.Matches(SubsetTag, baseFont);

        Assert.True(e.Top.ContainsKey("ToUnicode"));
        Assert.IsType<StreamObject>(e.Reader.Resolve(e.Top["ToUnicode"]));
    }

    [Fact]
    public void Liberation_DescendantIsCidFontType2()
    {
        var font = Type0EmbedSupport.LoadLiberation();
        var e = Type0EmbedSupport.Embed(font, Type0EmbedSupport.BuildMap(font, LiberationSample));

        Assert.Equal("Font", Type0EmbedSupport.Name(e.Reader, e.Descendant, "Type"));
        Assert.Equal("CIDFontType2", Type0EmbedSupport.Name(e.Reader, e.Descendant, "Subtype"));
        Assert.Equal("Identity", Type0EmbedSupport.Name(e.Reader, e.Descendant, "CIDToGIDMap"));

        var topBaseFont = Type0EmbedSupport.Name(e.Reader, e.Top, "BaseFont");
        Assert.Equal(topBaseFont, Type0EmbedSupport.Name(e.Reader, e.Descendant, "BaseFont"));

        var info = Type0EmbedSupport.Dict(e.Reader, e.Descendant["CIDSystemInfo"]);
        Assert.Equal("Adobe", Type0EmbedSupport.Str(e.Reader, info, "Registry"));
        Assert.Equal("Identity", Type0EmbedSupport.Str(e.Reader, info, "Ordering"));
        Assert.Equal(0, Type0EmbedSupport.Num(e.Reader, info, "Supplement"));

        Assert.True(e.Descendant.ContainsKey("DW"));
        Assert.True(e.Descendant.ContainsKey("W"));
    }

    [Fact]
    public void Liberation_DescriptorAndFontFile2()
    {
        var font = Type0EmbedSupport.LoadLiberation();
        var e = Type0EmbedSupport.Embed(font, Type0EmbedSupport.BuildMap(font, LiberationSample));
        var d = e.Descriptor;

        Assert.NotEqual(0, Type0EmbedSupport.Num(e.Reader, d, "Flags"));
        var bbox = (ArrayObject)e.Reader.Resolve(d["FontBBox"]);
        Assert.Equal(4, bbox.Count);
        Assert.True(Type0EmbedSupport.Num(e.Reader, d, "Ascent") > 0);
        Assert.True(Type0EmbedSupport.Num(e.Reader, d, "Descent") < 0);
        Assert.True(Type0EmbedSupport.Num(e.Reader, d, "CapHeight") > 0);
        Assert.Equal(0, Type0EmbedSupport.Num(e.Reader, d, "ItalicAngle"));
        Assert.True(Type0EmbedSupport.Num(e.Reader, d, "StemV") > 0);

        Assert.True(d.ContainsKey("FontFile2"));
        Assert.False(d.ContainsKey("FontFile3"));
        Assert.IsType<StreamObject>(e.Reader.Resolve(d["FontFile2"]));
    }

    [Fact]
    public void Liberation_WidthsMatchScaledAdvances()
    {
        var font = Type0EmbedSupport.LoadLiberation();
        var map = Type0EmbedSupport.BuildMap(font, LiberationSample);
        var e = Type0EmbedSupport.Embed(font, map);

        var widths = Type0EmbedSupport.ParseWidths(e.Reader, (ArrayObject)e.Reader.Resolve(e.Descendant["W"]));
        foreach (var gid in map.Keys)
        {
            Assert.True(widths.ContainsKey(gid), $"W missing CID {gid}");
            Assert.Equal(Type0EmbedSupport.ScaleWidth(font, gid), widths[gid]);
        }

        Assert.Equal(722, widths[53]);
        Assert.Equal(278, widths[3]);
        Assert.Equal(719, widths[976]);
    }

    [Fact]
    public void Liberation_FontFile2IsSubsetPreservingGlyphs()
    {
        var font = Type0EmbedSupport.LoadLiberation();
        var map = Type0EmbedSupport.BuildMap(font, LiberationSample);
        var e = Type0EmbedSupport.Embed(font, map);

        var fontFile = Type0EmbedSupport.Stream(e.Reader, e.Descriptor["FontFile2"]);
        var subset = SfntFont.Parse(Type0EmbedSupport.DecodeStream(e.Reader, fontFile));

        Assert.Equal(font.GlyphCount, subset.GlyphCount);
        foreach (var (gid, cp) in map)
        {
            Assert.Equal(gid, subset.GetGlyphId(cp));
            Assert.Equal(font.GetAdvanceWidth((ushort)gid), subset.GetAdvanceWidth((ushort)gid));
        }
    }

    [Fact]
    public void Noto_TopLevelAndDescendantAreCidFontType0()
    {
        var font = Type0EmbedSupport.LoadNoto();
        var e = Type0EmbedSupport.Embed(font, Type0EmbedSupport.BuildMap(font, NotoSample));

        Assert.Equal("Type0", Type0EmbedSupport.Name(e.Reader, e.Top, "Subtype"));
        Assert.Equal("Identity-H", Type0EmbedSupport.Name(e.Reader, e.Top, "Encoding"));
        Assert.Matches(SubsetTag, Type0EmbedSupport.Name(e.Reader, e.Top, "BaseFont"));

        Assert.Equal("CIDFontType0", Type0EmbedSupport.Name(e.Reader, e.Descendant, "Subtype"));

        var info = Type0EmbedSupport.Dict(e.Reader, e.Descendant["CIDSystemInfo"]);
        Assert.Equal("Adobe", Type0EmbedSupport.Str(e.Reader, info, "Registry"));
        Assert.Equal("Identity", Type0EmbedSupport.Str(e.Reader, info, "Ordering"));

        Assert.False(e.Descriptor.ContainsKey("FontFile2"));
        var fontFile = Type0EmbedSupport.Stream(e.Reader, e.Descriptor["FontFile3"]);
        Assert.Equal("CIDFontType0C", ((NameObject)fontFile.Dictionary["Subtype"]).Value);
    }

    [Fact]
    public void Noto_WidthsMatchAdvances()
    {
        var font = Type0EmbedSupport.LoadNoto();
        var map = Type0EmbedSupport.BuildMap(font, NotoSample);
        var e = Type0EmbedSupport.Embed(font, map);

        var widths = Type0EmbedSupport.ParseWidths(e.Reader, (ArrayObject)e.Reader.Resolve(e.Descendant["W"]));
        foreach (var gid in map.Keys)
        {
            Assert.True(widths.ContainsKey(gid), $"W missing CID {gid}");
            Assert.Equal(Type0EmbedSupport.ScaleWidth(font, gid), widths[gid]);
        }

        Assert.Equal(1000, widths[395]);
        Assert.Equal(812, widths[202]);
    }

    [Fact]
    public void Noto_FontFile3IsCidKeyedSubsetPreservingAdvances()
    {
        var font = Type0EmbedSupport.LoadNoto();
        var map = Type0EmbedSupport.BuildMap(font, NotoSample);
        var e = Type0EmbedSupport.Embed(font, map);

        var fontFile = Type0EmbedSupport.Stream(e.Reader, e.Descriptor["FontFile3"]);
        var cff = CffFont.Parse(Type0EmbedSupport.DecodeStream(e.Reader, fontFile));

        Assert.True(cff.IsCidKeyed);

        // The CFF subset renumbers glyphs; charset preserves CID == original gid.
        var localOfCid = new Dictionary<int, int>();
        for (var local = 0; local < cff.GlyphCount; local++)
        {
            localOfCid[cff.Charset[local]] = local;
        }

        foreach (var gid in map.Keys)
        {
            Assert.True(localOfCid.ContainsKey(gid), $"subset CFF missing CID {gid}");
            var local = localOfCid[gid];
            Assert.Equal(font.GetAdvanceWidth((ushort)gid), cff.GetAdvanceWidth(local));
        }
    }
}
