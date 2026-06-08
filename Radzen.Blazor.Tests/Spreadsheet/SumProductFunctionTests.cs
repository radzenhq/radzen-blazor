using Xunit;

using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet.Tests;

public class SumProductFunctionTests
{
    readonly Worksheet sheet = new(10, 10);

    [Fact]
    public void ShouldSumElementwiseProducts()
    {
        sheet.Cells["A1"].Value = 1;
        sheet.Cells["A2"].Value = 2;
        sheet.Cells["A3"].Value = 3;
        sheet.Cells["B1"].Value = 4;
        sheet.Cells["B2"].Value = 5;
        sheet.Cells["B3"].Value = 6;

        // 1*4 + 2*5 + 3*6 = 32
        sheet.Cells["C1"].Formula = "=SUMPRODUCT(A1:A3,B1:B3)";
        Assert.Equal(32d, sheet.Cells["C1"].Value);
    }

    [Fact]
    public void ShouldTreatNonNumericAsZero()
    {
        sheet.Cells["A1"].Value = 1;
        sheet.Cells["A2"].Value = "x";
        sheet.Cells["B1"].Value = 4;
        sheet.Cells["B2"].Value = 5;

        // 1*4 + 0*5 = 4
        sheet.Cells["C1"].Formula = "=SUMPRODUCT(A1:A2,B1:B2)";
        Assert.Equal(4d, sheet.Cells["C1"].Value);
    }

    [Fact]
    public void ShouldReturnValueOnDimensionMismatch()
    {
        sheet.Cells["A1"].Value = 1;
        sheet.Cells["A2"].Value = 2;
        sheet.Cells["B1"].Value = 4;

        sheet.Cells["C1"].Formula = "=SUMPRODUCT(A1:A2,B1:B1)";
        Assert.Equal(CellError.Value, sheet.Cells["C1"].Value);
    }
}
