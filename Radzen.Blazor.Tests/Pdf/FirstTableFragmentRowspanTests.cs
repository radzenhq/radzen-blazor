#nullable enable
using System.Linq;
using Radzen.Documents.Pdf;
using Xunit;

using Radzen.Documents.Pdf.Emit;
namespace Radzen.Blazor.Pdf.Tests;

// The pre-table flush check must account for the whole first body ROW GROUP that
// TablePaginator force-places together (a rowspan closure), not just the first body
// row. Otherwise a tall first group is drawn into the tiny space left on the current
// page and spills below the content box.
public class FirstTableFragmentRowspanTests
{
    private const double Tol = 1e-6;

    // Header row (1 line) then a first body row whose column-0 cell spans `span` rows, so
    // the first group is `span` unit-height body rows tall.
    private static Table RowspanTable(int span)
    {
        var table = new Table();
        table.Columns.Add(Unit.FromPoint(150));
        table.Columns.Add(Unit.FromPoint(150));

        var header = table.Rows.Add();
        header.IsHeader = true;
        TableLayoutSupport.Fill(header.Cells[0], "H0");
        TableLayoutSupport.Fill(header.Cells[1], "H1");

        var first = table.Rows.Add();
        TableLayoutSupport.Fill(first.Cells[0], "S");
        first.Cells[0].RowSpan = span;
        TableLayoutSupport.Fill(first.Cells[1], "B0");

        for (var i = 1; i < span; i++)
        {
            var row = table.Rows.Add();
            TableLayoutSupport.Fill(row.Cells[0], $"B{i}");
        }

        return table;
    }

    [Fact]
    public void FirstTableFragment_FlushesWhenRowspanGroupExceedsRemainingSpace()
    {
        var fonts = TableLayoutSupport.Fonts();
        var lh = TableLayoutSupport.LineHeight(fonts);

        // A 6-line content box, 4 filler lines placed first: 2 lines remain, but the
        // header + 3-row rowspan group needs 4 lines.
        var section = PaginationSupport.Section(400, PaginationSupport.HeightForLines(lh, 6));
        for (var i = 0; i < 4; i++)
        {
            section.Blocks.Add(PaginationSupport.Text($"f{i}"));
        }

        section.Blocks.Add(RowspanTable(span: 3));

        var pages = Paginator.Paginate(section, fonts);

        var contentHeight = PaginationSupport.HeightForLines(lh, 6);
        foreach (var page in pages)
        {
            foreach (var frag in page.Tables)
            {
                Assert.True(
                    frag.Y + frag.Fragment.Height <= contentHeight + Tol,
                    $"table fragment bottom {frag.Y + frag.Fragment.Height} exceeds content height {contentHeight}");
            }
        }

        // The table must move to its own fresh page rather than share the filler page.
        var tablePage = pages.Single(p => p.Tables.Count > 0);
        Assert.Empty(tablePage.Lines);
        Assert.Equal(0, tablePage.Tables[0].Y, Tol);
        Assert.Equal(4, tablePage.Tables[0].Fragment.Rows.Count);
    }
}
