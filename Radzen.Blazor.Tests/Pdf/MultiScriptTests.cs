#nullable enable
using System;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// L5 test 1 + 3: the first true end-to-end round trip. Author English (Liberation),
// Bulgarian/Cyrillic (Liberation) and CJK (Noto) paragraphs, Build -> save -> reload,
// and prove Type0 embedding + content emission + extraction agree. The subset prefix
// on the embedded Type0 /BaseFont is pinned to the standard ^[A-Z]{6}\+ tag.
public class MultiScriptTests
{
    private const string English = "Invoice";
    private const string Bulgarian = "Здравей";
    private const string Chinese = "中产";

    private static DocumentBuilder AuthorThreeScripts()
    {
        // Coverage guard: every code point must exist in its fixture face so a failing
        // round trip means a wiring bug, not a missing glyph.
        var liberation = Type0EmbedSupport.LoadLiberation();
        foreach (var c in English + Bulgarian)
        {
            Assert.NotEqual(0, liberation.GetGlyphId(c));
        }

        var noto = Type0EmbedSupport.LoadNoto();
        foreach (var c in Chinese)
        {
            Assert.NotEqual(0, noto.GetGlyphId(c));
        }

        var builder = new DocumentBuilder();
        BuildTestSupport.RegisterLatin(builder);
        BuildTestSupport.RegisterCjk(builder);

        var section = builder.Sections.Add();
        BuildTestSupport.AddText(section, English, BuildTestSupport.Latin);
        BuildTestSupport.AddText(section, Bulgarian, BuildTestSupport.Latin);
        BuildTestSupport.AddText(section, Chinese, BuildTestSupport.Cjk);
        return builder;
    }

    [Fact]
    public void ThreeScripts_RoundTripThroughExtractText()
    {
        var reloaded = BuildTestSupport.Reload(AuthorThreeScripts());
        var text = reloaded.ExtractText();

        var en = text.IndexOf(English, StringComparison.Ordinal);
        var bg = text.IndexOf(Bulgarian, StringComparison.Ordinal);
        var cjk = text.IndexOf(Chinese, StringComparison.Ordinal);

        Assert.True(en >= 0, "English line present");
        Assert.True(bg >= 0, "Cyrillic line present");
        Assert.True(cjk >= 0, "CJK line present");
        Assert.True(en < bg, "English precedes Cyrillic (top to bottom)");
        Assert.True(bg < cjk, "Cyrillic precedes CJK (top to bottom)");
    }

    [Fact]
    public void ThreeScripts_ProduceOneSinglePage()
    {
        var reloaded = BuildTestSupport.Reload(AuthorThreeScripts());
        Assert.Equal(1, reloaded.Pages.Count);
    }

    [Fact]
    public void EmbeddedType0Fonts_UseSubsetPrefixedBaseFont()
    {
        var reader = BuildTestSupport.Read(AuthorThreeScripts());
        var type0 = BuildTestSupport.Type0Fonts(reader);

        // Both registered sfnt families embed as Type0/CID.
        Assert.True(type0.Count >= 2, "both registered fonts embed as Type0");
        foreach (var font in type0)
        {
            var baseFont = BuildTestSupport.Name(reader, font, "BaseFont");
            Assert.Matches("^[A-Z]{6}\\+", baseFont);
            Assert.Equal("Type0", BuildTestSupport.Name(reader, font, "Subtype"));
        }
    }

    [Fact]
    public void EmbeddedType0Font_HasDescendantCidFont()
    {
        var reader = BuildTestSupport.Read(AuthorThreeScripts());
        var type0 = BuildTestSupport.Type0Fonts(reader);

        foreach (var font in type0)
        {
            var descendants = (ArrayObject)reader.Resolve(font["DescendantFonts"]);
            Assert.Equal(1, descendants.Count);
            var descendant = (DictionaryObject)reader.Resolve(descendants[0]);
            Assert.Matches("^CIDFontType[02]$", BuildTestSupport.Name(reader, descendant, "Subtype"));

            var prefix = BuildTestSupport.Name(reader, font, "BaseFont");
            Assert.Equal(prefix, BuildTestSupport.Name(reader, descendant, "BaseFont"));
        }
    }
}
