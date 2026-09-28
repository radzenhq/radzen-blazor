using Xunit;

using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet.Tests;

public class ArrayEvaluationTests
{
    readonly Workbook workbook = new();
    readonly Worksheet sheet;

    public ArrayEvaluationTests()
    {
        sheet = workbook.AddSheet("Sheet1", 10, 10);

        sheet.Cells["A1"].Value = 1;
        sheet.Cells["A2"].Value = 2;
        sheet.Cells["A3"].Value = 3;
        sheet.Cells["B1"].Value = 10;
        sheet.Cells["B2"].Value = 20;
        sheet.Cells["B3"].Value = 30;
    }

    [Theory]
    [InlineData("=SUMPRODUCT((A1:A3>=2)*B1:B3)", 50d)]
    [InlineData("=SUMPRODUCT(-(A1:A3=1),B1:B3)", -10d)]
    [InlineData("=SUMPRODUCT(--(A1:A3>1))", 2d)]
    [InlineData("=SUMPRODUCT((A1:A3=A1)*(B1:B3>=0.7*B1:B3))", 1d)]
    [InlineData("=SUMPRODUCT((A1:A3&\"x\"=\"2x\")*B1:B3)", 20d)]
    public void SumProductAppliesOperatorsToEveryCellOfARange(string formula, double expected)
    {
        sheet.Cells["H1"].Formula = formula;

        Assert.Equal(expected, sheet.Cells["H1"].Value);
    }

    [Theory]
    [InlineData("=SUM(IF((A1:A3>=2)*(B1:B3>0),B1:B3,0))", 50d)]
    [InlineData("=COUNT(IF(A1:A3>1,A1:A3))", 2d)]
    [InlineData("=SUM(IF(A1:A3<3,B1:B3))", 30d)]
    [InlineData("=SUM(IF(TRUE,A1:A3))", 6d)]
    [InlineData("=SUM(ABS(A1:A3-2))", 2d)]
    [InlineData("=SUM(A1:A3*B1:B3)", 140d)]
    public void AggregateFunctionEvaluatesItsArgumentForEveryCellOfARange(string formula, double expected)
    {
        sheet.Cells["H1"].Formula = formula;

        Assert.Equal(expected, sheet.Cells["H1"].Value);
    }

    [Theory]
    [InlineData("=INDEX(B1:B3,MATCH(1,(A1:A3=2)*(B1:B3=20),0))", 20d)]
    [InlineData("=MATCH(TRUE,A1:A3>1,0)", 2d)]
    public void LookupFunctionSearchesAComputedArray(string formula, double expected)
    {
        sheet.Cells["H1"].Formula = formula;

        Assert.Equal(expected, sheet.Cells["H1"].Value);
    }

    [Fact]
    public void SumProductOfCountIfOverItsOwnRangeCountsDistinctValues()
    {
        sheet.Cells["A4"].Value = 2;
        sheet.Cells["H1"].Formula = "=SUMPRODUCT(1/COUNTIF(A1:A4,A1:A4))";

        Assert.Equal(3d, sheet.Cells["H1"].Value);
    }

    [Fact]
    public void ASingleCellRangeBroadcastsAcrossTheOtherOperand()
    {
        sheet.Cells["H1"].Formula = "=SUMPRODUCT(A1:A3*A1:A1)";

        Assert.Equal(6d, sheet.Cells["H1"].Value);
    }

    [Fact]
    public void AColumnTimesARowProducesEveryCombination()
    {
        sheet.Cells["D1"].Value = 1;
        sheet.Cells["E1"].Value = 2;
        sheet.Cells["F1"].Value = 3;
        sheet.Cells["H1"].Formula = "=SUMPRODUCT(A1:A3*D1:F1)";

        Assert.Equal(36d, sheet.Cells["H1"].Value);
    }

    [Fact]
    public void RangesOfDifferentLengthsAreNotAvailablePastTheShorterOne()
    {
        sheet.Cells["H1"].Formula = "=SUMPRODUCT(A1:A3*B1:B4)";

        Assert.Equal(CellError.NA, sheet.Cells["H1"].Value);
    }

    [Theory]
    [InlineData("D2", "=A1:A3*10", 20d)]
    [InlineData("D3", "=A1:A3", 3d)]
    [InlineData("D2", "=ABS(A1:A3*-10)", 20d)]
    [InlineData("D2", "=ABS(A1:A3-5)", 3d)]
    [InlineData("B5", "=A1:B1*2", 20d)]
    [InlineData("D3", "=IF(A1:A3>2,\"yes\",\"no\")", "yes")]
    public void ARangeWhereOneValueIsNeededIntersectsTheFormulaCell(string address, string formula, object expected)
    {
        sheet.Cells[address].Formula = formula;

        Assert.Equal(expected, sheet.Cells[address].Value);
    }

    [Theory]
    [InlineData("D6", "=A1:A3*10")]
    [InlineData("D7", "=A1=A1:A3")]
    [InlineData("D6", "=ABS(A1:A3)")]
    [InlineData("D2", "=A1:B3")]
    public void ARangeWhereOneValueIsNeededIsAValueErrorWhenItDoesNotIntersectTheFormulaCell(string address, string formula)
    {
        sheet.Cells[address].Formula = formula;

        Assert.Equal(CellError.Value, sheet.Cells[address].Value);
    }

    [Fact]
    public void ARangeOnAnotherSheetIntersectsByRow()
    {
        var data = workbook.AddSheet("Data", 10, 10);
        data.Cells["A1"].Value = 1;
        data.Cells["A2"].Value = 2;
        data.Cells["A3"].Value = 3;

        sheet.Cells["D2"].Formula = "=Data!A1:A3*10";

        Assert.Equal(20d, sheet.Cells["D2"].Value);
    }

    [Fact]
    public void AReferencedFormulaIntersectsWithItsOwnCell()
    {
        sheet.Cells["D2"].Formula = "=A1:A3*2";
        sheet.Cells["E1"].Formula = "=D2+1";
        sheet.Cells["E5"].Formula = "=SUM(D1:D3)";

        Assert.Equal(5d, sheet.Cells["E1"].Value);
        Assert.Equal(4d, sheet.Cells["E5"].Value);
    }
}
