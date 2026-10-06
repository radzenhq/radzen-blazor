using Radzen.Documents.Spreadsheet;
using Xunit;

namespace Radzen.Blazor.Spreadsheet.Tests;

public class CalculationChainTests
{
    [Fact]
    public void EditingCellReadByTheEndOfALongChainReadsTheChainWithoutRecalculatingIt()
    {
        const int rows = 50000;
        var sheet = new Worksheet(rows, 2);

        sheet.BeginUpdate();
        sheet.Cells[0, 0].Value = 1;
        for (var row = 1; row < rows; row++)
        {
            sheet.Cells[row, 0].Formula = $"=A{row}+1";
        }
        sheet.Cells[0, 1].Value = 1;
        sheet.Cells[rows - 1, 1].Formula = $"=A{rows}+B1";
        sheet.EndUpdate();

        sheet.Cells[0, 1].Value = 2;

        Assert.Equal((double)rows + 2, sheet.Cells[rows - 1, 1].Value);
    }

    [Fact]
    public void ChangingTheStartOfALongChainRecalculatesItsEnd()
    {
        const int rows = 50000;
        var sheet = new Worksheet(rows, 1);

        sheet.BeginUpdate();
        sheet.Cells[0, 0].Value = 1;
        for (var row = 1; row < rows; row++)
        {
            sheet.Cells[row, 0].Formula = $"=A{row}+1";
        }
        sheet.EndUpdate();

        sheet.Cells[0, 0].Value = 10;

        Assert.Equal((double)rows + 9, sheet.Cells[rows - 1, 0].Value);
    }

    [Fact]
    public void IndexIntoRangesThatContainEachOthersCellsEvaluatesOnlyTheSelectedCells()
    {
        const int rows = 5000;
        var sheet = new Worksheet(rows, 2);

        sheet.BeginUpdate();
        sheet.Cells[0, 0].Value = 0;
        sheet.Cells[0, 1].Value = 0;
        for (var row = 2; row <= rows; row++)
        {
            sheet.Cells[$"A{row}"].Formula = $"=INDEX($B$1:$B${rows},{row - 1})+1";
            sheet.Cells[$"B{row}"].Formula = $"=INDEX($A$1:$A${rows},{row})+1";
        }
        sheet.EndUpdate();

        Assert.Equal(2d * rows - 3, sheet.Cells[$"A{rows}"].Value);
        Assert.Equal(2d * rows - 2, sheet.Cells[$"B{rows}"].Value);
    }

    [Fact]
    public void IndexWhoseSelectionDependsOnAnotherFormulaFollowsThatFormula()
    {
        var sheet = new Worksheet(10, 3);

        sheet.Cells["A1"].Value = 10;
        sheet.Cells["A2"].Value = 20;
        sheet.Cells["A3"].Formula = "=INDEX(A1:A5,C1)";
        sheet.Cells["B1"].Value = 1;
        sheet.Cells["C1"].Formula = "=B1";

        Assert.Equal(10d, sheet.Cells["A3"].Value);

        sheet.Cells["B1"].Value = 2;

        Assert.Equal(20d, sheet.Cells["A3"].Value);
    }

    [Fact]
    public void CellsThatReferenceEachOtherAreCircular()
    {
        var sheet = new Worksheet(10, 3);

        sheet.Cells["A1"].Formula = "=B1+1";
        sheet.Cells["B1"].Formula = "=A1+1";

        Assert.Equal(CellError.Circular, sheet.Cells["A1"].Value);
        Assert.Equal(CellError.Circular, sheet.Cells["B1"].Value);
    }

    [Fact]
    public void IndexThatSelectsItsOwnCellIsCircular()
    {
        var sheet = new Worksheet(10, 3);

        sheet.Cells["A1"].Value = 1;
        sheet.Cells["A2"].Formula = "=INDEX(A1:A3,2)";

        Assert.Equal(CellError.Circular, sheet.Cells["A2"].Value);
    }

