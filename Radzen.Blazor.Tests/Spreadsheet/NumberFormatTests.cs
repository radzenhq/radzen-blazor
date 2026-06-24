using System;
using Xunit;

using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet.Tests;

public class NumberFormatTests
{
    // Number formatting tests

    [Fact]
    public void Format_General_ReturnsNull()
    {
        Assert.Null(NumberFormat.Apply("General", 1234.56, CellDataType.Number));
    }

    [Fact]
    public void Format_Null_ReturnsNull()
    {
        Assert.Null(NumberFormat.Apply(null, 1234.56, CellDataType.Number));
    }

    [Fact]
    public void Format_Zero_Integer()
    {
        Assert.Equal("1235", NumberFormat.Apply("0", 1234.56, CellDataType.Number));
    }

    [Fact]
    public void Format_ZeroPointZeroZero()
    {
        Assert.Equal("1234.50", NumberFormat.Apply("0.00", 1234.5, CellDataType.Number));
    }

    [Fact]
    public void Format_ThousandsSeparator()
    {
        Assert.Equal("1,234,567", NumberFormat.Apply("#,##0", 1234567.0, CellDataType.Number));
    }

    [Fact]
    public void Format_ThousandsSeparatorWithDecimals()
    {
        Assert.Equal("1,234.50", NumberFormat.Apply("#,##0.00", 1234.5, CellDataType.Number));
    }

    [Fact]
    public void Format_Currency()
    {
        Assert.Equal("$1,234.50", NumberFormat.Apply("$#,##0.00", 1234.5, CellDataType.Number));
    }

    [Fact]
    public void Format_CurrencyNoDecimals()
    {
        Assert.Equal("$1,235", NumberFormat.Apply("$#,##0", 1234.5, CellDataType.Number));
    }

    [Fact]
    public void Format_Percentage()
    {
        Assert.Equal("12%", NumberFormat.Apply("0%", 0.1234, CellDataType.Number));
    }

    [Fact]
    public void Format_PercentageWithDecimals()
    {
        Assert.Equal("12.34%", NumberFormat.Apply("0.00%", 0.1234, CellDataType.Number));
    }

    [Fact]
    public void Format_NegativeNumber()
    {
        Assert.Equal("-1,234.50", NumberFormat.Apply("#,##0.00", -1234.5, CellDataType.Number));
    }

    [Fact]
    public void Format_Zero()
    {
        Assert.Equal("0.00", NumberFormat.Apply("#,##0.00", 0.0, CellDataType.Number));
    }

    [Fact]
    public void Format_HashHidesLeadingZeros()
    {
        Assert.Equal(".5", NumberFormat.Apply("#.##", 0.5, CellDataType.Number));
    }

    [Fact]
    public void Format_TextFormat()
    {
        Assert.Equal("hello", NumberFormat.Apply("@", "hello", CellDataType.String));
    }

    [Fact]
    public void Format_TextFormat_Number()
    {
        Assert.Equal("123", NumberFormat.Apply("@", 123, CellDataType.Number));
    }

    [Fact]
    public void Format_StringValue_Passthrough()
    {
        Assert.Equal("hello", NumberFormat.Apply("#,##0.00", "hello", CellDataType.String));
    }

    // Date formatting tests

    [Fact]
    public void Format_DateShort()
    {
        // 03/19/2026 as serial date
        var serial = new DateTime(2026, 3, 19).ToNumber();
        Assert.Equal("03/19/2026", NumberFormat.Apply("mm/dd/yyyy", serial, CellDataType.Number));
    }

    [Fact]
    public void Format_DateISO()
    {
        var serial = new DateTime(2026, 3, 19).ToNumber();
        Assert.Equal("2026-03-19", NumberFormat.Apply("yyyy-mm-dd", serial, CellDataType.Number));
    }

    [Fact]
    public void Format_DateMedium()
    {
        var serial = new DateTime(2026, 3, 19).ToNumber();
        Assert.Equal("19-Mar-26", NumberFormat.Apply("d-mmm-yy", serial, CellDataType.Number));
    }

