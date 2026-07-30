#nullable enable

using Radzen.Documents.Pdf;
using Xunit;
using Radzen.Documents;
using Document = Radzen.Documents.Pdf.Document;

namespace Radzen.Blazor.Pdf.Tests;

// ISO 32000-1 8.5.3.1 and 8.5.4 even-odd fill and path clipping.
public class PathContentClipTests
{
    private static byte[] Render(PathContent path)
    {
        var document = new Document();
        var page = document.Pages.Add();
        page.Content.Add(path);
        return ContentTestHelpers.PageContent(ContentTestHelpers.Reload(document), 0);
    }

    private static PathContent Triangle(bool stroke, bool fill)
    {
        var path = new PathContent { Stroke = stroke, Fill = fill };
        path.MoveTo(0, 0);
        path.LineTo(10, 0);
        path.LineTo(5, 10);
        path.Close();
        return path;
    }

    [Fact]
    public void EvenOddFill_EmitsFStarPaintOperator()
    {
        var path = Triangle(stroke: false, fill: true);
        path.EvenOdd = true;

        var operators = ContentStreamTokenizer.Operators(Render(path));

        Assert.Contains("f*", operators);
        Assert.DoesNotContain("f", operators);
    }

    [Fact]
    public void EvenOddFillAndStroke_EmitsBStarPaintOperator()
    {
        var path = Triangle(stroke: true, fill: true);
        path.EvenOdd = true;

        Assert.Contains("B*", ContentStreamTokenizer.Operators(Render(path)));
    }

    [Fact]
    public void NonZeroClip_EmitsWBeforeNoOpPaint()
    {
        var path = Triangle(stroke: false, fill: false);
        path.Clip = PathClipMode.NonZero;

        var operators = ContentStreamTokenizer.Operators(Render(path));

        var w = operators.IndexOf("W");
        var n = operators.IndexOf("n");
        Assert.True(w >= 0, "expected W operator");
        Assert.True(w < n, "W must precede the n paint operator");
    }

    [Fact]
    public void EvenOddClip_EmitsWStarOperator()
    {
        var path = Triangle(stroke: false, fill: true);
        path.Clip = PathClipMode.EvenOdd;

        Assert.Contains("W*", ContentStreamTokenizer.Operators(Render(path)));
    }

    [Fact]
    public void Defaults_UseNonZeroFillAndNoClip()
    {
        var operators = ContentStreamTokenizer.Operators(Render(Triangle(stroke: false, fill: true)));

        Assert.Contains("f", operators);
        Assert.DoesNotContain("f*", operators);
        Assert.DoesNotContain("W", operators);
        Assert.DoesNotContain("W*", operators);
    }
}
