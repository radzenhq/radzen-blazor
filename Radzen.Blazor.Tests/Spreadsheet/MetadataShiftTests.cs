using Xunit;

namespace Radzen.Blazor.Spreadsheet.Tests;

public class MetadataShiftTests
{
    // ===== Axis: custom sizes should shift on delete =====

    [Fact]
    public void DeleteRow_ShiftsCustomRowHeights()
    {
        var sheet = new Sheet(10, 5);
        sheet.Rows[5] = 40; // row 5 has custom height 40

        sheet.DeleteRow(3);

        // Row 5 is now logically row 4 — it should have the custom height
        Assert.Equal(40, sheet.Rows[4]);
        // Row 5 should now have default height (24)
        Assert.Equal(sheet.Rows.Size, sheet.Rows[5]);
    }

    [Fact]
    public void DeleteRow_ShiftsCustomRowHeights_BeforeTarget()
    {
        var sheet = new Sheet(10, 5);
        sheet.Rows[2] = 50; // row 2 has custom height

        sheet.DeleteRow(5); // delete row after the custom one

        // Row 2 should be unaffected
        Assert.Equal(50, sheet.Rows[2]);
    }

    [Fact]
    public void DeleteRow_RemovesCustomHeightOfDeletedRow()
    {
        var sheet = new Sheet(10, 5);
        sheet.Rows[3] = 60; // row 3 has custom height

        sheet.DeleteRow(3);

        // Row 3 should now have default height (the old row 4's height)
        Assert.Equal(sheet.Rows.Size, sheet.Rows[3]);
    }

    [Fact]
    public void DeleteColumn_ShiftsCustomColumnWidths()
    {
        var sheet = new Sheet(5, 10);
        sheet.Columns[5] = 200; // column 5 has custom width

        sheet.DeleteColumn(3);

        // Column 5 is now logically column 4
        Assert.Equal(200, sheet.Columns[4]);
        Assert.Equal(sheet.Columns.Size, sheet.Columns[5]);
    }

    // ===== Axis: hidden state should shift on delete =====

    [Fact]
    public void DeleteRow_ShiftsHiddenRows()
    {
        var sheet = new Sheet(10, 5);
        sheet.Rows.Hide(5);

        sheet.DeleteRow(3);

        // Row 5 is now logically row 4 — it should be hidden
        Assert.True(sheet.Rows.IsHidden(4));
        Assert.False(sheet.Rows.IsHidden(5));
    }

    [Fact]
    public void DeleteRow_RemovesHiddenStateOfDeletedRow()
    {
        var sheet = new Sheet(10, 5);
        sheet.Rows.Hide(3);

        sheet.DeleteRow(3);

        // The hidden row was deleted — row 3 should not be hidden
        Assert.False(sheet.Rows.IsHidden(3));
    }

    [Fact]
    public void DeleteColumn_ShiftsHiddenColumns()
    {
        var sheet = new Sheet(5, 10);
        sheet.Columns.Hide(5);

        sheet.DeleteColumn(3);

        Assert.True(sheet.Columns.IsHidden(4));
        Assert.False(sheet.Columns.IsHidden(5));
    }

    // ===== Axis: custom sizes should shift on insert =====

    [Fact]
    public void InsertRow_ShiftsCustomRowHeights()
    {
        var sheet = new Sheet(10, 5);
        sheet.Rows[3] = 40;

        sheet.InsertRow(2, 1);

        // Row 3 is now logically row 4
        Assert.Equal(40, sheet.Rows[4]);
        Assert.Equal(sheet.Rows.Size, sheet.Rows[3]);
    }

    [Fact]
    public void InsertRow_LeavesEarlierRowsUnchanged()
    {
        var sheet = new Sheet(10, 5);
        sheet.Rows[1] = 50;

        sheet.InsertRow(5, 1);

        // Row 1 is before the insert point — unaffected
        Assert.Equal(50, sheet.Rows[1]);
    }

    [Fact]
    public void InsertColumn_ShiftsCustomColumnWidths()
    {
        var sheet = new Sheet(5, 10);
        sheet.Columns[3] = 200;

        sheet.InsertColumn(2, 1);

        Assert.Equal(200, sheet.Columns[4]);
        Assert.Equal(sheet.Columns.Size, sheet.Columns[3]);
    }

    // ===== Axis: hidden state should shift on insert =====

    [Fact]
    public void InsertRow_ShiftsHiddenRows()
    {
        var sheet = new Sheet(10, 5);
        sheet.Rows.Hide(3);

        sheet.InsertRow(2, 1);

        Assert.True(sheet.Rows.IsHidden(4));
        Assert.False(sheet.Rows.IsHidden(3));
    }

    [Fact]
    public void InsertColumn_ShiftsHiddenColumns()
    {
        var sheet = new Sheet(5, 10);
        sheet.Columns.Hide(3);

        sheet.InsertColumn(2, 1);

        Assert.True(sheet.Columns.IsHidden(4));
        Assert.False(sheet.Columns.IsHidden(3));
    }

    // ===== MergedCellStore: ranges should shift on delete =====

