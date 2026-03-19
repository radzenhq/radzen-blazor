using Xunit;

namespace Radzen.Blazor.Spreadsheet.Tests;

public class NumberFormatColorTests
{
    [Fact]
    public void ApplyWithColor_Red_ReturnsRedColor()
    {
        var (text, color) = NumberFormat.ApplyWithColor("[Red]#,##0.00", 42.5, CellDataType.Number);
        Assert.NotNull(text);
        Assert.Equal("red", color);
    }

    [Fact]
    public void ApplyWithColor_Green_ReturnsGreenColor()
    {
        var (text, color) = NumberFormat.ApplyWithColor("[Green]0.00", 42.5, CellDataType.Number);
        Assert.NotNull(text);
        Assert.Equal("green", color);
    }

    [Fact]
    public void ApplyWithColor_Blue_ReturnsBlueColor()
    {
        var (text, color) = NumberFormat.ApplyWithColor("[Blue]0", 10, CellDataType.Number);
        Assert.NotNull(text);
        Assert.Equal("blue", color);
    }

    [Fact]
    public void ApplyWithColor_NoColor_ReturnsNull()
    {
        var (text, color) = NumberFormat.ApplyWithColor("#,##0.00", 42.5, CellDataType.Number);
        Assert.NotNull(text);
        Assert.Null(color);
    }

    [Fact]
    public void ApplyWithColor_General_ReturnsNulls()
    {
        var (text, color) = NumberFormat.ApplyWithColor("General", 42.5, CellDataType.Number);
        Assert.Null(text);
        Assert.Null(color);
    }
}
