using Xunit;

namespace Radzen.Blazor.Spreadsheet.Tests;

public class TextFunctionTests
{
    [Fact]
    public void Text_Currency_Thousands_TwoDecimals()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells["A1"].Value = 1234.567;
        sheet.Cells["B1"].Formula = "=TEXT(A1,\"$#,##0.00\")";
        Assert.Equal("$1,234.57", sheet.Cells["B1"].Data.Value);
    }

    [Fact]
    public void Text_Date_MMDDYY()
    {
        var sheet = new Sheet(10, 10);
        var dt = new System.DateTime(2012, 3, 14);
        sheet.Cells["A1"].Value = dt;
        sheet.Cells["B1"].Formula = "=TEXT(A1,\"MM/DD/YY\")";
        Assert.Equal("03/14/12", sheet.Cells["B1"].Data.Value);
    }

    [Fact]
    public void Text_Time_12h_AMPM()
    {
        var sheet = new Sheet(10, 10);
        var dt = new System.DateTime(2020, 1, 1, 13, 29, 0);
        sheet.Cells["A1"].Value = dt;
        sheet.Cells["B1"].Formula = "=TEXT(A1,\"H:MM AM/PM\")";
        Assert.Equal("1:29 PM", sheet.Cells["B1"].Data.Value);
    }

    [Fact]
    public void Text_Percent_OneDecimal()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells["A1"].Value = 0.285;
        sheet.Cells["B1"].Formula = "=TEXT(A1,\"0.0%\")";
        Assert.Equal("28.5%", sheet.Cells["B1"].Data.Value);
    }

    [Fact]
    public void Text_LeadingZeros()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells["A1"].Value = 1234;
        sheet.Cells["B1"].Formula = "=TEXT(A1,\"0000000\")";
        Assert.Equal("0001234", sheet.Cells["B1"].Data.Value);
    }
}