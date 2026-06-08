using Xunit;

using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet.Tests;

public class CountBlankFunctionTests
{
    readonly Worksheet sheet = new(10, 10);

    [Fact]
    public void ShouldCountEmptyAndFormulaEmptyString()
    {
        sheet.Cells["A1"].Value = 1;
        // A2 left empty
        sheet.Cells["A3"].Formula = "=IF(1=1,\"\",\"x\")"; // formula-produced empty string

        sheet.Cells["B1"].Formula = "=COUNTBLANK(A1:A3)";
        Assert.Equal(2d, sheet.Cells["B1"].Value);
    }
}