    [Theory]
    [InlineData("A5", "=ROW(A5)", 5d)]
    [InlineData("C5", "=COLUMN(C5)", 3d)]
    [InlineData("A5", "=ROWS(A1:A10)", 10d)]
    [InlineData("A5", "=COLUMNS(A1:C10)", 3d)]
    public void ShapeFunctionsDoNotReadTheCellsOfTheirReference(string address, string formula, double expected)
    {
        var sheet = new Worksheet(20, 5);

        sheet.Cells[address].Formula = formula;

        Assert.Equal(expected, sheet.Cells[address].Value);
    }

    [Fact]
    public void IfDoesNotEvaluateTheBranchItDoesNotTake()
    {
        var sheet = new Worksheet(10, 3);

        sheet.Cells["B1"].Value = 1;
        sheet.Cells["A1"].Formula = "=IF(B1>0,B1,A2)";
        sheet.Cells["A2"].Formula = "=A1";

        Assert.Equal(1d, sheet.Cells["A1"].Value);
        Assert.Equal(1d, sheet.Cells["A2"].Value);
    }

    [Theory]
    [InlineData("=CHOOSE(1,5,1/0)", 5d)]
    [InlineData("=IFS(TRUE,5,TRUE,1/0)", 5d)]
    [InlineData("=SWITCH(1,1,5,2,1/0)", 5d)]
    [InlineData("=IFERROR(5,1/0)", 5d)]
    public void BranchFunctionsEvaluateOnlyTheSelectedValue(string formula, double expected)
    {
        var sheet = new Worksheet(10, 3);

        sheet.Cells["A1"].Formula = formula;

        Assert.Equal(expected, sheet.Cells["A1"].Value);
    }

    [Fact]
    public void VlookupDoesNotDependOnTableCellsItDoesNotRead()
    {
        var sheet = new Worksheet(10, 3);

        sheet.Cells["A1"].Value = "a";
        sheet.Cells["A2"].Value = "b";
        sheet.Cells["B2"].Value = 7;
        sheet.Cells["C1"].Formula = "=B1+1";
        sheet.Cells["B1"].Formula = "=VLOOKUP(\"b\",A1:C2,2,FALSE)";

        Assert.Equal(7d, sheet.Cells["B1"].Value);
        Assert.Equal(8d, sheet.Cells["C1"].Value);
    }

    [Theory]
    [InlineData("=SUMIFS(B1:B3,A1:A3,OR(\"x\",\"y\"))", 0d)]
    [InlineData("=COUNTIFS(A1:A3,C1)", 1d)]
    [InlineData("=COUNTIF(A1:A3,C1)", 1d)]
    public void ErrorCriteriaMatchCellsHoldingTheSameError(string formula, double expected)
    {
        var sheet = new Worksheet(10, 5);

        sheet.Cells["A1"].Value = 1;
        sheet.Cells["A2"].Formula = "=1/0";
        sheet.Cells["A3"].Value = 3;
        sheet.Cells["B1"].Value = 10;
        sheet.Cells["B2"].Value = 20;
        sheet.Cells["B3"].Value = 30;
        sheet.Cells["C1"].Formula = "=1/0";
        sheet.Cells["D1"].Formula = formula;

        Assert.Equal(expected, sheet.Cells["D1"].Value);
    }

    [Fact]
    public void FormulasReadingTheSameRangeSeeFormulaCellsInsideItAfterTheyAreRecalculated()
    {
        var sheet = new Worksheet(10, 3);

        sheet.Cells["A1"].Value = 1;
        sheet.Cells["A2"].Value = 2;
        sheet.Cells["A3"].Formula = "=A1+A2";
        sheet.Cells["B1"].Formula = "=SUM(A1:A3)";
        sheet.Cells["B2"].Formula = "=SUM(A1:A3)";
        sheet.Cells["B3"].Formula = "=MAX(A1:A3)";

        sheet.Cells["A1"].Value = 10;

        Assert.Equal(24d, sheet.Cells["B1"].Value);
        Assert.Equal(24d, sheet.Cells["B2"].Value);
        Assert.Equal(12d, sheet.Cells["B3"].Value);
    }
}
