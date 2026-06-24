using Xunit;

using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet.Tests;

public class IfNaFunctionTests
{
    readonly Worksheet sheet = new(10, 10);

    [Fact]
    public void ShouldReplaceNa()
    {
        sheet.Cells["A1"].Value = "a";
        sheet.Cells["B1"].Formula = "=IFNA(MATCH(\"z\",A1:A1,0),\"missing\")";
        Assert.Equal("missing", sheet.Cells["B1"].Value);
    }

    [Fact]
    public void ShouldPassThroughNonNaValue()
    {
        sheet.Cells["A1"].Formula = "=IFNA(5,\"missing\")";
        Assert.Equal(5d, sheet.Cells["A1"].Value);
    }

    [Fact]
    public void ShouldNotReplaceOtherErrors()
    {
        sheet.Cells["A1"].Formula = "=IFNA(1/0,\"missing\")";
        Assert.Equal(CellError.Div0, sheet.Cells["A1"].Value);
    }
}
