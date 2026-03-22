using Xunit;

using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet.Tests;

public class AxisTotalTests
{
    [Fact]
    public void Total_ShouldSumDefaultSizes_WhenNoCustomOrHidden()
    {
        var axis = new Axis(10, 5);

        Assert.Equal(50, axis.Total);
    }

    [Fact]
    public void Total_ShouldIncludeCustomSizes()
    {
        var axis = new Axis(10, 5);
        axis[2] = 20;

        // 4 default (10) + 1 custom (20) = 60
        Assert.Equal(60, axis.Total);
    }

    [Fact]
    public void Total_ShouldExcludeHiddenDefaultItems()
    {
        var axis = new Axis(10, 5);
        axis.Hide(3);

        // 4 visible default (10) = 40
        Assert.Equal(40, axis.Total);
    }

    [Fact]
    public void Total_ShouldExcludeHiddenCustomItems()
    {
        var axis = new Axis(10, 5);
        axis[2] = 20;
        axis.Hide(2);

        // Item 2 is custom (20) and hidden → excluded
        // Remaining 4 items at default (10) = 40
        Assert.Equal(40, axis.Total);
    }

    [Fact]
    public void Total_ShouldHandleBothCustomAndHiddenItems()
    {
        var axis = new Axis(10, 5);
        axis[2] = 20; // custom and hidden
        axis.Hide(2);
        axis.Hide(3); // hidden but default size

        // Visible: items 0, 1, 4 at default (10) = 30
        Assert.Equal(30, axis.Total);
    }

    [Fact]
    public void Total_ShouldHandleMultipleCustomHiddenItems()
    {
        var axis = new Axis(10, 8);
        axis[1] = 15; // custom, visible
        axis[3] = 25; // custom, hidden
        axis.Hide(3);
        axis[5] = 30; // custom, hidden
        axis.Hide(5);
        axis.Hide(7); // default, hidden

        // Visible: 0(10) + 1(15) + 2(10) + 4(10) + 6(10) = 55
        Assert.Equal(55, axis.Total);
    }
}
