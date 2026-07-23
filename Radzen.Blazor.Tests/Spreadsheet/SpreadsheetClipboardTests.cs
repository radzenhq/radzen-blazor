using System.Threading.Tasks;
using Xunit;

using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet.Tests;

public class SpreadsheetClipboardTests
{
    [Fact]
    public void Copy_Down_AdjustsRelative_KeepsAbsolute()
    {
        var sheet = new Worksheet(10, 10);
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
        var sheet = new Worksheet(10, 10);
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
        var sheet = new Worksheet(10, 10);
        sheet.Cells[0, 0].Formula = "=A1"; // A1
        sheet.Selection.Select(CellRef.Parse("A1"));
        var clipboard = new SpreadsheetClipboard();
        clipboard.Cut(sheet);
        clipboard.Paste(sheet, RangeRef.Parse("B2"));
        Assert.Equal("=A1", sheet.Cells[1, 1].Formula); // not adjusted
        Assert.Null(sheet.Cells[0, 0].Formula);          // source cleared
        Assert.Null(sheet.Cells[0, 0].Value);
    }

    [Fact]
    public void Copy_AcrossSheets_AdjustsRelativeReferences()
    {
        var source = new Worksheet(10, 10);
        var target = new Worksheet(10, 10);

        source.Cells[0, 0].Formula = "=A1";

        // Copy A1 from source to B2 in target
        target.PasteRange(source, RangeRef.Parse("A1"), CellRef.Parse("B2"), FormulaAdjustment.AdjustRelative);

        Assert.Equal("=B2", target.Cells[1, 1].Formula);
    }

    [Fact]
    public void PasteCommand_Succeeds_When_All_Target_Cells_Unlocked_In_Protected_Sheet()
    {
        var workbook = new Workbook();
        var sheet = workbook.AddSheet("Test", 5, 5);
        var view = new WorkbookView(workbook).GetView(sheet);

        sheet.Cells[0, 0].Format = new Format { Locked = false };
        sheet.Cells[0, 1].Format = new Format { Locked = false };
        sheet.Protection.IsProtected = true;

        var clipboard = new SpreadsheetClipboard();
        var command = new PasteCommand(clipboard, sheet, RangeRef.Parse("A1"), "Data1\tData2");

        var result = view.Commands.Execute(command);

        Assert.True(result);
        Assert.Equal("Data1", sheet.Cells[0, 0].Value);
        Assert.Equal("Data2", sheet.Cells[0, 1].Value);
    }

    [Fact]
    public void PasteCommand_Fails_Entirely_If_Any_Target_Cell_Locked_In_Protected_Sheet()
    {
        var workbook = new Workbook();
        var sheet = workbook.AddSheet("Test", 5, 5);
        var view = new WorkbookView(workbook).GetView(sheet);

        sheet.Cells[0, 0].Format = new Format { Locked = false };
        sheet.Cells[0, 1].Format = new Format { Locked = true };
        sheet.Protection.IsProtected = true;

        var clipboard = new SpreadsheetClipboard();
        var command = new PasteCommand(clipboard, sheet, RangeRef.Parse("A1"), "Data1\tData2");

        var result = view.Commands.Execute(command);

        Assert.False(result);
        Assert.Null(sheet.Cells[0, 0].Value);
        Assert.Null(sheet.Cells[0, 1].Value);
    }

    [Fact]
    public void PasteCommand_Works_When_Protection_Is_Disabled()
    {
        var workbook = new Workbook();
        var sheet = workbook.AddSheet("Test", 5, 5);
        var view = new WorkbookView(workbook).GetView(sheet);

        sheet.Cells[0, 0].Format = new Format { Locked = true };
        sheet.Cells[0, 1].Format = new Format { Locked = true };
        sheet.Protection.IsProtected = false;

        var clipboard = new SpreadsheetClipboard();
        var command = new PasteCommand(clipboard, sheet, RangeRef.Parse("A1"), "Data1\tData2");

        var result = view.Commands.Execute(command);

        Assert.True(result);
        Assert.Equal("Data1", sheet.Cells[0, 0].Value);
        Assert.Equal("Data2", sheet.Cells[0, 1].Value);
    }

