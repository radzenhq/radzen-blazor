using Xunit;
using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet.Tests;

public class CellDataTests
{
    [Fact]
    public void IsEqualTo_BothEmpty_ReturnsTrue()
    {
        var a = new CellData(null);
        var b = new CellData(null);
        Assert.True(a.IsEqualTo(b));
    }

    [Fact]
    public void IsEqualTo_OnlyOtherEmpty_ReturnsTrue_WhenValueIsEmptyString()
    {
        var a = new CellData("");
        var b = new CellData(null);
        Assert.True(a.IsEqualTo(b));
    }

    [Fact]
    public void IsEqualTo_SameNumbers_ReturnsTrue()
    {
        var a = new CellData(42.0);
        var b = new CellData(42.0);
        Assert.True(a.IsEqualTo(b));
    }

    [Fact]
    public void IsEqualTo_DifferentTypes_ReturnsFalse()
    {
        var a = new CellData(1.0);
        var b = new CellData("hello");
        Assert.False(a.IsEqualTo(b));
    }
}
