#nullable enable

using System;
using System.Linq;
using System.Text;
using Radzen.Documents.Pdf;
using Radzen.Documents.Core;
using Xunit;
using static Radzen.Blazor.Pdf.Tests.RawPdfAssertions;

namespace Radzen.Blazor.Pdf.Tests;

// ISO 32000-1 8.5.3.1 and 8.5.4 even-odd fill and path clipping.
public class PathContentClipTests
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

        var content = Render(path);

        Carries("page content", "\nf*\n", content);
        Lacks("page content", "\nf\n", content);
    }

    [Fact]
    public void EvenOddFillAndStroke_EmitsBStarPaintOperator()
    {
        var path = Triangle(stroke: true, fill: true);
        path.EvenOdd = true;

        Carries("page content", "\nB*\n", Render(path));
    }

    [Fact]
    public void NonZeroClip_EmitsWBeforeNoOpPaint()
    {
        var path = Triangle(stroke: false, fill: false);
        path.Clip = PathClipMode.NonZero;

        var content = Render(path);

        Carries("page content", "\nW\n", content);
        Carries("page content", "\nn\n", content);

        var w = content.IndexOf("\nW\n", StringComparison.Ordinal);
        var n = content.IndexOf("\nn\n", StringComparison.Ordinal);
        Assert.True(
            w < n,
            $"'W' must precede the 'n' paint operator.\npage content:\n{Excerpt(content)}");
    }

    [Fact]
    public void EvenOddClip_EmitsWStarOperator()
    {
        var path = Triangle(stroke: false, fill: true);
        path.Clip = PathClipMode.EvenOdd;

        Carries("page content", "\nW*\n", Render(path));
    }

    [Fact]
    public void Defaults_UseNonZeroFillAndNoClip()
    {
        var content = Render(Triangle(stroke: false, fill: true));

        Carries("page content", "\nf\n", content);
        Lacks("page content", "\nf*\n", content);
        Lacks("page content", "\nW\n", content);
        Lacks("page content", "\nW*\n", content);
    }

    [Fact]
    public void LoadedClippingPath_ModificationIsRejected()
    {
        var document = new PortableDocument();
        document.Pages.Add().SetContent(Encoding.ASCII.GetBytes("0 0 20 20 re W n 0 0 100 100 re f"));
        var loaded = InterpreterTestSupport.SaveAndLoad(document);
        var clip = loaded.Pages[0].Content.OfType<PathContent>().First();

        clip.FillColor = Color.Red;

        var exception = Assert.Throws<NotSupportedException>(() => loaded.ToArray());
        Assert.Contains("clipping path", exception.Message, StringComparison.Ordinal);
    }
}
