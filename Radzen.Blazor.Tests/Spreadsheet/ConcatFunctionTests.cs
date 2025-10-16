using Xunit;

namespace Radzen.Blazor.Spreadsheet.Tests;

public class ConcatFunctionTests
{
    [Fact]
    public void Concat_Literals_Works()
    {
        var sheet = new Sheet(20, 10);
        sheet.Cells["A1"].Formula = "=CONCAT(\"The\",\" \",\"sun\",\" \",\"will\",\" \",\"come\",\" \",\"up\",\" \",\"tomorrow.\")";
        Assert.Equal("The sun will come up tomorrow.", sheet.Cells["A1"].Data.Value);
    }

    [Fact]
    public void Concat_SingleRange_LinearizesRowMajor()
    {
        var sheet = new Sheet(20, 10);
        sheet.Cells["B2"].Value = "a1";
        sheet.Cells["C2"].Value = "b1";
        sheet.Cells["B3"].Value = "a2";
        sheet.Cells["C3"].Value = "b2";
        sheet.Cells["B4"].Value = "a4";
        sheet.Cells["C4"].Value = "b4";
        sheet.Cells["B5"].Value = "a5";
        sheet.Cells["C5"].Value = "b5";
        sheet.Cells["B6"].Value = "a6";
        sheet.Cells["C6"].Value = "b6";
        sheet.Cells["B7"].Value = "a7";
        sheet.Cells["C7"].Value = "b7";
        sheet.Cells["A1"].Formula = "=CONCAT(B2:C7)";
        Assert.Equal("a1b1a2b2a4b4a5b5a6b6a7b7", sheet.Cells["A1"].Data.Value);
    }

    [Fact]
    public void Concat_MixedArgs_RangeAndLiterals()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells["B2"].Value = "Andreas";
        sheet.Cells["C2"].Value = "Hauser";
        sheet.Cells["A1"].Formula = "=CONCAT(B2,\" \",C2)";
        Assert.Equal("Andreas Hauser", sheet.Cells["A1"].Data.Value);
    }
}