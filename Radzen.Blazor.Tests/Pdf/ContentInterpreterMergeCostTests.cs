#nullable enable

using System.Text;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Content;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

public class ContentInterpreterMergeCostTests
{
    private static string ShowChain(int shows)
    {
        var builder = new StringBuilder("BT /F1 12 Tf 10 700 Td ");
        for (var i = 0; i < shows; i++)
        {
            builder.Append("(abcdefghij) Tj ");
        }

        return builder.Append("ET").ToString();
    }

    [Fact]
    public void MergedShowChain_FoldsToOneRunWithAllChunks()
    {
        var target = new ContentCollection();
        ContentInterpreter.Materialize(Encoding.ASCII.GetBytes(ShowChain(3)), target);

        var text = Assert.IsType<TextContent>(Assert.Single(target));
        Assert.Equal(string.Concat(System.Linq.Enumerable.Repeat("abcdefghij", 3)), text.Text);
    }
}
