#nullable enable

using System.Linq;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;
using Radzen.Documents;
using Radzen.Documents.Fonts;

namespace Radzen.Blazor.Pdf.Tests;

// ISO 32000-1 9.6.6.4: Symbol and ZapfDingbats have a built-in encoding, no /Encoding /WinAnsiEncoding.
public class SymbolicBase14EncodingTests
{
    private static DictionaryObject FontWithBaseFont(PortableDocument document, string baseFont)
    {
        var reader = ContentTestHelpers.Reload(document);
        var page = ContentTestHelpers.Kid(reader, 0);
        var resources = Assert.IsType<DictionaryObject>(reader.Resolve(page["Resources"]));
        var fonts = Assert.IsType<DictionaryObject>(reader.Resolve(resources["Font"]));
        foreach (var key in fonts.Keys)
        {
            if (reader.Resolve(fonts[key]) is DictionaryObject dict
                && dict.TryGetValue("BaseFont", out var name)
                && name is NameObject baseName && baseName.Value == baseFont)
            {
                return dict;
            }
        }

        Assert.Fail($"No font with BaseFont /{baseFont}");
        return null!;
    }

    [Theory]
    [InlineData("Symbol")]
    [InlineData("ZapfDingbats")]
    public void SymbolicBase14Font_OmitsEncoding(string family)
    {
        var document = new PortableDocument();
        var page = document.Pages.Add();
        page.Content.Add(new TextContent("abc", 72, 700) { Font = new Font { Family = family } });

        var dict = FontWithBaseFont(document, family);

        Assert.False(dict.ContainsKey("Encoding"), $"/{family} must omit /Encoding");
    }

    [Fact]
    public void NonSymbolicBase14Font_KeepsWinAnsiEncoding()
    {
        var document = new PortableDocument();
        var page = document.Pages.Add();
        page.Content.Add(new TextContent("abc", 72, 700) { Font = new Font { Family = "Helvetica" } });

        var dict = FontWithBaseFont(document, "Helvetica");

        Assert.True(dict.TryGetValue("Encoding", out var enc));
        Assert.Equal("WinAnsiEncoding", Assert.IsType<NameObject>(enc).Value);
    }
}
