using System;
using Xunit;

using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet.Tests;

public class InsertRowColumnTests
{
    [Fact]
    public void InsertColumn_ShiftsReferencesAndValues()
    {
        var sheet = new Worksheet(5, 5);

        sheet.Cells[1, 0].Value = 1;   // A2
        sheet.Cells[1, 1].Formula = "=A2+10"; // B2
        Assert.Equal(11d, sheet.Cells[1, 1].Value);

        // Insert a column before A (index 0)
        sheet.InsertColumn(0, 1);

        // Values shift right
        Assert.Equal(1d, sheet.Cells[1, 1].Value); // A2 moved to B2

        // Formula shifts position and updates referenced address
        Assert.Equal("=B2+10", sheet.Cells[1, 2].Formula); // original B2 moved to C2
        Assert.Equal(11d, sheet.Cells[1, 2].Value);
    }

    [Fact]
    public void InsertRow_ShiftsReferencesAndValues()
    {
        var sheet = new Worksheet(5, 5);

        sheet.Cells[1, 0].Value = 1;   // A2
        sheet.Cells[1, 1].Formula = "=A2+10"; // B2
        Assert.Equal(11d, sheet.Cells[1, 1].Value);

        // Insert a row before row 2 (index 1)
        sheet.InsertRow(1, 1);

        // Values shift down
        Assert.Equal(1d, sheet.Cells[2, 0].Value); // A2 moved to A3

        // Formula shifts position and updates referenced address
        Assert.Equal("=A3+10", sheet.Cells[2, 1].Formula); // original B2 moved to B3
        Assert.Equal(11d, sheet.Cells[2, 1].Value);
    }

    [Fact]
    public void InsertRow_IncreasesRowCount()
    {
        var sheet = new Worksheet(5, 5);
        sheet.InsertRow(2, 2);
        Assert.Equal(7, sheet.RowCount);
    }

    [Fact]
    public void InsertColumn_IncreasesColumnCount()
    {
        var sheet = new Worksheet(5, 5);
        sheet.InsertColumn(3, 3);
        Assert.Equal(8, sheet.ColumnCount);
    }

    [Fact]
    public void DeleteColumns_RemovesColumns()
    {
        var sheet = new Worksheet(5, 5);
        sheet.Cells[0, 0].Value = "A";
        sheet.Cells[0, 1].Value = "B";
        sheet.Cells[0, 2].Value = "C";

        var command = new DeleteColumnsCommand(sheet, 1, 1);
        command.Execute();

        Assert.Equal(4, sheet.ColumnCount);
        Assert.Equal("A", sheet.Cells[0, 0].Value);
        Assert.Equal("C", sheet.Cells[0, 1].Value);
    }

    [Fact]
    public void DeleteColumns_ThrowsWhenDeletingAllColumns()
    {
        var sheet = new Worksheet(5, 5);

        Assert.Throws<ArgumentOutOfRangeException>(() => new DeleteColumnsCommand(sheet, 0, 4));
    }

    [Fact]
    public void DeleteColumns_ThrowsWhenStartIndexNegative()
    {
        var sheet = new Worksheet(5, 5);

        Assert.Throws<ArgumentOutOfRangeException>(() => new DeleteColumnsCommand(sheet, -1, 2));
    }

    [Fact]
    public void DeleteColumns_ThrowsWhenEndExceedsColumnCount()
    {
        var sheet = new Worksheet(5, 5);

        Assert.Throws<ArgumentOutOfRangeException>(() => new DeleteColumnsCommand(sheet, 0, 5));
    }

    [Fact]
    public void DeleteRows_RemovesRows()
    {
        var sheet = new Worksheet(5, 5);
        sheet.Cells[0, 0].Value = "R1";
        sheet.Cells[1, 0].Value = "R2";
        sheet.Cells[2, 0].Value = "R3";

        var command = new DeleteRowsCommand(sheet, 1, 1);
        command.Execute();

        Assert.Equal(4, sheet.RowCount);
        Assert.Equal("R1", sheet.Cells[0, 0].Value);
        Assert.Equal("R3", sheet.Cells[1, 0].Value);
    }

    [Fact]
    public void DeleteRows_ThrowsWhenDeletingAllRows()
    {
        var sheet = new Worksheet(5, 5);

        Assert.Throws<ArgumentOutOfRangeException>(() => new DeleteRowsCommand(sheet, 0, 4));
    }

    [Fact]
    public void DeleteRows_ThrowsWhenStartIndexNegative()
    {
        var sheet = new Worksheet(5, 5);

        Assert.Throws<ArgumentOutOfRangeException>(() => new DeleteRowsCommand(sheet, -1, 2));
    }

    [Fact]
    public void DeleteRows_ThrowsWhenEndExceedsRowCount()
    {
        var sheet = new Worksheet(5, 5);

        Assert.Throws<ArgumentOutOfRangeException>(() => new DeleteRowsCommand(sheet, 0, 5));
    }
}
