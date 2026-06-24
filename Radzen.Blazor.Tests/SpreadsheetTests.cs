using System.Threading.Tasks;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Radzen.Blazor;
using Radzen.Blazor.Spreadsheet;
using Radzen.Documents.Spreadsheet;
using Xunit;

namespace Radzen.Blazor.Tests;

public class SpreadsheetTests
{
    private static TestContext CreateContext()
    {
        var ctx = new TestContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        // VirtualGrid relies on a JS-measured region. Loose mode returns null for class types,
        // which would NRE inside its first render. Hand it a stub so OnAfterRender completes.
        ctx.JSInterop.Setup<VirtualRegion>("Radzen.createVirtualItemContainer", _ => true)
            .SetResult(new VirtualRegion { Width = 800, Height = 600, ScrollWidth = 800, ScrollHeight = 600 });
        ctx.Services.AddRadzenComponents();
        return ctx;
    }

    private static Workbook NewWorkbook()
    {
        var wb = new Workbook();
        wb.AddSheet("Sheet1", 10, 10);
        return wb;
    }

    // ExecuteAsync triggers cell-change StateHasChanged callbacks that have to run on
    // the Blazor dispatcher. Wrap with InvokeAsync so tests can call the API freely.
    private static async Task<bool> Run(IRenderedComponent<RadzenSpreadsheet> c, ICommand command)
    {
        var result = false;
        await c.InvokeAsync(async () => result = await c.Instance.ExecuteAsync(command));
        return result;
    }

    // ── Default rendering ───────────────────────────────────────────────

    [Fact]
    public void Spreadsheet_Renders_HostCssClass()
    {
        using var ctx = CreateContext();
        var c = ctx.RenderComponent<RadzenSpreadsheet>();
        Assert.Contains("rz-spreadsheet", c.Markup);
    }

    [Fact]
    public void Spreadsheet_Defaults_RenderToolbarFormulaBarAndSheetTabs()
    {
        using var ctx = CreateContext();
        var c = ctx.RenderComponent<RadzenSpreadsheet>(p => p.Add(x => x.Workbook, NewWorkbook()));

        Assert.Contains("rz-tabview", c.Markup);                       // toolbar
        Assert.Contains("rz-spreadsheet-formula-editor", c.Markup); // formula bar
        Assert.Contains("rz-spreadsheet-sheet-tabs", c.Markup);     // sheet strip
    }

    // ── ShowToolbar / ShowFormulaBar / ShowSheetTabs ────────────────────

    [Fact]
    public void ShowToolbar_False_HidesToolbarToolsetLabels()
    {
        using var ctx = CreateContext();
        var c = ctx.RenderComponent<RadzenSpreadsheet>(p =>
        {
            p.Add(x => x.Workbook, NewWorkbook());
            p.Add(x => x.ShowToolbar, false);
        });

        // Every toolset label is gone; the bottom sheet-tabs strip stays.
        var home = RadzenStrings.ResourceManager.GetString(nameof(RadzenStrings.Spreadsheet_HomeTab))!;
        var insert = RadzenStrings.ResourceManager.GetString(nameof(RadzenStrings.Spreadsheet_InsertTab))!;
        Assert.DoesNotContain(home, c.Markup);
        Assert.DoesNotContain(insert, c.Markup);
    }

    [Fact]
    public void ShowFormulaBar_False_HidesFormulaEditor()
    {
        using var ctx = CreateContext();
        var c = ctx.RenderComponent<RadzenSpreadsheet>(p =>
        {
            p.Add(x => x.Workbook, NewWorkbook());
            p.Add(x => x.ShowFormulaBar, false);
        });

        Assert.DoesNotContain("rz-spreadsheet-formula-editor", c.Markup);
    }

    [Fact]
    public void ShowSheetTabs_False_HidesSheetTabsStrip()
    {
        using var ctx = CreateContext();
        var c = ctx.RenderComponent<RadzenSpreadsheet>(p =>
        {
            p.Add(x => x.Workbook, NewWorkbook());
            p.Add(x => x.ShowSheetTabs, false);
        });

        Assert.DoesNotContain("rz-spreadsheet-sheet-tabs", c.Markup);
    }

