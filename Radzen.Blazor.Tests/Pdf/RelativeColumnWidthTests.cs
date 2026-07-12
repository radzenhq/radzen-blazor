#nullable enable
using System;
using Radzen.Documents.Pdf;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// Star (relative) column widths: columns without a fixed Width share the remaining
// content width proportionally to Column.RelativeWidth; an unset weight counts as 1,
// and a fixed Width always wins over a weight.
public class RelativeColumnWidthTests
{
    [Fact]
    public void TwoStarColumns_SplitAvailableWidthProportionally()
    {
        var fonts = TableLayoutSupport.Fonts();
        var table = new Table();
        table.Columns.Add().RelativeWidth = 2;
        table.Columns.Add().RelativeWidth = 1;
        TableLayoutSupport.Fill(table.Rows.Add().Cells[0], "x");

        var laid = TableLayout.Layout(table, availableWidth: 300, fonts);

        Assert.Equal(200, laid.ColumnWidths[0], 6);
        Assert.Equal(100, laid.ColumnWidths[1], 6);
        Assert.Equal(300, laid.Width, 6);
    }

    [Fact]
    public void FixedAndStarColumns_StarsShareTheRemainder()
    {
        var fonts = TableLayoutSupport.Fonts();
        var table = new Table();
        table.Columns.Add(Unit.FromPoint(100));
        table.Columns.Add().RelativeWidth = 1;
        table.Columns.Add().RelativeWidth = 2;
        TableLayoutSupport.Fill(table.Rows.Add().Cells[0], "x");

        var laid = TableLayout.Layout(table, availableWidth: 400, fonts);

        Assert.Equal(100, laid.ColumnWidths[0], 6);
        Assert.Equal(100, laid.ColumnWidths[1], 6);
        Assert.Equal(200, laid.ColumnWidths[2], 6);
    }

    [Fact]
    public void AutoColumn_CountsAsWeightOne_NextToStarColumns()
    {
        var fonts = TableLayoutSupport.Fonts();
        var table = new Table();
        table.Columns.Add().RelativeWidth = 3;
        table.Columns.Add();
        TableLayoutSupport.Fill(table.Rows.Add().Cells[0], "x");

        var laid = TableLayout.Layout(table, availableWidth: 400, fonts);

        Assert.Equal(300, laid.ColumnWidths[0], 6);
        Assert.Equal(100, laid.ColumnWidths[1], 6);
    }

    [Fact]
    public void FixedWidth_WinsOverRelativeWidth()
    {
        var fonts = TableLayoutSupport.Fonts();
        var table = new Table();
        var first = table.Columns.Add(Unit.FromPoint(50));
        first.RelativeWidth = 5;
        table.Columns.Add().RelativeWidth = 1;
        TableLayoutSupport.Fill(table.Rows.Add().Cells[0], "x");

        var laid = TableLayout.Layout(table, availableWidth: 250, fonts);

        Assert.Equal(50, laid.ColumnWidths[0], 6);
        Assert.Equal(200, laid.ColumnWidths[1], 6);
    }

    [Fact]
    public void StarColumns_HonorFixedTableWidth()
    {
        var fonts = TableLayoutSupport.Fonts();
        var table = new Table { Width = Unit.FromPoint(90) };
        table.Columns.Add().RelativeWidth = 2;
        table.Columns.Add().RelativeWidth = 1;
        TableLayoutSupport.Fill(table.Rows.Add().Cells[0], "x");

        var laid = TableLayout.Layout(table, availableWidth: 900, fonts);

        Assert.Equal(60, laid.ColumnWidths[0], 6);
        Assert.Equal(30, laid.ColumnWidths[1], 6);
    }

    [Fact]
    public void RelativeWidth_RejectsNonPositiveValues()
    {
        var column = new Column();

        Assert.Throws<ArgumentOutOfRangeException>(() => column.RelativeWidth = 0);
        Assert.Throws<ArgumentOutOfRangeException>(() => column.RelativeWidth = -1);
        column.RelativeWidth = 1.5;
        column.RelativeWidth = null;
        Assert.Null(column.RelativeWidth);
    }
}
