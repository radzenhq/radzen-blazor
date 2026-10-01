using System.IO;
using Xunit;

using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet.Tests;

public class SheetRenameTests
{
    private static (Workbook Workbook, Worksheet Data, Worksheet Report) CreateWorkbook()
    {
        var workbook = new Workbook();
        var data = workbook.AddSheet("Data", 10, 5);
        var report = workbook.AddSheet("Report", 10, 5);
        data.Cells["A1"].Value = 1;
        data.Cells["A2"].Value = 2;
        return (workbook, data, report);
    }

    [Theory]
    [InlineData("=Data!A1*10", "=Other!A1*10", 10d)]
    [InlineData("=SUM(Data!A1:A2)", "=SUM(Other!A1:A2)", 3d)]
    [InlineData("=SUM(Data!A:A)", "=SUM(Other!A:A)", 3d)]
    [InlineData("=SUM(Data!1:2)", "=SUM(Other!1:2)", 3d)]
    [InlineData("=SUM(data!$A$1:$A$2)", "=SUM(Other!$A$1:$A$2)", 3d)]
    public void RenamingReferencedSheet_RewritesReferences(string formula, string expected, double value)
    {
        var (_, data, report) = CreateWorkbook();
        report.Cells["A1"].Formula = formula;

        data.Name = "Other";

        Assert.Equal(expected, report.Cells["A1"].Formula);
        Assert.Equal(value, report.Cells["A1"].Value);
    }

    [Fact]
    public void RenamedSheet_KeepsRecalculatingDependentFormulas()
    {
        var (_, data, report) = CreateWorkbook();
        report.Cells["A1"].Formula = "=Data!A1";

        data.Name = "Other";
        data.Cells["A1"].Value = 7;

        Assert.Equal(7d, report.Cells["A1"].Value);
    }

    [Fact]
    public void RenamingSheet_RewritesReferencesToItselfOnTheSameSheet()
    {
        var (_, data, _) = CreateWorkbook();
        data.Cells["B1"].Formula = "=Data!A1+A2";

        data.Name = "Other";

        Assert.Equal("=Other!A1+A2", data.Cells["B1"].Formula);
        Assert.Equal(3d, data.Cells["B1"].Value);
    }

    [Theory]
    [InlineData("My Data", "='My Data'!A1")]
    [InlineData("Q1-Sales", "='Q1-Sales'!A1")]
    [InlineData("Bob's", "='Bob''s'!A1")]
    [InlineData("2024", "='2024'!A1")]
    [InlineData("A1", "='A1'!A1")]
    [InlineData("R1C1", "='R1C1'!A1")]
    [InlineData("Sheet.1", "='Sheet.1'!A1")]
    [InlineData("Sheet2", "=Sheet2!A1")]
    [InlineData("Daten_2024", "=Daten_2024!A1")]
    public void RenamingToNameThatNeedsQuotes_WritesParsableFormula(string newName, string expected)
    {
        var (_, data, report) = CreateWorkbook();
        report.Cells["A1"].Formula = "=Data!A1";

        data.Name = newName;
        data.Cells["A1"].Value = 5;

        Assert.Equal(expected, report.Cells["A1"].Formula);
        Assert.Equal(5d, report.Cells["A1"].Value);
    }

    [Fact]
    public void RenamingQuotedSheetBackToSimpleName_DropsQuotes()
    {
        var (_, data, report) = CreateWorkbook();
        data.Name = "Bob's";
        report.Cells["A1"].Formula = "='Bob''s'!A1";

        data.Name = "Data";

        Assert.Equal("=Data!A1", report.Cells["A1"].Formula);
        Assert.Equal(1d, report.Cells["A1"].Value);
    }

    [Fact]
    public void FormulasNotReferencingRenamedSheet_KeepTheirText()
    {
        var (_, data, report) = CreateWorkbook();
        report.Cells["A1"].Formula = "=sum( A1 , 2 )";
        report.Cells["A2"].Formula = "=Report!A1";

        data.Name = "Other";

        Assert.Equal("=sum( A1 , 2 )", report.Cells["A1"].Formula);
        Assert.Equal("=Report!A1", report.Cells["A2"].Formula);
    }

    [Fact]
    public void RenamingSheet_RewritesDefinedNames()
    {
        var (workbook, data, report) = CreateWorkbook();
        workbook.DefinedNames["Amounts"] = "Data!$A$1:$A$2";
        report.Cells["A1"].Formula = "=SUM(Amounts)";

        data.Name = "Other";
        data.Cells["A2"].Value = 10;

        Assert.Equal("Other!$A$1:$A$2", workbook.DefinedNames["Amounts"]);
        Assert.Equal(11d, report.Cells["A1"].Value);
    }

    [Fact]
    public void RenamingSheet_RewritesChartSeriesRanges()
    {
        var (_, data, report) = CreateWorkbook();
        var chart = new SheetChart();
        chart.Series.Add(new ChartSeries { Categories = "Data!$A$1:$A$2", Values = "'Data'!$B$1:$B$2" });
        report.AddChart(chart);

        data.Name = "My Data";

        Assert.Equal("'My Data'!$A$1:$A$2", chart.Series[0].Categories);
        Assert.Equal("'My Data'!$B$1:$B$2", chart.Series[0].Values);
    }

    [Fact]
    public void RenamingSheet_RewritesValidationFormulas()
    {
        var (_, data, report) = CreateWorkbook();
        var rule = new DataValidationRule { Type = DataValidationType.Custom, Formula1 = "=B1<=Data!A2" };
        var listRule = new DataValidationRule { Type = DataValidationType.List, Formula1 = "Yes,No" };
        report.Validation.Add(RangeRef.Parse("B1:B5"), rule);
        report.Validation.Add(RangeRef.Parse("C1:C5"), listRule);

        data.Name = "Other";

        Assert.Equal("=B1<=Other!A2", rule.Formula1);
        Assert.Equal("Yes,No", listRule.Formula1);
    }

    [Fact]
    public void LoadingXlsx_DoesNotRewriteFormulasThatMentionTheDefaultSheetName()
    {
        var workbook = new Workbook();
        workbook.AddSheet("Worksheet1", 10, 5).Cells["A1"].Value = 4;
        var report = workbook.AddSheet("Report", 10, 5);
        report.Cells["A1"].Formula = "=Worksheet1!A1";

        using var stream = new MemoryStream();
        workbook.SaveToStream(stream);
        stream.Position = 0;
        var loaded = Workbook.LoadFromStream(stream);

        Assert.Equal("=Worksheet1!A1", loaded.Sheets[1].Cells["A1"].Formula);
        Assert.Equal(4d, loaded.Sheets[1].Cells["A1"].Value);
    }

    [Fact]
    public void QuotedSheetNameWithEscapedApostrophe_Evaluates()
    {
        var (_, data, report) = CreateWorkbook();
        data.Name = "Bob's";

        report.Cells["A1"].Formula = "='Bob''s'!A1+SUM('Bob''s'!A:A)";

        Assert.Equal(4d, report.Cells["A1"].Value);
    }

    [Fact]
    public void RenamingToNameOfAnotherSheet_DoesNotRewriteReferences()
    {
        var (_, data, report) = CreateWorkbook();
        report.Cells["A1"].Formula = "=Data!A1";

        data.Name = "report";

        Assert.Equal("=Data!A1", report.Cells["A1"].Formula);
        Assert.Equal(CellError.Ref, report.Cells["A1"].Value);
    }
}