    // ── ChildContent (custom toolbar) ──

    [Fact]
    public void ChildContent_Replaces_DefaultToolsets()
    {
        using var ctx = CreateContext();
        var c = ctx.RenderComponent<RadzenSpreadsheet>(p =>
        {
            p.Add(x => x.Workbook, NewWorkbook());
            p.AddChildContent("<RadzenTabsItem Text=\"My Tab\"><div>my tools</div></RadzenTabsItem>");
        });

        // Default labels are gone, the supplied toolset is rendered, the toolbar shell stays.
        var home = RadzenStrings.ResourceManager.GetString(nameof(RadzenStrings.Spreadsheet_HomeTab))!;
        Assert.DoesNotContain(home, c.Markup);
        Assert.Contains("My Tab", c.Markup);
        Assert.Contains("rz-tabview", c.Markup);
    }

    [Fact]
    public void ChildContent_DoesNotInclude_TableDesignToolset_AutomaticallyInsideTable()
    {
        // ChildContent fully replaces the toolbar — the host does NOT inject the
        // Table Design tab. Users add the TableDesignToolset component themselves to opt in.
        using var ctx = CreateContext();
        var wb = NewWorkbook();
        var sheet = wb.Sheets[0];
        sheet.AddTable("T1", RangeRef.Parse("A1:B2"));
        sheet.Selection.Select(CellRef.Parse("A1"));

        var c = ctx.RenderComponent<RadzenSpreadsheet>(p =>
        {
            p.Add(x => x.Workbook, wb);
            p.AddChildContent("<RadzenTabsItem Text=\"Only Mine\"><div /></RadzenTabsItem>");
        });

        var tableDesign = RadzenStrings.ResourceManager.GetString(nameof(RadzenStrings.Spreadsheet_TableDesignTab))!;
        Assert.Contains("Only Mine", c.Markup);
        Assert.DoesNotContain(tableDesign, c.Markup);
    }

    [Fact]
    public void TableDesignToolset_InChildContent_ShowsWhenCellInsideTable()
    {
        using var ctx = CreateContext();
        var wb = NewWorkbook();
        var sheet = wb.Sheets[0];
        sheet.AddTable("T1", RangeRef.Parse("A1:B2"));
        sheet.Selection.Select(CellRef.Parse("A1"));

        var c = ctx.RenderComponent<RadzenSpreadsheet>(p =>
        {
            p.Add(x => x.Workbook, wb);
            p.AddChildContent<Radzen.Blazor.RadzenSpreadsheetTableDesignToolset>(tab =>
                tab.Add(t => t.Worksheet, sheet));
        });

        var tableDesign = RadzenStrings.ResourceManager.GetString(nameof(RadzenStrings.Spreadsheet_TableDesignTab))!;
        Assert.Contains(tableDesign, c.Markup);
    }

    [Fact]
    public void TableDesignToolset_InChildContent_HiddenWhenCellOutsideTable()
    {
        using var ctx = CreateContext();
        var wb = NewWorkbook();
        var sheet = wb.Sheets[0];
        sheet.AddTable("T1", RangeRef.Parse("A1:B2"));
        sheet.Selection.Select(CellRef.Parse("E5")); // outside table

        var c = ctx.RenderComponent<RadzenSpreadsheet>(p =>
        {
            p.Add(x => x.Workbook, wb);
            p.AddChildContent<Radzen.Blazor.RadzenSpreadsheetTableDesignToolset>(tab =>
                tab.Add(t => t.Worksheet, sheet));
        });

        var tableDesign = RadzenStrings.ResourceManager.GetString(nameof(RadzenStrings.Spreadsheet_TableDesignTab))!;
        Assert.DoesNotContain(tableDesign, c.Markup);
    }

    // ── ReadOnly ────────────────────────────────────────────────────────

