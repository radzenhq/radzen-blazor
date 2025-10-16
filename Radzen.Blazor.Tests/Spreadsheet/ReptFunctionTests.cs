using Xunit;

namespace Radzen.Blazor.Spreadsheet.Tests;

public class ReptFunctionTests
{
    [Fact]
    public void Rept_Basic()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells["A1"].Formula = "=REPT(\"*-\",3)";
        Assert.Equal("*-*-*-", sheet.Cells["A1"].Data.Value);
    }

    [Fact]
    public void Rept_DashesTen()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells["A1"].Formula = "=REPT(\"-\",10)";
        Assert.Equal("----------", sheet.Cells["A1"].Data.Value);
    }

    [Fact]
    public void Rept_ZeroTimes_ReturnsEmpty()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells["A1"].Formula = "=REPT(\"x\",0)";
        Assert.Equal(string.Empty, sheet.Cells["A1"].Data.Value);
    }

    [Fact]
    public void Rept_Negative_ReturnsValueError()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells["A1"].Formula = "=REPT(\"x\",-1)";
        Assert.Equal(CellError.Value, sheet.Cells["A1"].Data.GetValueOrDefault<CellError>());
    }

    [Fact]
    public void Rept_Overflow_ReturnsValueError()
    {
        var sheet = new Sheet(10, 10);
        // text length 2 * 20000 = 40000 > 32767
        sheet.Cells["A1"].Formula = "=REPT(\"ab\",20000)";
        Assert.Equal(CellError.Value, sheet.Cells["A1"].Data.GetValueOrDefault<CellError>());
    }
}