#nullable enable

using Radzen.Documents.Pdf;
using Xunit;
using Radzen.Documents;
using static Radzen.Blazor.Pdf.Tests.RawPdfAssertions;

namespace Radzen.Blazor.Pdf.Tests;

// ISO 32000-1 8.4.3 line cap, join, miter limit and dash pattern.
public class PathContentStrokeStyleTests
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

    private static PathContent StrokedLine()
    {
        var path = new PathContent { Stroke = true };
        path.MoveTo(0, 0);
        path.LineTo(10, 10);
        return path;
    }

    [Fact]
    public void LineCap_EmitsJOperatorWithEnumValue()
    {
        var path = StrokedLine();
        path.Cap = LineCap.Round;

        Carries("page content", "\n1 J\n", Render(path));
    }

    [Fact]
    public void LineJoin_EmitsLowercaseJOperatorWithEnumValue()
    {
        var path = StrokedLine();
        path.Join = LineJoin.Bevel;

        Carries("page content", "\n2 j\n", Render(path));
    }

    [Fact]
    public void MiterLimit_EmitsMOperator()
    {
        var path = StrokedLine();
        path.MiterLimit = 4.5;

        Carries("page content", "\n4.5 M\n", Render(path));
    }

    [Fact]
    public void SetDash_EmitsDashArrayAndPhase()
    {
        var path = StrokedLine();
        path.SetDash([3, 2], 1);

        Carries("page content", "\n[3 2] 1 d\n", Render(path));
    }

    [Fact]
    public void Defaults_EmitNoStrokeStyleOperators()
    {
        var content = Render(StrokedLine());

        Lacks("page content", " J\n", content);
        Lacks("page content", " j\n", content);
        Lacks("page content", " M\n", content);
        Lacks("page content", " d\n", content);
    }
}
