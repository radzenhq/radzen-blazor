using Xunit;

namespace Radzen.Blazor.Spreadsheet.Tests;

public class LenFunctionTests
{
    [Fact]
    public void Len_String_ReturnsLength()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells["A1"].Value = "Phoenix, AZ"; // 11 characters
        sheet.Cells["B1"].Formula = "=LEN(A1)";
        Assert.Equal(11d, sheet.Cells["B1"].Data.Value);
    }

    [Fact]
    public void Len_BooleanCellTrue_ReturnsFour()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells["A1"].Value = true;
        sheet.Cells["B1"].Formula = "=LEN(A1)";
        Assert.Equal(4d, sheet.Cells["B1"].Data.Value);
    }

    [Fact]
    public void Len_BooleanCellFalse_ReturnsFive()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells["A1"].Value = false;
        sheet.Cells["B1"].Formula = "=LEN(A1)";
        Assert.Equal(5d, sheet.Cells["B1"].Data.Value);
    }
    [Fact]
    public void Len_Empty_ReturnsZero()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells["A1"].Value = null; // empty
        sheet.Cells["B1"].Formula = "=LEN(A1)";
        Assert.Equal(0d, sheet.Cells["B1"].Data.Value);
    }

    [Fact]
    public void Len_String_WithSpaces_CountsSpaces()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells["A1"].Value = "     One   "; // 11 characters including spaces
        sheet.Cells["B1"].Formula = "=LEN(A1)";
        Assert.Equal(11d, sheet.Cells["B1"].Data.Value);
    }

    [Fact]
    public void Len_Number_TreatsAsTextLength()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells["A1"].Value = 123.45; // "123.45" length 6
        sheet.Cells["B1"].Formula = "=LEN(A1)";
        Assert.Equal(6d, sheet.Cells["B1"].Data.Value);
    }
}