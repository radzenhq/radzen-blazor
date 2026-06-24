using Xunit;

using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet.Tests;

public class SwitchFunctionTests
{
    readonly Worksheet sheet = new(10, 10);

    [Fact]
    public void ShouldReturnMatchingResult()
    {
        sheet.Cells["A1"].Value = 2;
        sheet.Cells["B1"].Formula = "=SWITCH(A1,1,\"one\",2,\"two\",\"none\")";
        Assert.Equal("two", sheet.Cells["B1"].Value);
    }

    [Fact]
    public void ShouldReturnDefaultWhenNoMatch()
    {
        sheet.Cells["A1"].Value = 9;
        sheet.Cells["B1"].Formula = "=SWITCH(A1,1,\"one\",2,\"two\",\"none\")";
        Assert.Equal("none", sheet.Cells["B1"].Value);
    }

    [Fact]
    public void ShouldReturnNAWhenNoMatchAndNoDefault()
    {
        sheet.Cells["A1"].Value = 9;
        sheet.Cells["B1"].Formula = "=SWITCH(A1,1,\"one\",2,\"two\")";
        Assert.Equal(CellError.NA, sheet.Cells["B1"].Value);
    }

    [Fact]
    public void ShouldMatchTextCaseInsensitively()
    {
        sheet.Cells["A1"].Value = "yes";
        sheet.Cells["B1"].Formula = "=SWITCH(A1,\"YES\",1,\"NO\",0)";
        Assert.Equal(1d, sheet.Cells["B1"].Value);
    }
}
