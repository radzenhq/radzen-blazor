using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Radzen.Blazor;
using Radzen.Blazor.Rendering;
using Radzen.Blazor.Spreadsheet;
using Radzen.Documents.Spreadsheet;
using Xunit;

namespace Radzen.Blazor.Tests.Spreadsheet;

public class SpreadsheetAccessibilityTests
{
    private static TestContext CreateContext()
    {
        var ctx = new TestContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.JSInterop.Setup<VirtualRegion>("Radzen.createVirtualItemContainer", _ => true)
            .SetResult(new VirtualRegion { Width = 800, Height = 600, ScrollWidth = 800, ScrollHeight = 600 });
        ctx.Services.AddRadzenComponents();
        return ctx;
    }

    private static (Workbook Workbook, Worksheet Sheet) NewWorkbook()
    {
        var wb = new Workbook();
        var sheet = wb.AddSheet("Sheet1", 10, 10);
        return (wb, sheet);
    }

    // A test localizer: friendly text for the keys the announcer composes, and the position format.
    private static string L(string key) => key switch
    {
        nameof(RadzenStrings.Spreadsheet_A11yBlank) => "blank",
        nameof(RadzenStrings.Spreadsheet_A11yPosition) => "row {0} of {1}, column {2} of {3}",
        nameof(RadzenStrings.Spreadsheet_A11yMerged) => "merged",
        nameof(RadzenStrings.Spreadsheet_A11yFormula) => "has formula",
        _ => key
    };

    // ── Announcer compose (pure function) ───────────────────────────────

    [Fact]
    public void BuildAnnouncement_EmptyCell_AddressBlankAndPosition()
    {
        var (_, sheet) = NewWorkbook();

        var text = SpreadsheetAccessibility.BuildAnnouncement(sheet, new CellRef(0, 0), L);

        Assert.StartsWith("A1, blank", text);
        Assert.Contains("row 1 of 10, column A of 10", text);
    }

    [Fact]
    public void BuildAnnouncement_Position_IsOneBasedWithColumnLetter()
    {
        var (_, sheet) = NewWorkbook();

        var text = SpreadsheetAccessibility.BuildAnnouncement(sheet, new CellRef(2, 2), L);

        Assert.StartsWith("C3,", text);
        Assert.Contains("row 3 of 10, column C of 10", text);
    }

