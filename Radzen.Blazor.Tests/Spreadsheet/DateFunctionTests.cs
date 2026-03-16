using System;
using Xunit;

namespace Radzen.Blazor.Spreadsheet.Tests;

public class DateFunctionTests
{
    readonly Sheet sheet = new(5, 5);

    [Fact]
    public void ShouldCreateDateFromYearMonthDay()
    {
        sheet.Cells["A1"].Formula = "=DATE(2024,3,15)";
        var date = sheet.Cells["A1"].Data.GetValueOrDefault<DateTime>();

        Assert.Equal(new DateTime(2024, 3, 15), date);
    }

    [Fact]
    public void ShouldHandleMonthOverflow()
    {
        // Month 13 = January of next year
        sheet.Cells["A1"].Formula = "=DATE(2024,13,1)";
        var date = sheet.Cells["A1"].Data.GetValueOrDefault<DateTime>();

        Assert.Equal(new DateTime(2025, 1, 1), date);
    }

    [Fact]
    public void ShouldHandleZeroMonth()
    {
        // Month 0 = December of previous year
        sheet.Cells["A1"].Formula = "=DATE(2024,0,1)";
        var date = sheet.Cells["A1"].Data.GetValueOrDefault<DateTime>();

        Assert.Equal(new DateTime(2023, 12, 1), date);
    }

    [Fact]
    public void ShouldHandleNegativeMonth()
    {
        // Month -1 = November of previous year
        sheet.Cells["A1"].Formula = "=DATE(2024,-1,1)";
        var date = sheet.Cells["A1"].Data.GetValueOrDefault<DateTime>();

        Assert.Equal(new DateTime(2023, 11, 1), date);
    }

    [Fact]
    public void ShouldHandleDayOverflow()
    {
        // Day 32 in January = Feb 1
        sheet.Cells["A1"].Formula = "=DATE(2024,1,32)";
        var date = sheet.Cells["A1"].Data.GetValueOrDefault<DateTime>();

        Assert.Equal(new DateTime(2024, 2, 1), date);
    }

    [Fact]
    public void ShouldHandleZeroDay()
    {
        // Day 0 = last day of previous month
        sheet.Cells["A1"].Formula = "=DATE(2024,1,0)";
        var date = sheet.Cells["A1"].Data.GetValueOrDefault<DateTime>();

        Assert.Equal(new DateTime(2023, 12, 31), date);
    }

    [Fact]
    public void ShouldHandleNegativeDay()
    {
        sheet.Cells["A1"].Formula = "=DATE(2024,1,-1)";
        var date = sheet.Cells["A1"].Data.GetValueOrDefault<DateTime>();

        Assert.Equal(new DateTime(2023, 12, 30), date);
    }

    [Fact]
    public void ShouldTreatYearsBetween0And1899AsOffsetFrom1900()
    {
        // Year 0 = 1900
        sheet.Cells["A1"].Formula = "=DATE(0,1,1)";
        var date1 = sheet.Cells["A1"].Data.GetValueOrDefault<DateTime>();
        Assert.Equal(new DateTime(1900, 1, 1), date1);

        // Year 24 = 1924
        sheet.Cells["A2"].Formula = "=DATE(24,1,1)";
        var date2 = sheet.Cells["A2"].Data.GetValueOrDefault<DateTime>();
        Assert.Equal(new DateTime(1924, 1, 1), date2);
    }
}

public class EdateFunctionTests
{
    readonly Sheet sheet = new(5, 5);

    [Fact]
    public void ShouldAddOneMonth()
    {
        sheet.Cells["A1"].Value = new DateTime(2024, 1, 15);
        sheet.Cells["A2"].Formula = "=EDATE(A1,1)";
        var date = sheet.Cells["A2"].Data.GetValueOrDefault<DateTime>();

        Assert.Equal(new DateTime(2024, 2, 15), date);
    }

    [Fact]
    public void ShouldSubtractOneMonth()
    {
        sheet.Cells["A1"].Value = new DateTime(2024, 1, 15);
        sheet.Cells["A2"].Formula = "=EDATE(A1,-1)";
        var date = sheet.Cells["A2"].Data.GetValueOrDefault<DateTime>();

        Assert.Equal(new DateTime(2023, 12, 15), date);
    }

    [Fact]
    public void ShouldClampToEndOfShorterMonth()
    {
        // Jan 31 + 1 month = Feb 29 (2024 is leap year)
        sheet.Cells["A1"].Value = new DateTime(2024, 1, 31);
        sheet.Cells["A2"].Formula = "=EDATE(A1,1)";
        var date = sheet.Cells["A2"].Data.GetValueOrDefault<DateTime>();

        Assert.Equal(new DateTime(2024, 2, 29), date);
    }

    [Fact]
    public void ShouldClampToEndOfApril()
    {
        // Mar 31 + 1 month = Apr 30
        sheet.Cells["A1"].Value = new DateTime(2024, 3, 31);
        sheet.Cells["A2"].Formula = "=EDATE(A1,1)";
        var date = sheet.Cells["A2"].Data.GetValueOrDefault<DateTime>();

        Assert.Equal(new DateTime(2024, 4, 30), date);
    }
}

public class EomonthFunctionTests
{
    readonly Sheet sheet = new(5, 5);

    [Fact]
    public void ShouldReturnEndOfSameMonth()
    {
        sheet.Cells["A1"].Value = new DateTime(2024, 1, 15);
        sheet.Cells["A2"].Formula = "=EOMONTH(A1,0)";
        var date = sheet.Cells["A2"].Data.GetValueOrDefault<DateTime>();

        Assert.Equal(new DateTime(2024, 1, 31), date);
    }

