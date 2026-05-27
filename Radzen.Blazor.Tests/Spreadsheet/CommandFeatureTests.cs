using Radzen.Documents.Spreadsheet;
using Xunit;

namespace Radzen.Blazor.Spreadsheet.Tests;

/// <summary>
/// Verifies that every gated command reports the right <see cref="SpreadsheetFeature"/>
/// via <c>ICommand.Feature</c>. <c>RadzenSpreadsheet.ExecuteAsync</c> consults this to
/// route commands through the matching <c>Allow*</c> flag, so a wrong mapping silently
/// breaks gating for that command type.
/// </summary>
public class CommandFeatureTests
{
    private static Worksheet NewSheet() => new(10, 10);

    [Fact]
    public void Default_Feature_IsNull_OnPlainCommand()
    {
        ICommand command = new StubCommand();
        Assert.Null(command.Feature);
    }

    // ── Editing ─────────────────────────────────────────────────────────

    [Fact]
    public void ClearContents_IsEditing()
    {
        var sheet = NewSheet();
        Assert.Equal(SpreadsheetFeature.Editing, new ClearContentsCommand(sheet, RangeRef.Parse("A1")).Feature);
    }

    [Fact]
    public void InsertRow_IsEditing()
    {
        var sheet = NewSheet();
        Assert.Equal(SpreadsheetFeature.Editing, new InsertRowCommand(sheet, 0).Feature);
    }

    [Fact]
    public void InsertColumn_IsEditing()
    {
        var sheet = NewSheet();
        Assert.Equal(SpreadsheetFeature.Editing, new InsertColumnCommand(sheet, 0).Feature);
    }

    [Fact]
    public void DeleteRows_IsEditing()
    {
        var sheet = NewSheet();
        Assert.Equal(SpreadsheetFeature.Editing, new DeleteRowsCommand(sheet, 0, 0).Feature);
    }

    [Fact]
    public void DeleteColumns_IsEditing()
    {
        var sheet = NewSheet();
        Assert.Equal(SpreadsheetFeature.Editing, new DeleteColumnsCommand(sheet, 0, 0).Feature);
    }

    // ── Filtering ───────────────────────────────────────────────────────

    [Fact]
    public void Filter_IsFiltering()
    {
        var sheet = NewSheet();
        var filter = new SheetFilter(new EqualToCriterion { Column = 0, Value = "x" }, RangeRef.Parse("A1:A5"));
        Assert.Equal(SpreadsheetFeature.Filtering, new FilterCommand(sheet, filter).Feature);
    }

    [Fact]
    public void RemoveFilter_IsFiltering()
    {
        var sheet = NewSheet();
        var filter = new SheetFilter(new EqualToCriterion { Column = 0, Value = "x" }, RangeRef.Parse("A1:A5"));
        Assert.Equal(SpreadsheetFeature.Filtering, new RemoveFilterCommand(sheet, filter).Feature);
    }

    [Fact]
    public void SheetAutoFilter_IsFiltering()
    {
        var sheet = NewSheet();
        Assert.Equal(SpreadsheetFeature.Filtering, new SheetAutoFilterCommand(sheet, RangeRef.Parse("A1:B5")).Feature);
    }

    [Fact]
    public void TableFilter_IsFiltering()
    {
        var sheet = NewSheet();
        sheet.AddTable("T1", RangeRef.Parse("A1:B5"));
        Assert.Equal(SpreadsheetFeature.Filtering, new TableFilterCommand(sheet, 0).Feature);
    }

    // ── Sorting ─────────────────────────────────────────────────────────

    [Fact]
    public void Sort_IsSorting()
    {
        var sheet = NewSheet();
        Assert.Equal(SpreadsheetFeature.Sorting, new SortCommand(sheet, RangeRef.Parse("A1:A5"), SortOrder.Ascending, 0).Feature);
    }