    [Fact]
    public async Task ReadOnly_True_RejectsExecuteAsync()
    {
        using var ctx = CreateContext();
        var wb = NewWorkbook();
        var sheet = wb.Sheets[0];
        var c = ctx.RenderComponent<RadzenSpreadsheet>(p =>
        {
            p.Add(x => x.Workbook, wb);
            p.Add(x => x.ReadOnly, true);
        });

        var ran = await Run(c,new ClearContentsCommand(sheet, RangeRef.Parse("A1")));

        Assert.False(ran);
    }

    [Fact]
    public async Task ReadOnly_False_AllowsExecuteAsync()
    {
        using var ctx = CreateContext();
        var wb = NewWorkbook();
        var sheet = wb.Sheets[0];
        sheet.Cells["A1"].Value = "hello";
        var c = ctx.RenderComponent<RadzenSpreadsheet>(p => p.Add(x => x.Workbook, wb));

        var ran = await Run(c,new ClearContentsCommand(sheet, RangeRef.Parse("A1")));

        Assert.True(ran);
        Assert.Null(sheet.Cells["A1"].Value);
    }

    [Fact]
    public void ReadOnly_True_DisablesAddSheetButton()
    {
        using var ctx = CreateContext();
        var c = ctx.RenderComponent<RadzenSpreadsheet>(p =>
        {
            p.Add(x => x.Workbook, NewWorkbook());
            p.Add(x => x.ReadOnly, true);
        });

        // The add-sheet button is inside the sheet tabs strip and should be disabled.
        var addButton = c.Find(".rz-spreadsheet-add-sheet");
        Assert.True(addButton.HasAttribute("disabled"));
    }

    // ── Allow* gating ───────────────────────────────────────────────────

    [Fact]
    public async Task AllowEditing_False_RejectsClearContents()
    {
        using var ctx = CreateContext();
        var wb = NewWorkbook();
        var sheet = wb.Sheets[0];
        var c = ctx.RenderComponent<RadzenSpreadsheet>(p =>
        {
            p.Add(x => x.Workbook, wb);
            p.Add(x => x.AllowEditing, false);
        });

        var ran = await Run(c,new ClearContentsCommand(sheet, RangeRef.Parse("A1")));

        Assert.False(ran);
    }

    [Fact]
    public async Task AllowFiltering_False_RejectsFilterCommand()
    {
        using var ctx = CreateContext();
        var wb = NewWorkbook();
        var sheet = wb.Sheets[0];
        var c = ctx.RenderComponent<RadzenSpreadsheet>(p =>
        {
            p.Add(x => x.Workbook, wb);
            p.Add(x => x.AllowFiltering, false);
        });

        var filter = new SheetFilter(new EqualToCriterion { Column = 0, Value = "x" }, RangeRef.Parse("A1:A5"));
        var ran = await Run(c,new FilterCommand(sheet, filter));

        Assert.False(ran);
    }

    [Fact]
    public async Task AllowSorting_False_RejectsSortCommand()
    {
        using var ctx = CreateContext();
        var wb = NewWorkbook();
        var sheet = wb.Sheets[0];
        var c = ctx.RenderComponent<RadzenSpreadsheet>(p =>
        {
            p.Add(x => x.Workbook, wb);
            p.Add(x => x.AllowSorting, false);
        });

        var ran = await Run(c,new SortCommand(sheet, RangeRef.Parse("A1:A5"), SortOrder.Ascending, 0));

        Assert.False(ran);
    }

    [Fact]
    public async Task AllowCellFormatting_False_RejectsFormatCommand()
    {
        using var ctx = CreateContext();
        var wb = NewWorkbook();
        var sheet = wb.Sheets[0];
        var c = ctx.RenderComponent<RadzenSpreadsheet>(p =>
        {
            p.Add(x => x.Workbook, wb);
            p.Add(x => x.AllowCellFormatting, false);
        });

        var ran = await Run(c,new FormatCommand(sheet, RangeRef.Parse("A1"), new Format { Bold = true }));

        Assert.False(ran);
    }

