#nullable enable
using System;
using System.Collections.Generic;
using Radzen.Documents.Pdf;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

public class TableTests
{
    [Fact]
    public void Table_Defaults()
    {
        var t = new Table();
        Assert.NotNull(t.Columns);
        Assert.NotNull(t.Rows);
        Assert.NotNull(t.Borders);
        Assert.Empty(t.Columns);
        Assert.Empty(t.Rows);
        Assert.Null(t.Width);
    }

    [Fact]
    public void Collections_AreReadOnlyLists()
    {
        var t = new Table();
        Assert.IsAssignableFrom<IReadOnlyList<Column>>(t.Columns);
        Assert.IsAssignableFrom<IReadOnlyList<Row>>(t.Rows);
    }

    [Fact]
    public void Columns_AddAutoWidthHasNullWidth()
    {
        var t = new Table();
        var col = t.Columns.Add();
        Assert.Null(col.Width);
        Assert.Null(col.Alignment);
        Assert.Same(col, t.Columns[0]);
    }

    [Fact]
    public void Columns_AddWithWidthSetsWidth()
    {
        var t = new Table();
        var col = t.Columns.Add(Unit.FromPoint(80));
        Assert.NotNull(col.Width);
        Assert.Equal(80, col.Width!.Value.Point, 9);
    }

    [Fact]
    public void Column_PropertiesSettable()
    {
        var col = new Table().Columns.Add();
        col.Width = Unit.FromPoint(40);
        col.Alignment = HorizontalAlignment.Right;
        Assert.Equal(40, col.Width!.Value.Point, 9);
        Assert.Equal(HorizontalAlignment.Right, col.Alignment);
    }

    [Fact]
    public void ColumnsThenRow_MaterializesCellPerColumn()
    {
        var t = new Table();
        t.Columns.Add();
        t.Columns.Add();
        var row = t.Rows.Add();
        Assert.Equal(2, row.Cells.Count);
    }

    [Fact]
    public void RowWithNoColumns_HasNoCells()
    {
        var t = new Table();
        var row = t.Rows.Add();
        Assert.Empty(row.Cells);
    }

    [Fact]
    public void ColumnAddedAfterRows_RetrofitsCellToEveryRow()
    {
        var t = new Table();
        t.Columns.Add();
        t.Columns.Add();
        var r1 = t.Rows.Add();
        var r2 = t.Rows.Add();
        Assert.Equal(2, r1.Cells.Count);
        Assert.Equal(2, r2.Cells.Count);

        t.Columns.Add();
        Assert.Equal(3, r1.Cells.Count);
        Assert.Equal(3, r2.Cells.Count);
    }

    [Fact]
    public void Cells_IndexerAccess()
    {
        var t = new Table();
        t.Columns.Add();
        t.Columns.Add();
        var row = t.Rows.Add();
        Assert.NotNull(row.Cells[0]);
        Assert.NotNull(row.Cells[1]);
        Assert.NotSame(row.Cells[0], row.Cells[1]);
    }

    [Fact]
    public void Table_WidthSettable()
    {
        var t = new Table();
        t.Width = Unit.FromPoint(500);
        Assert.Equal(500, t.Width!.Value.Point, 9);
    }

    [Fact]
    public void Row_Defaults()
    {
        var t = new Table();
        t.Columns.Add();
        var row = t.Rows.Add();
        Assert.False(row.IsHeader);
        Assert.False(row.KeepTogether);
        Assert.NotNull(row.Font);
        Assert.Equal(HorizontalAlignment.Left, row.Alignment);
    }

    [Fact]
    public void Row_PropertiesSettable()
    {
        var t = new Table();
        t.Columns.Add();
        var row = t.Rows.Add();
        row.IsHeader = true;
        row.KeepTogether = true;
        row.Background = Color.LightGray;
        row.Alignment = HorizontalAlignment.Center;
        Assert.True(row.IsHeader);
        Assert.True(row.KeepTogether);
        Assert.Equal(Color.LightGray, row.Background);
        Assert.Equal(HorizontalAlignment.Center, row.Alignment);
    }

    [Fact]
    public void Cell_Defaults()
    {
        var t = new Table();
        t.Columns.Add();
        var cell = t.Rows.Add().Cells[0];
        Assert.NotNull(cell.Blocks);
        Assert.Empty(cell.Blocks);
        Assert.Null(cell.Text);
        Assert.Equal(1, cell.ColumnSpan);
        Assert.Equal(1, cell.RowSpan);
        Assert.NotNull(cell.Font);
        Assert.Equal(VerticalAlignment.Top, cell.VerticalAlignment);
        Assert.Equal(0, cell.Padding.Point, 9);
        Assert.NotNull(cell.Borders);
    }

