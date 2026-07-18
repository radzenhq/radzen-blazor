#nullable enable
using System;
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
    public void Columns_AddAutoWidthHasNullWidth()
    {
        var t = new Table();
        var col = t.Columns.Add();
        Assert.Null(col.Width);
        Assert.Null(col.Alignment);
        Assert.Same(col, t.Columns[0]);
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
