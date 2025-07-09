using Xunit;

namespace Radzen.Blazor.Spreadsheet.Tests;

public class SortingTests
{
    private readonly Sheet sheet = new(5, 5);

    [Fact]
    public void Should_SortStringsInAscendingOrder()
    {
        sheet.Cells[0, 0].Value = "C";
        sheet.Cells[1, 0].Value = "A";
        sheet.Cells[2, 0].Value = "B";

        sheet.Sort(RangeRef.Parse("A1:A3"), SortOrder.Ascending);

        Assert.Equal("A", sheet.Cells[0, 0].Value);
        Assert.Equal("B", sheet.Cells[1, 0].Value);
        Assert.Equal("C", sheet.Cells[2, 0].Value);
    }

    [Fact]
    public void Should_SortStringsInDescendingOrder()
    {
        sheet.Cells[0, 0].Value = "C";
        sheet.Cells[1, 0].Value = "A";
        sheet.Cells[2, 0].Value = "B";

        sheet.Sort(RangeRef.Parse("A1:A3"), SortOrder.Descending);

        Assert.Equal("C", sheet.Cells[0, 0].Value);
        Assert.Equal("B", sheet.Cells[1, 0].Value);
        Assert.Equal("A", sheet.Cells[2, 0].Value);
    }

    [Fact]
    public void Should_SortNumbersInDescendingOrder()
    {
        sheet.Cells[0, 0].Value = 1;
        sheet.Cells[1, 0].Value = 3;
        sheet.Cells[2, 0].Value = 2;

        sheet.Sort(RangeRef.Parse("A1:A3"), SortOrder.Descending);

        Assert.Equal(3d, sheet.Cells[0, 0].Value);
        Assert.Equal(2d, sheet.Cells[1, 0].Value);
        Assert.Equal(1d, sheet.Cells[2, 0].Value);
    }

    [Fact]
    public void Should_SortFullRowsByColumnA()
    {
        sheet.Cells[0, 0].Value = "B"; sheet.Cells[0, 1].Value = "B2";
        sheet.Cells[1, 0].Value = "A"; sheet.Cells[1, 1].Value = "A2";
        sheet.Cells[2, 0].Value = "C"; sheet.Cells[2, 1].Value = "C2";

        sheet.Sort(RangeRef.Parse("A1:B3"), SortOrder.Ascending);

        Assert.Equal("A", sheet.Cells[0, 0].Value);
        Assert.Equal("A2", sheet.Cells[0, 1].Value);
        Assert.Equal("B", sheet.Cells[1, 0].Value);
        Assert.Equal("B2", sheet.Cells[1, 1].Value);
        Assert.Equal("C", sheet.Cells[2, 0].Value);
        Assert.Equal("C2", sheet.Cells[2, 1].Value);
    }

    [Fact]
    public void Should_IgnoreHeaderRowInSort()
    {
        sheet.Cells[0, 0].Value = "Header";
        sheet.Cells[1, 0].Value = "C";
        sheet.Cells[2, 0].Value = "A";
        sheet.Cells[3, 0].Value = "B";

        sheet.Sort(RangeRef.Parse("A2:A4"), SortOrder.Ascending);

        Assert.Equal("Header", sheet.Cells[0, 0].Value); // header unchanged
        Assert.Equal("A", sheet.Cells[1, 0].Value);
        Assert.Equal("B", sheet.Cells[2, 0].Value);
        Assert.Equal("C", sheet.Cells[3, 0].Value);
    }

    [Fact]
    public void Should_SortOnlySelectedRange_NotWholeSheet()
    {
        sheet.Cells[0, 0].Value = "Z";
        sheet.Cells[1, 0].Value = "X";
        sheet.Cells[2, 0].Value = "Y";
        sheet.Cells[3, 0].Value = "A";

        sheet.Sort(RangeRef.Parse("A2:A4"), SortOrder.Ascending);

        Assert.Equal("Z", sheet.Cells[0, 0].Value); // unchanged
        Assert.Equal("A", sheet.Cells[1, 0].Value);
        Assert.Equal("X", sheet.Cells[2, 0].Value);
        Assert.Equal("Y", sheet.Cells[3, 0].Value);
    }

    [Fact]
    public void Should_SortBlanksLastInAscendingOrder()
    {
        sheet.Cells[0, 0].Value = "B";
        sheet.Cells[1, 0].Value = null; // blank
        sheet.Cells[2, 0].Value = "A";

        sheet.Sort(RangeRef.Parse("A1:A3"), SortOrder.Ascending);

        Assert.Equal("A", sheet.Cells[0, 0].Value);
        Assert.Equal("B", sheet.Cells[1, 0].Value);
        Assert.Null(sheet.Cells[2, 0].Value);
    }

