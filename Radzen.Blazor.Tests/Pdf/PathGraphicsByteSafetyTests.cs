#nullable enable

using Radzen.Documents.Pdf;
using Xunit;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

public class PathGraphicsByteSafetyTests
{
    private static PortableDocument BuildDocument()
    {
        var document = new PortableDocument();
        var page = document.Pages.Add();
        var path = new PathContent { Stroke = true, Fill = true, Thickness = 2, StrokeColor = Color.Blue, FillColor = Color.Red };
        path.MoveTo(10, 10);
        path.LineTo(100, 10);
        path.LineTo(100, 80);
        path.Close();
        page.Content.Add(path);
        return document;
    }

    [Fact]
    public void DefaultPath_IsDeterministicByteForByte()
    {
        Assert.Equal(BuildDocument().ToArray(), BuildDocument().ToArray());
    }

    [Fact]
    public void DefaultPath_EmitsNoNewGraphicsOperators()
    {
        var content = ContentTestHelpers.PageContent(ContentTestHelpers.Reload(BuildDocument()), 0);

        var operators = ContentStreamTokenizer.Operators(content);

        Assert.Contains("rg", operators);
        Assert.Contains("RG", operators);
        Assert.Contains("B", operators);
        foreach (var forbidden in new[] { "k", "K", "g", "G", "J", "j", "M", "ri", "d", "W", "W*", "f*", "B*", "scn", "cs" })
        {
            Assert.DoesNotContain(forbidden, operators);
        }
    }
}
