#nullable enable
using System;
using Xunit;

using Radzen.Documents;
using Radzen.Documents.Layout;
namespace Radzen.Blazor.Documents.Tests;

using Radzen.Blazor.Pdf.Tests;

public class SpanGeometryTests
{
    [Fact]
    public void ColSpan_SumsColumnWidths()
    {
        var fonts = TableLayoutSupport.Fonts();
        var table = new Table();
        table.Columns.Add(Unit.FromPoint(60));
        table.Columns.Add(Unit.FromPoint(80));
        table.Columns.Add(Unit.FromPoint(100));
        var row = table.Rows.Add();
        row.Cells[0].ColumnSpan = 2;
        TableLayoutSupport.Fill(row.Cells[0], "wide");
        TableLayoutSupport.Fill(row.Cells[1], "last");

        var laid = IsolatedTableLayout.LayoutIsolated(table, 1000, fonts);

        Assert.Equal(2, laid.Cells.Length);
        var span = TableLayoutSupport.CellAt(laid, 0, 0);
        Assert.Equal(2, span.ColumnSpan);
        Assert.Equal(0, span.Bounds.X, 6);
        Assert.Equal(140, span.Bounds.Width, 6);

        var trailing = TableLayoutSupport.CellAt(laid, 0, 2);
        Assert.Equal(140, trailing.Bounds.X, 6);
        Assert.Equal(100, trailing.Bounds.Width, 6);
    }

    [Fact]
    public void ColSpan_WithAuthoredOverflowCell_Throws()
    {
        var fonts = TableLayoutSupport.Fonts();
        var table = new Table();
        table.Columns.Add(Unit.FromPoint(100));
        table.Columns.Add(Unit.FromPoint(100));
        var row = table.Rows.Add();
        row.Cells[0].ColumnSpan = 2;
        TableLayoutSupport.Fill(row.Cells[0], "wide");
        TableLayoutSupport.Fill(row.Cells[1], "gone");

        Assert.Throws<InvalidOperationException>(
            () => IsolatedTableLayout.LayoutIsolated(table, 1000, fonts));
    }

    [Fact]
    public void RowSpan_SumsRowHeights_AndShiftsFollowingRow()
    {
        var fonts = TableLayoutSupport.Fonts();
        var lh = TableLayoutSupport.LineHeight(fonts);
        var space = TableLayoutSupport.Measure(fonts, " ", 12);
        var wHello = TableLayoutSupport.Measure(fonts, "Hello", 12);
        var narrow = wHello + (0.5 * space);

        var table = new Table();
        table.Columns.Add(Unit.FromPoint(100));
        table.Columns.Add(Unit.FromPoint(narrow));
        var r0 = table.Rows.Add();
        var r1 = table.Rows.Add();
        r0.Cells[0].RowSpan = 2;
        TableLayoutSupport.Fill(r0.Cells[0], "tall");
        TableLayoutSupport.Fill(r0.Cells[1], "Hello Hello");
        TableLayoutSupport.Fill(r1.Cells[0], "Hi");

        var laid = IsolatedTableLayout.LayoutIsolated(table, 1000, fonts);

        Assert.Equal(3, laid.Cells.Length);
        Assert.Equal(2 * lh, laid.RowHeights[0], 6);
        Assert.Equal(lh, laid.RowHeights[1], 6);

        var span = TableLayoutSupport.CellAt(laid, 0, 0);
        Assert.Equal(2, span.RowSpan);
        Assert.Equal(new Rect(0, 0, 100, 3 * lh), span.Bounds);

        var shifted = TableLayoutSupport.CellAt(laid, 1, 1);
        Assert.Equal(100, shifted.Bounds.X, 6);
        Assert.Equal(2 * lh, shifted.Bounds.Y, 6);
        Assert.Equal(lh, shifted.Bounds.Height, 6);
    }
}
