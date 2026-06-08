using Xunit;

using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet.Tests;

public class IfsFunctionTests
{
    readonly Worksheet sheet = new(10, 10);

    [Fact]
    public void ShouldReturnFirstTrueResult()
    {
        sheet.Cells["A1"].Value = 7;
        sheet.Cells["B1"].Formula = "=IFS(A1>10,\"big\",A1>5,\"med\",TRUE,\"small\")";
        Assert.Equal("med", sheet.Cells["B1"].Value);
    }

    [Fact]
    public void ShouldReturnNAWhenNoneTrue()
    {
        sheet.Cells["A1"].Value = 1;
        sheet.Cells["B1"].Formula = "=IFS(A1>10,\"big\",A1>5,\"med\")";
        Assert.Equal(CellError.NA, sheet.Cells["B1"].Value);
    }

    [Fact]
    public void ShouldNotBePoisonedByLaterErrorBranch()
    {
        sheet.Cells["A1"].Value = 1;
        // First condition true; the later 1/0 value branch must not poison the result.
        sheet.Cells["B1"].Formula = "=IFS(A1=1,\"ok\",TRUE,1/0)";
        Assert.Equal("ok", sheet.Cells["B1"].Value);
    }
}
