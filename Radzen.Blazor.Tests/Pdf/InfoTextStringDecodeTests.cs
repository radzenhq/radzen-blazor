#nullable enable
using System.IO;
using System.Text;
using Radzen.Documents.Pdf;
using Xunit;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

// ISO 32000 7.9.2.2: a text string may be UTF-16BE with a leading FE FF byte order mark.
public class InfoTextStringDecodeTests
{
    private static string Utf16BeLiteral(string text)
    {
        var document = new StringBuilder();
        document.Append('\u00FE').Append('\u00FF');
        foreach (var b in Encoding.BigEndianUnicode.GetBytes(text))
        {
            document.Append((char)b);
        }

        return document.ToString();
    }

    [Fact]
    public void Utf16BeTitleDecodesToText()
    {
        var document = new PortableDocument();
        document.Pages.Add();
        document.Info.Title = Utf16BeLiteral("Hello");

        var reloaded = PortableDocument.LoadFromStream(new MemoryStream(document.ToArray()));

        Assert.Equal("Hello", reloaded.Info.Title);
    }

    [Fact]
    public void Utf16BeNonAsciiTitleDecodes()
    {
        var expected = "H\u00E9llo W\u00F6rld";
        var document = new PortableDocument();
        document.Pages.Add();
        document.Info.Title = Utf16BeLiteral(expected);

        var reloaded = PortableDocument.LoadFromStream(new MemoryStream(document.ToArray()));

        Assert.Equal(expected, reloaded.Info.Title);
    }

    [Fact]
    public void PlainAsciiTitleRoundTrips()
    {
        var document = new PortableDocument();
        document.Pages.Add();
        document.Info.Title = "Quarterly Report 2026";

        var reloaded = PortableDocument.LoadFromStream(new MemoryStream(document.ToArray()));

        Assert.Equal("Quarterly Report 2026", reloaded.Info.Title);
    }
}
