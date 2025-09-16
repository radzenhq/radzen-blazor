using Xunit;

namespace Radzen.Blazor.Spreadsheet.Tests;

public class SumFunctionTests
{
    readonly Sheet sheet = new(5, 5);

    [Fact]
    public void ShouldEvaluateSumFunction()
    {
        sheet.Cells["A1"].Value = 1;
        sheet.Cells["A2"].Value = 2;
        sheet.Cells["A3"].Formula = "=SUM(A1,A2)";

        Assert.Equal(3d, sheet.Cells["A3"].Value);
    }

    [Fact]
    public void ShouldEvaluateSumFunctionWithEmptyCells()
    {
        sheet.Cells["A1"].Value = 1;
        sheet.Cells["A3"].Formula = "=SUM(A1,A2)";

        Assert.Equal(1d, sheet.Cells["A3"].Value);
    }

    [Fact]
    public void ShouldEvaluateSumFunctionWithMultipleArguments()
    {
        sheet.Cells["A1"].Value = 1;
        sheet.Cells["A2"].Value = 2;
        sheet.Cells["A3"].Value = 3;
        sheet.Cells["A4"].Formula = "=SUM(A1,A2,A3)";

        Assert.Equal(6d, sheet.Cells["A4"].Value);
    }

    [Fact]
    public void ShouldReturnValueErrorForEmptySumFunction()
    {
        sheet.Cells["A1"].Formula = "=SUM()";

        Assert.Equal(CellError.Value, sheet.Cells["A1"].Value);
    }

    [Fact]
    public void ShouldSumRangeOfCells()
    {
        sheet.Cells["A1"].Value = 1;
        sheet.Cells["A2"].Value = 2;
        sheet.Cells["A3"].Formula = "=SUM(A1:A2)";

        Assert.Equal(3d, sheet.Cells["A3"].Value);
    }

    [Fact]
    public void ShouldSumNumbersOfDifferentTypes()
    {
        sheet.Cells["A1"].Value = 1;
        sheet.Cells["A2"].Value = 2.5;
        sheet.Cells["A3"].Formula = "=SUM(A1,A2)";

        Assert.Equal(3.5, sheet.Cells["A3"].Value);

        sheet.Cells["A4"].Value = 2.5;
        sheet.Cells["A5"].Formula = "=SUM(A4,A1)";

        Assert.Equal(3.5, sheet.Cells["A5"].Value);
    }
}


