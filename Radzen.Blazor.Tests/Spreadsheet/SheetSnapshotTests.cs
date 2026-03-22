using Xunit;

using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet.Tests;

public class SheetSnapshotTests
{
    [Fact]
    public void InsertRow_ShouldNotCreateAllCells()
    {
        var sheet = new Worksheet(100, 26);

        // Only populate a few cells
        sheet.Cells["A1"].Value = 1;
        sheet.Cells["B2"].Value = 2;
        sheet.Cells["C3"].Value = 3;

        var populatedBefore = sheet.Cells.PopulatedCount;

        // Insert a row (creates a SheetSnapshotCommandBase internally)
        var command = new InsertRowBeforeCommand(sheet, 1);
        command.Execute();

        // The snapshot should NOT have created thousands of cells.
        // Allow some overhead for the insert operation itself, but not O(rows*cols).
        Assert.True(sheet.Cells.PopulatedCount < 50,
            $"Expected fewer than 50 populated cells after insert, got {sheet.Cells.PopulatedCount}. " +
            "SheetSnapshotCommandBase may be creating cells via Cells[r,c] indexer instead of TryGet.");
    }

    [Fact]
    public void InsertRow_Undo_ShouldRestoreOriginalValues()
    {
        var sheet = new Worksheet(5, 5);
        sheet.Cells["A1"].Value = 10;
        sheet.Cells["B2"].Value = 20;
        sheet.Cells["A3"].Value = 30;

        var command = new InsertRowBeforeCommand(sheet, 1);
        command.Execute();

        Assert.Equal(6, sheet.RowCount);
        command.Unexecute();

        // After undo, original values restored
        Assert.Equal(5, sheet.RowCount);
        Assert.Equal(10d, sheet.Cells["A1"].Value);
        Assert.Equal(20d, sheet.Cells["B2"].Value);
        Assert.Equal(30d, sheet.Cells["A3"].Value);
    }
}
