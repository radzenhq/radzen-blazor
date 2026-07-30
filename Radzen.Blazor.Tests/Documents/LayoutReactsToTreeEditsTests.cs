#nullable enable
using System.Collections.Generic;
using System.Linq;
using Radzen.Documents;
using Radzen.Documents.Layout;
using Xunit;

namespace Radzen.Blazor.Documents.Tests;

public class LayoutReactsToTreeEditsTests
{
    private static List<string> LaidOutText(Document document)
        => DocumentLayouter.Layout(document).Pages
            .SelectMany(page => page.Body.Lines)
            .Select(line => string.Concat(line.Line.Fragments.Where(fragment => !fragment.IsMarker).Select(fragment => fragment.Text)))
            .ToList();

    private static Document WithParagraphs(params string[] texts)
    {
        var document = new Document();
        var section = document.Sections.Add();
        foreach (var text in texts)
        {
            section.Blocks.AddParagraph(text);
        }

        return document;
    }

    [Fact]
    public void MutationAfterLayout_ProducesAFreshLayout()
    {
        var document = WithParagraphs("a", "b");
        Assert.Equal(["a", "b"], LaidOutText(document));

        document.Sections[0].Blocks.RemoveAt(0);
        Assert.Equal(["b"], LaidOutText(document));

        document.Sections[0].Blocks.AddParagraph("c");
        Assert.Equal(["b", "c"], LaidOutText(document));

        document.Sections[0].Blocks.Move(0, 1);
        Assert.Equal(["c", "b"], LaidOutText(document));
    }

    [Fact]
    public void TableMutation_ChangesTheLaidOutTable()
    {
        var document = WithParagraphs();
        var table = document.Sections[0].Blocks.AddTable();
        table.Columns.Add();
        table.Rows.Add().Cells[0].Text = "a";
        table.Rows.Add().Cells[0].Text = "b";

        var before = DocumentLayouter.Layout(document).Pages[0].Body.Tables[0].Layout.RowHeights.Length;
        table.Rows.RemoveAt(0);
        var after = DocumentLayouter.Layout(document).Pages[0].Body.Tables[0].Layout.RowHeights.Length;

        Assert.Equal(2, before);
        Assert.Equal(1, after);
    }
}
