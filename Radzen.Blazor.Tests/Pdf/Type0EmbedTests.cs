#nullable enable
using System.Linq;
using System.Text.RegularExpressions;
using Radzen.Documents.Pdf.Fonts.Cff;
using Radzen.Documents.Fonts.Sfnt;
using Radzen.Documents.Pdf.Objects;
using Xunit;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

// Type0 font dictionary shapes per ISO 32000-1 9.7
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
    public void Liberation_WidthsMatchScaledAdvancesKeyedByCompactGid()
    {
        var font = Type0EmbedSupport.LoadLiberation();
        var map = Type0EmbedSupport.BuildMap(font, LiberationSample);
        var e = Type0EmbedSupport.Embed(font, map);

        var stream = Type0EmbedSupport.Stream(e.Reader, e.Top["ToUnicode"]);
        var toUnicode = Type0EmbedSupport.ParseToUnicode(Type0EmbedSupport.DecodeStream(e.Reader, stream));

        var widths = Type0EmbedSupport.ParseWidths(e.Reader, (ArrayObject)e.Reader.Resolve(e.Descendant["W"]));
        foreach (var (gid, cp) in map)
        {
            var newGid = Type0EmbedSupport.NewGid(toUnicode, (char)cp);
            Assert.True(widths.ContainsKey(newGid), $"W missing compact CID {newGid} for U+{cp:X4}");
            Assert.Equal(Type0EmbedSupport.ScaleWidth(font, gid), widths[newGid]);
        }

        Assert.Equal(722, widths[Type0EmbedSupport.NewGid(toUnicode, 'R')]);
        Assert.Equal(278, widths[Type0EmbedSupport.NewGid(toUnicode, ' ')]);
        Assert.Equal(719, widths[Type0EmbedSupport.NewGid(toUnicode, 'П')]);
    }

    [Fact]
    public void Liberation_FontFile2IsCompactSubsetPreservingAdvances()
    {
        var font = Type0EmbedSupport.LoadLiberation();
        var map = Type0EmbedSupport.BuildMap(font, LiberationSample);
        var e = Type0EmbedSupport.Embed(font, map);

        var fontFile = Type0EmbedSupport.Stream(e.Reader, e.Descriptor["FontFile2"]);
        var subset = SfntFont.Parse(Type0EmbedSupport.DecodeStream(e.Reader, fontFile));

        var expected = Type0EmbedSupport.GlyfClosure(font, map.Keys.Select(gid => (int)gid));
        Assert.Equal(expected.Count, (int)subset.GlyphCount);
        Assert.True(subset.GlyphCount < font.GlyphCount, "subset must be compact");

        var stream = Type0EmbedSupport.Stream(e.Reader, e.Top["ToUnicode"]);
        var toUnicode = Type0EmbedSupport.ParseToUnicode(Type0EmbedSupport.DecodeStream(e.Reader, stream));

        foreach (var (gid, cp) in map)
        {
            var newGid = Type0EmbedSupport.NewGid(toUnicode, (char)cp);
            Assert.InRange(newGid, 0, subset.GlyphCount - 1);
            Assert.Equal(font.GetAdvanceWidth((ushort)gid), subset.GetAdvanceWidth((ushort)newGid));
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

        var fontFile = Type0EmbedSupport.Stream(e.Reader, e.Descriptor["FontFile3"]);
        var cff = CffFont.Parse(Type0EmbedSupport.DecodeStream(e.Reader, fontFile));

        var stream = Type0EmbedSupport.Stream(e.Reader, e.Top["ToUnicode"]);
        var toUnicode = Type0EmbedSupport.ParseToUnicode(Type0EmbedSupport.DecodeStream(e.Reader, stream));

        var widths = Type0EmbedSupport.ParseWidths(e.Reader, (ArrayObject)e.Reader.Resolve(e.Descendant["W"]));
        foreach (var cid in widths.Keys)
        {
            Assert.InRange(cid, 0, cff.GlyphCount - 1);
        }

        foreach (var (gid, cp) in map)
        {
            var newCid = Type0EmbedSupport.NewGid(toUnicode, (char)cp);
            Assert.True(widths.ContainsKey(newCid), $"W missing compact CID {newCid}");
            Assert.Equal(Type0EmbedSupport.ScaleWidth(font, gid), widths[newCid]);
        }

        Assert.Equal(1000, widths[Type0EmbedSupport.NewGid(toUnicode, '中')]);
        Assert.Equal(812, widths[Type0EmbedSupport.NewGid(toUnicode, 'М')]);
    }

    [Fact]
    public void Noto_FontFile3IsCidKeyedCompactSubsetPreservingAdvances()
    {
        var font = Type0EmbedSupport.LoadNoto();
        var map = Type0EmbedSupport.BuildMap(font, NotoSample);
        var e = Type0EmbedSupport.Embed(font, map);

        var fontFile = Type0EmbedSupport.Stream(e.Reader, e.Descriptor["FontFile3"]);
        var cff = CffFont.Parse(Type0EmbedSupport.DecodeStream(e.Reader, fontFile));

        Assert.True(cff.IsCidKeyed);

        Assert.Equal(map.Count + 1, cff.GlyphCount);
        for (var gid = 0; gid < cff.GlyphCount; gid++)
        {
            Assert.Equal(gid, cff.Charset[gid]);
        }

        var stream = Type0EmbedSupport.Stream(e.Reader, e.Top["ToUnicode"]);
        var toUnicode = Type0EmbedSupport.ParseToUnicode(Type0EmbedSupport.DecodeStream(e.Reader, stream));

        foreach (var (gid, cp) in map)
        {
            var newCid = Type0EmbedSupport.NewGid(toUnicode, (char)cp);
            Assert.InRange(newCid, 0, cff.GlyphCount - 1);
            Assert.Equal(font.GetAdvanceWidth((ushort)gid), cff.GetAdvanceWidth(newCid));
        }
    }
}
