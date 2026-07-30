#nullable enable

using System.Text;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Content;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

public class RectangleEmissionFormTests
{
    [Fact]
    public void PathContentRectangle_EmitsFourExplicitSegmentsRatherThanTheReOperator()
    {
        var path = PathContent.Rectangle(20, 30, 100, 50);
        path.Fill = true;

        var document = new PortableDocument();
        var page = document.Pages.Add();
        page.Content.Add(path);
        var content = ContentTestHelpers.PageContent(ContentTestHelpers.Reload(document), 0);
        var operators = ContentStreamTokenizer.Operators(content);

        Assert.DoesNotContain("re", operators);
        Assert.Equal(1, CountOf(operators, "m"));
        Assert.Equal(3, CountOf(operators, "l"));
        Assert.Equal(1, CountOf(operators, "h"));
    }

    [Fact]
    public void PathContentRectangle_ReportsTheWholeRectangleAsItsBounds()
    {
        var bounds = PathContent.Rectangle(20, 30, 100, 50).GetBounds();

        Assert.NotNull(bounds);
        Assert.Equal(20, bounds!.Value.Left, 3);
        Assert.Equal(30, bounds.Value.Bottom, 3);
        Assert.Equal(120, bounds.Value.Right, 3);
        Assert.Equal(80, bounds.Value.Top, 3);
    }

    [Fact]
    public void ContentEmitterRectangle_EmitsTheReOperator()
    {
        using var writer = new ContentWriter();

        ContentEmitter.WriteRectangle(writer, 20, 30, 100, 50);

        Assert.Equal("20 30 100 50 re", Encoding.ASCII.GetString(writer.ToArray()));
    }

    private static int CountOf(System.Collections.Generic.IReadOnlyList<string> operators, string name)
    {
        var count = 0;
        foreach (var op in operators)
        {
            if (op == name)
            {
                count++;
            }
        }

        return count;
    }
}