    [Fact]
    public void Format_DateDayMonth()
    {
        var serial = new DateTime(2026, 3, 19).ToNumber();
        Assert.Equal("19-Mar", NumberFormat.Apply("d-mmm", serial, CellDataType.Number));
    }

    [Fact]
    public void Format_DateMonthYear()
    {
        var serial = new DateTime(2026, 3, 19).ToNumber();
        Assert.Equal("Mar-26", NumberFormat.Apply("mmm-yy", serial, CellDataType.Number));
    }

    [Fact]
    public void Format_DateFromDateTime()
    {
        var dt = new DateTime(2026, 3, 19);
        Assert.Equal("2026-03-19", NumberFormat.Apply("yyyy-mm-dd", dt, CellDataType.Date));
    }

    [Fact]
    public void Format_Time12h()
    {
        // Serial date with time: 0.65625 = 15:45 (15.75 hours / 24)
        var serial = new DateTime(2026, 3, 19).ToNumber() + (15.0 + 45.0 / 60.0) / 24.0;
        Assert.Equal("3:45 PM", NumberFormat.Apply("h:mm AM/PM", serial, CellDataType.Number));
    }

    [Fact]
    public void Format_Time24h()
    {
        var serial = new DateTime(2026, 3, 19).ToNumber() + (15.0 + 45.0 / 60.0 + 30.0 / 3600.0) / 24.0;
        Assert.Equal("15:45:30", NumberFormat.Apply("h:mm:ss", serial, CellDataType.Number));
    }

    // Section handling tests

    [Fact]
    public void Format_PositiveSection()
    {
        Assert.Equal("1,235", NumberFormat.Apply("#,##0;(#,##0)", 1234.5, CellDataType.Number));
    }

    [Fact]
    public void Format_NegativeSection()
    {
        Assert.Equal("(1,235)", NumberFormat.Apply("#,##0;(#,##0)", -1234.5, CellDataType.Number));
    }

    [Fact]
    public void Format_ZeroSection()
    {
        Assert.Equal("-", NumberFormat.Apply("#,##0;(#,##0);-", 0.0, CellDataType.Number));
    }

    // IsDateFormat tests

    [Fact]
    public void IsDateFormat_True_ForDateCodes()
    {
        Assert.True(NumberFormat.IsDateFormat("yyyy-mm-dd"));
        Assert.True(NumberFormat.IsDateFormat("mm/dd/yyyy"));
        Assert.True(NumberFormat.IsDateFormat("d-mmm-yy"));
        Assert.True(NumberFormat.IsDateFormat("h:mm:ss"));
        Assert.True(NumberFormat.IsDateFormat("h:mm AM/PM"));
    }

    [Fact]
    public void IsDateFormat_False_ForNumberCodes()
    {
        Assert.False(NumberFormat.IsDateFormat("#,##0.00"));
        Assert.False(NumberFormat.IsDateFormat("0%"));
        Assert.False(NumberFormat.IsDateFormat("$#,##0"));
        Assert.False(NumberFormat.IsDateFormat("0"));
        Assert.False(NumberFormat.IsDateFormat("@"));
    }

    [Fact]
    public void IsDateFormat_False_ForMinuteAfterH()
    {
        // h:mm has m as minute, but the overall format IS date/time
        Assert.True(NumberFormat.IsDateFormat("h:mm"));
    }

    // AdjustDecimals tests

    [Fact]
    public void AdjustDecimals_IncreasesFromZero()
    {
        Assert.Equal("0.0", NumberFormat.AdjustDecimals("0", 1));
    }

    [Fact]
    public void AdjustDecimals_IncreasesExisting()
    {
        Assert.Equal("0.000", NumberFormat.AdjustDecimals("0.00", 1));
    }

    [Fact]
    public void AdjustDecimals_DecreasesExisting()
    {
        Assert.Equal("0.0", NumberFormat.AdjustDecimals("0.00", -1));
    }

    [Fact]
    public void AdjustDecimals_RemovesDecimalPoint()
    {
        Assert.Equal("0", NumberFormat.AdjustDecimals("0.0", -1));
    }

