using Xunit;

namespace Radzen.Blazor.Spreadsheet.Tests;

public class RowColumnCommandTests
{
    [Fact]
    public void DeleteRowsCommand_SingleRow_ExecuteAndUndo_RestoresState()
    {
        var sheet = new Sheet(4, 3);
        sheet.Cells[0, 0].Value = "R1";
        sheet.Cells[1, 0].Value = "R2";
        sheet.Cells[2, 0].Value = "R3";
        sheet.Cells[3, 0].Value = "R4";

        var cmd = new DeleteRowsCommand(sheet, 1, 1);
        Assert.True(sheet.Commands.Execute(cmd));

        Assert.Equal(3, sheet.RowCount);
        Assert.Equal("R1", sheet.Cells[0, 0].Value);
        Assert.Equal("R3", sheet.Cells[1, 0].Value);
        Assert.Equal("R4", sheet.Cells[2, 0].Value);

        sheet.Commands.Undo();

        Assert.Equal(4, sheet.RowCount);
        Assert.Equal("R1", sheet.Cells[0, 0].Value);
        Assert.Equal("R2", sheet.Cells[1, 0].Value);
        Assert.Equal("R3", sheet.Cells[2, 0].Value);
        Assert.Equal("R4", sheet.Cells[3, 0].Value);
    }

    [Fact]
    public void DeleteRowsCommand_ExecuteAndUndo_RestoresState()
    {
        var sheet = new Sheet(6, 3);
        sheet.Cells[0, 0].Value = "R1";
        sheet.Cells[1, 0].Value = "R2";
        sheet.Cells[2, 0].Value = "R3";
        sheet.Cells[3, 0].Value = "R4";
        sheet.Cells[4, 0].Value = "R5";
        sheet.Cells[5, 0].Value = "R6";

        var cmd = new DeleteRowsCommand(sheet, 1, 3); // delete rows 2..4
        Assert.True(sheet.Commands.Execute(cmd));

        Assert.Equal(3, sheet.RowCount);
        Assert.Equal("R1", sheet.Cells[0, 0].Value);
        Assert.Equal("R5", sheet.Cells[1, 0].Value);
        Assert.Equal("R6", sheet.Cells[2, 0].Value);

        sheet.Commands.Undo();
        Assert.Equal(6, sheet.RowCount);
        Assert.Equal("R1", sheet.Cells[0, 0].Value);
        Assert.Equal("R2", sheet.Cells[1, 0].Value);
        Assert.Equal("R3", sheet.Cells[2, 0].Value);
        Assert.Equal("R4", sheet.Cells[3, 0].Value);
        Assert.Equal("R5", sheet.Cells[4, 0].Value);
        Assert.Equal("R6", sheet.Cells[5, 0].Value);
    }

    [Fact]
    public void InsertRowAfterCommand_ExecuteAndUndo_RestoresState()
    {
        var sheet = new Sheet(4, 3);
        sheet.Cells[1, 0].Value = 1d; // A2
        sheet.Cells[1, 1].Formula = "=A2+10"; // B2

        var cmd = new InsertRowAfterCommand(sheet, 1); // after row 1 -> insert at index 2
        Assert.True(sheet.Commands.Execute(cmd));

        Assert.Equal(5, sheet.RowCount);
        // Inserting after row 1 does not move row 1; A2 and B2 stay
        Assert.Equal(1d, sheet.Cells[1, 0].Value);
        Assert.Equal("=A2+10", sheet.Cells[1, 1].Formula);
        Assert.Equal(11d, sheet.Cells[1, 1].Value);

        sheet.Commands.Undo();

        Assert.Equal(4, sheet.RowCount);
        Assert.Equal(1d, sheet.Cells[1, 0].Value);
        Assert.Equal("=A2+10", sheet.Cells[1, 1].Formula);
    }

    [Fact]
    public void InsertRowBeforeCommand_ExecuteAndUndo_RestoresState()
    {
        var sheet = new Sheet(4, 3);
        sheet.Cells[1, 0].Value = 1d; // A2
        sheet.Cells[1, 1].Formula = "=A2+10"; // B2

        var cmd = new InsertRowBeforeCommand(sheet, 1); // before row 1 -> insert at index 1
        Assert.True(sheet.Commands.Execute(cmd));

        Assert.Equal(5, sheet.RowCount);
        // value shifted down (A2 becomes A3)
        Assert.Equal(1d, sheet.Cells[2, 0].Value);
        // formula shifted down and reference updated A2->A3
        Assert.Equal("=A3+10", sheet.Cells[2, 1].Formula);
        Assert.Equal(11d, sheet.Cells[2, 1].Value);

        sheet.Commands.Undo();

        Assert.Equal(4, sheet.RowCount);
        Assert.Equal(1d, sheet.Cells[1, 0].Value);
        Assert.Equal("=A2+10", sheet.Cells[1, 1].Formula);
    }

