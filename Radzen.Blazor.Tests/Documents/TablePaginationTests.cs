#nullable enable
using System.Linq;
using Xunit;

using Radzen.Documents;
using Radzen.Documents.Layout;
using Radzen.Documents.Core;
namespace Radzen.Blazor.Documents.Tests;

using Radzen.Blazor.Pdf.Tests;
using Radzen.Blazor.Tests.Isolated;

public class TablePaginationTests
{
    [Fact]
    public void UniformRows_AllHaveLineHeight()
    {
        var fonts = TablePaginationSupport.Fonts();
        var lh = TablePaginationSupport.LineHeight();
        var table = TablePaginationSupport.Build(headers: 1, bodies: 10);
        var layout = IsolatedTableLayout.LayoutIsolated(table, 300, fonts);

        Assert.Equal(11, layout.RowHeights.Length);
        Assert.All(layout.RowHeights, h => Assert.Equal(lh, h, 6));
    }

    [Fact]
    public void TallerThanPage_SplitsIntoThreeFragments()
    {
        var fonts = TablePaginationSupport.Fonts();
        var lh = TablePaginationSupport.LineHeight();
        var table = TablePaginationSupport.Build(headers: 1, bodies: 10);
        var layout = IsolatedTableLayout.LayoutIsolated(table, 300, fonts);

        var fragments = IsolatedTablePaginator.Paginate(layout, table, TablePaginationSupport.Capacity(lh, 5));

        Assert.Equal(3, fragments.Count);
        Assert.Equal([1, 2, 3], fragments.Select(f => f.Number).ToArray());
        Assert.Equal(4, TablePaginationSupport.BodyRows(fragments[0]).Count);
        Assert.Equal(4, TablePaginationSupport.BodyRows(fragments[1]).Count);
        Assert.Equal(2, TablePaginationSupport.BodyRows(fragments[2]).Count);
    }

    [Fact]
    public void EveryFragmentStartsWithTheHeaderRow()
    {
        var fonts = TablePaginationSupport.Fonts();
        var lh = TablePaginationSupport.LineHeight();
        var table = TablePaginationSupport.Build(headers: 1, bodies: 10);
        var layout = IsolatedTableLayout.LayoutIsolated(table, 300, fonts);

        var fragments = IsolatedTablePaginator.Paginate(layout, table, TablePaginationSupport.Capacity(lh, 5));

        foreach (var fragment in fragments)
        {
            Assert.Equal(1, fragment.HeaderRowCount);
            Assert.True(fragment.Rows[0].IsHeader);
            Assert.Equal(0, fragment.Rows[0].SourceRow);
            Assert.Equal(0, fragment.Rows[0].Y, 6);
        }
    }

    [Fact]
    public void NoRowIsSplitMidRow()
    {
        var fonts = TablePaginationSupport.Fonts();
        var lh = TablePaginationSupport.LineHeight();
        var table = TablePaginationSupport.Build(headers: 1, bodies: 10);
        var layout = IsolatedTableLayout.LayoutIsolated(table, 300, fonts);

        var fragments = IsolatedTablePaginator.Paginate(layout, table, TablePaginationSupport.Capacity(lh, 5));

        foreach (var fragment in fragments)
        {
            foreach (var row in fragment.Rows)
            {
                Assert.Equal(layout.RowHeights[row.SourceRow], row.Height, 6);
                Assert.Equal(lh, row.Height, 6);
            }
        }
    }

    [Fact]
    public void BodyRows_CoveredExactlyOnceInOrder()
    {
        var fonts = TablePaginationSupport.Fonts();
        var lh = TablePaginationSupport.LineHeight();
        var table = TablePaginationSupport.Build(headers: 1, bodies: 10);
        var layout = IsolatedTableLayout.LayoutIsolated(table, 300, fonts);

        var fragments = IsolatedTablePaginator.Paginate(layout, table, TablePaginationSupport.Capacity(lh, 5));

        var body = fragments.SelectMany(TablePaginationSupport.BodyRows).ToArray();
        Assert.Equal(Enumerable.Range(1, 10).ToArray(), body);
    }

    [Fact]
    public void FragmentGeometry_IsContiguousFromZero()
    {
        var fonts = TablePaginationSupport.Fonts();
        var lh = TablePaginationSupport.LineHeight();
        var table = TablePaginationSupport.Build(headers: 1, bodies: 10);
        var layout = IsolatedTableLayout.LayoutIsolated(table, 300, fonts);

        var fragments = IsolatedTablePaginator.Paginate(layout, table, TablePaginationSupport.Capacity(lh, 5));

        foreach (var fragment in fragments)
        {
            double y = 0;
            foreach (var row in fragment.Rows)
            {
                Assert.Equal(y, row.Y, 6);
                y += row.Height;
            }

            Assert.Equal(y, fragment.Height, 6);
            Assert.Equal(lh, fragment.Rows[fragment.HeaderRowCount].Y, 6);
        }
    }

    [Fact]
    public void FirstTwoFragments_FillToCapacity()
    {
        var fonts = TablePaginationSupport.Fonts();
        var lh = TablePaginationSupport.LineHeight();
        var table = TablePaginationSupport.Build(headers: 1, bodies: 10);
        var layout = IsolatedTableLayout.LayoutIsolated(table, 300, fonts);
        var available = TablePaginationSupport.Capacity(lh, 5);

        var fragments = IsolatedTablePaginator.Paginate(layout, table, available);

        Assert.Equal(5 * lh, fragments[0].Height, 6);
        Assert.Equal(5 * lh, fragments[1].Height, 6);
        Assert.True(fragments[0].Height <= available + 1e-6);
        Assert.True(fragments[1].Height <= available + 1e-6);
        Assert.Equal(3 * lh, fragments[2].Height, 6);
    }