    [Fact]
    public void AdjustDecimals_NoNegativeDecimals()
    {
        Assert.Equal("0", NumberFormat.AdjustDecimals("0", -1));
    }

    [Fact]
    public void AdjustDecimals_WithThousands()
    {
        Assert.Equal("#,##0.000", NumberFormat.AdjustDecimals("#,##0.00", 1));
    }

    [Fact]
    public void AdjustDecimals_WithPercentage()
    {
        Assert.Equal("0.00%", NumberFormat.AdjustDecimals("0.0%", 1));
    }

    [Fact]
    public void AdjustDecimals_NullFormat()
    {
        Assert.Equal("0.0", NumberFormat.AdjustDecimals(null, 1));
    }

    // Scientific format tests

    [Fact]
    public void Format_Scientific_Basic()
    {
        Assert.Equal("1.23E+04", NumberFormat.Apply("0.00E+00", 12345.0, CellDataType.Number));
    }

    [Fact]
    public void Format_Scientific_SmallNumber()
    {
        Assert.Equal("5.00E-04", NumberFormat.Apply("0.00E+00", 0.0005, CellDataType.Number));
    }

    [Fact]
    public void Format_Scientific_Negative()
    {
        Assert.Equal("-1.23E+04", NumberFormat.Apply("0.00E+00", -12345.0, CellDataType.Number));
    }

    [Fact]
    public void Format_Scientific_Zero()
    {
        Assert.Equal("0.00E+00", NumberFormat.Apply("0.00E+00", 0.0, CellDataType.Number));
    }

    [Fact]
    public void Format_Scientific_OneDecimal()
    {
        Assert.Equal("1.2E+04", NumberFormat.Apply("0.0E+00", 12345.0, CellDataType.Number));
    }

    [Fact]
    public void Format_Scientific_NoDecimals()
    {
        Assert.Equal("1E+04", NumberFormat.Apply("0E+00", 12345.0, CellDataType.Number));
    }

    [Fact]
    public void Format_Scientific_MinimalExponent()
    {
        Assert.Equal("1.23E+4", NumberFormat.Apply("0.00E+0", 12345.0, CellDataType.Number));
    }

    // AdjustDecimals scientific tests

    [Fact]
    public void AdjustDecimals_Scientific()
    {
        Assert.Equal("0.000E+00", NumberFormat.AdjustDecimals("0.00E+00", 1));
    }

    [Fact]
    public void AdjustDecimals_Scientific_Decrease()
    {
        Assert.Equal("0.0E+00", NumberFormat.AdjustDecimals("0.00E+00", -1));
    }

    // Underscore/Asterisk/QuestionMark tests

    [Fact]
    public void Format_Underscore_ProducesSpace()
    {
        Assert.Equal(" 5", NumberFormat.Apply("_)0", 5.0, CellDataType.Number));
    }

    [Fact]
    public void Format_Asterisk_Ignored()
    {
        Assert.Equal("5", NumberFormat.Apply("*-0", 5.0, CellDataType.Number));
    }

    [Fact]
    public void Format_QuestionMark_PadsWithSpaces()
    {
        Assert.Equal("1.5 ", NumberFormat.Apply("0.??", 1.5, CellDataType.Number));
    }

    // Accounting format tests

    [Fact]
    public void Format_Accounting_Positive()
    {
        Assert.Equal(" $1,234.50 ", NumberFormat.Apply("_($* #,##0.00_);_($* (#,##0.00);_($* \"-\"??_);_(@_)", 1234.5, CellDataType.Number));
    }

    [Fact]
    public void Format_Accounting_Negative()
    {
        Assert.Equal(" $(1,234.50)", NumberFormat.Apply("_($* #,##0.00_);_($* (#,##0.00);_($* \"-\"??_);_(@_)", -1234.5, CellDataType.Number));
    }

    [Fact]
    public void Format_Accounting_Zero()
    {
        Assert.Equal(" $- ", NumberFormat.Apply("_($* #,##0.00_);_($* (#,##0.00);_($* \"-\"??_);_(@_)", 0.0, CellDataType.Number));
    }
}
