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
    public void DeleteColumn_DoesNotAdjustFormulas_RefsBecomeError()
    {
        var sheet = new Sheet(5, 5);
        sheet.Cells[0, 0].Value = 1;   // A1
        sheet.Cells[0, 1].Value = 2;   // B1
        sheet.Cells[0, 2].Value = 3;   // C1

        // Formula in B2 references A1 and C1
        sheet.Cells[1, 1].Formula = "=A1+C1";
        Assert.Equal(4d, sheet.Cells[1, 1].Value);

        // Delete referenced column A -> A1 becomes invalid => #REF!
        sheet.DeleteColumn(0);

        Assert.Equal(CellError.Ref, sheet.Cells[1, 0].Value);
        Assert.Equal("=#REF!+C1", sheet.Cells[1, 0].Formula);
    }

    [Fact]
    public void DeleteRow_DoesNotAdjustFormulas_RefsBecomeError()
    {
        var sheet = new Sheet(5, 5);
        sheet.Cells[0, 0].Value = 1;   // A1
        sheet.Cells[1, 0].Value = 2;   // A2
        sheet.Cells[2, 0].Value = 3;   // A3

        // Formula in B2 references A1 and A3
        sheet.Cells[1, 1].Formula = "=A1+A3";
        Assert.Equal(4d, sheet.Cells[1, 1].Value);

        // Delete referenced row 1 -> A1 becomes invalid => #REF!
        sheet.DeleteRow(0);

        Assert.Equal(CellError.Ref, sheet.Cells[0, 1].Value);
        Assert.Equal("=#REF!+A3", sheet.Cells[0, 1].Formula);
    }
}


