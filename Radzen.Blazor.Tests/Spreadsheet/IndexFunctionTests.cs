using Xunit;

namespace Radzen.Blazor.Spreadsheet.Tests;

public class IndexFunctionTests
{
    readonly Sheet sheet = new(10, 10);

    void Seed()
    {
        sheet.Cells["A2"].Value = "Apples";
        sheet.Cells["B2"].Value = "Lemons";
        sheet.Cells["A3"].Value = "Bananas";
        sheet.Cells["B3"].Value = "Pears";
    }

    [Fact]
    public void ShouldReturnIntersectionValue()
    {
        Seed();

        sheet.Cells["C1"].Formula = "=INDEX(A2:B3,2,2)";
        Assert.Equal("Pears", sheet.Cells["C1"].Value);

        sheet.Cells["C2"].Formula = "=INDEX(A2:B3,2,1)";
        Assert.Equal("Bananas", sheet.Cells["C2"].Value);
    }

    [Fact]
    public void ShouldReturnRefErrorIfOutOfRange()
    {
        // numeric values just to ensure range exists
        sheet.Cells["A1"].Value = 1;
        sheet.Cells["B1"].Value = 2;
        sheet.Cells["A2"].Value = 3;
        sheet.Cells["B2"].Value = 4;

        sheet.Cells["C1"].Formula = "=INDEX(A1:B2,3,1)"; // row 3 out of 2 rows
        Assert.Equal(CellError.Ref, sheet.Cells["C1"].Value);
    }

    [Fact]
    public void ShouldDefaultColumnToFirstWhenOmitted()
    {
        Seed();
        sheet.Cells["C1"].Formula = "=INDEX(A2:B3,2)"; // column omitted -> first column
        Assert.Equal("Bananas", sheet.Cells["C1"].Value);
    }

    [Fact]
    public void ShouldUseAreaOneWhenSpecified()
    {
        Seed();
        sheet.Cells["C1"].Formula = "=INDEX(A2:B3,2,2,1)";
        Assert.Equal("Pears", sheet.Cells["C1"].Value);
    }

    [Fact]
    public void ShouldReturnValueErrorWhenAreaGreaterThanOne()
    {
        Seed();
        sheet.Cells["C1"].Formula = "=INDEX(A2:B3,1,1,2)";
        Assert.Equal(CellError.Value, sheet.Cells["C1"].Value);
    }

    [Fact]
    public void ShouldReturnFirstOfEntireColumnWhenRowIsZero()
    {
        Seed();
        sheet.Cells["C1"].Formula = "=INDEX(A2:B3,0,2)";
        Assert.Equal("Lemons", sheet.Cells["C1"].Value);
    }

    [Fact]
    public void ShouldReturnFirstOfEntireRowWhenColumnIsZero()
    {
        Seed();
        sheet.Cells["C1"].Formula = "=INDEX(A2:B3,2,0)";
        Assert.Equal("Bananas", sheet.Cells["C1"].Value);
    }

    [Fact]
    public void ShouldReturnValueErrorWhenBothRowAndColumnOmitted()
    {
        Seed();
        sheet.Cells["C1"].Formula = "=INDEX(A2:B3)";
        Assert.Equal(CellError.Value, sheet.Cells["C1"].Value);
    }

    [Fact]
    public void ShouldReturnRefErrorOnNegativeIndices()
    {
        Seed();
        sheet.Cells["C1"].Formula = "=INDEX(A2:B3,0-1,1)"; // -1
        Assert.Equal(CellError.Ref, sheet.Cells["C1"].Value);

        sheet.Cells["C2"].Formula = "=INDEX(A2:B3,1,0-1)"; // -1
        Assert.Equal(CellError.Ref, sheet.Cells["C2"].Value);
    }
}