using Xunit;

namespace Radzen.Blazor.Spreadsheet.Tests;

public class ReplaceFunctionTests
{
    [Fact]
    public void Replace_Middle_WithAsterisk()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells["A2"].Value = "abcdefghijk";
        sheet.Cells["B1"].Formula = "=REPLACE(A2,6,5,\"*\")";
        Assert.Equal("abcde*k", sheet.Cells["B1"].Data.Value);
    }

    [Fact]
    public void Replace_LastTwoDigits_With10()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells["A3"].Value = "2009";
        sheet.Cells["B1"].Formula = "=REPLACE(A3,3,2,\"10\")";
        Assert.Equal("2010", sheet.Cells["B1"].Data.Value);
    }

    [Fact]
    public void Replace_FirstThree_WithAt()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells["A4"].Value = "123456";
        sheet.Cells["B1"].Formula = "=REPLACE(A4,1,3,\"@\")";
        Assert.Equal("@456", sheet.Cells["B1"].Data.Value);
    }

    [Fact]
    public void Replace_StartBeyond_Appends()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells["A1"].Value = "abc";
        sheet.Cells["B1"].Formula = "=REPLACE(A1,10,2,\"XYZ\")";
        Assert.Equal("abcXYZ", sheet.Cells["B1"].Data.Value);
    }

    [Fact]
    public void Replace_InvalidStartNum_ReturnsValueError()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells["A1"].Value = "abc";
        sheet.Cells["B1"].Formula = "=REPLACE(A1,0,2,\"X\")";
        Assert.Equal(CellError.Value, sheet.Cells["B1"].Data.GetValueOrDefault<CellError>());
    }
}