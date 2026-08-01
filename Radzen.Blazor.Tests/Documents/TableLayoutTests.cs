#nullable enable
using System.Linq;
using System;
using Radzen.Documents.Fonts;
using Radzen.Documents.LaidOut;
using Radzen.Documents.Layout;
using Radzen.Documents;
using Xunit;
using Radzen.Documents.Core;

namespace Radzen.Blazor.Documents.Tests;

using Radzen.Blazor.Pdf.Tests;
using Radzen.Blazor.Tests.Isolated;

public class TableLayoutTests
{
    private const int Precision = 6;

    private static void AssertRect(Rect expected, Rect actual)
    {
        Assert.Equal(expected.X, actual.X, Precision);
        Assert.Equal(expected.Y, actual.Y, Precision);
        Assert.Equal(expected.Width, actual.Width, Precision);
        Assert.Equal(expected.Height, actual.Height, Precision);
    }

    [Fact]
    public void RowWithMoreCellsThanColumns_Throws()
    {
        var table = new Table();
        table.Columns.Add(Unit.FromPoint(100));
        table.Columns.Add(Unit.FromPoint(100));
        var row = table.Rows.Add();
        row.Cells[0].ColumnSpan = 2;
        row.Cells[1].Text = "overflow";

        var exception = Assert.Throws<InvalidOperationException>(
            () => IsolatedTableLayout.LayoutIsolated(table, 200, TableLayoutSupport.Fonts()));

        Assert.Contains("more cells", exception.Message);
    }

    [Fact]
    public void Grid2x2_FixedColumns_CellRectangles()
    {
        var fonts = TableLayoutSupport.Fonts();
        var lh = TableLayoutSupport.LineHeight();
        var table = new Table();
        table.Columns.Add(Unit.FromPoint(100));
        table.Columns.Add(Unit.FromPoint(150));
        var r0 = table.Rows.Add();
        var r1 = table.Rows.Add();
        TableLayoutSupport.Fill(r0.Cells[0], "a");
        TableLayoutSupport.Fill(r0.Cells[1], "b");
        TableLayoutSupport.Fill(r1.Cells[0], "c");
        TableLayoutSupport.Fill(r1.Cells[1], "d");

        var laid = IsolatedTableLayout.LayoutIsolated(table, 300, fonts);

        Assert.Equal(250, laid.Width, 6);
        Assert.Equal(2 * lh, laid.Height, 6);

        var c00 = TableLayoutSupport.CellAt(laid, 0, 0).Bounds;
        AssertRect(new Rect(0, 0, 100, lh), c00);
        var c01 = TableLayoutSupport.CellAt(laid, 0, 1).Bounds;
        AssertRect(new Rect(100, 0, 150, lh), c01);
        var c10 = TableLayoutSupport.CellAt(laid, 1, 0).Bounds;
        AssertRect(new Rect(0, lh, 100, lh), c10);
        var c11 = TableLayoutSupport.CellAt(laid, 1, 1).Bounds;
        AssertRect(new Rect(100, lh, 150, lh), c11);
    }

    [Fact]
    public void Padding_InsetsContentBox_AndShiftsContent()
    {
        var fonts = TableLayoutSupport.Fonts();
        var lh = TableLayoutSupport.LineHeight();
        var table = new Table();
        table.Columns.Add(Unit.FromPoint(200));
        var cell = table.Rows.Add().Cells[0];
        cell.Padding = Unit.FromPoint(5);
        TableLayoutSupport.Fill(cell, "Hello");

        var laid = IsolatedTableLayout.LayoutIsolated(table, 200, fonts);
        var c = TableLayoutSupport.CellAt(laid, 0, 0);

        AssertRect(new Rect(0, 0, 200, lh + 10), c.Bounds);
        AssertRect(new Rect(5, 5, 190, lh), c.ContentBox);
        Assert.Equal(5, c.Lines[0].X, 6);
        Assert.Equal(5, c.Lines[0].Y, 6);
    }

    [Fact]
    public void RowHeight_IsMaxCellContentHeightPlusPadding()
    {
        var fonts = TableLayoutSupport.Fonts();
        var lh = TableLayoutSupport.LineHeight();
        const double Pad = 4;
        var space = TableLayoutSupport.Measure(fonts, " ", 12);
        var wHello = TableLayoutSupport.Measure(fonts, "Hello", 12);

        var narrowContent = wHello + (0.5 * space);
        var table = new Table();
        table.Columns.Add(Unit.FromPoint(narrowContent + (2 * Pad)));
        table.Columns.Add(Unit.FromPoint(300));
        var row = table.Rows.Add();
        row.Cells[0].Padding = Unit.FromPoint(Pad);
        row.Cells[1].Padding = Unit.FromPoint(Pad);
        TableLayoutSupport.Fill(row.Cells[0], "Hello Hello");
        TableLayoutSupport.Fill(row.Cells[1], "Hello");

        var laid = IsolatedTableLayout.LayoutIsolated(table, 1000, fonts);
        var wrapped = TableLayoutSupport.CellAt(laid, 0, 0);
        var single = TableLayoutSupport.CellAt(laid, 0, 1);

        Assert.Equal(2, wrapped.Lines.Length);
        Assert.Single(single.Lines);
        Assert.Equal((2 * lh) + (2 * Pad), laid.RowHeights[0], 6);
        Assert.Equal((2 * lh) + (2 * Pad), single.Bounds.Height, 6);
    }

