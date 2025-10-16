using Xunit;

namespace Radzen.Blazor.Spreadsheet.Tests;

public class SearchFunctionTests
{
    [Fact]
    public void Search_SimpleCharacter()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells["A1"].Formula = "=SEARCH(\"n\",\"printer\")";
        Assert.Equal(4d, sheet.Cells["A1"].Data.Value);
    }

    [Fact]
    public void Search_Substring()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells["A1"].Formula = "=SEARCH(\"base\",\"database\")";
        Assert.Equal(5d, sheet.Cells["A1"].Data.Value);
    }

    [Fact]
    public void Search_StartNum_SkipsPrefix()
    {
        var sheet = new Sheet(10, 30);
        sheet.Cells["A1"].Value = "AYF0093.YoungMensApparel";
        sheet.Cells["B1"].Formula = "=SEARCH(\"Y\",A1,8)";
        Assert.Equal(9d, sheet.Cells["B1"].Data.Value);
    }

    [Fact]
    public void Search_Wildcards_QuestionAndAsterisk()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells["A1"].Value = "Profit Margin";
        sheet.Cells["B1"].Formula = "=SEARCH(\"M*r?in\",A1)";
        Assert.Equal(8d, sheet.Cells["B1"].Data.Value);
    }

    [Fact]
    public void Search_TildeEscapesWildcards()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells["A1"].Value = "a*?c";
        sheet.Cells["B1"].Formula = "=SEARCH(\"~*~?\",A1)";
        Assert.Equal(2d, sheet.Cells["B1"].Data.Value);
    }

    [Fact]
    public void Search_StartNumInvalid_ReturnsValueError()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells["A1"].Formula = "=SEARCH(\"e\",\"printer\",0)";
        Assert.Equal(CellError.Value, sheet.Cells["A1"].Data.GetValueOrDefault<CellError>());
    }

    [Fact]
    public void Search_NotFound_ReturnsValueError()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells["A1"].Formula = "=SEARCH(\"zzz\",\"printer\")";
        Assert.Equal(CellError.Value, sheet.Cells["A1"].Data.GetValueOrDefault<CellError>());
    }
}