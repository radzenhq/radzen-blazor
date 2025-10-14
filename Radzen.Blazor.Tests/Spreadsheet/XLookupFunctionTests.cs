using Xunit;

namespace Radzen.Blazor.Spreadsheet.Tests;

public class XLookupFunctionTests
{
    readonly Sheet sheet = new(20, 10);

    [Fact]
    public void ShouldFindExactMatchAndReturnFromAnotherColumn()
    {
        sheet.Cells["A1"].Value = "P1";
        sheet.Cells["A2"].Value = "P2";
        sheet.Cells["B1"].Value = 10;
        sheet.Cells["B2"].Value = 20;

        sheet.Cells["C1"].Formula = "=XLOOKUP(\"P2\",A1:A2,B1:B2)";

        Assert.Equal(20d, sheet.Cells["C1"].Value);
    }

    [Fact]
    public void ShouldReturnIfNotFoundValue()
    {
        sheet.Cells["A1"].Value = "P1";
        sheet.Cells["B1"].Value = 10;

        sheet.Cells["C1"].Formula = "=XLOOKUP(\"P2\",A1:A1,B1:B1,\"Missing\")";

        Assert.Equal("Missing", sheet.Cells["C1"].Value);
    }

    [Fact]
    public void ShouldSupportWildcardMatch()
    {
        sheet.Cells["A1"].Value = "Item-100";
        sheet.Cells["A2"].Value = "Item-200";
        sheet.Cells["B1"].Value = "A";
        sheet.Cells["B2"].Value = "B";

        sheet.Cells["C1"].Formula = "=XLOOKUP(\"Item-2*\",A1:A2,B1:B2,\"\",2)";

        Assert.Equal("B", sheet.Cells["C1"].Value);
    }

    [Fact]
    public void ShouldFindNextSmallerWhenNotFound()
    {
        sheet.Cells["A1"].Value = 10;
        sheet.Cells["A2"].Value = 20;
        sheet.Cells["A3"].Value = 30;
        sheet.Cells["B1"].Value = "L";
        sheet.Cells["B2"].Value = "M";
        sheet.Cells["B3"].Value = "H";

        sheet.Cells["C1"].Formula = "=XLOOKUP(25,A1:A3,B1:B3,\"\",0-1)";

        Assert.Equal("M", sheet.Cells["C1"].Value);
    }

    [Fact]
    public void ShouldFindNextLargerWhenNotFound()
    {
        sheet.Cells["A1"].Value = 10;
        sheet.Cells["A2"].Value = 20;
        sheet.Cells["A3"].Value = 30;
        sheet.Cells["B1"].Value = "L";
        sheet.Cells["B2"].Value = "M";
        sheet.Cells["B3"].Value = "H";

        sheet.Cells["C1"].Formula = "=XLOOKUP(25,A1:A3,B1:B3,\"\",1)";

        Assert.Equal("H", sheet.Cells["C1"].Value);
    }

    [Fact]
    public void ShouldSupportReverseSearch()
    {
        sheet.Cells["A1"].Value = "A";
        sheet.Cells["A2"].Value = "A";
        sheet.Cells["B1"].Value = 1;
        sheet.Cells["B2"].Value = 2;

        sheet.Cells["C1"].Formula = "=XLOOKUP(\"A\",A1:A2,B1:B2,\"\",0,0-1)";

        Assert.Equal(2d, sheet.Cells["C1"].Value);
    }

    [Fact]
    public void ShouldSupportBinarySearchAscending()
    {
        sheet.Cells["A1"].Value = 10;
        sheet.Cells["A2"].Value = 20;
        sheet.Cells["A3"].Value = 30;
        sheet.Cells["B1"].Value = "L";
        sheet.Cells["B2"].Value = "M";
        sheet.Cells["B3"].Value = "H";

        sheet.Cells["C1"].Formula = "=XLOOKUP(20,A1:A3,B1:B3,\"\",0,2)";

        Assert.Equal("M", sheet.Cells["C1"].Value);
    }

    [Fact]
    public void ShouldReturnNAWhenNotFoundAndNoIfNotFound()
    {
        sheet.Cells["A1"].Value = "A";
        sheet.Cells["B1"].Value = 1;

        sheet.Cells["C1"].Formula = "=XLOOKUP(\"B\",A1:A1,B1:B1)";

        Assert.Equal(CellError.NA, sheet.Cells["C1"].Value);
    }
}