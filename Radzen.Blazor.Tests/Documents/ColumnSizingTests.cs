#nullable enable
using Xunit;

using Radzen.Documents;
using Radzen.Documents.Layout;
namespace Radzen.Blazor.Documents.Tests;

using Radzen.Blazor.Pdf.Tests;
using Radzen.Blazor.Tests.Isolated;

public class ColumnSizingTests
{
    [Fact]
    public void AllFixedColumns_WidthsHonored_AvailableIgnored()
    {
        var fonts = TableLayoutSupport.Fonts();
        var table = new Table();
        table.Columns.Add(Unit.FromPoint(60));
        table.Columns.Add(Unit.FromPoint(90));
        table.Columns.Add(Unit.FromPoint(120));
        TableLayoutSupport.Fill(table.Rows.Add().Cells[0], "x");
        TableLayoutSupport.Fill(table.Rows.Add().Cells[1], "y");

        var laid = IsolatedTableLayout.LayoutIsolated(table, availableWidth: 1000, fonts);

        Assert.Equal(60, laid.ColumnWidths[0], 6);
        Assert.Equal(90, laid.ColumnWidths[1], 6);
        Assert.Equal(120, laid.ColumnWidths[2], 6);
        Assert.Equal(270, laid.Width, 6);
    }

    [Fact]
    public void MixedFixedAndAuto_AutoSplitsRemainder()
    {
        var fonts = TableLayoutSupport.Fonts();
        var table = new Table();
        table.Columns.Add(Unit.FromPoint(100));
        table.Columns.Add();
        table.Columns.Add();
        TableLayoutSupport.Fill(table.Rows.Add().Cells[0], "x");

        var laid = IsolatedTableLayout.LayoutIsolated(table, availableWidth: 400, fonts);

        Assert.Equal(100, laid.ColumnWidths[0], 6);
        Assert.Equal(150, laid.ColumnWidths[1], 6);
        Assert.Equal(150, laid.ColumnWidths[2], 6);
        Assert.Equal(400, laid.Width, 6);
    }

    [Fact]
    public void ExplicitNullWidthColumn_BehavesAsAuto()
    {
        var fonts = TableLayoutSupport.Fonts();
        var table = new Table();
        var fixedCol = table.Columns.Add(Unit.FromPoint(80));
        var autoCol = table.Columns.Add();
        autoCol.Width = null;
        TableLayoutSupport.Fill(table.Rows.Add().Cells[0], "x");

        var laid = IsolatedTableLayout.LayoutIsolated(table, availableWidth: 300, fonts);

        Assert.NotNull(fixedCol.Width);
        Assert.Null(autoCol.Width);
        Assert.Equal(80, laid.ColumnWidths[0], 6);
        Assert.Equal(220, laid.ColumnWidths[1], 6);
    }

    [Fact]
    public void MultipleAuto_UnevenRemainder_SplitEquallyAndFractionally()
    {
        var fonts = TableLayoutSupport.Fonts();
        var table = new Table();
        table.Columns.Add();
        table.Columns.Add();
        table.Columns.Add();
        TableLayoutSupport.Fill(table.Rows.Add().Cells[0], "x");

        var laid = IsolatedTableLayout.LayoutIsolated(table, availableWidth: 100, fonts);

        var each = 100.0 / 3.0;
        Assert.Equal(each, laid.ColumnWidths[0], 6);
        Assert.Equal(each, laid.ColumnWidths[1], 6);
        Assert.Equal(each, laid.ColumnWidths[2], 6);
        Assert.Equal(100, laid.Width, 6);
    }

    [Fact]
    public void TableWidth_OverridesAvailableForAutoDistribution()
    {
        var fonts = TableLayoutSupport.Fonts();
        var table = new Table { Width = Unit.FromPoint(500) };
        table.Columns.Add(Unit.FromPoint(100));
        table.Columns.Add();
        TableLayoutSupport.Fill(table.Rows.Add().Cells[0], "x");

        var laid = IsolatedTableLayout.LayoutIsolated(table, availableWidth: 250, fonts);

        Assert.Equal(100, laid.ColumnWidths[0], 6);
        Assert.Equal(400, laid.ColumnWidths[1], 6);
        Assert.Equal(500, laid.Width, 6);
    }
}