    [Fact]
    public async Task CutSelectionAsync_PreventsLockedCells_OnProtectedSheet()
    {
        var workbook = new Workbook();
        var sheet = workbook.AddSheet("Test", 5, 5);
        var spreadsheet = new RadzenSpreadsheet { Workbook = workbook };

        sheet.Cells[0, 0].SetValue("CanCut");
        sheet.Cells[0, 0].Format = new Format { Locked = false };
        sheet.Cells[1, 0].SetValue("CannotCut");
        sheet.Cells[1, 0].Format = new Format { Locked = true };
        sheet.Protection.IsProtected = true;

        sheet.Selection.Select(new RangeRef(new CellRef(0, 0), new CellRef(1, 0)));
        await spreadsheet.CutSelectionAsync();

        Assert.Equal("CanCut", sheet.Cells[0, 0].Value);
        Assert.Equal("CannotCut", sheet.Cells[1, 0].Value);
    }
    [Fact]
    public void GetSource_ReturnsRange_OnlyForSourceSheet()
    {
        var sheet1 = new Worksheet(10, 10);
        var sheet2 = new Worksheet(10, 10);
        sheet1.Selection.Select(RangeRef.Parse("A1:B2"));
        
        var clipboard = new SpreadsheetClipboard();
        clipboard.Copy(sheet1);
        
        Assert.Equal(RangeRef.Parse("A1:B2"), clipboard.GetSource(sheet1));
        Assert.Equal(RangeRef.Invalid, clipboard.GetSource(sheet2)); // Isn't drawn on a different sheet
    }

    [Fact]
    public void GetSource_SurvivesCopyPaste_ButClearsOnExternalPaste()
    {
        var sheet = new Worksheet(10, 10);
        sheet.Cells[0, 0].Value = "Data";
        sheet.Selection.Select(RangeRef.Parse("A1"));
        
        var clipboard = new SpreadsheetClipboard();
        clipboard.Copy(sheet);
        
        // Internal Paste (matches CSV, simulating a normal app paste)
        clipboard.TryPaste(sheet, RangeRef.Parse("B1"), "Data");
        Assert.Equal(RangeRef.Parse("A1"), clipboard.GetSource(sheet)); // Survives copy-paste
        
        // External Paste (doesn't match CSV, simulating paste from another app)
        clipboard.TryPaste(sheet, RangeRef.Parse("C1"), "ExternalData");
        Assert.Equal(RangeRef.Invalid, clipboard.GetSource(sheet)); // Clears on external paste
    }

    [Fact]
    public void GetSource_ClearsOnCutPaste()
    {
        var sheet = new Worksheet(10, 10);
        sheet.Cells[0, 0].Value = "Data";
        sheet.Selection.Select(RangeRef.Parse("A1"));
        
        var clipboard = new SpreadsheetClipboard();
        clipboard.Cut(sheet);
        
        Assert.Equal(RangeRef.Parse("A1"), clipboard.GetSource(sheet));
        
        clipboard.Paste(sheet, RangeRef.Parse("B1"));
        Assert.Equal(RangeRef.Invalid, clipboard.GetSource(sheet)); // Clears after cut-paste completes
    }

    [Fact]
    public void Clear_RemovesSource()
    {
        var sheet = new Worksheet(10, 10);
        sheet.Selection.Select(RangeRef.Parse("A1"));
        
        var clipboard = new SpreadsheetClipboard();
        clipboard.Copy(sheet);
        
        clipboard.Clear(); // Simulates the Escape key or structural edits
        Assert.Equal(RangeRef.Invalid, clipboard.GetSource(sheet));
    }
}