    [Fact]
    public void Should_NotBreakDataAlignmentAcrossColumns()
    {
        sheet.Cells[0, 0].Value = "Charlie"; sheet.Cells[0, 1].Value = "30";
        sheet.Cells[1, 0].Value = "Alice"; sheet.Cells[1, 1].Value = "25";
        sheet.Cells[2, 0].Value = "Bob"; sheet.Cells[2, 1].Value = "20";

        sheet.Sort(RangeRef.Parse("A1:B3"), SortOrder.Ascending);

        Assert.Equal("Alice", sheet.Cells[0, 0].Value);
        Assert.Equal(25d, sheet.Cells[0, 1].Value);
        Assert.Equal("Bob", sheet.Cells[1, 0].Value);
        Assert.Equal(20d, sheet.Cells[1, 1].Value);
        Assert.Equal("Charlie", sheet.Cells[2, 0].Value);
        Assert.Equal(30d, sheet.Cells[2, 1].Value);
    }


    [Fact]
    public void Should_SortByAbsoluteColumnIndex()
    {
        // Data layout now starts at column B (index 1):
        // | B (Name) | C (Age) | D (Dept) |
        // |----------|---------|----------|
        // | Charlie  | 42      | Dev      |
        // | Alice    | 25      | HR       |
        // | Bob      | 30      | Sales    |

        sheet.Cells[0, 1].Value = "Charlie"; // B1
        sheet.Cells[0, 2].Value = 42;        // C1
        sheet.Cells[0, 3].Value = "Dev";     // D1

        sheet.Cells[1, 1].Value = "Alice";   // B2
        sheet.Cells[1, 2].Value = 25;        // C2
        sheet.Cells[1, 3].Value = "HR";      // D2

        sheet.Cells[2, 1].Value = "Bob";     // B3
        sheet.Cells[2, 2].Value = 30;        // C3
        sheet.Cells[2, 3].Value = "Sales";   // D3

        // Sort range B1:D3 by **column C** (absolute index 2, "Age")
        sheet.Sort(RangeRef.Parse("B1:D3"), SortOrder.Ascending, keyIndex: 2);

        // Sorted order: Alice (25), Bob (30), Charlie (42)

        Assert.Equal("Alice", sheet.Cells[0, 1].Value);
        Assert.Equal(25d, sheet.Cells[0, 2].Value);
        Assert.Equal("HR", sheet.Cells[0, 3].Value);

        Assert.Equal("Bob", sheet.Cells[1, 1].Value);
        Assert.Equal(30d, sheet.Cells[1, 2].Value);
        Assert.Equal("Sales", sheet.Cells[1, 3].Value);

        Assert.Equal("Charlie", sheet.Cells[2, 1].Value);
        Assert.Equal(42d, sheet.Cells[2, 2].Value);
        Assert.Equal("Dev", sheet.Cells[2, 3].Value);
    }

    [Fact]
    public void Should_SortCellsWithRelativeFormulasCorrectly()
    {
        // Initial setup:
        // A1:  2
        // A2:  =A1 + 1   (should be 3)
        // A3:  1

        sheet.Cells[0, 0].Value = 2;           // A1
        sheet.Cells[1, 0].Formula = "=A1 + 1"; // A2
        sheet.Cells[2, 0].Value = 1;           // A3

        // Confirm initial formula result
        Assert.Equal(3d, sheet.Cells[1, 0].Value);

        // Now sort A1:A3 in ascending order
        sheet.Sort(RangeRef.Parse("A1:A3"), SortOrder.Ascending);

        // The sorted order should be:
        // A1:  1         (was A3)
        // A2:  2         (was A1)
        // A3:  =A1 + 1   (was A2)

        Assert.Equal(1d, sheet.Cells[0, 0].Value);               // A1
        Assert.Equal(2d, sheet.Cells[1, 0].Value);               // A2
        Assert.Equal("=A1 + 1", sheet.Cells[2, 0].Formula);     // A3
        Assert.Equal(2d, sheet.Cells[2, 0].Value);               // A3 evaluated: =1 + 1
    }

    [Fact]
    public void Should_PlaceBlankValuesAtBottomInAscendingAndDescending()
    {
        sheet.Cells[0, 0].Value = 3;
        sheet.Cells[1, 0].Value = null; // blank
        sheet.Cells[2, 0].Value = 1;
        sheet.Cells[3, 0].Value = 2;

        // Sort ascending
        sheet.Sort(RangeRef.Parse("A1:A4"), SortOrder.Ascending);

        Assert.Equal(1d, sheet.Cells[0, 0].Value);
        Assert.Equal(2d, sheet.Cells[1, 0].Value);
        Assert.Equal(3d, sheet.Cells[2, 0].Value);
        Assert.Null(sheet.Cells[3, 0].Value);

        // Re-set original order
        sheet.Cells[0, 0].Value = 3;
        sheet.Cells[1, 0].Value = null;
        sheet.Cells[2, 0].Value = 1;
        sheet.Cells[3, 0].Value = 2;

        // Sort descending
        sheet.Sort(RangeRef.Parse("A1:A4"), SortOrder.Descending);

        Assert.Equal(3d, sheet.Cells[0, 0].Value);
        Assert.Equal(2d, sheet.Cells[1, 0].Value);
        Assert.Equal(1d, sheet.Cells[2, 0].Value);
        Assert.Null(sheet.Cells[3, 0].Value);
    }
}