#nullable enable
using System.Text;
using Radzen.Documents.Pdf.Objects;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

public class TjSpacingAndLeadingTests
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
    public void TjArray_LargeNegativeGap_InsertsSpace()
    {
        var text = Extract("BT /F1 12 Tf 72 700 Td [<48656C6C6F> -300 <576F726C64>] TJ ET\n");

        Assert.Equal("Hello World", text);
    }

    [Fact]
    public void TjArray_SmallKerning_DoesNotInsertSpace()
    {
        var text = Extract("BT /F1 12 Tf 72 700 Td [<48656C6C6F> -20 <576F726C64>] TJ ET\n");

        Assert.Equal("HelloWorld", text);
    }

    [Fact]
    public void Leading_TStar_StartsNewLine()
    {
        var text = Extract("BT /F1 12 Tf 72 700 Td 14 TL <4C696E6531> Tj T* <4C696E6532> Tj ET\n");

        Assert.Equal("Line1\nLine2", text);
    }

    [Fact]
    public void Apostrophe_MovesToNextLineThenShows()
    {
        var text = Extract("BT /F1 12 Tf 72 700 Td 14 TL <4C696E6531> Tj <4C696E6532> ' ET\n");

        Assert.Equal("Line1\nLine2", text);
    }
}
