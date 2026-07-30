#nullable enable

using System.Collections.Generic;
using Radzen.Documents.Pdf;
using Xunit;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

// ISO 32000-1 8.4.2 graphics state; stroke state confined to a q..Q scope.
public class PathContentStrokeScopeTests
{
    private static IReadOnlyList<string> Operators(params PathContent[] paths)
    {
        var document = new PortableDocument();
        var page = document.Pages.Add();
        foreach (var path in paths)
        {
            page.Content.Add(path);
        }

        var content = ContentTestHelpers.PageContent(ContentTestHelpers.Reload(document), 0);
        return ContentStreamTokenizer.Operators(content);
    }

    private static PathContent Line()
    {
        var path = new PathContent { Stroke = true };
        path.MoveTo(0, 0);
        path.LineTo(10, 10);
        return path;
    }

    private static int IndexOf(IReadOnlyList<string> operators, string op, int from = 0)
    {
        for (var i = from; i < operators.Count; i++)
        {
            if (operators[i] == op)
            {
                return i;
            }
        }

        return -1;
    }

    [Fact]
    public void DashedStroke_IsWrappedInSaveRestore()
    {
        var path = Line();
        path.SetDash([3, 2], 0);
        path.Cap = LineCap.Round;

        var operators = Operators(path);

        var q = IndexOf(operators, "q");
        var d = IndexOf(operators, "d");
        var s = IndexOf(operators, "S");
        var restore = IndexOf(operators, "Q");

        Assert.True(q >= 0, "expected a q");
        Assert.True(q < d, "the dash operator must be inside the saved state");
        Assert.True(d < s, "the stroke paints after the dash is set");
        Assert.True(s < restore, "the saved state is restored after the paint");
    }

    [Fact]
    public void PlainStroke_AfterDashedStroke_DoesNotInheritDash()
    {
        var dashed = Line();
        dashed.SetDash([3, 2], 0);

        var operators = Operators(dashed, Line());

        var first = IndexOf(operators, "d");
        Assert.True(first >= 0);
        Assert.Equal(-1, IndexOf(operators, "d", first + 1));

        var restore = IndexOf(operators, "Q", first);
        var firstStroke = IndexOf(operators, "S");
        var secondStroke = IndexOf(operators, "S", firstStroke + 1);
        Assert.True(restore >= 0 && secondStroke >= 0);
        Assert.True(restore < secondStroke, "the dash scope must close before the next path");
    }

    [Fact]
    public void PlainStroke_EmitsNoSaveRestore()
    {
        Assert.Equal(-1, IndexOf(Operators(Line()), "q"));
    }
}
