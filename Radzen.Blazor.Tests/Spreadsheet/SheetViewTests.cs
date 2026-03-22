using System;
using Xunit;

using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet.Tests;

public class SheetViewTests
{
    // --- Construction ---

    [Fact]
    public void Constructor_SetsSheet()
    {
        var sheet = new Worksheet(10, 10);
        var view = new SheetView(sheet);

        Assert.Same(sheet, view.Worksheet);
    }

    [Fact]
    public void Constructor_ThrowsOnNull()
    {
        Assert.Throws<ArgumentNullException>(() => new SheetView(null!));
    }

    [Fact]
    public void Constructor_CreatesCommands()
    {
        var sheet = new Worksheet(10, 10);
        var view = new SheetView(sheet);

        Assert.NotNull(view.Commands);
    }

    [Fact]
    public void DefaultOffsets()
    {
        var sheet = new Worksheet(10, 10);
        var view = new SheetView(sheet);

        Assert.Equal(24, view.RowHeaderOffset);
        Assert.Equal(100, view.ColumnHeaderOffset);
    }

    // --- Rendering method delegation ---

    [Fact]
    public void GetRowRange_AddsOffset()
    {
        var sheet = new Worksheet(100, 10);
        var view = new SheetView(sheet);

        // SheetView subtracts the header offset before computing the index range
        var fromView = view.GetRowRange(0, 500);

        Assert.True(fromView.Start >= 0);
        Assert.True(fromView.End >= fromView.Start);
    }

    [Fact]
    public void GetColumnRange_AddsOffset()
    {
        var sheet = new Worksheet(10, 100);
        var view = new SheetView(sheet);

        var fromView = view.GetColumnRange(0, 500);

        Assert.True(fromView.Start >= 0);
        Assert.True(fromView.End >= fromView.Start);
    }

    [Fact]
    public void GetRowPixelRange_AddsOffset()
    {
        var sheet = new Worksheet(10, 10);
        sheet.Rows[3] = 50;
        var view = new SheetView(sheet);

        var fromView = view.GetRowPixelRange(2, 4);

        // Row default size is 24, rows 0-1 = 2*24 = 48, row 2 = 24, row 3 = 50, row 4 = 24
        // Start = 48 + RowHeaderOffset, End = 48 + 24 + 50 + 24 + RowHeaderOffset
        Assert.Equal(48 + view.RowHeaderOffset, fromView.Start);
        Assert.Equal(48 + 24 + 50 + 24 + view.RowHeaderOffset, fromView.End);
    }

    [Fact]
    public void GetRowPixelRange_SingleIndex_AddsOffset()
    {
        var sheet = new Worksheet(10, 10);
        var view = new SheetView(sheet);

        var fromView = view.GetRowPixelRange(5);

        // Row default size is 24, rows 0-4 = 5*24 = 120, row 5 = 24
        Assert.Equal(120 + view.RowHeaderOffset, fromView.Start);
        Assert.Equal(144 + view.RowHeaderOffset, fromView.End);
    }

    [Fact]
    public void GetColumnPixelRange_AddsOffset()
    {
        var sheet = new Worksheet(10, 10);
        sheet.Columns[2] = 200;
        var view = new SheetView(sheet);

        var fromView = view.GetColumnPixelRange(1, 3);

        // Column default size is 100, col 0 = 100, col 1 = 100, col 2 = 200, col 3 = 100
        // Start = 100 + ColumnHeaderOffset, End = 100 + 100 + 200 + 100 + ColumnHeaderOffset
        Assert.Equal(100 + view.ColumnHeaderOffset, fromView.Start);
        Assert.Equal(100 + 100 + 200 + 100 + view.ColumnHeaderOffset, fromView.End);
    }

    [Fact]
    public void GetColumnPixelRange_SingleIndex_AddsOffset()
    {
        var sheet = new Worksheet(10, 10);
        var view = new SheetView(sheet);

        var fromView = view.GetColumnPixelRange(5);

        // Column default size is 100, cols 0-4 = 5*100 = 500, col 5 = 100
        Assert.Equal(500 + view.ColumnHeaderOffset, fromView.Start);
        Assert.Equal(600 + view.ColumnHeaderOffset, fromView.End);
    }

    [Fact]
    public void TotalHeight_IncludesOffset()
    {
        var sheet = new Worksheet(10, 10);
        var view = new SheetView(sheet);

        Assert.Equal(sheet.Rows.Total + view.RowHeaderOffset, view.TotalHeight);
    }

    [Fact]
    public void TotalWidth_IncludesOffset()
    {
        var sheet = new Worksheet(10, 10);
        var view = new SheetView(sheet);

        Assert.Equal(sheet.Columns.Total + view.ColumnHeaderOffset, view.TotalWidth);
    }

    // --- Per-sheet Commands ---

    [Fact]
    public void Commands_ExecuteAndUndo()
    {
        var sheet = new Worksheet(5, 5);
        var view = new SheetView(sheet);
        sheet.Cells[0, 0].Value = "A";

        var cmd = new ClearContentsCommand(sheet, new RangeRef(new CellRef(0, 0), new CellRef(0, 0)));
        view.Commands.Execute(cmd);

        Assert.Null(sheet.Cells[0, 0].Value);

        view.Commands.Undo();

        Assert.Equal("A", sheet.Cells[0, 0].Value);
    }

    [Fact]
    public void Commands_OwnedByView()
    {
        var sheet = new Worksheet(5, 5);
        var view = new SheetView(sheet);

        // Commands are owned by the view, not the sheet
        Assert.NotNull(view.Commands);
    }

