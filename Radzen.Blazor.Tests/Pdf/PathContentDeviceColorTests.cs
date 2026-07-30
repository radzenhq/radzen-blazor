#nullable enable

using Radzen.Documents.Pdf;
using Xunit;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

// ISO 32000-1 8.6.4 device colour spaces.
public class PathContentDeviceColorTests
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
        var document = new PortableDocument();
        var page = document.Pages.Add();
        page.Content.Add(path);
        return ContentTestHelpers.PageContent(ContentTestHelpers.Reload(document), 0);
    }

    [Fact]
    public void SetFillCmyk_EmitsKOperatorWithFourComponents()
    {
        var path = new PathContent { Fill = true };
        path.SetFillCmyk(0.1, 0.2, 0.3, 0.4);
        path.MoveTo(0, 0);
        path.LineTo(10, 0);
        path.LineTo(10, 10);
        path.Close();

        var content = Render(path);
        var k = Find(content, "k");

        Assert.Equal(4, k.Operands.Count);
        Assert.Equal(0.1, k.Num(0), 3);
        Assert.Equal(0.2, k.Num(1), 3);
        Assert.Equal(0.3, k.Num(2), 3);
        Assert.Equal(0.4, k.Num(3), 3);
    }

    [Fact]
    public void SetStrokeCmyk_EmitsUppercaseKOperator()
    {
        var path = new PathContent { Stroke = true };
        path.SetStrokeCmyk(0.0, 1.0, 1.0, 0.0);
        path.MoveTo(0, 0);
        path.LineTo(10, 10);

        var content = Render(path);
        var k = Find(content, "K");

        Assert.Equal(4, k.Operands.Count);
        Assert.Equal(1.0, k.Num(1), 3);
        Assert.Equal(1.0, k.Num(2), 3);
    }

    [Fact]
    public void SetFillGray_EmitsLowercaseGWithSingleComponent()
    {
        var path = new PathContent { Fill = true };
        path.SetFillGray(0.25);
        path.MoveTo(0, 0);
        path.LineTo(10, 0);
        path.LineTo(10, 10);
        path.Close();

        var content = Render(path);
        var g = Find(content, "g");

        Assert.Single(g.Operands);
        Assert.Equal(0.25, g.Num(0), 3);
    }

    [Fact]
    public void SetStrokeGray_EmitsUppercaseGOperator()
    {
        var path = new PathContent { Stroke = true };
        path.SetStrokeGray(0.5);
        path.MoveTo(0, 0);
        path.LineTo(10, 10);

        var content = Render(path);
        var g = Find(content, "G");

        Assert.Single(g.Operands);
        Assert.Equal(0.5, g.Num(0), 3);
    }

    [Fact]
    public void CmykFill_SuppressesRgbFillOperator()
    {
        var path = new PathContent { Fill = true };
        path.SetFillCmyk(0.1, 0.2, 0.3, 0.4);
        path.MoveTo(0, 0);
        path.LineTo(10, 0);
        path.LineTo(10, 10);
        path.Close();

        var operators = ContentStreamTokenizer.Operators(Render(path));

        Assert.Contains("k", operators);
        Assert.DoesNotContain("rg", operators);
    }

    [Fact]
    public void ComponentsClampToUnitRange()
    {
        var path = new PathContent { Fill = true };
        path.SetFillCmyk(-1, 2, 0.5, 0.5);
        path.MoveTo(0, 0);
        path.LineTo(10, 0);
        path.LineTo(10, 10);
        path.Close();

        var k = Find(Render(path), "k");

        Assert.Equal(0.0, k.Num(0), 3);
        Assert.Equal(1.0, k.Num(1), 3);
    }
}