    [Fact]
    public async Task AllowMerge_False_RejectsMergeCommand()
    {
        using var ctx = CreateContext();
        var wb = NewWorkbook();
        var sheet = wb.Sheets[0];
        var c = ctx.RenderComponent<RadzenSpreadsheet>(p =>
        {
            p.Add(x => x.Workbook, wb);
            p.Add(x => x.AllowMerge, false);
        });

        var ran = await Run(c,new MergeCellsCommand(sheet, RangeRef.Parse("A1:B2")));

        Assert.False(ran);
    }

    [Fact]
    public async Task AllowImages_False_RejectsInsertImage()
    {
        using var ctx = CreateContext();
        var wb = NewWorkbook();
        var sheet = wb.Sheets[0];
        var c = ctx.RenderComponent<RadzenSpreadsheet>(p =>
        {
            p.Add(x => x.Workbook, wb);
            p.Add(x => x.AllowImages, false);
        });

        var image = new SheetImage { From = new CellAnchor { Row = 0, Column = 0 }, Width = 100, Height = 100 };
        var ran = await Run(c,new InsertImageCommand(sheet, image));

        Assert.False(ran);
    }

    [Fact]
    public async Task AllowCharts_False_RejectsInsertChart()
    {
        using var ctx = CreateContext();
        var wb = NewWorkbook();
        var sheet = wb.Sheets[0];
        var c = ctx.RenderComponent<RadzenSpreadsheet>(p =>
        {
            p.Add(x => x.Workbook, wb);
            p.Add(x => x.AllowCharts, false);
        });

        var chart = new SheetChart { From = new CellAnchor { Row = 0, Column = 0 } };
        var ran = await Run(c,new InsertChartCommand(sheet, chart));

        Assert.False(ran);
    }

    [Fact]
    public async Task AllowTables_False_RejectsInsertTable()
    {
        using var ctx = CreateContext();
        var wb = NewWorkbook();
        var sheet = wb.Sheets[0];
        var c = ctx.RenderComponent<RadzenSpreadsheet>(p =>
        {
            p.Add(x => x.Workbook, wb);
            p.Add(x => x.AllowTables, false);
        });

        var ran = await Run(c,new InsertTableCommand(sheet, "T1", RangeRef.Parse("A1:B2")));

        Assert.False(ran);
    }

    [Fact]
    public async Task AllowHyperlinks_False_RejectsHyperlinkCommand()
    {
        using var ctx = CreateContext();
        var wb = NewWorkbook();
        var sheet = wb.Sheets[0];
        var c = ctx.RenderComponent<RadzenSpreadsheet>(p =>
        {
            p.Add(x => x.Workbook, wb);
            p.Add(x => x.AllowHyperlinks, false);
        });

        var ran = await Run(c,new HyperlinkCommand(sheet, CellRef.Parse("A1"), new Hyperlink { Url = "https://example.com" }));

        Assert.False(ran);
    }

    [Fact]
    public async Task AllowDataValidation_False_RejectsDataValidation()
    {
        using var ctx = CreateContext();
        var wb = NewWorkbook();
        var sheet = wb.Sheets[0];
        var c = ctx.RenderComponent<RadzenSpreadsheet>(p =>
        {
            p.Add(x => x.Workbook, wb);
            p.Add(x => x.AllowDataValidation, false);
        });

        var rule = new DataValidationRule { Type = DataValidationType.WholeNumber };
        var ran = await Run(c,new DataValidationCommand(sheet, RangeRef.Parse("A1"), rule));

        Assert.False(ran);
    }

    [Fact]
    public async Task AllowConditionalFormatting_False_RejectsConditionalFormat()
    {
        using var ctx = CreateContext();
        var wb = NewWorkbook();
        var sheet = wb.Sheets[0];
        var c = ctx.RenderComponent<RadzenSpreadsheet>(p =>
        {
            p.Add(x => x.Workbook, wb);
            p.Add(x => x.AllowConditionalFormatting, false);
        });

        var rule = new GreaterThanRule { Value = 10, Format = new Format() };
        var ran = await Run(c,new ConditionalFormatCommand(sheet, RangeRef.Parse("A1"), rule));

        Assert.False(ran);
    }

