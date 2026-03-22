using System;
using Xunit;

using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet.Tests;

public class SparseShiftTests
{
    // --- CellStore.ShiftRowsUp ---

    [Fact]
    public void ShiftRowsUp_RemovesDeletedRowAndShiftsCellsUp()
    {
        var sheet = new Sheet(5, 3);
        sheet.Cells[0, 0].Value = "R1";
        sheet.Cells[1, 0].Value = "R2";
        sheet.Cells[2, 0].Value = "R3";
        sheet.Cells[3, 0].Value = "R4";

        sheet.Cells.ShiftRowsUp(1);

        Assert.Equal("R1", sheet.Cells[0, 0].Value);
        Assert.Equal("R3", sheet.Cells[1, 0].Value);
        Assert.Equal("R4", sheet.Cells[2, 0].Value);
    }

    [Fact]
    public void ShiftRowsUp_UpdatesCellAddresses()
    {
        var sheet = new Sheet(5, 3);
        sheet.Cells[2, 1].Value = "target";

        sheet.Cells.ShiftRowsUp(0);

        var cell = sheet.Cells[1, 1];
        Assert.Equal("target", cell.Value);
        Assert.Equal(new CellRef(1, 1), cell.Address);
    }

    [Fact]
    public void ShiftRowsUp_LeavesLowerRowsUntouched()
    {
        var sheet = new Sheet(5, 3);
        sheet.Cells[0, 0].Value = "keep";
        sheet.Cells[3, 0].Value = "shift";

        sheet.Cells.ShiftRowsUp(2);

        Assert.Equal("keep", sheet.Cells[0, 0].Value);
        Assert.Equal(new CellRef(0, 0), sheet.Cells[0, 0].Address);
        Assert.Equal("shift", sheet.Cells[2, 0].Value);
    }

    [Fact]
    public void ShiftRowsUp_DeleteFirstRow()
    {
        var sheet = new Sheet(3, 2);
        sheet.Cells[0, 0].Value = "first";
        sheet.Cells[1, 0].Value = "second";
        sheet.Cells[2, 0].Value = "third";

        sheet.Cells.ShiftRowsUp(0);

        Assert.Equal("second", sheet.Cells[0, 0].Value);
        Assert.Equal("third", sheet.Cells[1, 0].Value);
        Assert.False(sheet.Cells.HasCell(2, 0));
    }

    [Fact]
    public void ShiftRowsUp_DeleteLastRow()
    {
        var sheet = new Sheet(3, 2);
        sheet.Cells[0, 0].Value = "first";
        sheet.Cells[2, 0].Value = "last";

        sheet.Cells.ShiftRowsUp(2);

        Assert.Equal("first", sheet.Cells[0, 0].Value);
        Assert.False(sheet.Cells.HasCell(2, 0));
    }

    [Fact]
    public void ShiftRowsUp_OnlySparsePopulatedCellsAreInStore()
    {
        var sheet = new Sheet(1000, 1000);
        sheet.Cells[500, 500].Value = "data";

        var countBefore = sheet.Cells.PopulatedCount;
        sheet.Cells.ShiftRowsUp(0);

        // Only the one populated cell should be in the store (re-keyed), no extras created
        Assert.Equal(countBefore, sheet.Cells.PopulatedCount);
        Assert.Equal("data", sheet.Cells[499, 500].Value);
    }

    // --- CellStore.ShiftRowsDown ---

    [Fact]
    public void ShiftRowsDown_ShiftsCellsDownByCount()
    {
        var sheet = new Sheet(10, 3);
        sheet.Cells[1, 0].Value = "A";
        sheet.Cells[2, 0].Value = "B";

        sheet.Cells.ShiftRowsDown(1, 2);

        Assert.False(sheet.Cells.HasCell(1, 0));
        Assert.False(sheet.Cells.HasCell(2, 0));
        Assert.Equal("A", sheet.Cells[3, 0].Value);
        Assert.Equal("B", sheet.Cells[4, 0].Value);
    }

    [Fact]
    public void ShiftRowsDown_UpdatesCellAddresses()
    {
        var sheet = new Sheet(10, 3);
        sheet.Cells[2, 1].Value = "target";

        sheet.Cells.ShiftRowsDown(2, 3);

        var cell = sheet.Cells[5, 1];
        Assert.Equal("target", cell.Value);
        Assert.Equal(new CellRef(5, 1), cell.Address);
    }