    [Fact]
    public void MultiKeySort_IsSorting()
    {
        var sheet = NewSheet();
        Assert.Equal(SpreadsheetFeature.Sorting, new MultiKeySortCommand(sheet, RangeRef.Parse("A1:A5"), [new SortKey { ColumnIndex = 0, Order = SortOrder.Ascending }]).Feature);
    }

    // ── Autofill / Merging ──────────────────────────────────────────────

    [Fact]
    public void Autofill_IsAutofill()
    {
        var sheet = NewSheet();
        var src = RangeRef.Parse("A1");
        var dst = RangeRef.Parse("A1:A3");
        Assert.Equal(SpreadsheetFeature.Autofill, new AutofillCommand(sheet, src, dst, AutofillDirection.Down).Feature);
    }

    [Fact]
    public void MergeCells_IsMerging()
    {
        var sheet = NewSheet();
        Assert.Equal(SpreadsheetFeature.Merging, new MergeCellsCommand(sheet, RangeRef.Parse("A1:B2")).Feature);
    }

    [Fact]
    public void UnmergeCells_IsMerging()
    {
        var sheet = NewSheet();
        Assert.Equal(SpreadsheetFeature.Merging, new UnmergeCellsCommand(sheet, CellRef.Parse("A1")).Feature);
    }

    // ── CellFormatting ──────────────────────────────────────────────────

    [Fact]
    public void Format_IsCellFormatting()
    {
        var sheet = NewSheet();
        Assert.Equal(SpreadsheetFeature.CellFormatting, new FormatCommand(sheet, RangeRef.Parse("A1"), new Format { Bold = true }).Feature);
    }

    [Fact]
    public void Border_IsCellFormatting()
    {
        var sheet = NewSheet();
        var style = new BorderStyle();
        Assert.Equal(SpreadsheetFeature.CellFormatting, new BorderCommand(sheet, RangeRef.Parse("A1"), style, style, style, style).Feature);
    }

    [Fact]
    public void AllBorders_IsCellFormatting()
    {
        var sheet = NewSheet();
        Assert.Equal(SpreadsheetFeature.CellFormatting, new AllBordersCommand(sheet, RangeRef.Parse("A1"), new BorderStyle()).Feature);
    }

    [Fact]
    public void NoBorders_IsCellFormatting()
    {
        var sheet = NewSheet();
        Assert.Equal(SpreadsheetFeature.CellFormatting, new NoBordersCommand(sheet, RangeRef.Parse("A1")).Feature);
    }

    // ── Hyperlinks / Images / Charts ────────────────────────────────────

    [Fact]
    public void Hyperlink_IsHyperlinks()
    {
        var sheet = NewSheet();
        Assert.Equal(SpreadsheetFeature.Hyperlinks, new HyperlinkCommand(sheet, CellRef.Parse("A1"), new Hyperlink { Url = "https://example.com" }).Feature);
    }

    [Fact]
    public void InsertImage_IsImages()
    {
        var sheet = NewSheet();
        var image = new SheetImage { From = new CellAnchor { Row = 0, Column = 0 }, Width = 100, Height = 100 };
        Assert.Equal(SpreadsheetFeature.Images, new InsertImageCommand(sheet, image).Feature);
    }

    [Fact]
    public void DeleteImage_IsImages()
    {
        var sheet = NewSheet();
        var image = new SheetImage { From = new CellAnchor { Row = 0, Column = 0 }, Width = 100, Height = 100 };
        Assert.Equal(SpreadsheetFeature.Images, new DeleteImageCommand(sheet, image).Feature);
    }

    [Fact]
    public void MoveImage_IsImages()
    {
        var image = new SheetImage { From = new CellAnchor { Row = 0, Column = 0 }, Width = 100, Height = 100 };
        Assert.Equal(SpreadsheetFeature.Images, new MoveAnchoredCommand<SheetImage>(image, new CellAnchor { Row = 1, Column = 1 }, null, SpreadsheetFeature.Images).Feature);
    }