    [Fact]
    public async Task DisabledAllowFlag_DoesNotAffectUnrelatedCommand()
    {
        // AllowImages = false should not block a FormatCommand. Independence check.
        using var ctx = CreateContext();
        var wb = NewWorkbook();
        var sheet = wb.Sheets[0];
        var c = ctx.RenderComponent<RadzenSpreadsheet>(p =>
        {
            p.Add(x => x.Workbook, wb);
            p.Add(x => x.AllowImages, false);
        });

        var ran = await Run(c,new FormatCommand(sheet, RangeRef.Parse("A1"), new Format { Bold = true }));

        Assert.True(ran);
    }

    // ── Undo / Redo gating ──────────────────────────────────────────────

    [Fact]
    public async Task AllowUndoRedo_False_DisablesCanUndoEvenWithHistory()
    {
        using var ctx = CreateContext();
        var wb = NewWorkbook();
        var sheet = wb.Sheets[0];
        sheet.Cells["A1"].Value = "hello";
        var c = ctx.RenderComponent<RadzenSpreadsheet>(p => p.Add(x => x.Workbook, wb));

        await Run(c, new ClearContentsCommand(sheet, RangeRef.Parse("A1")));
        Assert.True(c.Instance.CanUndo);

        c.SetParametersAndRender(p => p.Add(x => x.AllowUndoRedo, false));

        Assert.False(c.Instance.CanUndo);
        Assert.False(c.Instance.CanRedo);
    }

    [Fact]
    public async Task ReadOnly_True_DisablesCanUndo()
    {
        using var ctx = CreateContext();
        var wb = NewWorkbook();
        var sheet = wb.Sheets[0];
        sheet.Cells["A1"].Value = "hello";
        var c = ctx.RenderComponent<RadzenSpreadsheet>(p => p.Add(x => x.Workbook, wb));

        await Run(c, new ClearContentsCommand(sheet, RangeRef.Parse("A1")));
        Assert.True(c.Instance.CanUndo);

        c.SetParametersAndRender(p => p.Add(x => x.ReadOnly, true));

        Assert.False(c.Instance.CanUndo);
    }

    [Fact]
    public async Task Undo_IsNoOp_WhenAllowUndoRedoFalse()
    {
        using var ctx = CreateContext();
        var wb = NewWorkbook();
        var sheet = wb.Sheets[0];
        sheet.Cells["A1"].Value = "hello";
        var c = ctx.RenderComponent<RadzenSpreadsheet>(p => p.Add(x => x.Workbook, wb));

        await Run(c, new ClearContentsCommand(sheet, RangeRef.Parse("A1")));
        Assert.Null(sheet.Cells["A1"].Value);

        c.SetParametersAndRender(p => p.Add(x => x.AllowUndoRedo, false));
        c.Instance.Undo();

        Assert.Null(sheet.Cells["A1"].Value); // not restored
    }

    // ── IsFeatureAllowed ────────────────────────────────────────────────

    [Fact]
    public void IsFeatureAllowed_ReturnsTrue_ByDefault()
    {
        using var ctx = CreateContext();
        var c = ctx.RenderComponent<RadzenSpreadsheet>(p => p.Add(x => x.Workbook, NewWorkbook()));

        foreach (SpreadsheetFeature f in System.Enum.GetValues(typeof(SpreadsheetFeature)))
        {
            Assert.True(c.Instance.IsFeatureAllowed(f), $"Feature {f} should be allowed by default");
        }
    }