    [Fact]
    public void ShouldReturnEndOfNextMonth()
    {
        sheet.Cells["A1"].Value = new DateTime(2024, 1, 15);
        sheet.Cells["A2"].Formula = "=EOMONTH(A1,1)";
        var date = sheet.Cells["A2"].Data.GetValueOrDefault<DateTime>();

        Assert.Equal(new DateTime(2024, 2, 29), date);
    }

    [Fact]
    public void ShouldHandleLeapYear()
    {
        sheet.Cells["A1"].Value = new DateTime(2024, 2, 15);
        sheet.Cells["A2"].Formula = "=EOMONTH(A1,0)";
        var date = sheet.Cells["A2"].Data.GetValueOrDefault<DateTime>();

        Assert.Equal(new DateTime(2024, 2, 29), date);
    }

    [Fact]
    public void ShouldHandleNonLeapYear()
    {
        sheet.Cells["A1"].Value = new DateTime(2023, 2, 15);
        sheet.Cells["A2"].Formula = "=EOMONTH(A1,0)";
        var date = sheet.Cells["A2"].Data.GetValueOrDefault<DateTime>();

        Assert.Equal(new DateTime(2023, 2, 28), date);
    }
}

public class DatedifFunctionTests
{
    readonly Sheet sheet = new(5, 5);

    [Fact]
    public void ShouldReturnFullYears()
    {
        sheet.Cells["A1"].Value = new DateTime(2024, 1, 1);
        sheet.Cells["A2"].Value = new DateTime(2024, 12, 31);
        sheet.Cells["A3"].Formula = "=DATEDIF(A1,A2,\"Y\")";

        Assert.Equal(0d, sheet.Cells["A3"].Value);
    }

    [Fact]
    public void ShouldReturnFullMonths()
    {
        sheet.Cells["A1"].Value = new DateTime(2024, 1, 1);
        sheet.Cells["A2"].Value = new DateTime(2024, 12, 31);
        sheet.Cells["A3"].Formula = "=DATEDIF(A1,A2,\"M\")";

        Assert.Equal(11d, sheet.Cells["A3"].Value);
    }

    [Fact]
    public void ShouldReturnDays()
    {
        sheet.Cells["A1"].Value = new DateTime(2024, 1, 1);
        sheet.Cells["A2"].Value = new DateTime(2024, 12, 31);
        sheet.Cells["A3"].Formula = "=DATEDIF(A1,A2,\"D\")";

        Assert.Equal(365d, sheet.Cells["A3"].Value);
    }

    [Fact]
    public void ShouldReturnMonthsIgnoringYears()
    {
        sheet.Cells["A1"].Value = new DateTime(2024, 1, 1);
        sheet.Cells["A2"].Value = new DateTime(2025, 3, 15);
        sheet.Cells["A3"].Formula = "=DATEDIF(A1,A2,\"YM\")";

        Assert.Equal(2d, sheet.Cells["A3"].Value);
    }

    [Fact]
    public void ShouldReturnDaysIgnoringMonths()
    {
        sheet.Cells["A1"].Value = new DateTime(2024, 1, 1);
        sheet.Cells["A2"].Value = new DateTime(2025, 3, 15);
        sheet.Cells["A3"].Formula = "=DATEDIF(A1,A2,\"MD\")";

        Assert.Equal(14d, sheet.Cells["A3"].Value);
    }

    [Fact]
    public void ShouldReturnDaysIgnoringYears()
    {
        sheet.Cells["A1"].Value = new DateTime(2024, 1, 1);
        sheet.Cells["A2"].Value = new DateTime(2025, 3, 15);
        sheet.Cells["A3"].Formula = "=DATEDIF(A1,A2,\"YD\")";

        Assert.Equal(74d, sheet.Cells["A3"].Value);
    }

    [Fact]
    public void ShouldReturnErrorWhenStartDateIsAfterEndDate()
    {
        sheet.Cells["A1"].Value = new DateTime(2025, 1, 1);
        sheet.Cells["A2"].Value = new DateTime(2024, 1, 1);
        sheet.Cells["A3"].Formula = "=DATEDIF(A1,A2,\"D\")";

        Assert.Equal(CellError.Num, sheet.Cells["A3"].Value);
    }
}

public class TimeFunctionTests
{
    readonly Sheet sheet = new(5, 5);

    [Fact]
    public void ShouldReturnFractionOfDay()
    {
        sheet.Cells["A1"].Formula = "=TIME(12,0,0)";

        Assert.Equal(0.5, sheet.Cells["A1"].Data.GetValueOrDefault<double>());
    }

    [Fact]
    public void ShouldReturnZeroForMidnight()
    {
        sheet.Cells["A1"].Formula = "=TIME(0,0,0)";

        Assert.Equal(0d, sheet.Cells["A1"].Data.GetValueOrDefault<double>());
    }

    [Fact]
    public void ShouldHandleMinutesAndSeconds()
    {
        sheet.Cells["A1"].Formula = "=TIME(12,30,0)";
        var result = sheet.Cells["A1"].Data.GetValueOrDefault<double>();

        Assert.Equal(0.520833333333333, result, 10);
    }

    [Fact]
    public void ShouldWrapOverflowHours()
    {
        // 25 hours wraps to 1 hour
        sheet.Cells["A1"].Formula = "=TIME(25,0,0)";
        var result = sheet.Cells["A1"].Data.GetValueOrDefault<double>();

        Assert.Equal(1.0 / 24.0, result, 10);
    }
}