    [Fact]
    public void InsertRowAfterCommand_WithMultiSelection_InsertsAfterLastRow()
    {
        var sheet = new Sheet(6, 2);
        // Mark rows with their index in A
        for (int r = 0; r < 6; r++) sheet.Cells[r, 0].Value = r + 1;

        // Simulate selection rows 1..3 (0-based), last is 3 -> insert after 3 at index 4
        var cmd = new InsertRowAfterCommand(sheet, 3);
        Assert.True(sheet.Commands.Execute(cmd));

        Assert.Equal(7, sheet.RowCount);
        // Inserted row at index 4 is empty
        Assert.Null(sheet.Cells[4, 0].Value);
        // Row 5 takes previous row 4 value (5)
        Assert.Equal(5d, sheet.Cells[5, 0].Value);
        // Row 6 takes previous row 5 value (6)
        Assert.Equal(6d, sheet.Cells[6, 0].Value);

        sheet.Commands.Undo();
        Assert.Equal(6, sheet.RowCount);
        Assert.Equal(5d, sheet.Cells[4, 0].Value);
    }

    [Fact]
    public void InsertRowBeforeCommand_WithMultiSelection_InsertsBeforeFirstRow()
    {
        var sheet = new Sheet(6, 2);
        for (int r = 0; r < 6; r++) sheet.Cells[r, 0].Value = r + 1;

        // Simulate selection rows 2..4 (first is 2) -> insert at index 2
        var cmd = new InsertRowBeforeCommand(sheet, 2);
        Assert.True(sheet.Commands.Execute(cmd));

        Assert.Equal(7, sheet.RowCount);
        // Inserted row at index 2 is empty
        Assert.Null(sheet.Cells[2, 0].Value);
        // Row 3 takes previous row 2 value (3)
        Assert.Equal(3d, sheet.Cells[3, 0].Value);

        sheet.Commands.Undo();
        Assert.Equal(6, sheet.RowCount);
        Assert.Equal(3d, sheet.Cells[2, 0].Value);
    }

    [Fact]
    public void InsertColumnAfterCommand_WithMultiSelection_InsertsAfterLastColumn()
    {
        var sheet = new Sheet(2, 6);
        // Mark columns with their index in row 1
        for (int c = 0; c < 6; c++) sheet.Cells[0, c].Value = c + 1;

        // Simulate selection columns 1..3 (last is 3) -> insert after col 3 at index 4
        var cmd = new InsertColumnAfterCommand(sheet, 3);
        Assert.True(sheet.Commands.Execute(cmd));

        Assert.Equal(7, sheet.ColumnCount);
        // Inserted column at index 4 is empty
        Assert.Null(sheet.Cells[0, 4].Value);
        // Column 5 takes previous column 4 value (5)
        Assert.Equal(5d, sheet.Cells[0, 5].Value);

        sheet.Commands.Undo();
        Assert.Equal(6, sheet.ColumnCount);
        Assert.Equal(5d, sheet.Cells[0, 4].Value);
    }

    [Fact]
    public void InsertColumnBeforeCommand_WithMultiSelection_InsertsBeforeFirstColumn()
    {
        var sheet = new Sheet(2, 6);
        for (int c = 0; c < 6; c++) sheet.Cells[0, c].Value = c + 1;

        // Simulate selection columns 2..4 (first is 2) -> insert at index 2
        var cmd = new InsertColumnBeforeCommand(sheet, 2);
        Assert.True(sheet.Commands.Execute(cmd));

        Assert.Equal(7, sheet.ColumnCount);
        // Inserted column at index 2 is empty
        Assert.Null(sheet.Cells[0, 2].Value);
        // Column 3 takes previous column 2 value (3)
        Assert.Equal(3d, sheet.Cells[0, 3].Value);

        sheet.Commands.Undo();
        Assert.Equal(6, sheet.ColumnCount);
        Assert.Equal(3d, sheet.Cells[0, 2].Value);
    }

