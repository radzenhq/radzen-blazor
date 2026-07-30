#nullable enable
using System.Collections.Generic;
using System.Text;
using Radzen.Documents.Pdf.Objects;
using Xunit;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

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
        var subset = Radzen.Documents.Fonts.Sfnt.SfntFont.Parse(
            Type0EmbedSupport.DecodeStream(e.Reader, fontFile));

        foreach (var ch in LiberationSample)
        {
            var code = Type0EmbedSupport.NewGid(cmap, ch);
            Assert.InRange(code, 0, subset.GlyphCount - 1);
        }

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
