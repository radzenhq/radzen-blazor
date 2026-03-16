using Xunit;

namespace Radzen.Blazor.Spreadsheet.Tests;

public class BooleanArithmeticTests
{
    readonly Sheet sheet = new(5, 5);

    [Fact]
    public void TruePlusOneShouldEqualTwo()
    {
        sheet.Cells["A1"].Formula = "=TRUE+1";

        Assert.Equal(2d, sheet.Cells["A1"].Value);
    }

    [Fact]
    public void FalsePlusOneShouldEqualOne()
    {
        sheet.Cells["A1"].Formula = "=FALSE+1";

        Assert.Equal(1d, sheet.Cells["A1"].Value);
    }

    [Fact]
    public void TruePlusTrueShouldEqualTwo()
    {
        sheet.Cells["A1"].Formula = "=TRUE+TRUE";

        Assert.Equal(2d, sheet.Cells["A1"].Value);
    }

    [Fact]
    public void TrueTimesFiveShouldEqualFive()
    {
        sheet.Cells["A1"].Formula = "=TRUE*5";

        Assert.Equal(5d, sheet.Cells["A1"].Value);
    }

    [Fact]
    public void NegatedTrueShouldEqualMinusOne()
    {
        sheet.Cells["A1"].Formula = "=-TRUE";

        Assert.Equal(-1d, sheet.Cells["A1"].Value);
    }

    [Fact]
    public void TrueMinusFalseShouldEqualOne()
    {
        sheet.Cells["A1"].Formula = "=TRUE-FALSE";

        Assert.Equal(1d, sheet.Cells["A1"].Value);
    }

    [Fact]
    public void CellRefBooleanShouldCoerceInArithmetic()
    {
        sheet.Cells["A1"].Value = true;
        sheet.Cells["A2"].Formula = "=A1+1";

        Assert.Equal(2d, sheet.Cells["A2"].Value);
    }

    [Fact]
    public void UnaryPlusShouldNotCoerceBoolean()
    {
        sheet.Cells["A1"].Formula = "=+TRUE";

        Assert.Equal(true, sheet.Cells["A1"].Value);
    }

    [Fact]
    public void UnaryPlusShouldNotCoerceBooleanCellRef()
    {
        sheet.Cells["A1"].Value = true;
        sheet.Cells["A2"].Formula = "=+A1";

        Assert.Equal(true, sheet.Cells["A2"].Value);
    }
}

public class BooleanSumTests
{
    readonly Sheet sheet = new(5, 5);

    [Fact]
    public void SumWithDirectBooleanConstantsShouldCoerceToNumbers()
    {
        sheet.Cells["A1"].Formula = "=SUM(TRUE,FALSE,1,2)";

        Assert.Equal(4d, sheet.Cells["A1"].Value);
    }

    [Fact]
    public void SumWithBooleanCellRangeShouldSkipBooleans()
    {
        sheet.Cells["A1"].Value = true;
        sheet.Cells["A2"].Value = false;
        sheet.Cells["A3"].Value = 1;
        sheet.Cells["A4"].Value = 2;
        sheet.Cells["A5"].Formula = "=SUM(A1:A4)";

        Assert.Equal(3d, sheet.Cells["A5"].Value);
    }

    [Fact]
    public void SumWithBooleanCellRefsShouldSkipBooleans()
    {
        sheet.Cells["A1"].Value = true;
        sheet.Cells["A2"].Value = false;
        sheet.Cells["A3"].Value = 1;
        sheet.Cells["A4"].Value = 2;
        sheet.Cells["A5"].Formula = "=SUM(A1,A2,A3,A4)";

        Assert.Equal(3d, sheet.Cells["A5"].Value);
    }

    [Fact]
    public void SumWithSingleDirectTrueShouldEqualOne()
    {
        sheet.Cells["A1"].Formula = "=SUM(TRUE)";

        Assert.Equal(1d, sheet.Cells["A1"].Value);
    }

    [Fact]
    public void SumWithMixedRefAndLiteralShouldOnlyCoerceLiteral()
    {
        sheet.Cells["A1"].Value = true;
        sheet.Cells["A2"].Formula = "=SUM(A1,TRUE)";

        Assert.Equal(1d, sheet.Cells["A2"].Value);
    }
}

public class BooleanCountTests
{
    readonly Sheet sheet = new(5, 5);

    [Fact]
    public void CountWithDirectBooleanConstantsShouldCountThem()
    {
        sheet.Cells["A1"].Formula = "=COUNT(TRUE,FALSE,1,2)";

        Assert.Equal(4d, sheet.Cells["A1"].Value);
    }

    [Fact]
    public void CountWithBooleanCellRangeShouldSkipBooleans()
    {
        sheet.Cells["A1"].Value = true;
        sheet.Cells["A2"].Value = false;
        sheet.Cells["A3"].Value = 1;
        sheet.Cells["A4"].Value = 2;
        sheet.Cells["A5"].Formula = "=COUNT(A1:A4)";

        Assert.Equal(2d, sheet.Cells["A5"].Value);
    }

    [Fact]
    public void CountWithBooleanCellRefsShouldSkipBooleans()
    {
        sheet.Cells["A1"].Value = true;
        sheet.Cells["A2"].Value = false;
        sheet.Cells["A3"].Value = 1;
        sheet.Cells["A4"].Value = 2;
        sheet.Cells["A5"].Formula = "=COUNT(A1,A2,A3,A4)";

        Assert.Equal(2d, sheet.Cells["A5"].Value);
    }

    [Fact]
    public void CountWithSingleDirectTrueShouldEqualOne()
    {
        sheet.Cells["A1"].Formula = "=COUNT(TRUE)";

        Assert.Equal(1d, sheet.Cells["A1"].Value);
    }