    [Fact]
    public void IsFeatureAllowed_ReadOnly_TurnsOff_AllFeaturesExceptClipboard()
    {
        // Clipboard is intentionally decoupled from ReadOnly so view-only users can
        // still copy data unless the host explicitly sets AllowClipboard to false.
        using var ctx = CreateContext();
        var c = ctx.RenderComponent<RadzenSpreadsheet>(p =>
        {
            p.Add(x => x.Workbook, NewWorkbook());
            p.Add(x => x.ReadOnly, true);
        });

        foreach (SpreadsheetFeature f in System.Enum.GetValues(typeof(SpreadsheetFeature)))
        {
            if (f == SpreadsheetFeature.Clipboard)
            {
                Assert.True(c.Instance.IsFeatureAllowed(f), "Clipboard should stay on under ReadOnly");
            }
            else
            {
                Assert.False(c.Instance.IsFeatureAllowed(f), $"Feature {f} should be off in ReadOnly mode");
            }
        }
    }

    [Fact]
    public void IsFeatureAllowed_AllowClipboard_False_TurnsOff_Clipboard_EvenWhenNotReadOnly()
    {
        using var ctx = CreateContext();
        var c = ctx.RenderComponent<RadzenSpreadsheet>(p =>
        {
            p.Add(x => x.Workbook, NewWorkbook());
            p.Add(x => x.AllowClipboard, false);
        });

        Assert.False(c.Instance.IsFeatureAllowed(SpreadsheetFeature.Clipboard));
    }

    [Fact]
    public void IsFeatureAllowed_MapsToCorrectAllowParameter()
    {
        using var ctx = CreateContext();
        var c = ctx.RenderComponent<RadzenSpreadsheet>(p =>
        {
            p.Add(x => x.Workbook, NewWorkbook());
            p.Add(x => x.AllowCharts, false);
        });

        Assert.False(c.Instance.IsFeatureAllowed(SpreadsheetFeature.Charts));
        Assert.True(c.Instance.IsFeatureAllowed(SpreadsheetFeature.Editing));
        Assert.True(c.Instance.IsFeatureAllowed(SpreadsheetFeature.CellFormatting));
    }

    // ── CommandExecuting / PreventDefault ───────────────────────────────

    [Fact]
    public async Task CommandExecuting_Fires_BeforeCommandRuns()
    {
        using var ctx = CreateContext();
        var wb = NewWorkbook();
        var sheet = wb.Sheets[0];
        sheet.Cells["A1"].Value = "before";

        ICommand seen = null;
        var c = ctx.RenderComponent<RadzenSpreadsheet>(p =>
        {
            p.Add(x => x.Workbook, wb);
            p.Add(x => x.CommandExecuting, EventCallbackFactory.Create<SpreadsheetCommandEventArgs>(args =>
            {
                seen = args.Command;
                // When this fires, the command MUST NOT have run yet.
                Assert.Equal("before", sheet.Cells["A1"].Value);
            }));
        });

        var cmd = new ClearContentsCommand(sheet, RangeRef.Parse("A1"));
        var ran = await Run(c,cmd);

        Assert.True(ran);
        Assert.Same(cmd, seen);
        Assert.Null(sheet.Cells["A1"].Value);
    }

    [Fact]
    public async Task PreventDefault_Sync_RejectsCommand()
    {
        using var ctx = CreateContext();
        var wb = NewWorkbook();
        var sheet = wb.Sheets[0];
        sheet.Cells["A1"].Value = "keep";

        var c = ctx.RenderComponent<RadzenSpreadsheet>(p =>
        {
            p.Add(x => x.Workbook, wb);
            p.Add(x => x.CommandExecuting, EventCallbackFactory.Create<SpreadsheetCommandEventArgs>(args => args.PreventDefault()));
        });

        var ran = await Run(c,new ClearContentsCommand(sheet, RangeRef.Parse("A1")));

        Assert.False(ran);
        Assert.Equal("keep", sheet.Cells["A1"].Value);
    }