    [Fact]
    public void ShiftRowsDown_LeavesLowerRowsUntouched()
    {
        var sheet = new Sheet(10, 3);
        sheet.Cells[0, 0].Value = "stay";
        sheet.Cells[3, 0].Value = "move";

        sheet.Cells.ShiftRowsDown(2, 1);

        Assert.Equal("stay", sheet.Cells[0, 0].Value);
        Assert.Equal(new CellRef(0, 0), sheet.Cells[0, 0].Address);
        Assert.Equal("move", sheet.Cells[4, 0].Value);
    }

    // --- CellStore.ShiftColumnsLeft ---

    [Fact]
    public void ShiftColumnsLeft_RemovesDeletedColumnAndShiftsCellsLeft()
    {
        var sheet = new Sheet(3, 5);
        sheet.Cells[0, 0].Value = "A";
        sheet.Cells[0, 1].Value = "B";
        sheet.Cells[0, 2].Value = "C";
        sheet.Cells[0, 3].Value = "D";

        sheet.Cells.ShiftColumnsLeft(1);

        Assert.Equal("A", sheet.Cells[0, 0].Value);
        Assert.Equal("C", sheet.Cells[0, 1].Value);
        Assert.Equal("D", sheet.Cells[0, 2].Value);
    }

    [Fact]
    public void ShiftColumnsLeft_UpdatesCellAddresses()
    {
        var sheet = new Sheet(3, 5);
        sheet.Cells[1, 3].Value = "target";

        sheet.Cells.ShiftColumnsLeft(0);

        var cell = sheet.Cells[1, 2];
        Assert.Equal("target", cell.Value);
        Assert.Equal(new CellRef(1, 2), cell.Address);
    }

    // --- CellStore.ShiftColumnsRight ---

    [Fact]
    public void ShiftColumnsRight_ShiftsCellsRightByCount()
    {
        var sheet = new Sheet(3, 10);
        sheet.Cells[0, 1].Value = "A";
        sheet.Cells[0, 2].Value = "B";

        sheet.Cells.ShiftColumnsRight(1, 2);

        Assert.False(sheet.Cells.HasCell(0, 1));
        Assert.False(sheet.Cells.HasCell(0, 2));
        Assert.Equal("A", sheet.Cells[0, 3].Value);
        Assert.Equal("B", sheet.Cells[0, 4].Value);
    }

    [Fact]
    public void ShiftColumnsRight_UpdatesCellAddresses()
    {
        var sheet = new Sheet(3, 10);
        sheet.Cells[1, 2].Value = "target";

        sheet.Cells.ShiftColumnsRight(2, 3);

        var cell = sheet.Cells[1, 5];
        Assert.Equal("target", cell.Value);
        Assert.Equal(new CellRef(1, 5), cell.Address);
    }

    // --- Sheet.DeleteRow sparse behavior ---

    [Fact]
    public void DeleteRow_DoesNotCreateEmptyCells()
    {
        var sheet = new Sheet(1000, 1000);
        sheet.Cells[500, 500].Value = "data";

        var countBefore = sheet.Cells.PopulatedCount;
        sheet.DeleteRow(0);

        // Should not have created any cells beyond the one populated cell
        Assert.True(sheet.Cells.PopulatedCount <= countBefore);
    }

    [Fact]
    public void DeleteRow_PreservesDataCorrectly()
    {
        var sheet = new Sheet(5, 3);
        sheet.Cells[0, 0].Value = 10d;
        sheet.Cells[1, 0].Value = 20d;
        sheet.Cells[2, 0].Value = 30d;
        sheet.Cells[3, 0].Value = 40d;
        sheet.Cells[4, 0].Value = 50d;
        sheet.Cells[0, 2].Value = "col3";

        sheet.DeleteRow(2);

        Assert.Equal(4, sheet.RowCount);
        Assert.Equal(10d, sheet.Cells[0, 0].Value);
        Assert.Equal(20d, sheet.Cells[1, 0].Value);
        Assert.Equal(40d, sheet.Cells[2, 0].Value);
        Assert.Equal(50d, sheet.Cells[3, 0].Value);
        Assert.Equal("col3", sheet.Cells[0, 2].Value);
    }

