#nullable enable
using System;
using System.Text;
using Radzen.Documents.Pdf;
using Xunit;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

public class PathClipTests
{
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

        var content = Encoding.Latin1.GetString(
            ContentTestHelpers.PageContent(ContentTestHelpers.Reload(document), 0));

        Assert.Equal(
            BuildTestSupport.CountOccurrences(content, "q\n"),
            BuildTestSupport.CountOccurrences(content, "Q\n"));

        var w = content.IndexOf("W\n", StringComparison.Ordinal);
        var q = content.IndexOf("Q\n", w, StringComparison.Ordinal);
        var f = content.LastIndexOf("f\n", StringComparison.Ordinal);
        Assert.True(w >= 0, "expected a clip W operator");
        Assert.True(q > w, "the clip must be closed by a Q before anything else");
        Assert.True(f > q, "the following fill must paint after the clip's Q");
    }
}
