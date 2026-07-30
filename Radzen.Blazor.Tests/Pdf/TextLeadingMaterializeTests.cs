#nullable enable

using Radzen.Documents.Pdf;
using Xunit;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

public class TextLeadingMaterializeTests
{
    private static ContentCollection Materialize(string rawStream)
    {
        var document = new PortableDocument();
        document.Pages.Add().SetContent(InterpreterTestSupport.Ascii(rawStream));
        return InterpreterTestSupport.Load(document.ToArray()).Pages[0].Content;
    }

    [Fact]
    public void TStarAndQuote_AdvanceEachRunByLeading()
    {
        var content = Materialize(
            "BT /F0 12 Tf 72 700 Td 14 TL (L1) Tj T* (L2) Tj (L3) ' ET\n");

        Assert.Equal(3, content.Count);
        Assert.Equal("L1", Assert.IsType<TextContent>(content[0]).Text);
        Assert.Equal("L2", Assert.IsType<TextContent>(content[1]).Text);
        Assert.Equal("L3", Assert.IsType<TextContent>(content[2]).Text);

        Assert.Equal(700, content[0].Transform.F, 3);
        Assert.Equal(686, content[1].Transform.F, 3);
        Assert.Equal(672, content[2].Transform.F, 3);
    }
}
