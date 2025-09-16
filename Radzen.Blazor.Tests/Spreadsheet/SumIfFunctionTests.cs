using Xunit;

namespace Radzen.Blazor.Spreadsheet.Tests;

public class SumIfFunctionTests
{
    readonly Sheet sheet = new(10, 10);

    [Fact]
    public void ShouldEvaluateSumIfFunctionWithNumericCriteria()
    {
        // Test data from Excel example
        sheet.Cells["A1"].Value = 100000;
        sheet.Cells["A2"].Value = 200000;
        sheet.Cells["A3"].Value = 300000;
        sheet.Cells["A4"].Value = 400000;
        
        sheet.Cells["B1"].Value = 7000;
        sheet.Cells["B2"].Value = 14000;
        sheet.Cells["B3"].Value = 21000;
        sheet.Cells["B4"].Value = 28000;

        sheet.Cells["C1"].Formula = "=SUMIF(A1:A4,\">160000\",B1:B4)";

        Assert.Equal(63000d, sheet.Cells["C1"].Value);
    }

    [Fact]
    public void ShouldEvaluateSumIfFunctionWithoutSumRange()
    {
        // Test data from Excel example
        sheet.Cells["A1"].Value = 100000;
        sheet.Cells["A2"].Value = 200000;
        sheet.Cells["A3"].Value = 300000;
        sheet.Cells["A4"].Value = 400000;

        sheet.Cells["C1"].Formula = "=SUMIF(A1:A4,\">160000\")";

        Assert.Equal(900000d, sheet.Cells["C1"].Value);
    }

    [Fact]
    public void ShouldEvaluateSumIfFunctionWithExactMatch()
    {
        // Test data from Excel example
        sheet.Cells["A1"].Value = 100000;
        sheet.Cells["A2"].Value = 200000;
        sheet.Cells["A3"].Value = 300000;
        sheet.Cells["A4"].Value = 400000;
        
        sheet.Cells["B1"].Value = 7000;
        sheet.Cells["B2"].Value = 14000;
        sheet.Cells["B3"].Value = 21000;
        sheet.Cells["B4"].Value = 28000;

        sheet.Cells["C1"].Formula = "=SUMIF(A1:A4,300000,B1:B4)";

        Assert.Equal(21000d, sheet.Cells["C1"].Value);
    }

    [Fact]
    public void ShouldEvaluateSumIfFunctionWithStringCriteria()
    {
        // Test data from Excel example
        sheet.Cells["A1"].Value = "Vegetables";
        sheet.Cells["A2"].Value = "Vegetables";
        sheet.Cells["A3"].Value = "Fruits";
        sheet.Cells["A4"].Value = "";
        sheet.Cells["A5"].Value = "Vegetables";
        sheet.Cells["A6"].Value = "Fruits";
        
        sheet.Cells["C1"].Value = 2300;
        sheet.Cells["C2"].Value = 5500;
        sheet.Cells["C3"].Value = 800;
        sheet.Cells["C4"].Value = 400;
        sheet.Cells["C5"].Value = 4200;
        sheet.Cells["C6"].Value = 1200;

        sheet.Cells["D1"].Formula = "=SUMIF(A1:A6,\"Fruits\",C1:C6)";

        Assert.Equal(2000d, sheet.Cells["D1"].Value);
    }

    [Fact]
    public void ShouldEvaluateSumIfFunctionWithWildcardPattern()
    {
        // Test data from Excel example
        sheet.Cells["A1"].Value = "Vegetables";
        sheet.Cells["A2"].Value = "Vegetables";
        sheet.Cells["A3"].Value = "Fruits";
        sheet.Cells["A4"].Value = "";
        sheet.Cells["A5"].Value = "Vegetables";
        sheet.Cells["A6"].Value = "Fruits";
        
        sheet.Cells["B1"].Value = "Tomatoes";
        sheet.Cells["B2"].Value = "Celery";
        sheet.Cells["B3"].Value = "Oranges";
        sheet.Cells["B4"].Value = "Butter";
        sheet.Cells["B5"].Value = "Carrots";
        sheet.Cells["B6"].Value = "Apples";
        
        sheet.Cells["C1"].Value = 2300;
        sheet.Cells["C2"].Value = 5500;
        sheet.Cells["C3"].Value = 800;
        sheet.Cells["C4"].Value = 400;
        sheet.Cells["C5"].Value = 4200;
        sheet.Cells["C6"].Value = 1200;

        sheet.Cells["D1"].Formula = "=SUMIF(B1:B6,\"*es\",C1:C6)";

        Assert.Equal(4300d, sheet.Cells["D1"].Value);
    }

    [Fact]
    public void ShouldEvaluateSumIfFunctionWithEmptyCriteria()
    {
        // Test data from Excel example
        sheet.Cells["A1"].Value = "Vegetables";
        sheet.Cells["A2"].Value = "Vegetables";
        sheet.Cells["A3"].Value = "Fruits";
        sheet.Cells["A4"].Value = "";
        sheet.Cells["A5"].Value = "Vegetables";
        sheet.Cells["A6"].Value = "Fruits";
        
        sheet.Cells["C1"].Value = 2300;
        sheet.Cells["C2"].Value = 5500;
        sheet.Cells["C3"].Value = 800;
        sheet.Cells["C4"].Value = 400;
        sheet.Cells["C5"].Value = 4200;
        sheet.Cells["C6"].Value = 1200;

        sheet.Cells["D1"].Formula = "=SUMIF(A1:A6,\"\",C1:C6)";

        Assert.Equal(400d, sheet.Cells["D1"].Value);
    }