    [Fact]
    public void CellContent_LineBrokenAtContentBoxWidth()
    {
        var fonts = TableLayoutSupport.Fonts();
        var space = TableLayoutSupport.Measure(fonts, " ", 12);
        var wHello = TableLayoutSupport.Measure(fonts, "Hello", 12);
        var narrowContent = wHello + (0.5 * space);

        var table = new Table();
        table.Columns.Add(Unit.FromPoint(narrowContent));
        TableLayoutSupport.Fill(table.Rows.Add().Cells[0], "Hello Hello");

        var laid = IsolatedTableLayout.LayoutIsolated(table, 400, fonts);
        var c = TableLayoutSupport.CellAt(laid, 0, 0);

        Assert.Equal(2, c.Lines.Length);
        Assert.Equal("Hello", string.Concat(c.Lines[0].Line.Fragments.Select(f => f.Text)));
        Assert.Equal("Hello", string.Concat(c.Lines[1].Line.Fragments.Select(f => f.Text)));
    }

    [Theory]
    [InlineData(VerticalAlignment.Top, 0.0)]
    [InlineData(VerticalAlignment.Middle, 0.5)]
    [InlineData(VerticalAlignment.Bottom, 1.0)]
    public void VerticalAlignment_OffsetsContentWithinRow(VerticalAlignment valign, double factor)
    {
        var fonts = TableLayoutSupport.Fonts();
        var lh = TableLayoutSupport.LineHeight();
        var space = TableLayoutSupport.Measure(fonts, " ", 12);
        var widest = new[] { "Hello", "World", "Again" }
            .Select(w => TableLayoutSupport.Measure(fonts, w, 12)).Max();
        var narrowContent = widest + (0.5 * space);

        var table = new Table();
        table.Columns.Add(Unit.FromPoint(narrowContent));
        table.Columns.Add(Unit.FromPoint(120));
        var row = table.Rows.Add();
        TableLayoutSupport.Fill(row.Cells[0], "Hello World Again");
        var target = row.Cells[1];
        target.VerticalAlignment = valign;
        TableLayoutSupport.Fill(target, "Hi");

        var laid = IsolatedTableLayout.LayoutIsolated(table, 1000, fonts);
        Assert.Equal(3 * lh, laid.RowHeights[0], 6);

        var c = TableLayoutSupport.CellAt(laid, 0, 1);
        var expected = ((3 * lh) - lh) * factor;
        Assert.Single(c.Lines);
        Assert.Equal(expected, c.Lines[0].Y, 6);
    }

    [Fact]
    public void HorizontalAlignment_Default_IsLeft()
    {
        var fonts = TableLayoutSupport.Fonts();
        var table = new Table();
        table.Columns.Add(Unit.FromPoint(200));
        TableLayoutSupport.Fill(table.Rows.Add().Cells[0], "Hello");

        var laid = IsolatedTableLayout.LayoutIsolated(table, 200, fonts);
        var c = TableLayoutSupport.CellAt(laid, 0, 0);

        Assert.Equal(0, c.Lines[0].X, 6);
        Assert.Equal(0, c.Lines[0].Line.Fragments[0].XOffset, 6);
    }

    [Fact]
    public void HorizontalAlignment_ColumnRight_ShiftsToTrailingEdge()
    {
        var fonts = TableLayoutSupport.Fonts();
        var wHello = TableLayoutSupport.Measure(fonts, "Hello", 12);
        var table = new Table();
        var col = table.Columns.Add(Unit.FromPoint(200));
        col.Alignment = HorizontalAlignment.Right;
        TableLayoutSupport.Fill(table.Rows.Add().Cells[0], "Hello");

        var laid = IsolatedTableLayout.LayoutIsolated(table, 200, fonts);
        var c = TableLayoutSupport.CellAt(laid, 0, 0);

        Assert.Equal(200 - wHello, c.Lines[0].Line.Fragments[0].XOffset, 5);
    }

