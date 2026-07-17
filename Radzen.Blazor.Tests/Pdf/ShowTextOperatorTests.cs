#nullable enable

using Radzen.Documents.Pdf;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

public class ShowTextOperatorTests
{
    private static ContentCollection Materialize(string rawStream)
    {
        var document = new Document();
        var page = document.Pages.Add();
        page.SetContent(InterpreterTestSupport.Ascii(rawStream));

        return InterpreterTestSupport.Load(document.ToArray()).Pages[0].Content;
    }

    private static string Extract(string rawStream)
    {
        var document = new Document();
        var page = document.Pages.Add();
        page.SetContent(InterpreterTestSupport.Ascii(rawStream));

        return InterpreterTestSupport.Load(document.ToArray()).Pages[0].ExtractText();
    }

    [Fact]
    public void QuoteOperator_MaterializesTextContent()
    {
        var content = Materialize("BT /F0 12 Tf 10 700 Td (Hello) ' ET\n");

        var text = Assert.IsType<TextContent>(content[0]);
        Assert.Equal("Hello", text.Text);
    }

    [Fact]
    public void DoubleQuoteOperator_MaterializesTextContent()
    {
        var content = Materialize("BT /F0 12 Tf 10 700 Td 1 2 (World) \" ET\n");

        var text = Assert.IsType<TextContent>(content[0]);
        Assert.Equal("World", text.Text);
    }

    [Fact]
    public void QuoteOperators_ExtractShownText()
    {
        var text = Extract(
            "BT /F0 12 Tf 10 700 Td (Alpha) '\n" +
            "1 2 (Beta) \" ET\n");

        Assert.Contains("Alpha", text);
        Assert.Contains("Beta", text);
    }
}