    [Fact]
    public void ResizeImage_IsImages()
    {
        var image = new SheetImage { From = new CellAnchor { Row = 0, Column = 0 }, Width = 100, Height = 100 };
        Assert.Equal(SpreadsheetFeature.Images, new ResizeAnchoredCommand<SheetImage>(image, 200, 200, SpreadsheetFeature.Images).Feature);
    }

    [Fact]
    public void InsertChart_IsCharts()
    {
        var sheet = NewSheet();
        var chart = new SheetChart { From = new CellAnchor { Row = 0, Column = 0 } };
        Assert.Equal(SpreadsheetFeature.Charts, new InsertChartCommand(sheet, chart).Feature);
    }

    [Fact]
    public void DeleteChart_IsCharts()
    {
        var sheet = NewSheet();
        var chart = new SheetChart { From = new CellAnchor { Row = 0, Column = 0 } };
        Assert.Equal(SpreadsheetFeature.Charts, new DeleteChartCommand(sheet, chart).Feature);
    }

    [Fact]
    public void MoveChart_IsCharts()
    {
        var chart = new SheetChart { From = new CellAnchor { Row = 0, Column = 0 } };
        Assert.Equal(SpreadsheetFeature.Charts, new MoveAnchoredCommand<SheetChart>(chart, new CellAnchor { Row = 1, Column = 1 }, null, SpreadsheetFeature.Charts).Feature);
    }

    [Fact]
    public void ResizeChart_IsCharts()
    {
        var chart = new SheetChart { From = new CellAnchor { Row = 0, Column = 0 } };
        Assert.Equal(SpreadsheetFeature.Charts, new ResizeAnchoredCommand<SheetChart>(chart, 200, 200, SpreadsheetFeature.Charts).Feature);
    }

    [Fact]
    public void EditChart_IsCharts()
    {
        var chart = new SheetChart { From = new CellAnchor { Row = 0, Column = 0 } };
        var state = new EditChartState(SpreadsheetChartType.Bar, null, true, ChartLegendPosition.Right, []);
        Assert.Equal(SpreadsheetFeature.Charts, new EditChartCommand(chart, state).Feature);
    }

    // ── Tables / DataValidation / ConditionalFormatting ─────────────────

    [Fact]
    public void InsertTable_IsTables()
    {
        var sheet = NewSheet();
        Assert.Equal(SpreadsheetFeature.Tables, new InsertTableCommand(sheet, "T1", RangeRef.Parse("A1:B2")).Feature);
    }

    [Fact]
    public void RemoveTable_IsTables()
    {
        var sheet = NewSheet();
        var table = sheet.AddTable("T1", RangeRef.Parse("A1:B2"));
        Assert.Equal(SpreadsheetFeature.Tables, new RemoveTableCommand(sheet, table).Feature);
    }

    [Fact]
    public void DataValidation_IsDataValidation()
    {
        var sheet = NewSheet();
        var rule = new DataValidationRule { Type = DataValidationType.WholeNumber };
        Assert.Equal(SpreadsheetFeature.DataValidation, new DataValidationCommand(sheet, RangeRef.Parse("A1"), rule).Feature);
    }

    [Fact]
    public void ClearValidation_IsDataValidation()
    {
        var sheet = NewSheet();
        Assert.Equal(SpreadsheetFeature.DataValidation, new ClearValidationCommand(sheet, RangeRef.Parse("A1")).Feature);
    }

    [Fact]
    public void ConditionalFormat_IsConditionalFormatting()
    {
        var sheet = NewSheet();
        var rule = new GreaterThanRule { Value = 10, Format = new Format() };
        Assert.Equal(SpreadsheetFeature.ConditionalFormatting, new ConditionalFormatCommand(sheet, RangeRef.Parse("A1"), rule).Feature);
    }

    private sealed class StubCommand : ICommand
    {
        public bool Execute() => true;
        public void Unexecute() { }
    }
}
