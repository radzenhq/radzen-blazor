using Xunit;

namespace Radzen.Blazor.Spreadsheet.Tests;

public class SubtotalFunctionTests
{
    readonly Sheet sheet = new(10, 10);

    [Fact]
    public void ShouldSumWithCode9()
    {
        sheet.Cells["A2"].Value = 120;
        sheet.Cells["A3"].Value = 10;
        sheet.Cells["A4"].Value = 150;
        sheet.Cells["A5"].Value = 23;

        sheet.Cells["B1"].Formula = "=SUBTOTAL(9,A2:A5)";

        Assert.Equal(303d, sheet.Cells["B1"].Value);
    }

    [Fact]
    public void ShouldAverageWithCode1()
    {
        sheet.Cells["A2"].Value = 120;
        sheet.Cells["A3"].Value = 10;
        sheet.Cells["A4"].Value = 150;
        sheet.Cells["A5"].Value = 23;

        sheet.Cells["B1"].Formula = "=SUBTOTAL(1,A2:A5)";

        Assert.Equal(75.75, sheet.Cells["B1"].Value);
    }
    [Fact]
    public void ShouldCountWithCode2()
    {
        sheet.Cells["A2"].Value = 120;
        sheet.Cells["A3"].Value = "x"; // non-numeric, ignored by COUNT
        sheet.Cells["A4"].Value = 150;
        sheet.Cells["A5"].Value = null;

        sheet.Cells["B1"].Formula = "=SUBTOTAL(2,A2:A5)";
        Assert.Equal(2d, sheet.Cells["B1"].Value);
    }

    [Fact]
    public void ShouldCountAWithCode3()
    {
        sheet.Cells["A2"].Value = 120;
        sheet.Cells["A3"].Value = "x";
        sheet.Cells["A4"].Value = null;
        sheet.Cells["A5"].Value = 23;

        sheet.Cells["B1"].Formula = "=SUBTOTAL(3,A2:A5)";
        Assert.Equal(3d, sheet.Cells["B1"].Value);
    }

    [Fact]
    public void ShouldMaxWithCode4()
    {
        sheet.Cells["A2"].Value = 10;
        sheet.Cells["A3"].Value = 40;
        sheet.Cells["A4"].Value = 30;
        sheet.Cells["A5"].Value = 20;

        sheet.Cells["B1"].Formula = "=SUBTOTAL(4,A2:A5)";
        Assert.Equal(40d, sheet.Cells["B1"].Value);
    }

    [Fact]
    public void ShouldMinWithCode5()
    {
        sheet.Cells["A2"].Value = 10;
        sheet.Cells["A3"].Value = 40;
        sheet.Cells["A4"].Value = 30;
        sheet.Cells["A5"].Value = 20;

        sheet.Cells["B1"].Formula = "=SUBTOTAL(5,A2:A5)";
        Assert.Equal(10d, sheet.Cells["B1"].Value);
    }

    [Fact]
    public void ShouldRespectHiddenRowsWith109()
    {
        sheet.Cells["A2"].Value = 120;
        sheet.Cells["A3"].Value = 10;
        sheet.Cells["A4"].Value = 150;
        sheet.Cells["A5"].Value = 20;
        sheet.Rows.Hide(2); // hide row 3 (A3)

        sheet.Cells["B1"].Formula = "=SUBTOTAL(109,A2:A5)";
        Assert.Equal(290d, sheet.Cells["B1"].Value); // 120 + 150 + 20 (excludes hidden 10)
    }
}