#nullable enable
using System.Linq;
using Xunit;

using Radzen.Documents;
using Radzen.Documents.Layout;
namespace Radzen.Blazor.Documents.Tests;

using Radzen.Blazor.Pdf.Tests;

public class FirstTableFragmentRowspanTests
{
    private const double Tol = 1e-6;

    private static Table RowspanTable(int span)
    {
        var table = new Table();
        table.Columns.Add(Unit.FromPoint(150));
        table.Columns.Add(Unit.FromPoint(150));

        var header = table.Rows.Add();
        header.RepeatOnEveryPage = true;
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

        var section = PaginationSupport.Section(400, PaginationSupport.HeightForLines(lh, 6));
        for (var i = 0; i < 4; i++)
        {
            section.Blocks.Add(PaginationSupport.Text($"f{i}"));
        }

        section.Blocks.Add(RowspanTable(span: 3));

        var pages = IsolatedPaginator.PaginateIsolated(section, fonts);

        var contentHeight = PaginationSupport.HeightForLines(lh, 6);
        foreach (var page in pages)
        {
            foreach (var frag in page.Body.Tables)
            {
                Assert.True(
                    frag.Bounds.Y + frag.Fragment.Height <= contentHeight + Tol,
                    $"table fragment bottom {frag.Bounds.Y + frag.Fragment.Height} exceeds content height {contentHeight}");
            }
        }

        var tablePage = pages.Single(p => p.Body.Tables.Length > 0);
        Assert.Empty(tablePage.Body.Lines);
        Assert.Equal(0, tablePage.Body.Tables[0].Bounds.Y, Tol);
        Assert.Equal(4, tablePage.Body.Tables[0].Fragment.Rows.Length);
    }
}
