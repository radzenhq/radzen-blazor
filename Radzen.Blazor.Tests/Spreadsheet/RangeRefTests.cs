using Xunit;

using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet.Tests;

public class RangeRefTests
{
    [Fact]
    public void Intersection_ShouldReturnInvalidForNonOverlappingRanges()
    {
        var a = new RangeRef(new CellRef(0, 0), new CellRef(2, 2)); // A1:C3
        var b = new RangeRef(new CellRef(5, 5), new CellRef(7, 7)); // F6:H8

        var result = a.Intersection(b);

        Assert.Equal(RangeRef.Invalid, result);
    }

    [Fact]
    public void Intersection_ShouldReturnInvalidWhenRowsDontOverlap()
    {
        var a = new RangeRef(new CellRef(0, 0), new CellRef(2, 5)); // A1:F3
        var b = new RangeRef(new CellRef(4, 0), new CellRef(6, 5)); // A5:F7

        var result = a.Intersection(b);

        Assert.Equal(RangeRef.Invalid, result);
    }

    [Fact]
    public void Intersection_ShouldReturnInvalidWhenColumnsDontOverlap()
    {
        var a = new RangeRef(new CellRef(0, 0), new CellRef(5, 2)); // A1:C6
        var b = new RangeRef(new CellRef(0, 4), new CellRef(5, 6)); // E1:G6

        var result = a.Intersection(b);

        Assert.Equal(RangeRef.Invalid, result);
    }

    [Fact]
    public void Intersection_ShouldReturnCorrectRangeForOverlappingRanges()
    {
        var a = new RangeRef(new CellRef(0, 0), new CellRef(3, 3)); // A1:D4
        var b = new RangeRef(new CellRef(2, 2), new CellRef(5, 5)); // C3:F6

        var result = a.Intersection(b);

        Assert.Equal(new RangeRef(new CellRef(2, 2), new CellRef(3, 3)), result);
    }
}