    [Fact]
    public void DeleteRow_MultipleColumns()
    {
        var sheet = new Sheet(4, 3);
        sheet.Cells[0, 0].Value = "A1";
        sheet.Cells[0, 1].Value = "B1";
        sheet.Cells[0, 2].Value = "C1";
        sheet.Cells[1, 0].Value = "A2";
        sheet.Cells[1, 1].Value = "B2";
        sheet.Cells[2, 0].Value = "A3";

        sheet.DeleteRow(0);

        Assert.Equal(3, sheet.RowCount);
        Assert.Equal("A2", sheet.Cells[0, 0].Value);
        Assert.Equal("B2", sheet.Cells[0, 1].Value);
        Assert.Equal("A3", sheet.Cells[1, 0].Value);
    }

    [Fact]
    public void DeleteRow_InvalidatesFormulasReferencingDeletedRow()
    {
        var sheet = new Sheet(5, 5);
        sheet.Cells[0, 0].Value = 10;
        sheet.Cells[2, 0].Value = 30;
        sheet.Cells[1, 1].Formula = "=A1+A3";
        Assert.Equal(40d, sheet.Cells[1, 1].Value);

        sheet.DeleteRow(0);

        Assert.Equal("=#REF!+A3", sheet.Cells[0, 1].Formula);
        Assert.Equal(CellError.Ref, sheet.Cells[0, 1].Value);
    }

