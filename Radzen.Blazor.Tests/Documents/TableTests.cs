#nullable enable
using System;
using Xunit;
using Radzen.Documents;

namespace Radzen.Blazor.Documents.Tests;

public class TableTests
{
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
    public void Cell_TextSet_ReplacesBlocksWithSingleParagraph()
    {
        var t = new Table();
        t.Columns.Add();
        var cell = t.Rows.Add().Cells[0];
        cell.Blocks.Add(new Paragraph("old"));
        cell.Blocks.Add(new Paragraph("more"));
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

        cell.Blocks.Add(new Paragraph("a"));
        cell.Blocks.Add(new Paragraph("b"));
        Assert.Null(cell.Text);
    }

    [Fact]
    public void Cell_TextGet_ReturnsSingleParagraphText()
    {
        var t = new Table();
        t.Columns.Add();
        var cell = t.Rows.Add().Cells[0];
        cell.Blocks.Add(new Paragraph("solo"));
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
