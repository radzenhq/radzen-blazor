#nullable enable
using System;
using Radzen.Documents.Pdf;
using Xunit;
using static Radzen.Blazor.Pdf.Tests.RawPdfAssertions;

namespace Radzen.Blazor.Pdf.Tests;

public class PathClipTests
{
    private static string PageContent(string emission)
        => IndirectObject(
            emission,
            Shaped("page", @"/Contents (\d+) 0 R", Line(emission, "/Type /Page ")).Groups[1].Value);

    [Fact]
    public void PathClip_IsScoped_AndDoesNotLeak()
    {
        var document = new PortableDocument();
        var page = document.Pages.Add();

        var clip = new PathContent { Clip = PathClipMode.NonZero };
        clip.MoveTo(0, 0);
        clip.LineTo(10, 0);
        clip.LineTo(10, 10);
        clip.Close();
        page.Content.Add(clip);

        var fill = new PathContent { Fill = true };
        fill.MoveTo(0, 0);
        fill.LineTo(500, 0);
        fill.LineTo(500, 500);
        fill.Close();
        page.Content.Add(fill);

        var content = PageContent(Emit(document));

        Assert.Equal(
            BuildTestSupport.CountOccurrences(content, "q\n"),
            BuildTestSupport.CountOccurrences(content, "Q\n"));

        Carries("page content", "\nW\n", content);
        var w = content.IndexOf("\nW\n", StringComparison.Ordinal);
        var q = content.IndexOf("\nQ\n", w, StringComparison.Ordinal);
        var f = content.LastIndexOf("\nf\n", StringComparison.Ordinal);

        Assert.True(
            q > w,
            $"the clip must be closed by a 'Q' before anything else.\npage content:\n{Excerpt(content)}");
        Assert.True(
            f > q,
            $"the following fill must paint after the clip's 'Q'.\npage content:\n{Excerpt(content)}");
    }
}
