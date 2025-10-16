using Xunit;

namespace Radzen.Blazor.Spreadsheet.Tests;

public class TextJoinFunctionTests
{
    [Fact]
    public void TextJoin_Literals_IgnoreEmptyTrue()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells["A1"].Formula = "=TEXTJOIN(\" \",TRUE,\"The\",\"sun\",\"will\",\"come\",\"up\",\"tomorrow.\")";
        Assert.Equal("The sun will come up tomorrow.", sheet.Cells["A1"].Data.Value);
    }

    [Fact]
    public void TextJoin_Range_CommaSpace_IgnoreEmptyTrue()
    {
        var sheet = new Sheet(20, 10);
        // A2:A8 values
        sheet.Cells["A2"].Value = "US Dollar";
        sheet.Cells["A3"].Value = "Australian Dollar";
        sheet.Cells["A4"].Value = "Chinese Yuan";
        sheet.Cells["A5"].Value = "Hong Kong Dollar";
        sheet.Cells["A6"].Value = "Israeli Shekel";
        sheet.Cells["A7"].Value = "South Korean Won";
        sheet.Cells["A8"].Value = "Russian Ruble";
        sheet.Cells["B1"].Formula = "=TEXTJOIN(\", \", TRUE, A2:A8)";
        var result = sheet.Cells["B1"].Data.GetValueOrDefault<string>();
        Assert.Equal("US Dollar, Australian Dollar, Chinese Yuan, Hong Kong Dollar, Israeli Shekel, South Korean Won, Russian Ruble", result);
    }

    [Fact]
    public void TextJoin_Range2D_CommaSpace_IgnoreEmptyVariants()
    {
        var sheet = new Sheet(20, 10);
        // A2:B8 grid
        sheet.Cells["A2"].Value = "a1";
        sheet.Cells["B2"].Value = "b1";
        sheet.Cells["A3"].Value = "a2";
        sheet.Cells["B3"].Value = "b2";
        sheet.Cells["A4"].Value = string.Empty; // empty cell value
        sheet.Cells["B4"].Value = string.Empty;
        sheet.Cells["A5"].Value = "a5";
        sheet.Cells["B5"].Value = "b5";
        sheet.Cells["A6"].Value = "a6";
        sheet.Cells["B6"].Value = "b6";
        sheet.Cells["A7"].Value = "a7";
        sheet.Cells["B7"].Value = "b7";
        sheet.Cells["B1"].Formula = "=TEXTJOIN(\", \", TRUE, A2:B7)";
        Assert.Equal("a1, b1, a2, b2, a5, b5, a6, b6, a7, b7", sheet.Cells["B1"].Data.Value);

        sheet.Cells["C1"].Formula = "=TEXTJOIN(\", \", FALSE, A2:B7)";
        Assert.Equal("a1, b1, a2, b2, , , a5, b5, a6, b6, a7, b7", sheet.Cells["C1"].Data.Value);
    }
}


