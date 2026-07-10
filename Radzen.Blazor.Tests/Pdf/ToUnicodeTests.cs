#nullable enable
using System.Collections.Generic;
using Radzen.Documents.Pdf.Objects;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// Contract for the /ToUnicode CMap the embedder attaches to the Type0 font. The
// CMap keys are CIDs (== glyph ids under Identity-H) and map to the source text.
// The stream is parsed back and inverted to recover a Cyrillic+ASCII string (glyf
// font) and a CJK string (CFF font).
public class ToUnicodeTests
{
    private const string LiberationSample = "Radzen Привет";
    private const string NotoSample = "Ab Мир 中产";

    [Fact]
    public void Liberation_ToUnicodeRecoversCyrillicAndAscii()
    {
        var font = Type0EmbedSupport.LoadLiberation();
        var e = Type0EmbedSupport.Embed(font, Type0EmbedSupport.BuildMap(font, LiberationSample));

        var stream = Type0EmbedSupport.Stream(e.Reader, e.Top["ToUnicode"]);
        var bytes = Type0EmbedSupport.DecodeStream(e.Reader, stream);
        var cmap = Type0EmbedSupport.ParseToUnicode(bytes);

        Assert.Equal(LiberationSample, Type0EmbedSupport.Reconstruct(font, cmap, LiberationSample));

        // Cyrillic 'е' (U+0435) and Latin 'e' (U+0065) are distinct glyphs.
        Assert.Equal("е", cmap[font.GetGlyphId('е')]);
        Assert.Equal("e", cmap[font.GetGlyphId('e')]);
        Assert.Equal(" ", cmap[font.GetGlyphId(' ')]);
    }

    [Fact]
    public void Noto_ToUnicodeRecoversCjk()
    {
        var font = Type0EmbedSupport.LoadNoto();
        var e = Type0EmbedSupport.Embed(font, Type0EmbedSupport.BuildMap(font, NotoSample));

        var stream = Type0EmbedSupport.Stream(e.Reader, e.Top["ToUnicode"]);
        var bytes = Type0EmbedSupport.DecodeStream(e.Reader, stream);
        var cmap = Type0EmbedSupport.ParseToUnicode(bytes);

        Assert.Equal(NotoSample, Type0EmbedSupport.Reconstruct(font, cmap, NotoSample));

        Assert.Equal("中", cmap[font.GetGlyphId('中')]); // gid 395
        Assert.Equal("产", cmap[font.GetGlyphId('产')]); // gid 396
        Assert.Equal("М", cmap[font.GetGlyphId('М')]); // Cyrillic, gid 202
    }

    [Fact]
    public void ToUnicode_CarriesCodespaceAndCmapWrapper()
    {
        var font = Type0EmbedSupport.LoadNoto();
        var e = Type0EmbedSupport.Embed(font, Type0EmbedSupport.BuildMap(font, NotoSample));

        var stream = Type0EmbedSupport.Stream(e.Reader, e.Top["ToUnicode"]);
        var text = System.Text.Encoding.Latin1.GetString(Type0EmbedSupport.DecodeStream(e.Reader, stream));

        Assert.Contains("begincodespacerange", text);
        Assert.Contains("endcodespacerange", text);
        Assert.Contains("endcmap", text);
    }
}
