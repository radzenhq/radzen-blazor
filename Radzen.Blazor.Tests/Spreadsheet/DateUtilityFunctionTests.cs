using Xunit;

using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet.Tests;

public class DateUtilityFunctionTests
{
    readonly Worksheet sheet = new(10, 10);

    [Fact]
    public void DaysShouldReturnDayDifference()
    {
        sheet.Cells["A1"].Formula = "=DAYS(DATE(2020,1,10),DATE(2020,1,1))";
        Assert.Equal(9d, sheet.Cells["A1"].Value);
    }

    [Fact]
    public void NetworkDaysShouldExcludeWeekends()
    {
        // 2020-01-01 is a Wednesday; Jan 1-7 has 5 weekdays.
        sheet.Cells["A1"].Formula = "=NETWORKDAYS(DATE(2020,1,1),DATE(2020,1,7))";
        Assert.Equal(5d, sheet.Cells["A1"].Value);
    }

    [Fact]
    public void WorkdayShouldSkipWeekends()
    {
        // Wed 2020-01-01 + 5 workdays = Wed 2020-01-08.
        sheet.Cells["A1"].Formula = "=DAY(WORKDAY(DATE(2020,1,1),5))";
        Assert.Equal(8d, sheet.Cells["A1"].Value);
    }

    [Fact]
    public void DateValueShouldParseText()
    {
        sheet.Cells["A1"].Formula = "=DAY(DATEVALUE(\"2020-01-15\"))";
        Assert.Equal(15d, sheet.Cells["A1"].Value);
    }

    [Fact]
    public void DateValueShouldReturnValueForUnparseable()
    {
        sheet.Cells["A1"].Formula = "=DATEVALUE(\"not a date\")";
        Assert.Equal(CellError.Value, sheet.Cells["A1"].Value);
    }
}