    [Fact]
    public void DeleteColumnsCommand_SingleColumn_ExecuteAndUndo_RestoresState()
    {
        var sheet = new Sheet(3, 4);
        sheet.Cells[0, 0].Value = "A";
        sheet.Cells[0, 1].Value = "B";
        sheet.Cells[0, 2].Value = "C";
        sheet.Cells[0, 3].Value = "D";

        var cmd = new DeleteColumnsCommand(sheet, 1, 1);
        Assert.True(sheet.Commands.Execute(cmd));

        Assert.Equal(3, sheet.ColumnCount);
        Assert.Equal("A", sheet.Cells[0, 0].Value);
        Assert.Equal("C", sheet.Cells[0, 1].Value);
        Assert.Equal("D", sheet.Cells[0, 2].Value);

        sheet.Commands.Undo();

        Assert.Equal(4, sheet.ColumnCount);
        Assert.Equal("A", sheet.Cells[0, 0].Value);
        Assert.Equal("B", sheet.Cells[0, 1].Value);
        Assert.Equal("C", sheet.Cells[0, 2].Value);
        Assert.Equal("D", sheet.Cells[0, 3].Value);
    }

    [Fact]
    public void DeleteColumnsCommand_ExecuteAndUndo_RestoresState()
    {
        var sheet = new Sheet(3, 6);
        sheet.Cells[0, 0].Value = "A";
        sheet.Cells[0, 1].Value = "B";
        sheet.Cells[0, 2].Value = "C";
        sheet.Cells[0, 3].Value = "D";
        sheet.Cells[0, 4].Value = "E";
        sheet.Cells[0, 5].Value = "F";

        var cmd = new DeleteColumnsCommand(sheet, 1, 3); // delete B..D
        Assert.True(sheet.Commands.Execute(cmd));

        Assert.Equal(3, sheet.ColumnCount);
        Assert.Equal("A", sheet.Cells[0, 0].Value);
        Assert.Equal("E", sheet.Cells[0, 1].Value);
        Assert.Equal("F", sheet.Cells[0, 2].Value);

        sheet.Commands.Undo();

        Assert.Equal(6, sheet.ColumnCount);
        Assert.Equal("A", sheet.Cells[0, 0].Value);
        Assert.Equal("B", sheet.Cells[0, 1].Value);
        Assert.Equal("C", sheet.Cells[0, 2].Value);
        Assert.Equal("D", sheet.Cells[0, 3].Value);
        Assert.Equal("E", sheet.Cells[0, 4].Value);
        Assert.Equal("F", sheet.Cells[0, 5].Value);
    }

    [Fact]
    public void InsertColumnBeforeCommand_ExecuteAndUndo_RestoresState()
    {
        var sheet = new Sheet(5, 5);
        sheet.Cells[1, 0].Value = 1d;   // A2
        sheet.Cells[1, 1].Formula = "=A2+10"; // B2

        var cmd = new InsertColumnBeforeCommand(sheet, 0); // before A
        Assert.True(sheet.Commands.Execute(cmd));

        Assert.Equal(6, sheet.ColumnCount);
        // value shifted right (A2 becomes B2)
        Assert.Equal(1d, sheet.Cells[1, 1].Value);
        // formula shifted right and reference updated A2->B2
        Assert.Equal("=B2+10", sheet.Cells[1, 2].Formula); // original B2 moved to C2
        Assert.Equal(11d, sheet.Cells[1, 2].Value);

        sheet.Commands.Undo();

        Assert.Equal(5, sheet.ColumnCount);
        Assert.Equal(1d, sheet.Cells[1, 0].Value);
        Assert.Equal("=A2+10", sheet.Cells[1, 1].Formula);
    }

    [Fact]
    public void InsertColumnAfterCommand_ExecuteAndUndo_RestoresState()
    {
        var sheet = new Sheet(5, 5);
        sheet.Cells[1, 0].Value = 1d;   // A2
        sheet.Cells[1, 1].Formula = "=A2+10"; // B2

        var cmd = new InsertColumnAfterCommand(sheet, 0); // after A
        Assert.True(sheet.Commands.Execute(cmd));

        Assert.Equal(6, sheet.ColumnCount);
        // value at A2 stays, but B2 moves to C2; formula references should update if referencing >= inserted column
        Assert.Equal(1d, sheet.Cells[1, 0].Value);
        Assert.Equal("=A2+10", sheet.Cells[1, 2].Formula); // original B2 moved to C2
        Assert.Equal(11d, sheet.Cells[1, 2].Value);

        sheet.Commands.Undo();

        Assert.Equal(5, sheet.ColumnCount);
        Assert.Equal(1d, sheet.Cells[1, 0].Value);
        Assert.Equal("=A2+10", sheet.Cells[1, 1].Formula);
    }
}