    [Fact]
    public void DeleteRow_ShiftsMergedRangesDown()
    {
        var sheet = new Sheet(10, 10);
        var range = new RangeRef(new CellRef(5, 0), new CellRef(6, 2));
        sheet.MergedCells.Add(range);

        sheet.DeleteRow(3);

        // Merged range A6:C7 should become A5:C6
        var expected = new RangeRef(new CellRef(4, 0), new CellRef(5, 2));
        Assert.True(sheet.MergedCells.Contains(expected));
        Assert.False(sheet.MergedCells.Contains(range));
        Assert.True(sheet.MergedCells.Contains(new CellRef(4, 0)));
        Assert.False(sheet.MergedCells.Contains(new CellRef(6, 0)));
    }

    [Fact]
    public void DeleteRow_LeavesEarlierMergedRangesUnchanged()
    {
        var sheet = new Sheet(10, 10);
        var range = new RangeRef(new CellRef(0, 0), new CellRef(1, 2));
        sheet.MergedCells.Add(range);

        sheet.DeleteRow(5);

        Assert.True(sheet.MergedCells.Contains(range));
        Assert.True(sheet.MergedCells.Contains(new CellRef(0, 0)));
    }

    [Fact]
    public void DeleteColumn_ShiftsMergedRangesLeft()
    {
        var sheet = new Sheet(10, 10);
        var range = new RangeRef(new CellRef(0, 5), new CellRef(2, 6));
        sheet.MergedCells.Add(range);

        sheet.DeleteColumn(3);

        var expected = new RangeRef(new CellRef(0, 4), new CellRef(2, 5));
        Assert.True(sheet.MergedCells.Contains(expected));
        Assert.False(sheet.MergedCells.Contains(range));
    }

    // ===== MergedCellStore: ranges should shift on insert =====

    [Fact]
    public void InsertRow_ShiftsMergedRangesDown()
    {
        var sheet = new Sheet(10, 10);
        var range = new RangeRef(new CellRef(3, 0), new CellRef(4, 2));
        sheet.MergedCells.Add(range);

        sheet.InsertRow(2, 1);

        var expected = new RangeRef(new CellRef(4, 0), new CellRef(5, 2));
        Assert.True(sheet.MergedCells.Contains(expected));
        Assert.False(sheet.MergedCells.Contains(range));
    }

    [Fact]
    public void InsertRow_LeavesEarlierMergedRangesUnchanged()
    {
        var sheet = new Sheet(10, 10);
        var range = new RangeRef(new CellRef(0, 0), new CellRef(1, 2));
        sheet.MergedCells.Add(range);

        sheet.InsertRow(5, 1);

        Assert.True(sheet.MergedCells.Contains(range));
    }

    [Fact]
    public void InsertColumn_ShiftsMergedRangesRight()
    {
        var sheet = new Sheet(10, 10);
        var range = new RangeRef(new CellRef(0, 3), new CellRef(2, 4));
        sheet.MergedCells.Add(range);

        sheet.InsertColumn(2, 1);

        var expected = new RangeRef(new CellRef(0, 4), new CellRef(2, 5));
        Assert.True(sheet.MergedCells.Contains(expected));
        Assert.False(sheet.MergedCells.Contains(range));
    }

    [Fact]
    public void InsertColumn_LeavesEarlierMergedRangesUnchanged()
    {
        var sheet = new Sheet(10, 10);
        var range = new RangeRef(new CellRef(0, 0), new CellRef(2, 1));
        sheet.MergedCells.Add(range);

        sheet.InsertColumn(5, 1);

        Assert.True(sheet.MergedCells.Contains(range));
    }

    // ===== MergedCellStore: delete row/column that intersects a merged range =====

    [Fact]
    public void DeleteRow_RemovesMergedRangeThatIsFullyContained()
    {
        var sheet = new Sheet(10, 10);
        // Single-row merged range at row 3
        var range = new RangeRef(new CellRef(3, 0), new CellRef(3, 2));
        sheet.MergedCells.Add(range);

        sheet.DeleteRow(3);

        // The merged range was entirely on the deleted row — it should be gone
        Assert.Empty(sheet.MergedCells.Ranges);
    }

    [Fact]
    public void DeleteRow_ShrinksMergedRangeThatSpansDeletedRow()
    {
        var sheet = new Sheet(10, 10);
        // Range spans rows 2-5
        var range = new RangeRef(new CellRef(2, 0), new CellRef(5, 2));
        sheet.MergedCells.Add(range);

        sheet.DeleteRow(3); // delete a row in the middle

        // Range should shrink: rows 2-4 (was 2-5, lost one row, shifted)
        var expected = new RangeRef(new CellRef(2, 0), new CellRef(4, 2));
        Assert.Single(sheet.MergedCells.Ranges);
        Assert.True(sheet.MergedCells.Contains(expected));
    }

    [Fact]
    public void DeleteColumn_ShrinksMergedRangeThatSpansDeletedColumn()
    {
        var sheet = new Sheet(10, 10);
        var range = new RangeRef(new CellRef(0, 2), new CellRef(2, 5));
        sheet.MergedCells.Add(range);

        sheet.DeleteColumn(3);

        var expected = new RangeRef(new CellRef(0, 2), new CellRef(2, 4));
        Assert.Single(sheet.MergedCells.Ranges);
        Assert.True(sheet.MergedCells.Contains(expected));
    }
}
