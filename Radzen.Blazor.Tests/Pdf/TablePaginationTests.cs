#nullable enable
using System.Linq;
using Radzen.Documents.Pdf;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// L4b: how a laid-out table splits across page fragments given a uniform available height.
public class TablePaginationTests
{
    [Fact]
    public void UniformRows_AllHaveLineHeight()
    {
        var fonts = TablePaginationSupport.Fonts();
        var lh = TablePaginationSupport.LineHeight(fonts);
        var table = TablePaginationSupport.Build(headers: 1, bodies: 10);
        var layout = TableLayout.Layout(table, 300, fonts);

        Assert.Equal(11, layout.RowHeights.Count);
        Assert.All(layout.RowHeights, h => Assert.Equal(lh, h, 6));
    }

    [Fact]
    public void TallerThanPage_SplitsIntoThreeFragments()
    {
        var fonts = TablePaginationSupport.Fonts();
        var lh = TablePaginationSupport.LineHeight(fonts);
        var table = TablePaginationSupport.Build(headers: 1, bodies: 10);
        var layout = TableLayout.Layout(table, 300, fonts);

        // Fits header + 4 body rows per fragment: 4 + 4 + 2 = 3 fragments.
        var fragments = TablePaginator.Paginate(layout, table, TablePaginationSupport.Capacity(lh, 5));

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
        var lh = TablePaginationSupport.LineHeight(fonts);
        var table = TablePaginationSupport.Build(headers: 1, bodies: 10);
        var layout = TableLayout.Layout(table, 300, fonts);

        var fragments = TablePaginator.Paginate(layout, table, TablePaginationSupport.Capacity(lh, 5));

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
        var lh = TablePaginationSupport.LineHeight(fonts);
        var table = TablePaginationSupport.Build(headers: 1, bodies: 10);
        var layout = TableLayout.Layout(table, 300, fonts);

        var fragments = TablePaginator.Paginate(layout, table, TablePaginationSupport.Capacity(lh, 5));

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
        var lh = TablePaginationSupport.LineHeight(fonts);
        var table = TablePaginationSupport.Build(headers: 1, bodies: 10);
        var layout = TableLayout.Layout(table, 300, fonts);

        var fragments = TablePaginator.Paginate(layout, table, TablePaginationSupport.Capacity(lh, 5));

        var body = fragments.SelectMany(TablePaginationSupport.BodyRows).ToArray();
        // Source rows 1..10 are the body (row 0 is the header).
        Assert.Equal(Enumerable.Range(1, 10).ToArray(), body);
    }

    [Fact]
    public void FragmentGeometry_IsContiguousFromZero()
    {
        var fonts = TablePaginationSupport.Fonts();
        var lh = TablePaginationSupport.LineHeight(fonts);
        var table = TablePaginationSupport.Build(headers: 1, bodies: 10);
        var layout = TableLayout.Layout(table, 300, fonts);

        var fragments = TablePaginator.Paginate(layout, table, TablePaginationSupport.Capacity(lh, 5));

        foreach (var fragment in fragments)
        {
            double y = 0;
            foreach (var row in fragment.Rows)
            {
                Assert.Equal(y, row.Y, 6);
                y += row.Height;
            }

            Assert.Equal(y, fragment.Height, 6);
            // First body row sits directly below the repeated header.
            Assert.Equal(lh, fragment.Rows[fragment.HeaderRowCount].Y, 6);
        }
    }

    [Fact]
    public void FirstTwoFragments_FillToCapacity()
    {
        var fonts = TablePaginationSupport.Fonts();
        var lh = TablePaginationSupport.LineHeight(fonts);
        var table = TablePaginationSupport.Build(headers: 1, bodies: 10);
        var layout = TableLayout.Layout(table, 300, fonts);
        var available = TablePaginationSupport.Capacity(lh, 5);

        var fragments = TablePaginator.Paginate(layout, table, available);

        // Header + 4 body == 5 rows == 5*lh, which is <= 5.4*lh and cannot grow to 6*lh.
        Assert.Equal(5 * lh, fragments[0].Height, 6);
        Assert.Equal(5 * lh, fragments[1].Height, 6);
        Assert.True(fragments[0].Height <= available + 1e-6);
        Assert.True(fragments[1].Height <= available + 1e-6);
        // Last fragment: header + 2 body == 3*lh.
        Assert.Equal(3 * lh, fragments[2].Height, 6);
    }

