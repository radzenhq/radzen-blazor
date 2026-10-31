using Radzen.Documents.Spreadsheet;
using Xunit;

namespace Radzen.Blazor.Spreadsheet.Tests;

public class UnparsedFormulaShiftTests
{
    [Theory]
    [InlineData("=2^3")]
    [InlineData("=\"a\"\"b\"")]
    [InlineData("=A1:B2 C1:D2")]
    [InlineData("=10%")]
    [InlineData("={1,2}")]
    [InlineData("=Table1[Col]")]
    [InlineData("=A1#")]
    [InlineData("=A1:A2:A3")]
    [InlineData("=SUM((A1,A2))")]
    public void InsertRow_KeepsFormulaTheParserCannotRepresent(string formula)
    {
        var sheet = new Worksheet(10, 10);
        sheet.Cells["E5"].Formula = formula;

        sheet.InsertRow(0);

        Assert.Equal(formula, sheet.Cells["E6"].Formula);
    }

    [Fact]
    public void InsertRow_KeepsValidationFormulaTheParserCannotParse()
    {
        var sheet = new Worksheet(10, 10);
        var rule = new DataValidationRule { Type = DataValidationType.Custom, Formula1 = "=2^3" };
        sheet.Validation.Add(RangeRef.Parse("A5:A6"), rule);

        sheet.InsertRow(0);

        Assert.Equal("=2^3", rule.Formula1);
    }
}