    [Fact]
    public void LargeTable_ForcesManyFragments()
    {
        var fonts = TablePaginationSupport.Fonts();
        var lh = TablePaginationSupport.LineHeight();
        var table = TablePaginationSupport.Build(headers: 1, bodies: 100);
        var layout = IsolatedTableLayout.LayoutIsolated(table, 300, fonts);

        var fragments = IsolatedTablePaginator.Paginate(layout, table, TablePaginationSupport.Capacity(lh, 5));

        Assert.Equal(25, fragments.Count);
        Assert.All(fragments, f => Assert.True(f.Rows[0].IsHeader));
        Assert.All(fragments, f => Assert.Equal(0, f.Rows[0].SourceRow));
        Assert.Equal(4, TablePaginationSupport.BodyRows(fragments[^1]).Count);

        var body = fragments.SelectMany(TablePaginationSupport.BodyRows).ToArray();
        Assert.Equal(Enumerable.Range(1, 100).ToArray(), body);
    }

    [Fact]
    public void NoHeader_PlainRowSplit()
    {
        var fonts = TablePaginationSupport.Fonts();
        var lh = TablePaginationSupport.LineHeight();
        var table = TablePaginationSupport.Build(headers: 0, bodies: 10);
        var layout = IsolatedTableLayout.LayoutIsolated(table, 300, fonts);

        var fragments = IsolatedTablePaginator.Paginate(layout, table, TablePaginationSupport.Capacity(lh, 4));

        Assert.Equal(3, fragments.Count);
        Assert.All(fragments, f => Assert.Equal(0, f.HeaderRowCount));
        Assert.False(fragments[0].Rows[0].IsHeader);
        Assert.Equal(0, fragments[0].Rows[0].SourceRow);
        Assert.Equal(4, fragments[0].Rows.Length);
        Assert.Equal(2, fragments[^1].Rows.Length);
    }

    [Fact]
    public void SmallTable_FitsInSingleFragment()
    {
        var fonts = TablePaginationSupport.Fonts();
        var lh = TablePaginationSupport.LineHeight();
        var table = TablePaginationSupport.Build(headers: 1, bodies: 3);
        var layout = IsolatedTableLayout.LayoutIsolated(table, 300, fonts);

        var fragments = IsolatedTablePaginator.Paginate(layout, table, TablePaginationSupport.Capacity(lh, 20));

        Assert.Single(fragments);
        Assert.Equal(1, fragments[0].HeaderRowCount);
        Assert.Equal(4, fragments[0].Rows.Length);
        Assert.Equal(4 * lh, fragments[0].Height, 6);
        Assert.Equal([1, 2, 3], TablePaginationSupport.BodyRows(fragments[0]).ToArray());
    }

    [Fact]
    public void OversizedRow_MovesWhole_ToOwnFragment()
    {
        var fonts = TablePaginationSupport.Fonts();
        var lh = TablePaginationSupport.LineHeight();

        var table = new Table();
        table.Columns.Add(Unit.FromPoint(300));
        var header = table.Rows.Add();
        header.RepeatOnEveryPage = true;
        TableLayoutSupport.Fill(header.Cells[0], "H0");
        TableLayoutSupport.Fill(table.Rows.Add().Cells[0], "R0");
        var tall = TablePaginationSupport.AddTallRow(table, 6, "T");
        tall.KeepTogether = true;
        TableLayoutSupport.Fill(table.Rows.Add().Cells[0], "R1");

        var layout = IsolatedTableLayout.LayoutIsolated(table, 300, fonts);
        Assert.Equal(6 * lh, layout.RowHeights[2], 6);

        var available = TablePaginationSupport.Capacity(lh, 5);
        var fragments = IsolatedTablePaginator.Paginate(layout, table, available);

        Assert.Equal(3, fragments.Count);
        Assert.Equal([1], TablePaginationSupport.BodyRows(fragments[0]).ToArray());

        var oversized = fragments[1];
        Assert.Equal([2], TablePaginationSupport.BodyRows(oversized).ToArray());
        Assert.True(oversized.Rows[0].IsHeader);
        Assert.Equal(lh + (6 * lh), oversized.Height, 6);
        Assert.True(oversized.Height > available + 1e-6);

        Assert.Equal([3], TablePaginationSupport.BodyRows(fragments[2]).ToArray());
    }

    [Fact]
    public void RowThatDoesNotFit_StartsNextFragment_NeverSplits()
    {
        var fonts = TablePaginationSupport.Fonts();
        var lh = TablePaginationSupport.LineHeight();
        var table = TablePaginationSupport.Build(headers: 1, bodies: 5);
        var layout = IsolatedTableLayout.LayoutIsolated(table, 300, fonts);

        var fragments = IsolatedTablePaginator.Paginate(layout, table, TablePaginationSupport.Capacity(lh, 4));

        Assert.Equal(2, fragments.Count);
        Assert.Equal([1, 2, 3], TablePaginationSupport.BodyRows(fragments[0]).ToArray());
        Assert.Equal([4, 5], TablePaginationSupport.BodyRows(fragments[1]).ToArray());
    }
}