    [Fact]
    public void BuildAnnouncement_NullWorksheet_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, SpreadsheetAccessibility.BuildAnnouncement(null!, new CellRef(0, 0), L));
    }

    [Fact]
    public void BuildAnnouncement_InvalidCell_ReturnsEmpty()
    {
        var (_, sheet) = NewWorkbook();
        Assert.Equal(string.Empty, SpreadsheetAccessibility.BuildAnnouncement(sheet, CellRef.Invalid, L));
    }

    // ── Root application region + instructions (WI1) ────────────────────

    [Fact]
    public void Root_Has_ApplicationRole_Keyshortcuts_And_Describedby()
    {
        using var ctx = CreateContext();
        var (wb, _) = NewWorkbook();
        var c = ctx.RenderComponent<RadzenSpreadsheet>(p => p.Add(x => x.Workbook, wb));

        Assert.Contains("role=\"application\"", c.Markup);
        Assert.Contains("aria-keyshortcuts=\"F6 Shift+F6\"", c.Markup);
        Assert.Contains("-a11y-instructions", c.Markup); // aria-describedby target + the span id
    }

    // ── Toolbar group + tool names (WI5) ────────────────────────────────

    [Fact]
    public void Toolbar_Has_GroupRole_And_NamedTools()
    {
        using var ctx = CreateContext();
        var (wb, _) = NewWorkbook();
        var c = ctx.RenderComponent<RadzenSpreadsheet>(p => p.Add(x => x.Workbook, wb));

        Assert.Contains("role=\"group\"", c.Markup);
        Assert.Contains("aria-label=\"Bold\"", c.Markup);  // RadzenToggleButton AriaLabel
        Assert.Contains("aria-label=\"Undo\"", c.Markup);  // RadzenButton splatted aria-label
    }

    // ── Editor naming (WI6) ─────────────────────────────────────────────

    [Fact]
    public void FormulaBar_Editor_Has_TextboxRole_And_Name()
    {
        using var ctx = CreateContext();
        var (wb, _) = NewWorkbook();
        var c = ctx.RenderComponent<RadzenSpreadsheet>(p => p.Add(x => x.Workbook, wb));

        Assert.Contains("role=\"textbox\"", c.Markup);
        Assert.Contains("aria-label=\"Formula bar\"", c.Markup);
    }

    // ── Contextual keyboard gate (WI2) ──────────────────────────────────

    [Fact]
    public async Task Gate_TabInGridContext_MovesActiveCell()
    {
        using var ctx = CreateContext();
        var (wb, sheet) = NewWorkbook();
        var c = ctx.RenderComponent<RadzenSpreadsheet>(p => p.Add(x => x.Workbook, wb));

        await c.InvokeAsync(() => sheet.Selection.Select(new CellRef(0, 0)));
        await c.InvokeAsync(() => c.Instance.OnKeyDownAsync(new KeyboardEventArgs { Key = "Tab", Code = "Tab" }, true));

        Assert.Equal(1, sheet.Selection.Cell.Column);
    }

    [Fact]
    public async Task Gate_TabOutsideGridContext_DoesNotMoveActiveCell()
    {
        using var ctx = CreateContext();
        var (wb, sheet) = NewWorkbook();
        var c = ctx.RenderComponent<RadzenSpreadsheet>(p => p.Add(x => x.Workbook, wb));

        await c.InvokeAsync(() => sheet.Selection.Select(new CellRef(0, 0)));
        await c.InvokeAsync(() => c.Instance.OnKeyDownAsync(new KeyboardEventArgs { Key = "Tab", Code = "Tab" }, false));

        Assert.Equal(0, sheet.Selection.Cell.Column);
    }

    // ── Extended navigation keys (WI2) ──────────────────────────────────

    [Fact]
    public async Task NavKey_CtrlHome_MovesToA1()
    {
        using var ctx = CreateContext();
        var (wb, sheet) = NewWorkbook();
        var c = ctx.RenderComponent<RadzenSpreadsheet>(p => p.Add(x => x.Workbook, wb));

        await c.InvokeAsync(() => sheet.Selection.Select(new CellRef(2, 2)));
        await c.InvokeAsync(() => c.Instance.OnKeyDownAsync(new KeyboardEventArgs { Key = "Home", Code = "Home", CtrlKey = true }, true));

        Assert.Equal(0, sheet.Selection.Cell.Row);
        Assert.Equal(0, sheet.Selection.Cell.Column);
    }

    [Fact]
    public async Task NavKey_Home_MovesToRowStart()
    {
        using var ctx = CreateContext();
        var (wb, sheet) = NewWorkbook();
        var c = ctx.RenderComponent<RadzenSpreadsheet>(p => p.Add(x => x.Workbook, wb));

        await c.InvokeAsync(() => sheet.Selection.Select(new CellRef(2, 2)));
        await c.InvokeAsync(() => c.Instance.OnKeyDownAsync(new KeyboardEventArgs { Key = "Home", Code = "Home" }, true));

        Assert.Equal(2, sheet.Selection.Cell.Row);
        Assert.Equal(0, sheet.Selection.Cell.Column);
    }

    [Fact]
    public async Task NavKey_CtrlA_SelectsFromA1()
    {
        using var ctx = CreateContext();
        var (wb, sheet) = NewWorkbook();
        var c = ctx.RenderComponent<RadzenSpreadsheet>(p => p.Add(x => x.Workbook, wb));

        await c.InvokeAsync(() => sheet.Selection.Select(new CellRef(2, 2)));
        await c.InvokeAsync(() => c.Instance.OnKeyDownAsync(new KeyboardEventArgs { Key = "a", Code = "KeyA", CtrlKey = true }, true));

        Assert.Equal(0, sheet.Selection.Range.Start.Row);
        Assert.Equal(0, sheet.Selection.Range.Start.Column);
    }

    [Fact]
    public async Task NavKey_GridOnly_DoesNotActOutsideGrid()
    {
        using var ctx = CreateContext();
        var (wb, sheet) = NewWorkbook();
        var c = ctx.RenderComponent<RadzenSpreadsheet>(p => p.Add(x => x.Workbook, wb));

        await c.InvokeAsync(() => sheet.Selection.Select(new CellRef(2, 2)));
        await c.InvokeAsync(() => c.Instance.OnKeyDownAsync(new KeyboardEventArgs { Key = "Home", Code = "Home", CtrlKey = true }, false));

        Assert.Equal(2, sheet.Selection.Cell.Column); // Ctrl+Home is grid-only; blocked outside the grid
    }

    [Fact]
    public async Task NavKey_CtrlDown_EmptySheet_JumpsToLastRow()
    {
        using var ctx = CreateContext();
        var (wb, sheet) = NewWorkbook(); // 10x10
        var c = ctx.RenderComponent<RadzenSpreadsheet>(p => p.Add(x => x.Workbook, wb));

        await c.InvokeAsync(() => sheet.Selection.Select(new CellRef(0, 0)));
        await c.InvokeAsync(() => c.Instance.OnKeyDownAsync(new KeyboardEventArgs { Key = "ArrowDown", Code = "ArrowDown", CtrlKey = true }, true));

        Assert.Equal(9, sheet.Selection.Cell.Row); // edge jump to the last row of a 10-row sheet
        Assert.Equal(0, sheet.Selection.Cell.Column);
    }

    [Fact]
    public async Task NavKey_CtrlSpace_SelectsWholeColumn()
    {
        using var ctx = CreateContext();
        var (wb, sheet) = NewWorkbook(); // 10x10
        var c = ctx.RenderComponent<RadzenSpreadsheet>(p => p.Add(x => x.Workbook, wb));

        await c.InvokeAsync(() => sheet.Selection.Select(new CellRef(2, 2)));
        await c.InvokeAsync(() => c.Instance.OnKeyDownAsync(new KeyboardEventArgs { Key = " ", Code = "Space", CtrlKey = true }, true));

        Assert.Equal(2, sheet.Selection.Range.Start.Column);
        Assert.Equal(2, sheet.Selection.Range.End.Column);
        Assert.Equal(0, sheet.Selection.Range.Start.Row);
        Assert.Equal(9, sheet.Selection.Range.End.Row); // full column of a 10-row sheet
    }

    // ── Shortcut help dialog (Alt+/) ────────────────────────────────────

    [Fact]
    public void ShortcutsDialog_Renders_RowsInTable()
    {
        using var ctx = CreateContext();
        var rows = new System.Collections.Generic.List<SpreadsheetShortcutsDialog.Shortcut>
        {
            new("F6", "Move between regions"),
            new("Ctrl+A", "Select the used range")
        };

        var c = ctx.RenderComponent<SpreadsheetShortcutsDialog>(p => p
            .Add(x => x.Shortcuts, rows)
            .Add(x => x.ShortcutColumn, "Shortcut")
            .Add(x => x.ActionColumn, "Action"));

        Assert.Contains("rz-datatable", c.Markup);              // RadzenTable, not a naked <table>
        Assert.Contains("<kbd>F6</kbd>", c.Markup);
        Assert.Contains("Move between regions", c.Markup);
        Assert.Contains("Select the used range", c.Markup);
    }

    // ── Image/chart keyboard layer (WI7) ────────────────────────────────

    [Fact]
    public async Task Drawing_CtrlAlt5Selects_ArrowMoves_EscapeDeselects()
    {
        using var ctx = CreateContext();
        var (wb, sheet) = NewWorkbook();
        var image = new SheetImage
        {
            AnchorMode = DrawingAnchorMode.OneCellAnchor,
            From = new CellAnchor { Column = 1, ColumnOffset = 0, Row = 1, RowOffset = 0 },
            Width = 100,
            Height = 100
        };
        sheet.AddImage(image);

        var c = ctx.RenderComponent<RadzenSpreadsheet>(p => p.Add(x => x.Workbook, wb));

        // Ctrl+Alt+5 enters the drawing layer and selects the first image.
        await c.InvokeAsync(() => c.Instance.OnKeyDownAsync(
            new KeyboardEventArgs { Key = "5", Code = "Digit5", CtrlKey = true, AltKey = true }, true));
        Assert.Same(image, sheet.SelectedImage);

        // ArrowRight moves it (one undoable command); the anchor advances.
        await c.InvokeAsync(() => c.Instance.OnKeyDownAsync(
            new KeyboardEventArgs { Key = "ArrowRight", Code = "ArrowRight" }, true));
        Assert.True(image.From.ColumnOffset > 0 || image.From.Column > 1);

        // Escape returns to the grid.
        await c.InvokeAsync(() => c.Instance.OnKeyDownAsync(
            new KeyboardEventArgs { Key = "Escape", Code = "Escape" }, true));
        Assert.Null(sheet.SelectedImage);
    }

    // ── Column/row resize undo (in scope) ───────────────────────────────

    [Fact]
    public async Task ResizeColumn_DragPushesOneUndoableCommand()
    {
        using var ctx = CreateContext();
        var (wb, sheet) = NewWorkbook();
        var c = ctx.RenderComponent<RadzenSpreadsheet>(p => p.Add(x => x.Workbook, wb));

        var startWidth = sheet.Columns[2];

        await c.InvokeAsync(() => c.Instance.OnColumnResizePointerDownAsync(
            new CellEventArgs { Column = 2, Pointer = new PointerEventArgs { ClientX = 100 } }));
        await c.InvokeAsync(() => c.Instance.OnColumnResizePointerMoveAsync(new PointerEventArgs { ClientX = 160 }));
        await c.InvokeAsync(() => c.Instance.OnColumnResizePointerUpAsync(new PointerEventArgs { ClientX = 160 }));

        Assert.True(sheet.Columns[2] > startWidth); // drag widened it, applied via the command

        await c.InvokeAsync(() => c.Instance.Undo());

        Assert.Equal(startWidth, sheet.Columns[2]); // Ctrl+Z reverts the resize
    }
}
