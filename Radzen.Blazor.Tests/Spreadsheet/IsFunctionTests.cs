using Xunit;

using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet.Tests;

public class IsFunctionTests
{
    readonly Worksheet sheet = new(10, 10);

    [Fact]
    public void IsBlankShouldBeTrueForEmptyCell()
    {
        sheet.Cells["B1"].Formula = "=ISBLANK(A1)";
        Assert.Equal(true, sheet.Cells["B1"].Value);
    }

    [Fact]
    public void IsBlankShouldBeFalseForFormulaEmptyString()
    {
        sheet.Cells["A1"].Formula = "=IF(1=1,\"\",\"x\")";
        sheet.Cells["B1"].Formula = "=ISBLANK(A1)";
        Assert.Equal(false, sheet.Cells["B1"].Value);
    }

    [Fact]
    public void IsNumberShouldBeTrueForNumber()
    {
        sheet.Cells["A1"].Value = 5;
        sheet.Cells["B1"].Formula = "=ISNUMBER(A1)";
        Assert.Equal(true, sheet.Cells["B1"].Value);
    }

    [Fact]
    public void IsTextShouldBeTrueForText()
    {
        sheet.Cells["A1"].Value = "hi";
        sheet.Cells["B1"].Formula = "=ISTEXT(A1)";
        Assert.Equal(true, sheet.Cells["B1"].Value);
    }

    [Fact]
    public void IsErrorShouldBeTrueForError()
    {
        sheet.Cells["B1"].Formula = "=ISERROR(1/0)";
        Assert.Equal(true, sheet.Cells["B1"].Value);
    }

    [Fact]
    public void IsNaShouldDistinguishNaFromOtherErrors()
    {
        sheet.Cells["A1"].Value = "a";
        sheet.Cells["B1"].Formula = "=ISNA(MATCH(\"z\",A1:A1,0))";
        Assert.Equal(true, sheet.Cells["B1"].Value);

        sheet.Cells["B2"].Formula = "=ISNA(1/0)";
        Assert.Equal(false, sheet.Cells["B2"].Value);
    }
}
