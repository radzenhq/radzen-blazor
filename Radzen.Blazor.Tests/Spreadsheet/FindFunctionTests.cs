using Xunit;

namespace Radzen.Blazor.Spreadsheet.Tests;

public class FindFunctionTests
{
    [Fact]
    public void Find_CaseSensitive_MatchesUppercase()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells["A2"].Value = "Miriam McGovern";
        sheet.Cells["B1"].Formula = "=FIND(\"M\",A2)";
        Assert.Equal(1d, sheet.Cells["B1"].Data.Value);
    }

    [Fact]
    public void Find_CaseSensitive_MatchesLowercase()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells["A2"].Value = "Miriam McGovern";
        sheet.Cells["B1"].Formula = "=FIND(\"m\",A2)";
        Assert.Equal(6d, sheet.Cells["B1"].Data.Value);
    }

    [Fact]
    public void Find_WithStartNum()
    {
        var sheet = new Sheet(10, 30);
        sheet.Cells["A1"].Value = "AYF0093.YoungMensApparel";
        sheet.Cells["B1"].Formula = "=FIND(\"Y\",A1,8)";
        Assert.Equal(9d, sheet.Cells["B1"].Data.Value);
    }

    [Fact]
    public void Find_EmptyFindText_ReturnsStart()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells["A1"].Value = "abc";
        sheet.Cells["B1"].Formula = "=FIND(\"\",A1,2)";
        Assert.Equal(2d, sheet.Cells["B1"].Data.Value);
    }

    [Fact]
    public void Find_NotFound_ReturnsValue()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells["A1"].Value = "abc";
        sheet.Cells["B1"].Formula = "=FIND(\"z\",A1)";
        Assert.Equal(CellError.Value, sheet.Cells["B1"].Data.GetValueOrDefault<CellError>());
    }
}