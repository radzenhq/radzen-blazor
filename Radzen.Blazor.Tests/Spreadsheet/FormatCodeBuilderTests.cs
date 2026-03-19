using Xunit;

namespace Radzen.Blazor.Spreadsheet.Tests;

public class FormatCodeBuilderTests
{
    [Fact]
    public void BuildFormatCode_Number_WithDecimals()
    {
        Assert.Equal("#,##0.00", FormatCodeBuilder.Build(NumberFormatCategory.Number, decimalPlaces: 2, useThousandsSeparator: true));
    }

    [Fact]
    public void BuildFormatCode_Number_NoDecimals()
    {
        Assert.Equal("#,##0", FormatCodeBuilder.Build(NumberFormatCategory.Number, decimalPlaces: 0, useThousandsSeparator: true));
    }

    [Fact]
    public void BuildFormatCode_Number_NoThousands()
    {
        Assert.Equal("0.00", FormatCodeBuilder.Build(NumberFormatCategory.Number, decimalPlaces: 2, useThousandsSeparator: false));
    }

    [Fact]
    public void BuildFormatCode_Currency_Default()
    {
        Assert.Equal("$#,##0.00", FormatCodeBuilder.Build(NumberFormatCategory.Currency, decimalPlaces: 2, currencySymbol: "$"));
    }

    [Fact]
    public void BuildFormatCode_Currency_Euro()
    {
        Assert.Equal("\u20AC#,##0.00", FormatCodeBuilder.Build(NumberFormatCategory.Currency, decimalPlaces: 2, currencySymbol: "\u20AC"));
    }

    [Fact]
    public void BuildFormatCode_Accounting_Default()
    {
        var result = FormatCodeBuilder.Build(NumberFormatCategory.Accounting, decimalPlaces: 2, currencySymbol: "$");
        Assert.Contains("_($*", result);
        Assert.Contains("#,##0.00", result);
        Assert.Contains(";", result);
    }

    [Fact]
    public void BuildFormatCode_Percentage_OneDecimal()
    {
        Assert.Equal("0.0%", FormatCodeBuilder.Build(NumberFormatCategory.Percentage, decimalPlaces: 1));
    }

    [Fact]
    public void BuildFormatCode_Scientific_ThreeDecimals()
    {
        Assert.Equal("0.000E+00", FormatCodeBuilder.Build(NumberFormatCategory.Scientific, decimalPlaces: 3));
    }

    [Fact]
    public void BuildFormatCode_Date_ReturnsPreset()
    {
        Assert.Equal("yyyy-mm-dd", FormatCodeBuilder.Build(NumberFormatCategory.Date, selectedPreset: "yyyy-mm-dd"));
    }

    [Fact]
    public void BuildFormatCode_Text_ReturnsAt()
    {
        Assert.Equal("@", FormatCodeBuilder.Build(NumberFormatCategory.Text));
    }

    [Fact]
    public void BuildFormatCode_General()
    {
        Assert.Equal("General", FormatCodeBuilder.Build(NumberFormatCategory.General));
    }
}
