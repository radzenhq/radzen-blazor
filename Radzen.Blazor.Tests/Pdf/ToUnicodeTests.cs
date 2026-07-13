#nullable enable
using System.Collections.Generic;
using System.Text;
using Radzen.Documents.Pdf.Objects;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// Contract for the /ToUnicode CMap the embedder attaches to the Type0 font. The
// CMap keys are the CIDs used in the content stream and map to the source text.
// On BOTH paths the CIDs are COMPACT renumbered ids (all < the embedded subset's
// glyph count). The stream is parsed back and inverted to recover a
// Cyrillic+ASCII string (glyf font) and a CJK string (CFF font).
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

        var fontFile = Type0EmbedSupport.Stream(e.Reader, e.Descriptor["FontFile2"]);
        var subset = Radzen.Documents.Pdf.Fonts.Sfnt.SfntFont.Parse(
            Type0EmbedSupport.DecodeStream(e.Reader, fontFile));

        // Every character of the sample is recoverable from exactly one CMap code,
        // and every code lies in the compact glyph space of the embedded subset.
        foreach (var ch in LiberationSample)
        {
            var code = Type0EmbedSupport.NewGid(cmap, ch);
            Assert.InRange(code, 0, subset.GlyphCount - 1);
        }

        // Cyrillic 'е' (U+0435) and Latin 'e' (U+0065) are distinct glyphs.
        Assert.NotEqual(
            Type0EmbedSupport.NewGid(cmap, 'е'),
            Type0EmbedSupport.NewGid(cmap, 'e'));
    }

    [Fact]
    public void Noto_ToUnicodeRecoversCjk()
    {
        var font = Type0EmbedSupport.LoadNoto();
        var e = Type0EmbedSupport.Embed(font, Type0EmbedSupport.BuildMap(font, NotoSample));

        var stream = Type0EmbedSupport.Stream(e.Reader, e.Top["ToUnicode"]);
        var bytes = Type0EmbedSupport.DecodeStream(e.Reader, stream);
        var cmap = Type0EmbedSupport.ParseToUnicode(bytes);

        var fontFile = Type0EmbedSupport.Stream(e.Reader, e.Descriptor["FontFile3"]);
        var subset = Radzen.Documents.Pdf.Fonts.Cff.CffFont.Parse(
            Type0EmbedSupport.DecodeStream(e.Reader, fontFile));

        // Every character of the sample is recoverable from exactly one CMap code,
        // and every code lies in the compact CID space of the embedded CFF subset.
        foreach (var ch in NotoSample)
        {
            var code = Type0EmbedSupport.NewGid(cmap, ch);
            Assert.InRange(code, 0, subset.GlyphCount - 1);
        }
    }

    [Fact]
    public void ToUnicode_CarriesCodespaceAndCmapWrapper()
    {
        var font = Type0EmbedSupport.LoadNoto();
        var e = Type0EmbedSupport.Embed(font, Type0EmbedSupport.BuildMap(font, NotoSample));

        var stream = Type0EmbedSupport.Stream(e.Reader, e.Top["ToUnicode"]);
        var text = Encoding.Latin1.GetString(Type0EmbedSupport.DecodeStream(e.Reader, stream));

        Assert.Contains("begincodespacerange", text);
        Assert.Contains("endcodespacerange", text);
        Assert.Contains("endcmap", text);
    }
}
