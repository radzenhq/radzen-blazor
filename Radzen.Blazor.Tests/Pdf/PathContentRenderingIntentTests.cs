#nullable enable

using Radzen.Documents.Pdf;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// Rendering intent (the ri operator) on a public PathContent, per ISO 32000-1 8.6.5.8.
public class PathContentRenderingIntentTests
{
    private static ContentOperation Find(byte[] content, string op)
    {
        foreach (var operation in ContentStreamTokenizer.Parse(content))
        {
            if (operation.Operator == op)
            {
                return operation;
            }
        }

        throw new Xunit.Sdk.XunitException($"operator '{op}' not found");
    }

    private static byte[] Render(PathContent path)
    {
        var document = new Document();
        var page = document.Pages.Add();
        page.Content.Add(path);
        return ContentTestHelpers.PageContent(ContentTestHelpers.Reload(document), 0);
    }

    private static PathContent Line()
    {
        var path = new PathContent { Stroke = true };
        path.MoveTo(0, 0);
        path.LineTo(10, 10);
        return path;
    }

    [Theory]
    [InlineData(RenderingIntent.AbsoluteColorimetric, "AbsoluteColorimetric")]
    [InlineData(RenderingIntent.RelativeColorimetric, "RelativeColorimetric")]
    [InlineData(RenderingIntent.Saturation, "Saturation")]
    [InlineData(RenderingIntent.Perceptual, "Perceptual")]
    public void Intent_EmitsRiOperatorWithSpecName(RenderingIntent intent, string expected)
    {
        var path = Line();
        path.Intent = intent;

        var ri = Find(Render(path), "ri");

        Assert.Single(ri.Operands);
        Assert.Equal(expected, ri.Operands[0].Text);
    }

    [Fact]
    public void Default_EmitsNoRiOperator()
    {
        Assert.DoesNotContain("ri", ContentStreamTokenizer.Operators(Render(Line())));
    }
}
