#nullable enable
using System.Text;
using Radzen.Documents.Pdf.Objects;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

public class SameLineFragmentSpacingTests
{
    private static DictionaryObject Helvetica() => new()
    {
        ["Type"] = new NameObject("Font"),
        ["Subtype"] = new NameObject("Type1"),
        ["BaseFont"] = new NameObject("Helvetica"),
        ["Encoding"] = new NameObject("WinAnsiEncoding"),
    };

    private static string Extract(string body)
    {
        var content = Encoding.ASCII.GetBytes(body);
        var document = ExtractionSupport.BuildSinglePage(_ => Helvetica(), content);
        return document.Pages[0].ExtractText();
    }

    [Fact]
    public void AbuttingFragments_SameLine_NoSpuriousSpace()
    {
        var text = Extract("BT /F1 12 Tf 72 700 Td <48656C> Tj <6C6F> Tj ET\n");

        Assert.Equal("Hello", text);
    }

    [Fact]
    public void PositionalGap_SameLine_InsertsWordSpace()
    {
        var text = Extract("BT /F1 12 Tf 72 700 Td <48656C6C6F> Tj 40 0 Td <576F726C64> Tj ET\n");

        Assert.Equal("Hello World", text);
    }
}
