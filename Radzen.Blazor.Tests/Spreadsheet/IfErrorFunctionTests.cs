using Xunit;

namespace Radzen.Blazor.Spreadsheet.Tests;

public class IfErrorFunctionTests
{
    readonly Sheet sheet = new(5, 5);

    [Fact]
    public void ShouldEvaluateIfErrorFunctionWithNoError()
    {
        sheet.Cells["A1"].Value = 10;
        sheet.Cells["A2"].Value = 2;
        sheet.Cells["A3"].Formula = "=IFERROR(A1/A2, \"Error in calculation\")";

        Assert.Equal(5d, sheet.Cells["A3"].Value);
    }

    [Fact]
    public void ShouldEvaluateIfErrorFunctionWithDivisionByZero()
    {
        sheet.Cells["A1"].Value = 10;
        sheet.Cells["A2"].Value = 0;
        sheet.Cells["A3"].Formula = "=IFERROR(A1/A2, \"Error in calculation\")";

        Assert.Equal("Error in calculation", sheet.Cells["A3"].Value);
    }

    [Fact]
    public void ShouldEvaluateIfErrorFunctionWithReferenceError()
    {
        sheet.Cells["A1"].Formula = "=IFERROR(A6, \"Error in calculation\")";

        Assert.Equal("Error in calculation", sheet.Cells["A1"].Value);
    }

    [Fact]
    public void ShouldEvaluateIfErrorFunctionWithEmptyStringForError()
    {
        sheet.Cells["A1"].Value = 10;
        sheet.Cells["A2"].Value = 0;
        sheet.Cells["A3"].Formula = "=IFERROR(A1/A2, \"\")";

        Assert.Equal("", sheet.Cells["A3"].Value);
    }

    [Fact]
    public void ShouldEvaluateIfErrorFunctionWithNumericErrorValue()
    {
        sheet.Cells["A1"].Value = 10;
        sheet.Cells["A2"].Value = 0;
        sheet.Cells["A3"].Formula = "=IFERROR(A1/A2, 0)";

        Assert.Equal(0d, sheet.Cells["A3"].Value);
    }

    [Fact]
    public void ShouldEvaluateIfErrorFunctionWithEmptyCellAsErrorValue()
    {
        sheet.Cells["A1"].Value = 10;
        sheet.Cells["A2"].Value = 0;
        sheet.Cells["A3"].Formula = "=IFERROR(A1/A2, A4)";

        Assert.Equal("", sheet.Cells["A3"].Value);
    }

    [Fact]
    public void ShouldEvaluateIfErrorFunctionWithEmptyCellAsValue()
    {
        sheet.Cells["A1"].Formula = "=IFERROR(A2, \"Empty cell\")";

        Assert.Equal("", sheet.Cells["A1"].Value);
    }

    [Fact]
    public void ShouldEvaluateIfErrorFunctionWithStringValue()
    {
        sheet.Cells["A1"].Value = "Hello";
        sheet.Cells["A2"].Formula = "=IFERROR(A1, \"Error\")";

        Assert.Equal("Hello", sheet.Cells["A2"].Value);
    }

    [Fact]
    public void ShouldEvaluateIfErrorFunctionWithBooleanValue()
    {
        sheet.Cells["A1"].Value = true;
        sheet.Cells["A2"].Formula = "=IFERROR(A1, \"Error\")";

        Assert.Equal(true, sheet.Cells["A2"].Value);
    }

    [Fact]
    public void ShouldReturnValueErrorForIfErrorTooFewArguments()
    {
        sheet.Cells["A1"].Formula = "=IFERROR(A2)";

        Assert.Equal(CellError.Value, sheet.Cells["A1"].Value);
    }

    [Fact]
    public void ShouldReturnValueErrorForIfErrorTooManyArguments()
    {
        sheet.Cells["A1"].Formula = "=IFERROR(A2, \"Error\", \"Extra\")";

        Assert.Equal(CellError.Value, sheet.Cells["A1"].Value);
    }

    [Fact]
    public void ShouldEvaluateIfErrorFunctionWithNestedFormula()
    {
        sheet.Cells["A1"].Value = 10;
        sheet.Cells["A2"].Value = 0;
        sheet.Cells["A3"].Formula = "=IFERROR(A1/A2, IFERROR(A1/0, \"Nested Error\"))";

        Assert.Equal("Nested Error", sheet.Cells["A3"].Value);
    }

    [Fact]
    public void ShouldEvaluateIfErrorFunctionWithSumFunction()
    {
        sheet.Cells["A1"].Value = 10;
        sheet.Cells["A2"].Value = 20;
        sheet.Cells["A3"].Formula = "=IFERROR(SUM(A1:A2), \"Error\")";

        Assert.Equal(30d, sheet.Cells["A3"].Value);
    }

    [Fact]
    public void ShouldEvaluateIfErrorFunctionWithSumFunctionError()
    {
        sheet.Cells["A1"].Formula = "=IFERROR(SUM(A6:A8), \"Error\")";

        Assert.Equal("Error", sheet.Cells["A1"].Value);
    }
}


