using Xunit;

using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet.Tests;

public class ExactCharCodeFunctionTests
{
    readonly Worksheet sheet = new(5, 5);

    [Fact]
    public void ExactShouldBeCaseSensitive()
    {
        sheet.Cells["A1"].Formula = "=EXACT(\"abc\",\"abc\")";
        Assert.Equal(true, sheet.Cells["A1"].Value);

        sheet.Cells["A2"].Formula = "=EXACT(\"abc\",\"ABC\")";
        Assert.Equal(false, sheet.Cells["A2"].Value);
    }

    [Fact]
    public void CharShouldReturnCharacter()
    {
        sheet.Cells["A1"].Formula = "=CHAR(65)";
        Assert.Equal("A", sheet.Cells["A1"].Value);
    }

    [Fact]
    public void CharShouldReturnValueOutOfRange()
    {
        sheet.Cells["A1"].Formula = "=CHAR(0)";
        Assert.Equal(CellError.Value, sheet.Cells["A1"].Value);
    }

    [Fact]
    public void CodeShouldReturnCharacterCode()
    {
        sheet.Cells["A1"].Formula = "=CODE(\"A\")";
        Assert.Equal(65d, sheet.Cells["A1"].Value);
    }
}