    [Fact]
    public void ShouldEvaluateSumIfFunctionWithLessThanCriteria()
    {
        sheet.Cells["A1"].Value = 10;
        sheet.Cells["A2"].Value = 20;
        sheet.Cells["A3"].Value = 30;
        sheet.Cells["A4"].Value = 40;
        
        sheet.Cells["B1"].Value = 100;
        sheet.Cells["B2"].Value = 200;
        sheet.Cells["B3"].Value = 300;
        sheet.Cells["B4"].Value = 400;

        sheet.Cells["C1"].Formula = "=SUMIF(A1:A4,\"<25\",B1:B4)";

        Assert.Equal(300d, sheet.Cells["C1"].Value);
    }

    [Fact]
    public void ShouldEvaluateSumIfFunctionWithGreaterThanOrEqualCriteria()
    {
        sheet.Cells["A1"].Value = 10;
        sheet.Cells["A2"].Value = 20;
        sheet.Cells["A3"].Value = 30;
        sheet.Cells["A4"].Value = 40;
        
        sheet.Cells["B1"].Value = 100;
        sheet.Cells["B2"].Value = 200;
        sheet.Cells["B3"].Value = 300;
        sheet.Cells["B4"].Value = 400;

        sheet.Cells["C1"].Formula = "=SUMIF(A1:A4,\">=25\",B1:B4)";

        Assert.Equal(700d, sheet.Cells["C1"].Value);
    }

    [Fact]
    public void ShouldEvaluateSumIfFunctionWithNotEqualCriteria()
    {
        sheet.Cells["A1"].Value = 10;
        sheet.Cells["A2"].Value = 20;
        sheet.Cells["A3"].Value = 30;
        sheet.Cells["A4"].Value = 40;
        
        sheet.Cells["B1"].Value = 100;
        sheet.Cells["B2"].Value = 200;
        sheet.Cells["B3"].Value = 300;
        sheet.Cells["B4"].Value = 400;

        sheet.Cells["C1"].Formula = "=SUMIF(A1:A4,\"<>20\",B1:B4)";

        Assert.Equal(800d, sheet.Cells["C1"].Value);
    }

    [Fact]
    public void ShouldEvaluateSumIfFunctionWithQuestionMarkWildcard()
    {
        sheet.Cells["A1"].Value = "cat";
        sheet.Cells["A2"].Value = "bat";
        sheet.Cells["A3"].Value = "rat";
        sheet.Cells["A4"].Value = "goat";
        
        sheet.Cells["B1"].Value = 10;
        sheet.Cells["B2"].Value = 20;
        sheet.Cells["B3"].Value = 30;
        sheet.Cells["B4"].Value = 40;

        sheet.Cells["C1"].Formula = "=SUMIF(A1:A4,\"?at\",B1:B4)";

        Assert.Equal(60d, sheet.Cells["C1"].Value);
    }

    [Fact]
    public void ShouldEvaluateSumIfFunctionWithEscapedWildcards()
    {
        sheet.Cells["A1"].Value = "*";
        sheet.Cells["A2"].Value = "?";
        sheet.Cells["A3"].Value = "test";
        
        sheet.Cells["B1"].Value = 10;
        sheet.Cells["B2"].Value = 20;
        sheet.Cells["B3"].Value = 30;

        sheet.Cells["C1"].Formula = "=SUMIF(A1:A3,\"~*\",B1:B3)";

        Assert.Equal(10d, sheet.Cells["C1"].Value);
    }

    [Fact]
    public void ShouldReturnValueErrorForInvalidArgumentCount()
    {
        sheet.Cells["A1"].Formula = "=SUMIF(A2)";
        Assert.Equal(CellError.Value, sheet.Cells["A1"].Value);

        sheet.Cells["A2"].Formula = "=SUMIF(A3,A4,A5,A6)";
        Assert.Equal(CellError.Value, sheet.Cells["A2"].Value);
    }

    [Fact]
    public void ShouldReturnValueErrorForMismatchedRangeSizes()
    {
        sheet.Cells["A1"].Value = 1;
        sheet.Cells["A2"].Value = 2;
        sheet.Cells["B1"].Value = 10;

        sheet.Cells["C1"].Formula = "=SUMIF(A1:A2,\">0\",B1)";

        Assert.Equal(CellError.Value, sheet.Cells["C1"].Value);
    }

    [Fact]
    public void ShouldHandleEmptyCellsInSumRange()
    {
        sheet.Cells["A1"].Value = 10;
        sheet.Cells["A2"].Value = 20;
        sheet.Cells["A3"].Value = 30;
        
        // B2 is empty
        sheet.Cells["B1"].Value = 100;
        sheet.Cells["B3"].Value = 300;

        sheet.Cells["C1"].Formula = "=SUMIF(A1:A3,\">15\",B1:B3)";

        Assert.Equal(300d, sheet.Cells["C1"].Value);
    }

    [Fact]
    public void ShouldHandleNonNumericValuesInSumRange()
    {
        sheet.Cells["A1"].Value = 10;
        sheet.Cells["A2"].Value = 20;
        sheet.Cells["A3"].Value = 30;
        
        sheet.Cells["B1"].Value = 100;
        sheet.Cells["B2"].Value = "text"; // Non-numeric
        sheet.Cells["B3"].Value = 300;

        sheet.Cells["C1"].Formula = "=SUMIF(A1:A3,\">15\",B1:B3)";

        Assert.Equal(300d, sheet.Cells["C1"].Value);
    }
}
