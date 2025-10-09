using Xunit;

namespace Radzen.Blazor.Spreadsheet.Tests;

public class DeleteRowColumnTests
{
    [Fact]
    public void DeleteColumn_ShiftsDataAndDecreasesColumnCount()
    {
        var sheet = new Sheet(3, 4);
        sheet.Cells[0, 0].Value = "A";
        sheet.Cells[0, 1].Value = "B";
        sheet.Cells[0, 2].Value = "C";
        sheet.Cells[0, 3].Value = "D";

        sheet.DeleteColumn(1); // delete column B

        Assert.Equal(3, sheet.ColumnCount);
        Assert.Equal("A", sheet.Cells[0, 0].Value);
        Assert.Equal("C", sheet.Cells[0, 1].Value);
        Assert.Equal("D", sheet.Cells[0, 2].Value);
    }

    [Fact]
    public void DeleteRow_ShiftsDataAndDecreasesRowCount()
    {
        var sheet = new Sheet(4, 2);
        sheet.Cells[0, 0].Value = "R1";
        sheet.Cells[1, 0].Value = "R2";
        sheet.Cells[2, 0].Value = "R3";
        sheet.Cells[3, 0].Value = "R4";

        sheet.DeleteRow(1); // delete row 2

        Assert.Equal(3, sheet.RowCount);
        Assert.Equal("R1", sheet.Cells[0, 0].Value);
        Assert.Equal("R3", sheet.Cells[1, 0].Value);
        Assert.Equal("R4", sheet.Cells[2, 0].Value);
    }

    [Fact]
    public void DeleteColumn_UpdatesRelativeReferencesOnly()
    {
        var sheet = new Sheet(5, 5);
        sheet.Cells[0, 0].Value = 1;   // A1
        sheet.Cells[0, 1].Value = 2;   // B1
        sheet.Cells[0, 2].Value = 3;   // C1

        // Place formula in B2 so it survives deleting column A and shifts to A2
        // Formula references B1 (relative) and $C$1 (absolute both)
        sheet.Cells[1, 1].Formula = "=B1+$C$1";
        Assert.Equal(5d, sheet.Cells[1, 1].Value);

        sheet.DeleteColumn(0); // delete column A, shift left

        // After deletion, formula cell B2 moved to A2; B1 moved to A1; $C$1 remains C1
        Assert.Equal(5d, sheet.Cells[1, 0].Value);
    }

    [Fact]
    public void DeleteRow_UpdatesRelativeReferencesOnly()
    {
        var sheet = new Sheet(5, 5);
        sheet.Cells[0, 0].Value = 1;   // A1
        sheet.Cells[1, 0].Value = 2;   // A2
        sheet.Cells[2, 0].Value = 3;   // A3

        // Place formula in B2 so it survives deleting row 1 and shifts to B1
        // Formula references A2 (relative) and $A$3 (absolute both)
        sheet.Cells[1, 1].Formula = "=A2+$A$3";
        Assert.Equal(5d, sheet.Cells[1, 1].Value);

        sheet.DeleteRow(0); // delete row 1, shift up

        // After deletion, formula cell B2 moved to B1; A2 moved to A1; $A$3 remains A3
        Assert.Equal(5d, sheet.Cells[0, 1].Value);
    }
}