    [Fact]
    public void HorizontalAlignment_CellCenter_WhenColumnUnset()
    {
        var fonts = TableLayoutSupport.Fonts();
        var wHello = TableLayoutSupport.Measure(fonts, "Hello", 12);
        var table = new Table();
        table.Columns.Add(Unit.FromPoint(200));
        var cell = table.Rows.Add().Cells[0];
        cell.Alignment = HorizontalAlignment.Center;
        TableLayoutSupport.Fill(cell, "Hello");

        var laid = IsolatedTableLayout.LayoutIsolated(table, 200, fonts);
        var c = TableLayoutSupport.CellAt(laid, 0, 0);

        Assert.Equal((200 - wHello) / 2.0, c.Lines[0].Line.Fragments[0].XOffset, 5);
    }

    [Fact]
    public void HorizontalAlignment_CellOverridesColumn()
    {
        var fonts = TableLayoutSupport.Fonts();
        var wHello = TableLayoutSupport.Measure(fonts, "Hello", 12);
        var table = new Table();
        var col = table.Columns.Add(Unit.FromPoint(200));
        col.Alignment = HorizontalAlignment.Right;
        var cell = table.Rows.Add().Cells[0];
        cell.Alignment = HorizontalAlignment.Center;
        TableLayoutSupport.Fill(cell, "Hello");

        var laid = IsolatedTableLayout.LayoutIsolated(table, 200, fonts);
        var c = TableLayoutSupport.CellAt(laid, 0, 0);

        Assert.Equal((200 - wHello) / 2.0, c.Lines[0].Line.Fragments[0].XOffset, 5);
    }

    [Fact]
    public void PerEdgePadding_InsetsContentBoxByEachEdge()
    {
        var fonts = TableLayoutSupport.Fonts();
        var lh = TableLayoutSupport.LineHeight();
        var table = new Table();
        table.Columns.Add(Unit.FromPoint(200));
        var cell = table.Rows.Add().Cells[0];
        cell.PaddingLeft = Unit.FromPoint(3);
        cell.PaddingRight = Unit.FromPoint(7);
        cell.PaddingTop = Unit.FromPoint(11);
        cell.PaddingBottom = Unit.FromPoint(13);
        TableLayoutSupport.Fill(cell, "Hi");

        var laid = IsolatedTableLayout.LayoutIsolated(table, 200, fonts);
        var c = TableLayoutSupport.CellAt(laid, 0, 0);

        AssertRect(new Rect(0, 0, 200, lh + 24), c.Bounds);
        AssertRect(new Rect(3, 11, 190, lh), c.ContentBox);
        Assert.Equal(3, c.Lines[0].X, 6);
        Assert.Equal(11, c.Lines[0].Y, 6);
    }

    [Fact]
    public void PerEdgePadding_UnsetEdgeFallsBackToUniformPadding()
    {
        var fonts = TableLayoutSupport.Fonts();
        var lh = TableLayoutSupport.LineHeight();
        var table = new Table();
        table.Columns.Add(Unit.FromPoint(200));
        var cell = table.Rows.Add().Cells[0];
        cell.Padding = Unit.FromPoint(5);
        cell.PaddingLeft = Unit.FromPoint(20);
        TableLayoutSupport.Fill(cell, "Hi");

        var laid = IsolatedTableLayout.LayoutIsolated(table, 200, fonts);
        var c = TableLayoutSupport.CellAt(laid, 0, 0);

        AssertRect(new Rect(20, 5, 175, lh), c.ContentBox);
        AssertRect(new Rect(0, 0, 200, lh + 10), c.Bounds);
    }

    [Fact]
    public void PadEdges_AllUnset_MatchesUniformPaddingGeometry()
    {
        var fonts = TableLayoutSupport.Fonts();

        static LaidOutCell Build(FontCollection f, bool perEdge)
        {
            var table = new Table();
            table.Columns.Add(Unit.FromPoint(200));
            var cell = table.Rows.Add().Cells[0];
            if (perEdge)
            {
                cell.PaddingLeft = Unit.FromPoint(6);
                cell.PaddingRight = Unit.FromPoint(6);
                cell.PaddingTop = Unit.FromPoint(6);
                cell.PaddingBottom = Unit.FromPoint(6);
            }
            else
            {
                cell.Padding = Unit.FromPoint(6);
            }

            TableLayoutSupport.Fill(cell, "Hello World Again");
            return TableLayoutSupport.CellAt(IsolatedTableLayout.LayoutIsolated(table, 200, f), 0, 0);
        }

        var legacy = Build(fonts, perEdge: false);
        var perEdge = Build(fonts, perEdge: true);

        AssertRect(legacy.Bounds, perEdge.Bounds);
        AssertRect(legacy.ContentBox, perEdge.ContentBox);
    }
}
