#nullable enable
using System.Collections.Generic;
using System.Linq;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

public class SharedGlyphToUnicodeTests
{
    private const char SoftHyphen = '\u00AD';
    private const char Hyphen = '-';

    private static ushort SharedGid()
    {
        var font = Type0EmbedSupport.LoadNoto();
        var a = font.GetGlyphId(SoftHyphen);
        var b = font.GetGlyphId(Hyphen);
        Assert.NotEqual(0, a);
        Assert.Equal(a, b);
        return a;
    }

    private static ICollection<string> ToUnicodeValues(string text)
    {
        var document = new Document();
        BuildTestSupport.RegisterCjk(document);
        BuildTestSupport.AddText(document.Sections.Add(), text, BuildTestSupport.Cjk);

        var reader = BuildTestSupport.Read(document);
        var font = Assert.Single(BuildTestSupport.Type0Fonts(reader));
        var stream = (StreamObject)reader.Resolve(font["ToUnicode"]);
        var cmap = Type0EmbedSupport.ParseToUnicode(Type0EmbedSupport.DecodeStream(reader, stream));
        return cmap.Values;
    }

    [Fact]
    public void LeadingSoftHyphen_IsNotDrawnBeforeSharedHyphenGlyph()
    {
        _ = SharedGid();
        var values = ToUnicodeValues($"{SoftHyphen}{Hyphen}");

        Assert.Contains(Hyphen.ToString(), values);
        Assert.DoesNotContain(SoftHyphen.ToString(), values);
    }

    [Fact]
    public void SharedGlyph_MapsToFirstDrawnCodepoint_HyphenBeforeSoftHyphen()
    {
        _ = SharedGid();
        var values = ToUnicodeValues($"{Hyphen}{SoftHyphen}");

        Assert.Contains(Hyphen.ToString(), values);
        Assert.DoesNotContain(SoftHyphen.ToString(), values);
    }
}
