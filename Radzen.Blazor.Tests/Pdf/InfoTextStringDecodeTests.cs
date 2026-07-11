#nullable enable
using System.IO;
using System.Text;
using Radzen.Documents.Pdf;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// A loaded Info dictionary string may be a UTF-16BE text string carrying a leading
// FE FF byte order mark (ISO 32000 7.9.2.2); it must decode to the .NET string, while
// a plain PDFDocEncoding/Latin1 value keeps round-tripping verbatim.
public class InfoTextStringDecodeTests
{
    // The raw bytes of a UTF-16BE literal (FE FF BOM + big-endian code units) exposed
    // one-byte-per-char, exactly as StringObject.Value would surface them once loaded.
    private static string Utf16BeLiteral(string text)
    {
        var builder = new StringBuilder();
        builder.Append('\u00FE').Append('\u00FF');
        foreach (var b in Encoding.BigEndianUnicode.GetBytes(text))
        {
            builder.Append((char)b);
        }

        return builder.ToString();
    }

    [Fact]
    public void Utf16BeTitleDecodesToText()
    {
        var document = new Document();
        document.Pages.Add();
        document.Info.Title = Utf16BeLiteral("Hello");

        var reloaded = Document.LoadFromStream(new MemoryStream(document.ToArray()));

        Assert.Equal("Hello", reloaded.Info.Title);
    }

    [Fact]
    public void Utf16BeNonAsciiTitleDecodes()
    {
        var expected = "H\u00E9llo W\u00F6rld";
        var document = new Document();
        document.Pages.Add();
        document.Info.Title = Utf16BeLiteral(expected);

        var reloaded = Document.LoadFromStream(new MemoryStream(document.ToArray()));

        Assert.Equal(expected, reloaded.Info.Title);
    }

    [Fact]
    public void PlainAsciiTitleRoundTrips()
    {
        var document = new Document();
        document.Pages.Add();
        document.Info.Title = "Quarterly Report 2026";

        var reloaded = Document.LoadFromStream(new MemoryStream(document.ToArray()));

        Assert.Equal("Quarterly Report 2026", reloaded.Info.Title);
    }
}
