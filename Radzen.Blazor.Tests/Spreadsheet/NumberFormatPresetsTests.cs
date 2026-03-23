using Xunit;

using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet.Tests;

#nullable enable

public class NumberFormatPresetsTests
{
    [Theory]
    [InlineData(0, "General")]
    [InlineData(1, "0")]
    [InlineData(2, "0.00")]
    [InlineData(3, "#,##0")]
    [InlineData(4, "#,##0.00")]
    [InlineData(9, "0%")]
    [InlineData(10, "0.00%")]
    [InlineData(14, "mm/dd/yyyy")]
    [InlineData(22, "m/d/yy h:mm")]
    [InlineData(49, "@")]
    public void GetFormatCode_BuiltInIds(int id, string expectedCode)
    {
        Assert.Equal(expectedCode, NumberFormatPresets.GetFormatCode(id));
    }

    [Fact]
    public void GetFormatCode_Unknown_ReturnsNull()
    {
        Assert.Null(NumberFormatPresets.GetFormatCode(999));
    }

    [Theory]
    [InlineData("General", 0)]
    [InlineData("0", 1)]
    [InlineData("0.00", 2)]
    [InlineData("#,##0", 3)]
    [InlineData("#,##0.00", 4)]
    [InlineData("0%", 9)]
    [InlineData("0.00%", 10)]
    [InlineData("mm/dd/yyyy", 14)]
    [InlineData("@", 49)]
    public void GetNumFmtId_KnownFormats(string code, int expectedId)
    {
        Assert.Equal(expectedId, NumberFormatPresets.GetNumFmtId(code));
    }

    [Fact]
    public void GetNumFmtId_Unknown_ReturnsMinusOne()
    {
        Assert.Equal(-1, NumberFormatPresets.GetNumFmtId("xyz"));
    }

    [Theory]
    [InlineData("0", (int)NumberFormatCategory.Number)]
    [InlineData("0.00", (int)NumberFormatCategory.Number)]
    [InlineData("#,##0", (int)NumberFormatCategory.Number)]
    [InlineData("#,##0.00", (int)NumberFormatCategory.Number)]
    public void GetCategory_NumberFormats(string code, int expected)
    {
        Assert.Equal((NumberFormatCategory)expected, NumberFormatPresets.GetCategory(code));
    }

    [Theory]
    [InlineData("mm/dd/yyyy", (int)NumberFormatCategory.Date)]
    [InlineData("yyyy-mm-dd", (int)NumberFormatCategory.Date)]
    [InlineData("d-mmm-yy", (int)NumberFormatCategory.Date)]
    public void GetCategory_DateFormats(string code, int expected)
    {
        Assert.Equal((NumberFormatCategory)expected, NumberFormatPresets.GetCategory(code));
    }

    [Theory]
    [InlineData("0%", (int)NumberFormatCategory.Percentage)]
    [InlineData("0.00%", (int)NumberFormatCategory.Percentage)]
    public void GetCategory_PercentageFormats(string code, int expected)
    {
        Assert.Equal((NumberFormatCategory)expected, NumberFormatPresets.GetCategory(code));
    }

    [Theory]
    [InlineData(null, (int)NumberFormatCategory.General)]
    [InlineData("", (int)NumberFormatCategory.General)]
    [InlineData("General", (int)NumberFormatCategory.General)]
    public void GetCategory_NullOrGeneral_ReturnsGeneral(string? code, int expected)
    {
        Assert.Equal((NumberFormatCategory)expected, NumberFormatPresets.GetCategory(code));
    }

    [Fact]
    public void GetCategory_Currency()
    {
        Assert.Equal(NumberFormatCategory.Currency, NumberFormatPresets.GetCategory("$#,##0.00"));
    }

    [Fact]
    public void GetCategory_Time()
    {
        Assert.Equal(NumberFormatCategory.Time, NumberFormatPresets.GetCategory("h:mm AM/PM"));
        Assert.Equal(NumberFormatCategory.Time, NumberFormatPresets.GetCategory("h:mm:ss"));
    }

    [Fact]
    public void GetCategory_Text()
    {
        Assert.Equal(NumberFormatCategory.Text, NumberFormatPresets.GetCategory("@"));
    }

    [Fact]
    public void GetCategory_Scientific()
    {
        Assert.Equal(NumberFormatCategory.Scientific, NumberFormatPresets.GetCategory("0.00E+00"));
    }

    [Fact]
    public void GetCategory_Accounting()
    {
        Assert.Equal(NumberFormatCategory.Accounting, NumberFormatPresets.GetCategory("_($* #,##0.00_);_($* (#,##0.00);_($* \"-\"??_);_(@_)"));
    }

    [Fact]
    public void GetPresets_Scientific_ReturnsFormats()
    {
        Assert.NotEmpty(NumberFormatPresets.GetPresets(NumberFormatCategory.Scientific));
    }

    [Fact]
    public void GetPresets_Accounting_ReturnsFormats()
    {
        Assert.NotEmpty(NumberFormatPresets.GetPresets(NumberFormatCategory.Accounting));
    }
}
