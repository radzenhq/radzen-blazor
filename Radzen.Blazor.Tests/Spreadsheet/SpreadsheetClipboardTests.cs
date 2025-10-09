using Xunit;

namespace Radzen.Blazor.Spreadsheet.Tests;

public class SpreadsheetClipboardTests
{
    [Fact]
    public void Copy_Down_AdjustsRelative_KeepsAbsolute()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells[0, 0].Formula = "=A1";        // A1
        sheet.PasteRange(sheet, RangeRef.Parse("A1"), CellRef.Parse("A2"), FormulaAdjustment.AdjustRelative);
        Assert.Equal("=A2", sheet.Cells[1, 0].Formula);

        sheet.Cells[0, 1].Formula = "=$A$1";     // B1
        sheet.PasteRange(sheet, RangeRef.Parse("B1"), CellRef.Parse("B2"), FormulaAdjustment.AdjustRelative);
        Assert.Equal("=$A$1", sheet.Cells[1, 1].Formula);
    }

    [Fact]
    public void Copy_Right_AdjustsColumn_RetainsAbsoluteColumn()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells[0, 0].Formula = "=A1"; // A1
        sheet.PasteRange(sheet, RangeRef.Parse("A1"), CellRef.Parse("B1"), FormulaAdjustment.AdjustRelative);
        Assert.Equal("=B1", sheet.Cells[0, 1].Formula);

        sheet.Cells[0, 1].Formula = "=$A1"; // B1 absolute column
        sheet.PasteRange(sheet, RangeRef.Parse("B1"), CellRef.Parse("C1"), FormulaAdjustment.AdjustRelative);
        Assert.Equal("=$A1", sheet.Cells[0, 2].Formula);
    }

    [Fact]
    public void Cut_DoesNotAdjustFormula()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells[0, 0].Formula = "=A1"; // A1
        sheet.Selection.Select(CellRef.Parse("A1"));
        var clipboard = new SpreadsheetClipboard();
        clipboard.Cut(sheet);
        clipboard.Paste(sheet, CellRef.Parse("B2"));
        Assert.Equal("=A1", sheet.Cells[1, 1].Formula); // not adjusted
        Assert.Null(sheet.Cells[0, 0].Formula);          // source cleared
        Assert.Null(sheet.Cells[0, 0].Value);
    }

    [Fact]
    public void Copy_AcrossSheets_AdjustsRelativeReferences()
    {
        var source = new Sheet(10, 10);
        var target = new Sheet(10, 10);

        source.Cells[0, 0].Formula = "=A1";

        // Copy A1 from source to B2 in target
        target.PasteRange(source, RangeRef.Parse("A1"), CellRef.Parse("B2"), FormulaAdjustment.AdjustRelative);

        Assert.Equal("=B2", target.Cells[1, 1].Formula);
    }
}