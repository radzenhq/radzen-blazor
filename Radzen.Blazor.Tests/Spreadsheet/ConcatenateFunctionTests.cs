using Xunit;

using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet.Tests;

public class ConcatenateFunctionTests
{
    readonly Worksheet sheet = new(5, 5);

    [Fact]
    public void ShouldConcatenateStrings()
    {
        sheet.Cells["A1"].Formula = "=CONCATENATE(\"a\",\"b\",\"c\")";
        Assert.Equal("abc", sheet.Cells["A1"].Value);
    }

    [Fact]
    public void ShouldConcatenateCellValues()
    {
        sheet.Cells["A1"].Value = "Hello";
        sheet.Cells["A2"].Value = " World";
        sheet.Cells["B1"].Formula = "=CONCATENATE(A1,A2)";
        Assert.Equal("Hello World", sheet.Cells["B1"].Value);
    }
}
