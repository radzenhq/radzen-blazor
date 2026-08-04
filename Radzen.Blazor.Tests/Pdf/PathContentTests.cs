#nullable enable

using System;
using Radzen.Documents.Pdf;
using Xunit;
using Radzen.Documents.Core;
using static Radzen.Blazor.Pdf.Tests.RawPdfAssertions;

namespace Radzen.Blazor.Pdf.Tests;

public class PathContentTests
{
    private static string Render(PathContent path)
    {
        var document = new PortableDocument();
        var page = document.Pages.Add();
        page.Content.Add(path);

        var emission = Emit(document);
        return IndirectObject(
            emission,
            Shaped("page", @"/Contents (\d+) 0 R", Line(emission, "/Type /Page ")).Groups[1].Value);
    }

    private static int At(string content, string fragment)
    {
        Carries("page content", fragment, content);
        return content.IndexOf(fragment, StringComparison.Ordinal);
    }

    [Fact]
    public void MoveTo_EmitsMOperatorWithCoordinates()
    {
        var path = new PathContent { Stroke = true };
        path.MoveTo(72, 700);
        path.LineTo(144, 700);

        Carries("page content", "\n72 700 m\n", Render(path));
    }

    [Fact]
    public void LineTo_EmitsLOperatorWithCoordinates()
    {
        var path = new PathContent { Stroke = true };
        path.MoveTo(0, 0);
        path.LineTo(120, 240);

        Carries("page content", "\n120 240 l\n", Render(path));
    }

    [Fact]
    public void CurveTo_EmitsCOperatorWithSixControlValues()
    {
        var path = new PathContent { Stroke = true };
        path.MoveTo(0, 0);
        path.CurveTo(10, 20, 30, 40, 50, 60);

        Carries("page content", "\n10 20 30 40 50 60 c\n", Render(path));
    }

    [Fact]
    public void Close_EmitsHOperator()
    {
        var path = new PathContent { Stroke = true };
        path.MoveTo(0, 0);
        path.LineTo(10, 0);
        path.LineTo(10, 10);
        path.Close();

        Carries("page content", "\nh\n", Render(path));
    }

    [Fact]
    public void StrokeOnly_EmitsSPaintOperator()
    {
        var path = new PathContent { Stroke = true, Fill = false };
        path.MoveTo(0, 0);
        path.LineTo(10, 10);

        var content = Render(path);

        Carries("page content", "\nS\n", content);
        Lacks("page content", "\nf\n", content);
        Lacks("page content", "\nB\n", content);
    }

    [Fact]
    public void FillOnly_EmitsFPaintOperator()
    {
        var path = new PathContent { Stroke = false, Fill = true };
        path.MoveTo(0, 0);
        path.LineTo(10, 0);
        path.LineTo(10, 10);
        path.Close();

        var content = Render(path);

        Carries("page content", "\nf\n", content);
        Lacks("page content", "\nS\n", content);
    }

    [Fact]
    public void StrokeAndFill_EmitsBPaintOperator()
    {
        var path = new PathContent { Stroke = true, Fill = true };
        path.MoveTo(0, 0);
        path.LineTo(10, 0);
        path.LineTo(10, 10);
        path.Close();

        Carries("page content", "\nB\n", Render(path));
    }

    [Fact]
    public void Thickness_EmittedAsWOperator()
    {
        var path = new PathContent { Stroke = true, Thickness = 2.5 };
        path.MoveTo(0, 0);
        path.LineTo(10, 10);

        Carries("page content", "\n2.5 w\n", Render(path));
    }

    [Fact]
    public void StrokeColor_EmittedAsUppercaseRg()
    {
        var path = new PathContent { Stroke = true, StrokeColor = Color.Blue };
        path.MoveTo(0, 0);
        path.LineTo(10, 10);

        Carries("page content", "\n0 0 1 RG\n", Render(path));
    }

    [Fact]
    public void FillColor_EmittedAsLowercaseRg()
    {
        var path = new PathContent { Stroke = false, Fill = true, FillColor = Color.Red };
        path.MoveTo(0, 0);
        path.LineTo(10, 0);
        path.LineTo(10, 10);
        path.Close();

        Carries("page content", "\n1 0 0 rg\n", Render(path));
    }

    [Fact]
    public void Operators_FollowConstructionOrder()
    {
        var path = new PathContent { Stroke = true };
        path.MoveTo(0, 0);
        path.LineTo(10, 10);
        path.CurveTo(1, 2, 3, 4, 5, 6);

        var content = Render(path);

        var m = At(content, " m\n");
        var l = At(content, " l\n");
        var c = At(content, " c\n");
        var s = At(content, "\nS\n");

        Assert.True(m < l, $"'m' must precede 'l'.\npage content:\n{Excerpt(content)}");
        Assert.True(l < c, $"'l' must precede 'c'.\npage content:\n{Excerpt(content)}");
        Assert.True(c < s, $"'c' must precede 'S'.\npage content:\n{Excerpt(content)}");
    }
}