    [Fact]
    public void LargeTable_ForcesManyFragments()
    {
        var fonts = TablePaginationSupport.Fonts();
        var lh = TablePaginationSupport.LineHeight(fonts);
        var table = TablePaginationSupport.Build(headers: 1, bodies: 100);
        var layout = TableLayout.Layout(table, 300, fonts);

        // Header + 4 body per fragment -> ceil(100 / 4) == 25 fragments.
        var fragments = TablePaginator.Paginate(layout, table, TablePaginationSupport.Capacity(lh, 5));

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
        var lh = TablePaginationSupport.LineHeight(fonts);
        var table = TablePaginationSupport.Build(headers: 0, bodies: 10);
        var layout = TableLayout.Layout(table, 300, fonts);

        // No header consumes space: 4 body rows per fragment -> 4 + 4 + 2.
        var fragments = TablePaginator.Paginate(layout, table, TablePaginationSupport.Capacity(lh, 4));

        Assert.Equal(3, fragments.Count);
        Assert.All(fragments, f => Assert.Equal(0, f.HeaderRowCount));
        Assert.False(fragments[0].Rows[0].IsHeader);
        Assert.Equal(0, fragments[0].Rows[0].SourceRow);
        Assert.Equal(4, fragments[0].Rows.Count);
        Assert.Equal(2, fragments[^1].Rows.Count);
    }

    [Fact]
    public void SmallTable_FitsInSingleFragment()
    {
        var fonts = TablePaginationSupport.Fonts();
        var lh = TablePaginationSupport.LineHeight(fonts);
        var table = TablePaginationSupport.Build(headers: 1, bodies: 3);
        var layout = TableLayout.Layout(table, 300, fonts);

        var fragments = TablePaginator.Paginate(layout, table, TablePaginationSupport.Capacity(lh, 20));

        Assert.Single(fragments);
        Assert.Equal(1, fragments[0].HeaderRowCount);
        Assert.Equal(4, fragments[0].Rows.Count);
        Assert.Equal(4 * lh, fragments[0].Height, 6);
        Assert.Equal([1, 2, 3], TablePaginationSupport.BodyRows(fragments[0]).ToArray());
    }

    [Fact]
    public void OversizedRow_MovesWhole_ToOwnFragment()
    {
        var fonts = TablePaginationSupport.Fonts();
        var lh = TablePaginationSupport.LineHeight(fonts);

        var table = new Table();
        table.Columns.Add(Unit.FromPoint(300));
        var header = table.Rows.Add();
        header.IsHeader = true;
        TableLayoutSupport.Fill(header.Cells[0], "H0");
        TableLayoutSupport.Fill(table.Rows.Add().Cells[0], "R0");
        // A 6-line-tall atomic row that cannot fit even alongside the header.
        var tall = TablePaginationSupport.AddTallRow(table, 6, "T");
        tall.KeepTogether = true;
        TableLayoutSupport.Fill(table.Rows.Add().Cells[0], "R1");

        var layout = TableLayout.Layout(table, 300, fonts);
        Assert.Equal(6 * lh, layout.RowHeights[2], 6);

        var available = TablePaginationSupport.Capacity(lh, 5);
        var fragments = TablePaginator.Paginate(layout, table, available);

        // Fragment 1: header + R0. Fragment 2: header + oversized row (overflows). Fragment 3: header + R1.
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
        var lh = TablePaginationSupport.LineHeight(fonts);
        var table = TablePaginationSupport.Build(headers: 1, bodies: 5);
        var layout = TableLayout.Layout(table, 300, fonts);

        // Header + 3 body per fragment -> 3 + 2.
        var fragments = TablePaginator.Paginate(layout, table, TablePaginationSupport.Capacity(lh, 4));

        Assert.Equal(2, fragments.Count);
        Assert.Equal([1, 2, 3], TablePaginationSupport.BodyRows(fragments[0]).ToArray());
        Assert.Equal([4, 5], TablePaginationSupport.BodyRows(fragments[1]).ToArray());
    }
}
