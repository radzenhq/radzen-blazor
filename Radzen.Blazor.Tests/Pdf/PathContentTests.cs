#nullable enable

using Radzen.Documents.Pdf;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

public class PathContentTests
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

    [Fact]
    public void MoveTo_EmitsMOperatorWithCoordinates()
    {
        var path = new PathContent { Stroke = true };
        path.MoveTo(72, 700);
        path.LineTo(144, 700);

        var content = Render(path);
        var m = Find(content, "m");

        Assert.Equal(2, m.Operands.Count);
        Assert.Equal(72, m.Num(0), 3);
        Assert.Equal(700, m.Num(1), 3);
    }

    [Fact]
    public void LineTo_EmitsLOperatorWithCoordinates()
    {
        var path = new PathContent { Stroke = true };
        path.MoveTo(0, 0);
        path.LineTo(120, 240);

        var content = Render(path);
        var l = Find(content, "l");

        Assert.Equal(120, l.Num(0), 3);
        Assert.Equal(240, l.Num(1), 3);
    }

    [Fact]
    public void CurveTo_EmitsCOperatorWithSixControlValues()
    {
        var path = new PathContent { Stroke = true };
        path.MoveTo(0, 0);
        path.CurveTo(10, 20, 30, 40, 50, 60);

        var content = Render(path);
        var c = Find(content, "c");

        Assert.Equal(6, c.Operands.Count);
        Assert.Equal(10, c.Num(0), 3);
        Assert.Equal(20, c.Num(1), 3);
        Assert.Equal(30, c.Num(2), 3);
        Assert.Equal(40, c.Num(3), 3);
        Assert.Equal(50, c.Num(4), 3);
        Assert.Equal(60, c.Num(5), 3);
    }

    [Fact]
    public void Close_EmitsHOperator()
    {
        var path = new PathContent { Stroke = true };
        path.MoveTo(0, 0);
        path.LineTo(10, 0);
        path.LineTo(10, 10);
        path.Close();

        var content = Render(path);

        Assert.Contains("h", ContentStreamTokenizer.Operators(content));
    }

    [Fact]
    public void StrokeOnly_EmitsSPaintOperator()
    {
        var path = new PathContent { Stroke = true, Fill = false };
        path.MoveTo(0, 0);
        path.LineTo(10, 10);

        var operators = ContentStreamTokenizer.Operators(Render(path));

        Assert.Contains("S", operators);
        Assert.DoesNotContain("f", operators);
        Assert.DoesNotContain("B", operators);
    }

    [Fact]
    public void FillOnly_EmitsFPaintOperator()
    {
        var path = new PathContent { Stroke = false, Fill = true };
        path.MoveTo(0, 0);
        path.LineTo(10, 0);
        path.LineTo(10, 10);
        path.Close();

        var operators = ContentStreamTokenizer.Operators(Render(path));

        Assert.Contains("f", operators);
        Assert.DoesNotContain("S", operators);
    }

    [Fact]
    public void StrokeAndFill_EmitsBPaintOperator()
    {
        var path = new PathContent { Stroke = true, Fill = true };
        path.MoveTo(0, 0);
        path.LineTo(10, 0);
        path.LineTo(10, 10);
        path.Close();

        var operators = ContentStreamTokenizer.Operators(Render(path));

        Assert.Contains("B", operators);
    }

    [Fact]
    public void Thickness_EmittedAsWOperator()
    {
        var path = new PathContent { Stroke = true, Thickness = 2.5 };
        path.MoveTo(0, 0);
        path.LineTo(10, 10);

        var content = Render(path);
        var w = Find(content, "w");

        Assert.Equal(2.5, w.Num(0), 3);
    }

    [Fact]
    public void StrokeColor_EmittedAsUppercaseRg()
    {
        var path = new PathContent { Stroke = true, StrokeColor = Color.Blue };
        path.MoveTo(0, 0);
        path.LineTo(10, 10);

        var content = Render(path);
        var rg = Find(content, "RG");

        Assert.Equal(3, rg.Operands.Count);
        Assert.Equal(0.0, rg.Num(0), 3);
        Assert.Equal(0.0, rg.Num(1), 3);
        Assert.Equal(1.0, rg.Num(2), 3);
    }

    [Fact]
    public void FillColor_EmittedAsLowercaseRg()
    {
        var path = new PathContent { Stroke = false, Fill = true, FillColor = Color.Red };
        path.MoveTo(0, 0);
        path.LineTo(10, 0);
        path.LineTo(10, 10);
        path.Close();

        var content = Render(path);
        var rg = Find(content, "rg");

        Assert.Equal(1.0, rg.Num(0), 3);
        Assert.Equal(0.0, rg.Num(1), 3);
        Assert.Equal(0.0, rg.Num(2), 3);
    }

    [Fact]
    public void Operators_FollowConstructionOrder()
    {
        var path = new PathContent { Stroke = true };
        path.MoveTo(0, 0);
        path.LineTo(10, 10);
        path.CurveTo(1, 2, 3, 4, 5, 6);

        var operators = ContentStreamTokenizer.Operators(Render(path));

        var m = operators.IndexOf("m");
        var l = operators.IndexOf("l");
        var c = operators.IndexOf("c");
        var s = operators.IndexOf("S");
        Assert.True(m < l);
        Assert.True(l < c);
        Assert.True(c < s);
    }
}