    [Fact]
    public async Task PreventDefault_Async_RejectsCommand()
    {
        // Whole point of the async refactor: PreventDefault must work after awaits.
        using var ctx = CreateContext();
        var wb = NewWorkbook();
        var sheet = wb.Sheets[0];
        sheet.Cells["A1"].Value = "keep";

        var c = ctx.RenderComponent<RadzenSpreadsheet>(p =>
        {
            p.Add(x => x.Workbook, wb);
            p.Add(x => x.CommandExecuting, EventCallbackFactory.Create<SpreadsheetCommandEventArgs>(async args =>
            {
                await Task.Yield();
                args.PreventDefault();
            }));
        });

        var ran = await Run(c,new ClearContentsCommand(sheet, RangeRef.Parse("A1")));

        Assert.False(ran);
        Assert.Equal("keep", sheet.Cells["A1"].Value);
    }

    [Fact]
    public async Task PreventDefault_OnlyForMatchingCommand()
    {
        // Veto FormatCommand but let ClearContentsCommand through.
        using var ctx = CreateContext();
        var wb = NewWorkbook();
        var sheet = wb.Sheets[0];
        sheet.Cells["A1"].Value = "hello";

        var c = ctx.RenderComponent<RadzenSpreadsheet>(p =>
        {
            p.Add(x => x.Workbook, wb);
            p.Add(x => x.CommandExecuting, EventCallbackFactory.Create<SpreadsheetCommandEventArgs>(args =>
            {
                if (args.Command is FormatCommand)
                {
                    args.PreventDefault();
                }
            }));
        });

        var formatRan = await Run(c, new FormatCommand(sheet, RangeRef.Parse("A1"), new Format { Bold = true }));
        var clearRan = await Run(c, new ClearContentsCommand(sheet, RangeRef.Parse("A1")));

        Assert.False(formatRan);
        Assert.True(clearRan);
        Assert.Null(sheet.Cells["A1"].Value);
    }

    [Fact]
    public async Task CommandExecuting_NotFired_WhenReadOnly()
    {
        // ReadOnly is the first gate; the event should not fire for rejected commands.
        using var ctx = CreateContext();
        var wb = NewWorkbook();
        var sheet = wb.Sheets[0];

        var fired = false;
        var c = ctx.RenderComponent<RadzenSpreadsheet>(p =>
        {
            p.Add(x => x.Workbook, wb);
            p.Add(x => x.ReadOnly, true);
            p.Add(x => x.CommandExecuting, EventCallbackFactory.Create<SpreadsheetCommandEventArgs>(_ => fired = true));
        });

        var ran = await Run(c,new ClearContentsCommand(sheet, RangeRef.Parse("A1")));

        Assert.False(ran);
        Assert.False(fired);
    }

    [Fact]
    public async Task CommandExecuting_NotFired_WhenAllowFlagDisabled()
    {
        // Allow* check happens before the event, same shape as ReadOnly.
        using var ctx = CreateContext();
        var wb = NewWorkbook();
        var sheet = wb.Sheets[0];

        var fired = false;
        var c = ctx.RenderComponent<RadzenSpreadsheet>(p =>
        {
            p.Add(x => x.Workbook, wb);
            p.Add(x => x.AllowCharts, false);
            p.Add(x => x.CommandExecuting, EventCallbackFactory.Create<SpreadsheetCommandEventArgs>(_ => fired = true));
        });

        var chart = new SheetChart { From = new CellAnchor { Row = 0, Column = 0 } };
        var ran = await Run(c,new InsertChartCommand(sheet, chart));

        Assert.False(ran);
        Assert.False(fired);
    }

    private static class EventCallbackFactory
    {
        // Bunit doesn't expose Microsoft.AspNetCore.Components.EventCallbackFactory directly,
        // so we wrap the typed factory with a tiny helper for readability.
        public static Microsoft.AspNetCore.Components.EventCallback<T> Create<T>(System.Action<T> handler)
            => new Microsoft.AspNetCore.Components.EventCallbackFactory().Create<T>(new object(), handler);

        public static Microsoft.AspNetCore.Components.EventCallback<T> Create<T>(System.Func<T, Task> handler)
            => new Microsoft.AspNetCore.Components.EventCallbackFactory().Create<T>(new object(), handler);
    }
}
