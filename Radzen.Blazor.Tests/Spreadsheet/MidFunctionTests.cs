using Xunit;

namespace Radzen.Blazor.Spreadsheet.Tests;

public class MidFunctionTests
{
    [Fact]
    public void Mid_Start1_Take5()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells["A2"].Value = "Fluid Flow"; // length 10
        sheet.Cells["B1"].Formula = "=MID(A2,1,5)";
        Assert.Equal("Fluid", sheet.Cells["B1"].Data.Value);
    }

    [Fact]
    public void Mid_Start7_Take20_Clamped()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells["A2"].Value = "Fluid Flow"; // length 10
        sheet.Cells["B1"].Formula = "=MID(A2,7,20)";
        Assert.Equal("Flow", sheet.Cells["B1"].Data.Value);
    }

    [Fact]
    public void Mid_StartBeyondLength_ReturnsEmpty()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells["A2"].Value = "Fluid Flow"; // length 10
        sheet.Cells["B1"].Formula = "=MID(A2,20,5)";
        Assert.Equal(string.Empty, sheet.Cells["B1"].Data.Value);
    }

    [Fact]
    public void Mid_StartLessThan1_ReturnsValueError()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells["A2"].Value = "Fluid Flow";
        sheet.Cells["B1"].Formula = "=MID(A2,0,5)";
        Assert.Equal(CellError.Value, sheet.Cells["B1"].Data.GetValueOrDefault<CellError>());
    }

    [Fact]
    public void Mid_NegativeNumChars_ReturnsValueError()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells["A2"].Value = "Fluid Flow";
        sheet.Cells["B1"].Formula = "=MID(A2,1,-1)";
        Assert.Equal(CellError.Value, sheet.Cells["B1"].Data.GetValueOrDefault<CellError>());
    }
}