    [Fact]
    public void Commands_ViaViewCommands_ExecutesAndUndoes()
    {
        var sheet = new Worksheet(5, 5);
        var view = new SheetView(sheet);
        sheet.Cells[0, 0].Value = "A";

        // Execute through view.Commands (the way RadzenSpreadsheet does it)
        var cmd = new ClearContentsCommand(sheet, new RangeRef(new CellRef(0, 0), new CellRef(0, 0)));
        view.Commands.Execute(cmd);

        Assert.Null(sheet.Cells[0, 0].Value);
        Assert.True(view.Commands.CanUndo);

        // Undo through view.Commands
        view.Commands.Undo();
        Assert.Equal("A", sheet.Cells[0, 0].Value);
    }

    [Fact]
    public void Commands_IndependentPerView()
    {
        var sheet1 = new Worksheet(5, 5);
        var sheet2 = new Worksheet(5, 5);
        var view1 = new SheetView(sheet1);
        var view2 = new SheetView(sheet2);

        sheet1.Cells[0, 0].Value = "S1";
        sheet2.Cells[0, 0].Value = "S2";

        var cmd1 = new ClearContentsCommand(sheet1, new RangeRef(new CellRef(0, 0), new CellRef(0, 0)));
        view1.Commands.Execute(cmd1);

        Assert.Null(sheet1.Cells[0, 0].Value);
        Assert.Equal("S2", sheet2.Cells[0, 0].Value);

        // Undo on view1 doesn't affect view2
        view1.Commands.Undo();
        Assert.Equal("S1", sheet1.Cells[0, 0].Value);
        Assert.Equal("S2", sheet2.Cells[0, 0].Value);

        Assert.False(view2.Commands.CanUndo);
    }
}

public class WorkbookViewTests
{
    [Fact]
    public void Constructor_SetsWorkbook()
    {
        var wb = new Workbook();
        var view = new WorkbookView(wb);

        Assert.Same(wb, view.Workbook);
    }

    [Fact]
    public void Constructor_ThrowsOnNull()
    {
        Assert.Throws<ArgumentNullException>(() => new WorkbookView(null!));
    }

    [Fact]
    public void GetView_CreatesViewForSheet()
    {
        var wb = new Workbook();
        var sheet = wb.AddSheet("Worksheet1", 10, 10);
        var view = new WorkbookView(wb);

        var sheetView = view.GetView(sheet);

        Assert.NotNull(sheetView);
        Assert.Same(sheet, sheetView.Worksheet);
    }

    [Fact]
    public void GetView_ReturnsSameViewForSameSheet()
    {
        var wb = new Workbook();
        var sheet = wb.AddSheet("Worksheet1", 10, 10);
        var view = new WorkbookView(wb);

        var v1 = view.GetView(sheet);
        var v2 = view.GetView(sheet);

        Assert.Same(v1, v2);
    }

    [Fact]
    public void GetView_DifferentViewsForDifferentSheets()
    {
        var wb = new Workbook();
        var s1 = wb.AddSheet("Worksheet1", 10, 10);
        var s2 = wb.AddSheet("Worksheet2", 10, 10);
        var view = new WorkbookView(wb);

        var v1 = view.GetView(s1);
        var v2 = view.GetView(s2);

        Assert.NotSame(v1, v2);
        Assert.Same(s1, v1.Worksheet);
        Assert.Same(s2, v2.Worksheet);
    }

    [Fact]
    public void GetView_PerSheetUndoHistory()
    {
        var wb = new Workbook();
        var s1 = wb.AddSheet("Worksheet1", 5, 5);
        var s2 = wb.AddSheet("Worksheet2", 5, 5);
        var wbView = new WorkbookView(wb);

        s1.Cells[0, 0].Value = "A";
        s2.Cells[0, 0].Value = "B";

        var v1 = wbView.GetView(s1);
        var v2 = wbView.GetView(s2);

        // Execute command on sheet1's view
        v1.Commands.Execute(new ClearContentsCommand(s1, new RangeRef(new CellRef(0, 0), new CellRef(0, 0))));

        Assert.True(v1.Commands.CanUndo);
        Assert.False(v2.Commands.CanUndo);

        // Worksheet2 is unaffected
        Assert.Equal("B", s2.Cells[0, 0].Value);
    }

    [Fact]
    public void Remove_DisposesView()
    {
        var wb = new Workbook();
        var sheet = wb.AddSheet("Worksheet1", 10, 10);
        var view = new WorkbookView(wb);

        var v1 = view.GetView(sheet);
        v1.Commands.Execute(new ClearContentsCommand(sheet, new RangeRef(new CellRef(0, 0), new CellRef(0, 0))));

        Assert.True(view.Remove(sheet));

        // New view has empty undo stack
        var v2 = view.GetView(sheet);
        Assert.NotSame(v1, v2);
        Assert.False(v2.Commands.CanUndo);
    }

    [Fact]
    public void Remove_ReturnsFalseForUnknownSheet()
    {
        var wb = new Workbook();
        var view = new WorkbookView(wb);
        var sheet = new Worksheet(5, 5);

        Assert.False(view.Remove(sheet));
    }

    [Fact]
    public void GetView_ThrowsOnNull()
    {
        var wb = new Workbook();
        var view = new WorkbookView(wb);

        Assert.Throws<ArgumentNullException>(() => view.GetView(null!));
    }
}