    [Fact]
    public void CountWithMixedRefAndLiteralShouldOnlyCountLiteral()
    {
        sheet.Cells["A1"].Value = true;
        sheet.Cells["A2"].Formula = "=COUNT(A1,TRUE)";

        Assert.Equal(1d, sheet.Cells["A2"].Value);
    }
}

public class BooleanAverageTests
{
    readonly Sheet sheet = new(5, 5);

    [Fact]
    public void AverageWithDirectBooleanConstantsShouldIncludeThem()
    {
        sheet.Cells["A1"].Formula = "=AVERAGE(TRUE,FALSE,1,2)";

        Assert.Equal(1d, sheet.Cells["A1"].Value);
    }

    [Fact]
    public void AverageWithBooleanCellRangeShouldSkipBooleans()
    {
        sheet.Cells["A1"].Value = true;
        sheet.Cells["A2"].Value = false;
        sheet.Cells["A3"].Value = 1;
        sheet.Cells["A4"].Value = 2;
        sheet.Cells["A5"].Formula = "=AVERAGE(A1:A4)";

        Assert.Equal(1.5, sheet.Cells["A5"].Value);
    }

    [Fact]
    public void AverageWithBooleanCellRefsShouldSkipBooleans()
    {
        sheet.Cells["A1"].Value = true;
        sheet.Cells["A2"].Value = false;
        sheet.Cells["A3"].Value = 1;
        sheet.Cells["A4"].Value = 2;
        sheet.Cells["A5"].Formula = "=AVERAGE(A1,A2,A3,A4)";

        Assert.Equal(1.5, sheet.Cells["A5"].Value);
    }

    [Fact]
    public void AverageWithOnlyBooleanCellRangeShouldReturnDiv0Error()
    {
        sheet.Cells["A1"].Value = true;
        sheet.Cells["A2"].Value = false;
        sheet.Cells["A3"].Formula = "=AVERAGE(A1:A2)";

        Assert.Equal(CellError.Div0, sheet.Cells["A3"].Value);
    }

    [Fact]
    public void AverageWithSingleDirectTrueShouldEqualOne()
    {
        sheet.Cells["A1"].Formula = "=AVERAGE(TRUE)";

        Assert.Equal(1d, sheet.Cells["A1"].Value);
    }

    [Fact]
    public void AverageWithDirectTrueAndFalseShouldEqualPointFive()
    {
        sheet.Cells["A1"].Formula = "=AVERAGE(TRUE,FALSE)";

        Assert.Equal(0.5, sheet.Cells["A1"].Value);
    }
}

public class BooleanComparisonTests
{
    readonly Sheet sheet = new(5, 5);

    [Fact]
    public void TrueEqualsOneShouldBeFalse()
    {
        sheet.Cells["A1"].Formula = "=TRUE=1";

        Assert.Equal(false, sheet.Cells["A1"].Value);
    }

    [Fact]
    public void TrueEqualsTrueShouldBeTrue()
    {
        sheet.Cells["A1"].Formula = "=TRUE=TRUE";

        Assert.Equal(true, sheet.Cells["A1"].Value);
    }

    [Fact]
    public void FalseEqualsZeroShouldBeFalse()
    {
        sheet.Cells["A1"].Formula = "=FALSE=0";

        Assert.Equal(false, sheet.Cells["A1"].Value);
    }

    [Fact]
    public void FalseEqualsFalseShouldBeTrue()
    {
        sheet.Cells["A1"].Formula = "=FALSE=FALSE";

        Assert.Equal(true, sheet.Cells["A1"].Value);
    }

    [Fact]
    public void TrueGreaterThanFalseShouldBeTrue()
    {
        sheet.Cells["A1"].Formula = "=TRUE>FALSE";

        Assert.Equal(true, sheet.Cells["A1"].Value);
    }

    [Fact]
    public void TrueNotEqualToFalseShouldBeTrue()
    {
        sheet.Cells["A1"].Formula = "=TRUE<>FALSE";

        Assert.Equal(true, sheet.Cells["A1"].Value);
    }
}

public class BooleanMinMaxTests
{
    readonly Sheet sheet = new(5, 5);

    [Fact]
    public void MinWithDirectBooleanConstantsShouldCoerceToNumbers()
    {
        sheet.Cells["A1"].Formula = "=MIN(TRUE,2,3)";

        Assert.Equal(1d, sheet.Cells["A1"].Value);
    }

    [Fact]
    public void MinWithBooleanCellRefsShouldSkipBooleans()
    {
        sheet.Cells["A1"].Value = true;
        sheet.Cells["A2"].Value = false;
        sheet.Cells["A3"].Formula = "=MIN(A1,A2)";

        Assert.Equal(0d, sheet.Cells["A3"].Value);
    }

    [Fact]
    public void MaxWithDirectBooleanConstantsShouldCoerceToNumbers()
    {
        sheet.Cells["A1"].Formula = "=MAX(TRUE,FALSE,0)";

        Assert.Equal(1d, sheet.Cells["A1"].Value);
    }

    [Fact]
    public void MaxWithBooleanCellRefsShouldSkipBooleans()
    {
        sheet.Cells["A1"].Value = true;
        sheet.Cells["A2"].Value = false;
        sheet.Cells["A3"].Formula = "=MAX(A1,A2)";

        Assert.Equal(0d, sheet.Cells["A3"].Value);
    }
}

public class BooleanDisplayTests
{
    [Fact]
    public void TrueShouldDisplayAsUppercase()
    {
        var data = CellData.FromBoolean(true);

        Assert.Equal("TRUE", data.ToString());
    }

    [Fact]
    public void FalseShouldDisplayAsUppercase()
    {
        var data = CellData.FromBoolean(false);

        Assert.Equal("FALSE", data.ToString());
    }
}