    [Fact]
    public void DeleteRow_ThrowsOnInvalidIndex()
    {
        var sheet = new Sheet(5, 5);
        Assert.Throws<ArgumentOutOfRangeException>(() => sheet.DeleteRow(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => sheet.DeleteRow(5));
    }

    // --- Sheet.DeleteColumn sparse behavior ---

    [Fact]
    public void DeleteColumn_DoesNotCreateEmptyCells()
    {
        var sheet = new Sheet(1000, 1000);
        sheet.Cells[500, 500].Value = "data";

        var countBefore = sheet.Cells.PopulatedCount;
        sheet.DeleteColumn(0);

        Assert.True(sheet.Cells.PopulatedCount <= countBefore);
    }

    [Fact]
    public void DeleteColumn_PreservesDataCorrectly()
    {
        var sheet = new Sheet(3, 5);
        sheet.Cells[0, 0].Value = "A";
        sheet.Cells[0, 1].Value = "B";
        sheet.Cells[0, 2].Value = "C";
        sheet.Cells[0, 3].Value = "D";
        sheet.Cells[0, 4].Value = "E";

        sheet.DeleteColumn(2);

        Assert.Equal(4, sheet.ColumnCount);
        Assert.Equal("A", sheet.Cells[0, 0].Value);
        Assert.Equal("B", sheet.Cells[0, 1].Value);
        Assert.Equal("D", sheet.Cells[0, 2].Value);
        Assert.Equal("E", sheet.Cells[0, 3].Value);
    }

    [Fact]
    public void DeleteColumn_InvalidatesFormulasReferencingDeletedColumn()
    {
        var sheet = new Sheet(5, 5);
        sheet.Cells[0, 0].Value = 1;
        sheet.Cells[0, 2].Value = 3;
        sheet.Cells[1, 1].Formula = "=A1+C1";
        Assert.Equal(4d, sheet.Cells[1, 1].Value);

        sheet.DeleteColumn(0);

        Assert.Equal("=#REF!+C1", sheet.Cells[1, 0].Formula);
        Assert.Equal(CellError.Ref, sheet.Cells[1, 0].Value);
    }

    // --- Sheet.InsertRow sparse behavior ---

    [Fact]
    public void InsertRow_DoesNotCreateEmptyCells()
    {
        var sheet = new Sheet(1000, 1000);
        sheet.Cells[500, 500].Value = "data";

        var countBefore = sheet.Cells.PopulatedCount;
        sheet.InsertRow(0, 1);

        // Only the original cell should be present (re-keyed), no extras
        Assert.Equal(countBefore, sheet.Cells.PopulatedCount);
    }

    [Fact]
    public void InsertRow_ShiftsValuesDown()
    {
        var sheet = new Sheet(5, 3);
        sheet.Cells[0, 0].Value = "A";
        sheet.Cells[1, 0].Value = "B";
        sheet.Cells[2, 0].Value = "C";

        sheet.InsertRow(1, 1);

        Assert.Equal(6, sheet.RowCount);
        Assert.Equal("A", sheet.Cells[0, 0].Value);
        Assert.Null(sheet.Cells[1, 0].Value);
        Assert.Equal("B", sheet.Cells[2, 0].Value);
        Assert.Equal("C", sheet.Cells[3, 0].Value);
    }

    [Fact]
    public void InsertRow_AdjustsFormulaReferences()
    {
        var sheet = new Sheet(5, 5);
        sheet.Cells[1, 0].Value = 42;
        sheet.Cells[1, 1].Formula = "=A2+10";
        Assert.Equal(52d, sheet.Cells[1, 1].Value);

        sheet.InsertRow(1, 1);

        Assert.Equal("=A3+10", sheet.Cells[2, 1].Formula);
        Assert.Equal(52d, sheet.Cells[2, 1].Value);
    }

    [Fact]
    public void InsertRow_MultipleRows()
    {
        var sheet = new Sheet(5, 3);
        sheet.Cells[0, 0].Value = "X";
        sheet.Cells[1, 0].Value = "Y";

        sheet.InsertRow(1, 3);

        Assert.Equal(8, sheet.RowCount);
        Assert.Equal("X", sheet.Cells[0, 0].Value);
        Assert.Null(sheet.Cells[1, 0].Value);
        Assert.Null(sheet.Cells[2, 0].Value);
        Assert.Null(sheet.Cells[3, 0].Value);
        Assert.Equal("Y", sheet.Cells[4, 0].Value);
    }

    // --- Sheet.InsertColumn sparse behavior ---

    [Fact]
    public void InsertColumn_DoesNotCreateEmptyCells()
    {
        var sheet = new Sheet(1000, 1000);
        sheet.Cells[500, 500].Value = "data";

        var countBefore = sheet.Cells.PopulatedCount;
        sheet.InsertColumn(0, 1);

        Assert.Equal(countBefore, sheet.Cells.PopulatedCount);
    }

    [Fact]
    public void InsertColumn_ShiftsValuesRight()
    {
        var sheet = new Sheet(3, 5);
        sheet.Cells[0, 0].Value = "A";
        sheet.Cells[0, 1].Value = "B";
        sheet.Cells[0, 2].Value = "C";

        sheet.InsertColumn(1, 1);

        Assert.Equal(6, sheet.ColumnCount);
        Assert.Equal("A", sheet.Cells[0, 0].Value);
        Assert.Null(sheet.Cells[0, 1].Value);
        Assert.Equal("B", sheet.Cells[0, 2].Value);
        Assert.Equal("C", sheet.Cells[0, 3].Value);
    }

    [Fact]
    public void InsertColumn_AdjustsFormulaReferences()
    {
        var sheet = new Sheet(5, 5);
        sheet.Cells[1, 0].Value = 42;
        sheet.Cells[1, 1].Formula = "=A2+10";
        Assert.Equal(52d, sheet.Cells[1, 1].Value);

        sheet.InsertColumn(0, 1);

        Assert.Equal("=B2+10", sheet.Cells[1, 2].Formula);
        Assert.Equal(52d, sheet.Cells[1, 2].Value);
    }

    // --- Edge cases ---

    [Fact]
    public void DeleteRow_EmptySheet_DeletesWithoutError()
    {
        var sheet = new Sheet(3, 3);
        sheet.DeleteRow(1);
        Assert.Equal(2, sheet.RowCount);
    }

    [Fact]
    public void DeleteColumn_EmptySheet_DeletesWithoutError()
    {
        var sheet = new Sheet(3, 3);
        sheet.DeleteColumn(1);
        Assert.Equal(2, sheet.ColumnCount);
    }

    [Fact]
    public void InsertRow_AtEnd_IncreasesCount()
    {
        var sheet = new Sheet(3, 3);
        sheet.Cells[2, 0].Value = "last";

        sheet.InsertRow(3, 1);

        Assert.Equal(4, sheet.RowCount);
        Assert.Equal("last", sheet.Cells[2, 0].Value);
    }

    [Fact]
    public void InsertColumn_AtEnd_IncreasesCount()
    {
        var sheet = new Sheet(3, 3);
        sheet.Cells[0, 2].Value = "last";

        sheet.InsertColumn(3, 1);

        Assert.Equal(4, sheet.ColumnCount);
        Assert.Equal("last", sheet.Cells[0, 2].Value);
    }

    [Fact]
    public void MultipleDeleteRows_CumulativeCorrectness()
    {
        var sheet = new Sheet(6, 2);
        for (int r = 0; r < 6; r++)
            sheet.Cells[r, 0].Value = (double)(r + 1);

        sheet.DeleteRow(1);
        sheet.DeleteRow(1);

        Assert.Equal(4, sheet.RowCount);
        Assert.Equal(1d, sheet.Cells[0, 0].Value);
        Assert.Equal(4d, sheet.Cells[1, 0].Value);
        Assert.Equal(5d, sheet.Cells[2, 0].Value);
        Assert.Equal(6d, sheet.Cells[3, 0].Value);
    }

    [Fact]
    public void DeleteThenInsert_RestoresRowCount()
    {
        var sheet = new Sheet(5, 3);
        sheet.Cells[2, 0].Value = "middle";

        sheet.DeleteRow(0);
        Assert.Equal(4, sheet.RowCount);

        sheet.InsertRow(0, 1);
        Assert.Equal(5, sheet.RowCount);
    }

    [Fact]
    public void ShiftRowsUp_MultipleColumnsPreserved()
    {
        var sheet = new Sheet(5, 4);
        sheet.Cells[2, 0].Value = "A";
        sheet.Cells[2, 1].Value = "B";
        sheet.Cells[2, 2].Value = "C";
        sheet.Cells[2, 3].Value = "D";

        sheet.Cells.ShiftRowsUp(1);

        Assert.Equal("A", sheet.Cells[1, 0].Value);
        Assert.Equal("B", sheet.Cells[1, 1].Value);
        Assert.Equal("C", sheet.Cells[1, 2].Value);
        Assert.Equal("D", sheet.Cells[1, 3].Value);
    }

    [Fact]
    public void ShiftColumnsLeft_MultipleRowsPreserved()
    {
        var sheet = new Sheet(4, 5);
        sheet.Cells[0, 2].Value = "R1";
        sheet.Cells[1, 2].Value = "R2";
        sheet.Cells[2, 2].Value = "R3";
        sheet.Cells[3, 2].Value = "R4";

        sheet.Cells.ShiftColumnsLeft(1);

        Assert.Equal("R1", sheet.Cells[0, 1].Value);
        Assert.Equal("R2", sheet.Cells[1, 1].Value);
        Assert.Equal("R3", sheet.Cells[2, 1].Value);
        Assert.Equal("R4", sheet.Cells[3, 1].Value);
    }

    [Fact]
    public void PopulatedCount_ReflectsOnlyAccessedCells()
    {
        var sheet = new Sheet(100, 100);

        Assert.Equal(0, sheet.Cells.PopulatedCount);

        sheet.Cells[5, 5].Value = "x";
        Assert.Equal(1, sheet.Cells.PopulatedCount);

        sheet.Cells[10, 10].Value = "y";
        Assert.Equal(2, sheet.Cells.PopulatedCount);
    }

    [Fact]
    public void HasCell_ReturnsFalseForUnpopulated()
    {
        var sheet = new Sheet(10, 10);

        Assert.False(sheet.Cells.HasCell(5, 5));

        sheet.Cells[5, 5].Value = "present";
        Assert.True(sheet.Cells.HasCell(5, 5));
    }

    [Fact]
    public void DeleteRow_FormulaReferencingDifferentRowNotInvalidated()
    {
        var sheet = new Sheet(5, 5);
        sheet.Cells[0, 0].Value = 10;
        sheet.Cells[3, 0].Value = 30;
        sheet.Cells[3, 1].Formula = "=A1+100";

        sheet.DeleteRow(1);

        // Formula at (now row 2, col 1) should still reference A1 and not be invalidated
        Assert.Equal("=A1+100", sheet.Cells[2, 1].Formula);
        Assert.Equal(110d, sheet.Cells[2, 1].Value);
    }

    [Fact]
    public void InsertRow_FormulaBeforeInsertPointNotAdjusted()
    {
        var sheet = new Sheet(5, 5);
        sheet.Cells[0, 0].Value = 10;
        sheet.Cells[0, 1].Formula = "=A1+5";

        sheet.InsertRow(3, 1);

        // Formula referencing row 0 (before insert at row 3) should not be adjusted
        Assert.Equal("=A1+5", sheet.Cells[0, 1].Formula);
        Assert.Equal(15d, sheet.Cells[0, 1].Value);
    }
}