    [Fact]
    public void Cell_TextSet_ReplacesBlocksWithSingleParagraph()
    {
        var t = new Table();
        t.Columns.Add();
        var cell = t.Rows.Add().Cells[0];
        cell.Blocks.AddParagraph("old");
        cell.Blocks.AddParagraph("more");
        cell.Text = "new";
        Assert.Single(cell.Blocks);
        var p = Assert.IsType<Paragraph>(cell.Blocks[0]);
        Assert.Equal("new", p.Text);
        Assert.Equal("new", cell.Text);
    }

    [Fact]
    public void Cell_TextGet_NullWhenNotExactlyOneParagraph()
    {
        var t = new Table();
        t.Columns.Add();
        var cell = t.Rows.Add().Cells[0];
        Assert.Null(cell.Text);

        cell.Blocks.AddParagraph("a");
        cell.Blocks.AddParagraph("b");
        Assert.Null(cell.Text);
    }

    [Fact]
    public void Cell_TextGet_ReturnsSingleParagraphText()
    {
        var t = new Table();
        t.Columns.Add();
        var cell = t.Rows.Add().Cells[0];
        cell.Blocks.AddParagraph("solo");
        Assert.Equal("solo", cell.Text);
    }

    [Fact]
    public void Cell_PropertiesSettable()
    {
        var t = new Table();
        t.Columns.Add();
        var cell = t.Rows.Add().Cells[0];
        cell.ColumnSpan = 2;
        cell.RowSpan = 3;
        cell.Background = Color.Yellow;
        cell.Alignment = HorizontalAlignment.Right;
        cell.VerticalAlignment = VerticalAlignment.Middle;
        cell.Padding = Unit.FromPoint(4);
        Assert.Equal(2, cell.ColumnSpan);
        Assert.Equal(3, cell.RowSpan);
        Assert.Equal(Color.Yellow, cell.Background);
        Assert.Equal(HorizontalAlignment.Right, cell.Alignment);
        Assert.Equal(VerticalAlignment.Middle, cell.VerticalAlignment);
        Assert.Equal(4, cell.Padding.Point, 9);
    }

    [Fact]
    public void Rows_RemoveAt_RemovesTargetRowAndKeepsOthers()
    {
        var t = new Table();
        t.Columns.Add();
        var r0 = t.Rows.Add();
        r0.Cells[0].Text = "zero";
        var r1 = t.Rows.Add();
        r1.Cells[0].Text = "one";
        var r2 = t.Rows.Add();
        r2.Cells[0].Text = "two";

        t.Rows.RemoveAt(1);

        Assert.Equal(2, t.Rows.Count);
        Assert.Same(r0, t.Rows[0]);
        Assert.Same(r2, t.Rows[1]);
        Assert.Equal("zero", t.Rows[0].Cells[0].Text);
        Assert.Equal("two", t.Rows[1].Cells[0].Text);
    }

    [Fact]
    public void Rows_Remove_RemovesMatchingRow()
    {
        var t = new Table();
        t.Columns.Add();
        var r0 = t.Rows.Add();
        var r1 = t.Rows.Add();

        Assert.True(t.Rows.Remove(r0));
        Assert.Single(t.Rows);
        Assert.Same(r1, t.Rows[0]);
        Assert.False(t.Rows.Remove(r0));
    }

    [Fact]
    public void Rows_RemoveAt_OutOfRange_Throws()
    {
        var t = new Table();
        t.Columns.Add();
        t.Rows.Add();
        Assert.Throws<ArgumentOutOfRangeException>(() => t.Rows.RemoveAt(5));
    }

    [Fact]
    public void Cell_ColumnSpanLessThanOne_Throws()
    {
        var t = new Table();
        t.Columns.Add();
        var cell = t.Rows.Add().Cells[0];
        Assert.Throws<ArgumentOutOfRangeException>(() => cell.ColumnSpan = 0);
    }

    [Fact]
    public void Cell_RowSpanLessThanOne_Throws()
    {
        var t = new Table();
        t.Columns.Add();
        var cell = t.Rows.Add().Cells[0];
        Assert.Throws<ArgumentOutOfRangeException>(() => cell.RowSpan = 0);
    }
}
