#nullable enable

using System;
using Radzen.Documents.Pdf;
using Xunit;
using Radzen.Documents;
using static Radzen.Blazor.Pdf.Tests.RawPdfAssertions;

namespace Radzen.Blazor.Pdf.Tests;

// ISO 32000-1 8.4.2 graphics state; stroke state confined to a q..Q scope.
public class PathContentStrokeScopeTests
{
    private static string Render(params PathContent[] paths)
    {
        var document = new PortableDocument();
        var page = document.Pages.Add();
        foreach (var path in paths)
        {
            page.Content.Add(path);
        }

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

    private static int At(string content, string fragment)
    {
        Carries("page content", fragment, content);
        return content.IndexOf(fragment, StringComparison.Ordinal);
    }

    [Fact]
    public void DashedStroke_IsWrappedInSaveRestore()
    {
        var path = StrokedLine();
        path.SetDash([3, 2], 0);
        path.Cap = LineCap.Round;

        var content = Render(path);

        var q = At(content, "\nq\n");
        var d = At(content, " d\n");
        var s = At(content, "\nS\n");
        var restore = At(content, "\nQ\n");

        Assert.True(
            q < d,
            $"the dash operator must be inside the saved state.\npage content:\n{Excerpt(content)}");
        Assert.True(
            d < s,
            $"the stroke paints after the dash is set.\npage content:\n{Excerpt(content)}");
        Assert.True(
            s < restore,
            $"the saved state is restored after the paint.\npage content:\n{Excerpt(content)}");
    }

    [Fact]
    public void PlainStroke_AfterDashedStroke_DoesNotInheritDash()
    {
        var dashed = StrokedLine();
        dashed.SetDash([3, 2], 0);

        var content = Render(dashed, StrokedLine());

        var dash = At(content, " d\n");
        Assert.Equal(1, BuildTestSupport.CountOccurrences(content, " d\n"));

        var restore = At(content, "\nQ\n");
        var firstStroke = At(content, "\nS\n");
        var secondStroke = content.IndexOf("\nS\n", firstStroke + 1, StringComparison.Ordinal);

        Assert.True(
            secondStroke > 0,
            $"the second path is missing its own 'S' stroke.\npage content:\n{Excerpt(content)}");
        Assert.True(
            restore > dash,
            $"the dash must be closed by a later 'Q'.\npage content:\n{Excerpt(content)}");
        Assert.True(
            restore < secondStroke,
            $"the dash scope must close before the next path.\npage content:\n{Excerpt(content)}");
    }

    [Fact]
    public void PlainStroke_EmitsNoSaveRestore()
    {
        Lacks("page content", "\nq\n", Render(StrokedLine()));
    }
}
