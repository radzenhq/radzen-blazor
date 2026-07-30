#nullable enable
using System;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

public class MultiScriptTests
{
    private const string English = "Invoice";
    private const string Bulgarian = "Здравей";
    private const string Chinese = "中产";

    private static Document AuthorThreeScripts()
    {
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

        var document = new Document();
        BuildTestSupport.RegisterLatin(document);
        BuildTestSupport.RegisterCjk(document);

        var section = document.Sections.Add();
        BuildTestSupport.AddText(section, English, BuildTestSupport.Latin);
        BuildTestSupport.AddText(section, Bulgarian, BuildTestSupport.Latin);
        BuildTestSupport.AddText(section, Chinese, BuildTestSupport.Cjk);
        return document;
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
