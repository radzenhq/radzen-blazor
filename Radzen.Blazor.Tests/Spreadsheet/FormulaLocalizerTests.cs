using System.Globalization;
using Xunit;

using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet.Tests;

public class FormulaLocalizerTests
{
    private static readonly CultureInfo German = CultureInfo.GetCultureInfo("de-DE");
    private static readonly CultureInfo Brazilian = CultureInfo.GetCultureInfo("pt-BR");
    private static readonly CultureInfo English = CultureInfo.GetCultureInfo("en-US");

    [Theory]
    [InlineData("=SUM(A1,B1)", "=SUM(A1;B1)")]
    [InlineData("=A1*1.5", "=A1*1,5")]
    [InlineData("=.5", "=,5")]
    [InlineData("=A1-.5", "=A1-,5")]
    [InlineData("=5.5%", "=5,5%")]
    [InlineData("=1.5E+3", "=1,5E+3")]
    [InlineData("=IF(D6>1000,-100.5,0)", "=IF(D6>1000;-100,5;0)")]
    [InlineData("=INDEX(I42:I46, MATCH(104, H42:H46, 0))", "=INDEX(I42:I46; MATCH(104; H42:H46; 0))")]
    [InlineData("=SUM(A1,.5)", "=SUM(A1;,5)")]
    public void ToLocalized_ConvertsSeparatorsForGerman(string invariant, string localized)
    {
        Assert.Equal(localized, FormulaLocalizer.ToLocalized(invariant, German));
    }

    [Theory]
    [InlineData("=SUM(A1;B1)", "=SUM(A1,B1)")]
    [InlineData("=A1*1,5", "=A1*1.5")]
    [InlineData("=,5", "=.5")]
    [InlineData("=A1-,5", "=A1-.5")]
    [InlineData("=5,5%", "=5.5%")]
    [InlineData("=1,5E+3", "=1.5E+3")]
    [InlineData("=SUM(A1;,5)", "=SUM(A1,.5)")]
    public void ToInvariant_ConvertsSeparatorsForGerman(string localized, string invariant)
    {
        Assert.Equal(invariant, FormulaLocalizer.ToInvariant(localized, German));
    }

    [Theory]
    [InlineData("=SUM(A1,A2)", "=SUM(A1,A2)")]
    [InlineData("=SUM(A1, 5)", "=SUM(A1, 5)")]
    [InlineData("=SUM(A1:B2,5)", "=SUM(A1:B2,5)")]
    public void ToInvariant_LenientCommaIsSeparatorWhereItCannotBeDecimal(string localized, string invariant)
    {
        Assert.Equal(invariant, FormulaLocalizer.ToInvariant(localized, German));
    }

    [Fact]
    public void ToInvariant_DigitAdjacentCommaBindsAsDecimal()
    {
        Assert.Equal("=SUM(1.5)", FormulaLocalizer.ToInvariant("=SUM(1,5)", German));
    }

    [Theory]
    [InlineData("=IF(A1>1,\"a,b\",\"c;d\")", "=IF(A1>1;\"a,b\";\"c;d\")")]
    [InlineData("='My,Sheet'!B2+1.5", "='My,Sheet'!B2+1,5")]
    public void Transform_LeavesStringAndSheetLiteralsUntouched(string invariant, string localized)
    {
        Assert.Equal(localized, FormulaLocalizer.ToLocalized(invariant, German));
        Assert.Equal(invariant, FormulaLocalizer.ToInvariant(localized, German));
    }

    [Fact]
    public void Transform_LeavesBracketedSegmentsUntouched()
    {
        var formula = "=SUM(Table[[Col1],[Col2]])";
        Assert.Equal(formula, FormulaLocalizer.ToLocalized(formula, German));
        Assert.Equal(formula, FormulaLocalizer.ToInvariant(formula, German));
    }

    [Fact]
    public void Transform_LeavesArrayLiteralsUntouched()
    {
        var formula = "={1,2,3}";
        Assert.Same(formula, FormulaLocalizer.ToLocalized(formula, German));
        Assert.Same(formula, FormulaLocalizer.ToInvariant(formula, German));
    }

    [Fact]
    public void ToLocalized_ReturnsSameReferenceForDotDecimalCultures()
    {
        var formula = "=SUM(A1,B1)";
        Assert.Same(formula, FormulaLocalizer.ToLocalized(formula, English));
        Assert.Same(formula, FormulaLocalizer.ToLocalized(formula, CultureInfo.InvariantCulture));
    }

    [Theory]
    [InlineData("=SUM(A1,B1)")]
    [InlineData("=A1*1.5")]
    [InlineData("=IF(D6>1000,-100,0)")]
    [InlineData("=VLOOKUP(103, H42:J46, 2, FALSE)")]
    [InlineData("=ROUND(AVERAGE(E2:E9), 1)")]
    [InlineData("=AGGREGATE(14,6,A1:A11,3)")]
    [InlineData("=CONCAT(\"a\",\"b\",1.5)")]
    [InlineData("=AVERAGEIF(A1:A2,\">1\")")]
    [InlineData("=TEXT(A1,\"#,##0.00\")")]
    [InlineData("=SUM(1, 2, 3) + IF(2>1, 10, 20)")]
    public void RoundTrip_IsIdentityOverFormulaCorpus(string invariant)
    {
        Assert.Equal(invariant, FormulaLocalizer.ToInvariant(FormulaLocalizer.ToLocalized(invariant, German), German));
        Assert.Equal(invariant, FormulaLocalizer.ToInvariant(FormulaLocalizer.ToLocalized(invariant, Brazilian), Brazilian));
    }

    [Theory]
    [InlineData("=SUM(1,5,2,5)")]
    [InlineData("=SUM(1,5E3,10)")]
    public void MalformedLocalizedInput_ProducesErrorTreeNotThrow(string localized)
    {
        var invariant = FormulaLocalizer.ToInvariant(localized, German);
        var tree = FormulaParser.Parse(invariant);
        Assert.NotEmpty(tree.Errors);
        Assert.NotNull(tree.Root);
    }

    [Fact]
    public void MalformedInvariantFormula_ProducesErrorTreeNotThrow()
    {
        var tree = FormulaParser.Parse("=1.5.2");
        Assert.NotEmpty(tree.Errors);
        Assert.NotNull(tree.Root);
    }
}
