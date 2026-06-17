using Bunit;
using Xunit;

using Radzen.Documents.Spreadsheet;

namespace Radzen.Blazor.Spreadsheet.Tests;

public class FormulaDisplayTests
{
    [Fact]
    public void CrossSheetFormula_ModelRetainsSheetPrefix()
    {
        var wb = new Workbook();
        var q1 = wb.AddSheet("Q1 Sales", 20, 10);
        var summary = wb.AddSheet("Summary", 20, 10);

        q1.Cells["B2"].Value = 10d;
        q1.Cells["C2"].Value = 20d;
        q1.Cells["D2"].Value = 30d;

        summary.Cells["B2"].Formula = "=SUM('Q1 Sales'!B2:D2)";

        Assert.Equal(60d, summary.Cells["B2"].Value);
        Assert.Equal("=SUM('Q1 Sales'!B2:D2)", summary.Cells["B2"].Formula);
        Assert.Equal("=SUM('Q1 Sales'!B2:D2)", summary.Cells["B2"].GetValue());
    }

    [Fact]
    public void Highlight_CrossSheetReference_KeepsSheetPrefix()
    {
        using var ctx = new TestContext();

        var cut = ctx.RenderComponent<SheetEditorHighlight>(
            parameters => parameters.Add(p => p.Value, "=SUM('Q1 Sales'!B2:D2)"));

        // The syntax-highlight overlay must keep the worksheet prefix (it previously rendered
        // the bare cell address, dropping 'Q1 Sales'!).
        Assert.Contains("'Q1 Sales'!B2", cut.Markup);
    }

    [Fact]
    public void Highlight_BareCellReference_StillRenders()
    {
        using var ctx = new TestContext();

        var cut = ctx.RenderComponent<SheetEditorHighlight>(
            parameters => parameters.Add(p => p.Value, "=B2+C2"));

        Assert.Contains("B2", cut.Markup);
        Assert.Contains("C2", cut.Markup);
    }
}
