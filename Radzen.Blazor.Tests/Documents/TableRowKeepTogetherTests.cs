#nullable enable
using System.Linq;
using Xunit;
using Radzen.Documents;
using Radzen.Documents.Layout;
using Radzen.Documents.Geometry;

namespace Radzen.Blazor.Documents.Tests;

public class TableRowKeepTogetherTests
{
    private static (Table Table, LaidOutTable Layout, Row Tall) BuildTable()
    {
        var fonts = TablePaginationSupport.Fonts();
        var table = TablePaginationSupport.Build(headers: 1, bodies: 0);
        var tall = TablePaginationSupport.AddTallRow(table, 3, "T");
        TablePaginationSupport.AddTallRow(table, 1, "A");
        TablePaginationSupport.AddTallRow(table, 1, "B");
        var layout = IsolatedTableLayout.LayoutIsolated(table, 300, fonts);
        return (table, layout, tall);
    }

    [Fact]
    public void KeepTogetherRowMovesToNextPageIntact()
    {
        var (table, layout, tall) = BuildTable();
        tall.KeepTogether = true;

        var header = layout.RowHeights[0];
        var tallHeight = layout.RowHeights[1];
        var firstAvailable = header + (tallHeight / 2);
        var subsequentAvailable = layout.RowHeights.Sum() + 1;

        var fragments = IsolatedTablePaginator.Paginate(layout, table, firstAvailable, subsequentAvailable);

        Assert.Single(fragments);
        Assert.Equal([1, 2, 3], TablePaginationSupport.BodyRows(fragments[0]));
        Assert.True(fragments[0].Height <= subsequentAvailable + 1e-6);
        var tallRow = fragments[0].Rows.Single(r => r.SourceRow == 1);
        Assert.Equal(tallHeight, tallRow.Height, 6);
    }

    [Fact]
    public void NonKeepTogetherRowStillSplits()
    {
        var (table, layout, _) = BuildTable();

        var header = layout.RowHeights[0];
        var tallHeight = layout.RowHeights[1];
        var firstAvailable = header + (tallHeight / 2);
        var subsequentAvailable = layout.RowHeights.Sum() + 1;

        var fragments = IsolatedTablePaginator.Paginate(layout, table, firstAvailable, subsequentAvailable);

        Assert.Equal(2, fragments.Count);
        Assert.Equal([1], TablePaginationSupport.BodyRows(fragments[0]));
        Assert.Equal([2, 3], TablePaginationSupport.BodyRows(fragments[1]));
        Assert.True(fragments[0].Height > firstAvailable + 1e-6);
    }

    [Fact]
    public void KeepTogetherRowTallerThanPageStillRenders()
    {
        var fonts = TablePaginationSupport.Fonts();
        var table = TablePaginationSupport.Build(headers: 1, bodies: 0);
        var giant = TablePaginationSupport.AddTallRow(table, 10, "G");
        giant.KeepTogether = true;
        var layout = IsolatedTableLayout.LayoutIsolated(table, 300, fonts);

        var header = layout.RowHeights[0];
        var giantHeight = layout.RowHeights[1];
        var firstAvailable = header + (giantHeight / 4);
        var subsequentAvailable = header + (giantHeight / 2);

        var fragments = IsolatedTablePaginator.Paginate(layout, table, firstAvailable, subsequentAvailable);

        Assert.Single(fragments);
        Assert.Equal([1], TablePaginationSupport.BodyRows(fragments[0]));
        Assert.Equal(header + giantHeight, fragments[0].Height, 6);
    }
}
