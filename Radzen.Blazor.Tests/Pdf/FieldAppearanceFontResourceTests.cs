#nullable enable
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Emit;
using Radzen.Documents.Pdf.Objects;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// ISO 32000-1 9.6.6.4: a widget appearance must reach its base-14 face through the same
// dictionary builder the page resources use, or the symbolic faces get their glyphs remapped.
public class FieldAppearanceFontResourceTests
{
    [Fact]
    public void BuildText_SymbolFont_OmitsWinAnsiEncoding()
    {
        Assert.False(FontDictionaryOf("Symbol").ContainsKey("Encoding"));
        Assert.False(FontDictionaryOf("ZapfDingbats").ContainsKey("Encoding"));
    }

    [Fact]
    public void BuildText_NonSymbolicFont_KeepsWinAnsiEncoding()
    {
        var dictionary = FontDictionaryOf("Helvetica");

        Assert.Equal("WinAnsiEncoding", Assert.IsType<NameObject>(dictionary["Encoding"]).Value);
        Assert.Equal("Helvetica", Assert.IsType<NameObject>(dictionary["BaseFont"]).Value);
        Assert.Equal("Type1", Assert.IsType<NameObject>(dictionary["Subtype"]).Value);
    }

    private static DictionaryObject FontDictionaryOf(string family)
    {
        var appearance = FieldAppearances.BuildText(
            "a", 80, 20, new Font { Name = family, Size = 12 }, scope: default);
        var resources = Assert.IsType<DictionaryObject>(appearance.Dictionary["Resources"]);
        var fonts = Assert.IsType<DictionaryObject>(resources["Font"]);

        return Assert.IsType<DictionaryObject>(fonts[Assert.Single(fonts.Keys)]);
    }
}
