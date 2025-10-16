using Xunit;

namespace Radzen.Blazor.Spreadsheet.Tests;

public class ValueFunctionTests
{
    [Fact]
    public void Value_ParsesCurrency()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells["A1"].Formula = "=VALUE(\"$1,000\")";
        Assert.Equal(1000d, sheet.Cells["A1"].Data.Value);
    }

    [Fact]
    public void Value_TimeDifferenceFractionOfDay()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells["A1"].Formula = "=VALUE(\"16:48:00\")-VALUE(\"12:00:00\")";
        Assert.Equal(0.2d, sheet.Cells["A1"].Data.Value);
    }
}