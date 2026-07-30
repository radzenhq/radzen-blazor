#nullable enable

using System.Text;
using Radzen.Documents.Pdf;
using Xunit;
using Radzen.Documents;
using Document = Radzen.Documents.Pdf.Document;

namespace Radzen.Blazor.Pdf.Tests;

// ISO 32000-1 8.4.3 line cap, join, miter limit and dash pattern.
public class PathContentStrokeStyleTests
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

    [Fact]
    public void LineCap_EmitsJOperatorWithEnumValue()
    {
        var path = Line();
        path.Cap = LineCap.Round;

        Assert.Equal(1, Find(Render(path), "J").Num(0), 3);
    }

    [Fact]
    public void LineJoin_EmitsLowercaseJOperatorWithEnumValue()
    {
        var path = Line();
        path.Join = LineJoin.Bevel;

        Assert.Equal(2, Find(Render(path), "j").Num(0), 3);
    }

    [Fact]
    public void MiterLimit_EmitsMOperator()
    {
        var path = Line();
        path.MiterLimit = 4.5;

        Assert.Equal(4.5, Find(Render(path), "M").Num(0), 3);
    }

    [Fact]
    public void SetDash_EmitsDashArrayAndPhase()
    {
        var path = Line();
        path.SetDash([3, 2], 1);

        var content = Encoding.ASCII.GetString(Render(path));

        Assert.Contains("[3 2] 1 d", content);
    }

    [Fact]
    public void Defaults_EmitNoStrokeStyleOperators()
    {
        var operators = ContentStreamTokenizer.Operators(Render(Line()));

        Assert.DoesNotContain("J", operators);
        Assert.DoesNotContain("j", operators);
        Assert.DoesNotContain("M", operators);
        Assert.DoesNotContain("d", operators);
    }
}
