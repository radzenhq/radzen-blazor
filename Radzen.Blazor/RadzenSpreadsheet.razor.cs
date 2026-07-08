using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Radzen.Blazor.Spreadsheet;
using Radzen.Documents.Spreadsheet;
using Radzen.Blazor.Rendering;
using Radzen;
using System.Linq;

namespace Radzen.Blazor;

#nullable enable

/// <summary>
/// Exposes some of the methods of the RadzenSpreadsheet component.
/// </summary>
public interface ISpreadsheet
{
    /// <summary>
    /// Accepts the current edit in the spreadsheet.
    /// </summary>
    /// <returns></returns>
    Task<bool> AcceptAsync();

    /// <summary>
    /// Replaces the displayed workbook, resets the active sheet and cached view, and
    /// raises <c>WorkbookChanged</c>. Used by the Open tool so it works in any toolbar.
    /// </summary>
    /// <param name="workbook">The workbook to display.</param>
    Task LoadWorkbookAsync(Workbook workbook);

    /// <summary>
    /// Executes a command through the active view's undo/redo stack. Returns
    /// <c>true</c> when the command ran, <c>false</c> when it was rejected by
    /// read-only mode, an <c>Allow*</c> flag, sheet protection, or a
    /// <c>CommandExecuting</c> handler that called <c>PreventDefault()</c>.
    /// </summary>
    Task<bool> ExecuteAsync(ICommand command);

    /// <summary>
    /// Undoes the last command.
    /// </summary>
    void Undo();

    /// <summary>
    /// Redoes the last undone command.
    /// </summary>
    void Redo();

    /// <summary>
    /// Gets the UI culture used for localized strings. Defaults to <see cref="System.Globalization.CultureInfo.CurrentUICulture"/>.
    /// </summary>
    System.Globalization.CultureInfo UICulture => System.Globalization.CultureInfo.CurrentUICulture;

    /// <summary>
    /// Gets whether there is a command to undo.
    /// </summary>
    bool CanUndo { get; }

    /// <summary>
    /// Gets whether there is a command to redo.
    /// </summary>
    bool CanRedo { get; }

    /// <summary>
    /// Gets whether the spreadsheet is in read-only mode. When <c>true</c>, every
    /// mutation is rejected regardless of per-feature <c>Allow*</c> flags.
    /// </summary>
    bool ReadOnly { get; }

    /// <summary>
    /// Returns <c>true</c> when the given feature is enabled on the host
    /// <see cref="RadzenSpreadsheet"/>. Tools should use this to decide whether
    /// they should appear disabled.
    /// </summary>
    /// <param name="feature">The feature to check.</param>
    bool IsFeatureAllowed(SpreadsheetFeature feature);

    /// <summary>
    /// Returns a localized string for the specified key.
    /// </summary>
    /// <param name="key">The resource key.</param>
    /// <returns>The localized string.</returns>
    string Localize(string key);
}

/// <summary>
/// A spreadsheet component that allows users to view and edit workbooks.
/// It supports features like cell selection, editing, and keyboard shortcuts.
/// The component can display a workbook with multiple sheets and allows users to navigate through cells using keyboard shortcuts.
/// It also supports mouse interactions for selecting and editing cells, rows, and columns.
/// </summary>
public partial class RadzenSpreadsheet : RadzenComponent, IAsyncDisposable, ISpreadsheet
{
    private Workbook? workbook;

    /// <summary>
    /// The workbook to display in the spreadsheet.
    /// </summary>
    [Parameter]
    public Workbook? Workbook { get; set; }

    /// <summary>
    /// The name of the file to export the workbook to when using the export functionality.
    /// When the user picks "Save as CSV" the extension is replaced with <c>.csv</c>.
    /// </summary>
    [Parameter]
    public string ExportFileName { get; set; } = "sheet.xlsx";

    /// <summary>
    /// Options applied when the user exports the workbook as CSV. When null, defaults are
    /// used (comma separator, UTF-8 with BOM, CRLF line endings, RFC 4180 minimal quoting,
    /// active sheet only).
    /// </summary>
    [Parameter]
    public CsvExportOptions? CsvExportOptions { get; set; }

    /// <summary>
    /// Options applied when the user opens a CSV file. When null, defaults are used
    /// (comma separator, UTF-8, value and formula auto-detection on).
    /// </summary>
    [Parameter]
    public CsvImportOptions? CsvImportOptions { get; set; }

    /// <summary>
    /// Event callback that is invoked when the workbook changes.
    /// </summary>
    [Parameter]
    public EventCallback<Workbook?> WorkbookChanged { get; set; }

    /// <summary>
    /// Gets or sets the custom cell type definitions.
    /// Maps cell type names to their renderer and editor component types.
    /// </summary>
    [Parameter]
    public Dictionary<string, Spreadsheet.SpreadsheetCellType>? CellTypes { get; set; }

    /// <summary>
    /// The index of the active toolset in the toolbar. Defaults to <c>1</c> so the
    /// Home toolset is shown first. Supports two-way binding via
    /// <c>@bind-SelectedToolsetIndex</c>.
    /// </summary>
    [Parameter]
    public int SelectedToolsetIndex { get; set; } = 1;

    /// <summary>
    /// Fired when the active toolset changes. Used by <c>@bind-SelectedToolsetIndex</c>.
    /// </summary>
    [Parameter]
    public EventCallback<int> SelectedToolsetIndexChanged { get; set; }

    /// <summary>
    /// When <c>true</c>, the spreadsheet rejects every command that mutates the
    /// workbook. The user can still select cells, scroll, and copy. Defaults to
    /// <c>false</c>.
    /// </summary>
    [Parameter]
    public bool ReadOnly { get; set; }

    /// <summary>
    /// When <c>true</c> (the default) the toolbar is rendered above the grid.
    /// Set to <c>false</c> for kiosk or view-only embeds.
    /// </summary>
    [Parameter]
    public bool ShowToolbar { get; set; } = true;

    /// <summary>
    /// When <c>true</c> (the default) the formula bar is rendered between the toolbar
    /// and the grid.
    /// </summary>
    [Parameter]
    public bool ShowFormulaBar { get; set; } = true;

    /// <summary>
    /// Gets or sets the accessible label (<c>aria-label</c>) announced for the spreadsheet's
    /// <c>role="application"</c> region. Defaults to a localized "Spreadsheet".
    /// </summary>
    [Parameter]
    public string? AriaLabel { get; set; }

    /// <summary>
    /// When <c>true</c> (the default) the sheet tab strip is rendered below the grid.
    /// </summary>
    [Parameter]
    public bool ShowSheetTabs { get; set; } = true;

    /// <summary>
    /// The zero-based index of the active sheet. Supports two-way binding via
    /// <c>@bind-SelectedSheetIndex</c>. Values outside the sheet range are clamped.
    /// Setting it selects the matching sheet even when <see cref="ShowSheetTabs"/> is
    /// <c>false</c>; loading a different <see cref="Workbook"/> resets it to the bound value
    /// (or <c>0</c> when unbound).
    /// </summary>
    [Parameter]
    public int SelectedSheetIndex { get; set; }

    /// <summary>
    /// Fired when the active sheet changes. Used by <c>@bind-SelectedSheetIndex</c>.
    /// </summary>
    [Parameter]
    public EventCallback<int> SelectedSheetIndexChanged { get; set; }

    /// <summary>Allows direct cell editing (type-to-edit, double-click, paste-into-cell, delete-key, autoaccept).</summary>
    [Parameter] public bool AllowEditing { get; set; } = true;

    /// <summary>Allows filter and auto-filter commands and the filter UI affordances.</summary>
    [Parameter] public bool AllowFiltering { get; set; } = true;

    /// <summary>Allows single- and multi-key sort commands.</summary>
    [Parameter] public bool AllowSorting { get; set; } = true;

    /// <summary>Allows drag-to-fill (autofill) gestures.</summary>
    [Parameter] public bool AllowAutofill { get; set; } = true;

    /// <summary>Allows cell merge and unmerge commands.</summary>
    [Parameter] public bool AllowMerge { get; set; } = true;

    /// <summary>Allows row and column resize gestures.</summary>
    [Parameter] public bool AllowResizing { get; set; } = true;

    /// <summary>Allows font, color, alignment, and border formatting commands.</summary>
    [Parameter] public bool AllowCellFormatting { get; set; } = true;

    /// <summary>Allows inserting, editing, and following hyperlinks.</summary>
    [Parameter] public bool AllowHyperlinks { get; set; } = true;

    /// <summary>Allows inserting, moving, resizing, and deleting images.</summary>
    [Parameter] public bool AllowImages { get; set; } = true;

    /// <summary>Allows inserting, editing, moving, resizing, and deleting charts.</summary>
    [Parameter] public bool AllowCharts { get; set; } = true;

    /// <summary>Allows creating, editing, and removing structured tables.</summary>
    [Parameter] public bool AllowTables { get; set; } = true;

    /// <summary>Allows adding and clearing data-validation rules.</summary>
    [Parameter] public bool AllowDataValidation { get; set; } = true;

    /// <summary>Allows adding and clearing conditional formatting rules.</summary>
    [Parameter] public bool AllowConditionalFormatting { get; set; } = true;

    /// <summary>
    /// Allows cut, copy, and paste through the system clipboard. Independent of
    /// <see cref="ReadOnly"/>, so view-only users can still copy unless this is set
    /// to <c>false</c>. Cut and paste also require <see cref="AllowEditing"/>.
    /// </summary>
    [Parameter] public bool AllowClipboard { get; set; } = true;

    /// <summary>Allows undo and redo of previously executed commands.</summary>
    [Parameter] public bool AllowUndoRedo { get; set; } = true;

    /// <summary>
    /// Fires before a command is pushed onto the undo stack. Call
    /// <see cref="SpreadsheetCommandEventArgs.PreventDefault"/> from the handler to veto
    /// the command. Fires after <see cref="ReadOnly"/>, the matching <c>Allow*</c> flag,
    /// and the sheet's <c>Protection</c> have already approved the command.
    /// </summary>
    [Parameter]
    public EventCallback<SpreadsheetCommandEventArgs> CommandExecuting { get; set; }

    /// <summary>
    /// Replaces the built-in toolsets. When set, the supplied content sits inside the
    /// toolbar's <see cref="RadzenTabs.Tabs"/> slot — each child should be a
    /// <see cref="RadzenTabsItem"/>. Add
    /// <see cref="RadzenSpreadsheetTableDesignToolset"/> to keep the
    /// contextual "Table Design" toolset.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <inheritdoc/>
    bool ISpreadsheet.ReadOnly => ReadOnly;

    /// <inheritdoc/>
    bool ISpreadsheet.IsFeatureAllowed(SpreadsheetFeature feature) => IsFeatureAllowed(feature);

    /// <summary>
    /// Returns <c>true</c> when the given feature is enabled. <see cref="ReadOnly"/>
    /// forces every feature off except <see cref="SpreadsheetFeature.Clipboard"/>, which
    /// stays governed solely by <see cref="AllowClipboard"/> so view-only users can still
    /// copy data unless the host explicitly opts out.
    /// </summary>
    /// <param name="feature">The feature to check.</param>
    public bool IsFeatureAllowed(SpreadsheetFeature feature)
    {
        // Clipboard is decoupled from ReadOnly. Cut and Paste require both Clipboard
        // and Editing, so ReadOnly still blocks them through the Editing gate.
        if (feature == SpreadsheetFeature.Clipboard)
        {
            return AllowClipboard;
        }

        if (ReadOnly)
        {
            return false;
        }

        return feature switch
        {
            SpreadsheetFeature.Editing => AllowEditing,
            SpreadsheetFeature.Filtering => AllowFiltering,
            SpreadsheetFeature.Sorting => AllowSorting,
            SpreadsheetFeature.Autofill => AllowAutofill,
            SpreadsheetFeature.Merging => AllowMerge,
            SpreadsheetFeature.Resizing => AllowResizing,
            SpreadsheetFeature.CellFormatting => AllowCellFormatting,
            SpreadsheetFeature.Hyperlinks => AllowHyperlinks,
            SpreadsheetFeature.Images => AllowImages,
            SpreadsheetFeature.Charts => AllowCharts,
            SpreadsheetFeature.Tables => AllowTables,
            SpreadsheetFeature.DataValidation => AllowDataValidation,
            SpreadsheetFeature.ConditionalFormatting => AllowConditionalFormatting,
            SpreadsheetFeature.Clipboard => AllowClipboard,
            SpreadsheetFeature.UndoRedo => AllowUndoRedo,
            _ => true,
        };
    }

    /// <inheritdoc/>
    protected override string GetComponentCssClass() => "rz-spreadsheet";

    private VirtualGrid? grid;
    private Spreadsheet.SpreadsheetAccessibility? accessibility;
    private RadzenPopup? cellMenuPopup;
    private RadzenPopup? validationListPopup;
    private RangePickerBar? rangePickerBar;
    private int cellMenuRow = -1;
    private int cellMenuColumn = -1;
    private int validationListRow = -1;
    private int validationListColumn = -1;
    private IReadOnlyList<string> validationListItems = [];
    private RangeRef autofillSource = RangeRef.Invalid;
    private PointerEventArgs? autofillStartPointer;

    /// <inheritdoc/>
    public override async Task SetParametersAsync(ParameterView parameters)
    {
        var didWorkbookChange = parameters.DidParameterChange(nameof(Workbook), Workbook);
        var didSheetIndexChange = parameters.DidParameterChange(nameof(SelectedSheetIndex), SelectedSheetIndex);

        await base.SetParametersAsync(parameters);

        if (didWorkbookChange)
        {
            SetActiveWorkbook(Workbook, SelectedSheetIndex);
        }
        else if (didSheetIndexChange)
        {
            SetActiveSheet(SelectedSheetIndex);
        }
    }

    private int sheetIndex;

    private Worksheet? Worksheet => workbook != null && sheetIndex < workbook.Sheets.Count ? workbook.Sheets[sheetIndex] : null;

    private WorkbookView? workbookView;

    private SheetView? ActiveView => Worksheet != null ? (workbookView ??= new WorkbookView(workbook!)).GetView(Worksheet) : null;

    private Editor? Editor => ActiveView?.Editor;

    /// <inheritdoc/>
    public async Task<bool> ExecuteAsync(ICommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (ReadOnly)
        {
            return false;
        }

        if (command.Feature is SpreadsheetFeature feature && !IsFeatureAllowed(feature))
        {
            return false;
        }

        if (command is IProtectedCommand pc && Worksheet?.Protection.IsActionBlocked(pc.RequiredAction) == true)
        {
            // PasteCommand is a special case: it bypasses the sheet-level EditCell block
            // because it validates locking at the cell level during DoExecute().
            if (command is not PasteCommand)
            {
                return false;
            }
        }

        if (CommandExecuting.HasDelegate)
        {
            var args = new SpreadsheetCommandEventArgs(command);
            await CommandExecuting.InvokeAsync(args);
            if (args.DefaultPrevented)
            {
                return false;
            }
        }

        var executed = ActiveView?.Commands.Execute(command) ?? false;

        if (executed && DescribeCommand(command) is string message)
        {
            accessibility?.AnnounceAction(message);
        }

        return executed;
    }

    /// <inheritdoc/>
    public void Undo()
    {
        if (ReadOnly || !AllowUndoRedo)
        {
            return;
        }

        ActiveView?.Commands.Undo();
    }

    /// <inheritdoc/>
    public void Redo()
    {
        if (ReadOnly || !AllowUndoRedo)
        {
            return;
        }

        ActiveView?.Commands.Redo();
    }

    /// <inheritdoc/>
    public bool CanUndo => !ReadOnly && AllowUndoRedo && (ActiveView?.Commands.CanUndo ?? false);

    /// <inheritdoc/>
    public bool CanRedo => !ReadOnly && AllowUndoRedo && (ActiveView?.Commands.CanRedo ?? false);

    /// <inheritdoc/>
    public async Task LoadWorkbookAsync(Workbook workbook)
    {
        var previous = sheetIndex;
        SetActiveWorkbook(workbook, 0);

        await CloseMenusAsync();
        cellMenuRow = cellMenuColumn = -1;
        validationListRow = validationListColumn = -1;

        StateHasChanged();

        await WorkbookChanged.InvokeAsync(workbook);
        await NotifySelectedSheetIndexChangedAsync(previous);
    }

    private void SetActiveWorkbook(Workbook? value, int index)
    {
        workbook = value;
        workbookView = null;
        SetActiveSheet(index);
    }

    private void SetActiveSheet(int index)
    {
        var count = workbook?.Sheets.Count ?? 0;
        sheetIndex = count == 0 ? 0 : Math.Clamp(index, 0, count - 1);

        if (Worksheet?.Selection.Cell == CellRef.Invalid)
        {
            Worksheet.Selection.Select(new CellRef(0, 0));
        }
    }

    private async Task SelectSheetAsync(int index)
    {
        var previous = sheetIndex;
        SetActiveSheet(index);
        await NotifySelectedSheetIndexChangedAsync(previous);
    }

    private async Task NotifySelectedSheetIndexChangedAsync(int previous)
    {
        if (sheetIndex != previous)
        {
            await SelectedSheetIndexChanged.InvokeAsync(sheetIndex);
        }
    }

    private async Task CloseMenusAsync()
    {
        if (cellMenuPopup != null)
        {
            await cellMenuPopup.CloseAsync();
        }

        if (validationListPopup != null)
        {
            await validationListPopup.CloseAsync();
        }
    }

    private async Task OnSheetTabChanged(int index)
    {
        await AcceptAsync();

        await CloseMenusAsync();

        await SelectSheetAsync(index);
    }

    private async Task OnAddSheetAsync()
    {
        await AcceptAsync();

        if (ReadOnly || workbook is null || workbook.Protection.LockStructure)
        {
            return;
        }

        var name = GenerateSheetName();
        workbook.AddSheet(name, 100, 26);
        await SelectSheetAsync(workbook.Sheets.Count - 1);
    }

    private async Task OnSheetAction(RadzenSplitButtonItem? item, Worksheet sheet)
    {
        if (item is null)
        {
            return;
        }

        switch (item.Value)
        {
            case "rename":
                await OnRenameSheetAsync(sheet);
                break;
            case "delete":
                await OnRemoveSheetAsync(sheet);
                break;
            case "move-left":
                await OnMoveSheetLeftAsync(sheet);
                break;
            case "move-right":
                await OnMoveSheetRightAsync(sheet);
                break;
        }
    }

    private async Task OnRemoveSheetAsync(Worksheet sheet)
    {
        await AcceptAsync();

        if (ReadOnly || workbook is null || workbook.Sheets.Count <= 1 || workbook.Protection.LockStructure)
        {
            return;
        }

        var removedIndex = workbook.IndexOf(sheet);
        workbook.RemoveSheet(sheet);
        workbookView?.Remove(sheet);

        // Re-activate even when the index stays the same: deleting the active, non-last sheet shifts a
        // different sheet into that index, which still needs its active cell ensured.
        await SelectSheetAsync(
            sheetIndex >= workbook.Sheets.Count ? workbook.Sheets.Count - 1 :
            removedIndex < sheetIndex ? sheetIndex - 1 :
            sheetIndex);
    }

    private async Task OnRenameSheetAsync(Worksheet sheet)
    {
        if (ReadOnly || workbook?.Protection.LockStructure == true)
        {
            return;
        }

        var existingNames = workbook!.Sheets
            .Where(s => s != sheet)
            .Select(s => s.Name)
            .ToList();

        var name = await OpenDialogAsync<Spreadsheet.RenameSheetDialog>(Localize(nameof(RadzenStrings.Spreadsheet_RenameSheetTitle)),
            new Dictionary<string, object?> { { "Name", sheet.Name }, { "ExistingNames", existingNames } },
            new DialogOptions { Width = "300px" });

        if (name is string newName && !string.IsNullOrWhiteSpace(newName))
        {
            sheet.Name = newName;
        }
    }

    private async Task OnMoveSheetLeftAsync(Worksheet sheet)
    {
        if (ReadOnly || workbook is null || workbook.Protection.LockStructure)
        {
            return;
        }

        var index = workbook.IndexOf(sheet);

        if (index > 0)
        {
            var previous = sheetIndex;
            workbook.MoveSheet(index, index - 1);

            if (sheetIndex == index)
            {
                sheetIndex--;
            }
            else if (sheetIndex == index - 1)
            {
                sheetIndex++;
            }

            await NotifySelectedSheetIndexChangedAsync(previous);
        }
    }

    private async Task OnMoveSheetRightAsync(Worksheet sheet)
    {
        if (ReadOnly || workbook is null || workbook.Protection.LockStructure)
        {
            return;
        }

        var index = workbook.IndexOf(sheet);

        if (index < workbook.Sheets.Count - 1)
        {
            var previous = sheetIndex;
            workbook.MoveSheet(index, index + 1);

            if (sheetIndex == index)
            {
                sheetIndex++;
            }
            else if (sheetIndex == index + 1)
            {
                sheetIndex--;
            }

            await NotifySelectedSheetIndexChangedAsync(previous);
        }
    }

    private string GenerateSheetName()
    {
        var index = 1;

        while (true)
        {
            var name = $"Sheet{index}";

            if (workbook!.GetSheet(name) == null)
            {
                return name;
            }

            index++;
        }
    }

    private async Task OnCellToggleAsync(CellMenuToggleEventArgs args)
    {
        ArgumentNullException.ThrowIfNull(args);

        // Check if this cell has list validation
        if (Worksheet != null)
        {
            var validators = Worksheet.Validation.GetValidatorsForCell(new CellRef(args.Row, args.Column));
            foreach (var v in validators)
            {
                if (v is DataValidationRule rule && rule.Type == DataValidationType.List)
                {
                    validationListRow = args.Row;
                    validationListColumn = args.Column;
                    validationListItems = rule.GetListItems(Worksheet);
                    if (validationListPopup != null)
                    {
                        await validationListPopup.ToggleAsync(args.Element);
                    }
                    return;
                }
            }
        }

        if (cellMenuPopup != null)
        {
            cellMenuRow = args.Row;
            cellMenuColumn = args.Column;
            await cellMenuPopup.ToggleAsync(args.Element);
        }
    }

    private string? GetValidationListValue()
    {
        if (Worksheet != null && validationListRow >= 0 && validationListColumn >= 0
            && Worksheet.Cells.TryGet(validationListRow, validationListColumn, out var cell))
        {
            return cell.Value?.ToString();
        }

        return null;
    }

    private async Task OnValidationListValueSelectedAsync(string value)
    {
        if (Worksheet != null && validationListRow >= 0 && validationListColumn >= 0)
        {
            var address = new CellRef(validationListRow, validationListColumn);
            Editor!.StartEdit(address, value);
            await AcceptAsync();
        }

        if (validationListPopup != null)
        {
            await validationListPopup.CloseAsync();
        }
    }

    private async Task OnCellMenuCancelAsync()
    {
        if (cellMenuPopup != null)
        {
            await cellMenuPopup.CloseAsync();
        }
    }

    private async Task OnCellMenuApplyAsync(SheetFilter? filter)
    {
        if (filter != null && Worksheet != null)
        {
            var command = new FilterCommand(Worksheet, filter);
            await ExecuteAsync(command);
        }

        if (cellMenuPopup != null)
        {
            await cellMenuPopup.CloseAsync();
        }
    }

    private async Task OnCellMenuSortAsync(SortOrder order)
    {
        if (Worksheet != null)
        {
            await ExecuteAsync(BuildSortCommand(cellMenuRow, cellMenuColumn, order));
        }

        if (cellMenuPopup != null)
        {
            await cellMenuPopup.CloseAsync();
        }
    }

    private SortCommand BuildSortCommand(int row, int column, SortOrder order)
    {
        foreach (var table in Worksheet!.Tables)
        {
            if (table.Range.Contains(row, column))
            {
                return new SortCommand(Worksheet, table.DataBodyRange, order, column);
            }
        }

        if (Worksheet.AutoFilter.Range is { } afRange && afRange.Contains(row, column))
        {
            return new SortCommand(Worksheet, afRange, order, column, skipHeaderRow: true);
        }

        return new SortCommand(Worksheet, new RangeRef(new CellRef(0, 0), new CellRef(Worksheet.RowCount - 1, Worksheet.ColumnCount - 1)), order, column);
    }

    private async Task OnCellMenuClearAsync()
    {
        if (Worksheet != null)
        {
            // Remove all filters that affect the current column
            var filtersToRemove = new List<SheetFilter>();

            foreach (var filter in Worksheet.Filters)
            {
                if (filter.Range.Contains(cellMenuRow, cellMenuColumn))
                {
                    filtersToRemove.Add(filter);
                }
            }

            // Execute remove commands for each filter
            foreach (var filter in filtersToRemove)
            {
                var command = new RemoveFilterCommand(Worksheet, filter);
                await ExecuteAsync(command);
            }
        }

        if (cellMenuPopup != null)
        {
            await cellMenuPopup.CloseAsync();
        }
    }

    private async Task OnCellMenuCustomFilterAsync()
    {
        if (cellMenuPopup != null)
        {
            await cellMenuPopup.CloseAsync();
        }

        if (Worksheet != null)
        {
            FilterCriterion? existingFilter = Worksheet.Filters.FirstOrDefault(f => f.Range.Contains(cellMenuRow, cellMenuColumn))?.Criterion;


            var parameters = new Dictionary<string, object?>
            {
                { nameof(FilterDialog.Worksheet), Worksheet },
                { nameof(FilterDialog.Column), cellMenuColumn },
                { nameof(FilterDialog.Row), cellMenuRow }
            };

            if (existingFilter != null)
            {
                parameters.Add(nameof(FilterDialog.Filter), existingFilter);
            }

            var result = await OpenDialogAsync<FilterDialog>(Localize(nameof(RadzenStrings.Spreadsheet_CustomFilterTitle)), parameters, new DialogOptions
            {
                Width = "600px",
            });

            if (result is SheetFilter filter)
            {
                var command = new FilterCommand(Worksheet, filter);
                await ExecuteAsync(command);
            }
        }
    }

    private async Task OnCellMenuTop10FilterAsync()
    {
        if (cellMenuPopup != null)
        {
            await cellMenuPopup.CloseAsync();
        }

        if (Worksheet is null)
        {
            return;
        }

        var range = ResolveColumnFilterRange();
        if (range == RangeRef.Invalid)
        {
            return;
        }

        var result = await OpenDialogAsync<Spreadsheet.Top10FilterDialog>(
            Localize(nameof(RadzenStrings.Spreadsheet_Top10Filter)),
            null,
            new DialogOptions { Width = "380px", ShowClose = true });

        if (result is Spreadsheet.Top10FilterDialog.Result r)
        {
            var sliceRange = new RangeRef(
                new CellRef(range.Start.Row, cellMenuColumn),
                new CellRef(range.End.Row, cellMenuColumn));
            var criterion = new TopFilterCriterion
            {
                Column = cellMenuColumn,
                Count = r.Count,
                Percent = r.Percent,
                Bottom = r.Bottom,
            };
            await ExecuteAsync(new FilterCommand(Worksheet, new SheetFilter(criterion, sliceRange)));
        }
    }

    private async Task OnCellMenuDynamicFilterAsync(DynamicFilterType type)
    {
        if (Worksheet is null)
        {
            return;
        }

        if (cellMenuPopup != null)
        {
            await cellMenuPopup.CloseAsync();
        }

        var range = ResolveColumnFilterRange();
        if (range == RangeRef.Invalid)
        {
            return;
        }

        var sliceRange = new RangeRef(
            new CellRef(range.Start.Row, cellMenuColumn),
            new CellRef(range.End.Row, cellMenuColumn));
        var criterion = new DynamicFilterCriterion { Column = cellMenuColumn, Type = type };
        await ExecuteAsync(new FilterCommand(Worksheet, new SheetFilter(criterion, sliceRange)));
    }

    private RangeRef ResolveColumnFilterRange()
    {
        if (Worksheet is null)
        {
            return RangeRef.Invalid;
        }

        foreach (var t in Worksheet.Tables)
        {
            if (t.Range.Contains(cellMenuRow, cellMenuColumn))
            {
                return t.Range;
            }
        }
        if (Worksheet.AutoFilter.Range is { } afRange && afRange.Contains(cellMenuRow, cellMenuColumn))
        {
            return afRange;
        }
        return RangeRef.Invalid;
    }

    private async Task OnFormatCellsAsync()
    {
        if (Worksheet != null && Worksheet.Selection.Cell != CellRef.Invalid &&
            Worksheet.Cells.TryGet(Worksheet.Selection.Cell.Row, Worksheet.Selection.Cell.Column, out var cell))
        {
            var parameters = new Dictionary<string, object?>
            {
                { nameof(FormatCellsDialog.CurrentFormat), cell.Format.NumberFormat },
                { nameof(FormatCellsDialog.SampleValue), cell.Value ?? 1234.5 },
                { nameof(FormatCellsDialog.ValueType), cell.ValueType }
            };

            var result = await OpenDialogAsync<FormatCellsDialog>(
                Localize(nameof(RadzenStrings.Spreadsheet_FormatCellsTitle)), parameters, new DialogOptions { Width = "600px" });

            if (result is string formatCode)
            {
                var format = string.Equals(formatCode, "General", StringComparison.OrdinalIgnoreCase) ? null : formatCode;
                var command = new FormatCommand(Worksheet, Worksheet.Selection.Range, cell.Format.WithNumberFormat(format));
                await ExecuteAsync(command);
            }
        }
    }

    private async Task MoveSelectionAsync(int rowOffset, int columnOffset)
    {
        if (Worksheet is not null)
        {
            var address = Worksheet.Selection.Move(rowOffset, columnOffset);
            await ScrollToAsync(address);
        }
    }

    private bool isAccepting;

    /// <summary>
    /// Accepts the current edit in the spreadsheet.
    /// </summary>
    public async Task<bool> AcceptAsync()
    {
        var result = true;

        if (isAccepting)
        {
            return result;
        }

        isAccepting = true;

        if (Worksheet is not null)
        {
            if (Editor!.HasChanges)
            {
                var command = new AcceptEditCommand(ActiveView!);

                var valid = await ExecuteAsync(command);

                if (!valid && Editor!.Cell is not null)
                {
                    var error = string.Join(Environment.NewLine, Editor!.Cell.ValidationErrors);
                    var errorStyle = Worksheet.Validation.GetErrorStyleForCell(Editor!.Cell.Address);

                    switch (errorStyle)
                    {
                        case DataValidationErrorStyle.Information:
                            await DialogService.Alert(error, Localize(nameof(RadzenStrings.Spreadsheet_InformationTitle)));
                            // Accept the value despite validation failure - keep validation errors to show indicator
                            Editor!.EndEdit();
                            break;

                        case DataValidationErrorStyle.Warning:
                            var confirmed = await DialogService.Confirm(
                                error + Environment.NewLine + Environment.NewLine + Localize(nameof(RadzenStrings.Spreadsheet_DoYouWantToContinue)),
                                Localize(nameof(RadzenStrings.Spreadsheet_WarningTitle)));

                            if (confirmed == true)
                            {
                                // Accept the value despite validation failure - keep validation errors to show indicator
                                Editor!.EndEdit();
                            }
                            else
                            {
                                Editor!.Cell.ClearValidationErrors();
                                command.Unexecute();
                                Editor!.Cancel();
                                result = false;
                            }
                            break;

                        default: // Stop
                            await DialogService.Alert(error, Localize(nameof(RadzenStrings.Spreadsheet_InvalidValueTitle)));
                            Editor!.Cell.ClearValidationErrors();
                            command.Unexecute();
                            Editor!.Cancel();
                            result = false;
                            break;
                    }

                    // Return focus to the grid after the validation dialog closes (WCAG 2.4.3).
                    await Element.FocusAsync();
                }
                else
                {
                    await Element.FocusAsync();
                }
            }
            else
            {
                Editor!.EndEdit();
            }
        }

        isAccepting = false;

        return result;
    }

    private async Task CycleSelectionAsync(int rowOffset, int columnOffset)
    {
        // The formula bar is a persistent editor, so after Tab/Enter commits there focus stays in it
        // unless we move it back to the grid (Excel/Sheets parity). The in-cell editor closes on
        // commit, so it doesn't need this. AcceptAsync resets the mode, so capture it first.
        var fromFormulaBar = Editor?.Mode == EditMode.Formula;

        if (await AcceptAsync())
        {
            if (Worksheet is not null)
            {
                var address = Worksheet.Selection.Cycle(rowOffset, columnOffset);

                await ScrollToAsync(address);

                if (fromFormulaBar)
                {
                    await Element.FocusAsync();
                }
            }
        }
    }

    private async Task ExtendSelectionAsync(int rowOffset, int columnOffset)
    {
        if (Worksheet is not null)
        {
            var address = Worksheet.Selection.Extend(rowOffset, columnOffset);

            await ScrollToAsync(address);
        }
    }

    // Absolute-move keys (Home/End/Ctrl+Home/Ctrl+End): the mapper turns the current active cell
    // into the destination, which is then clamped to the sheet and selected.
    private async Task MoveToAsync(Func<CellRef, CellRef> targetOf)
    {
        if (Worksheet is null || !await AcceptAsync())
        {
            return;
        }

        var target = Worksheet.Clamp(targetOf(Worksheet.Selection.Cell));
        Worksheet.Selection.Select(target);
        await ScrollToAsync(target);
    }

    // Shift variants: extend the selection from the active cell to the mapped destination.
    private Task ExtendToAsync(Func<CellRef, CellRef> targetOf)
    {
        if (Worksheet is null)
        {
            return Task.CompletedTask;
        }

        var current = Worksheet.Selection.Cell;
        var target = Worksheet.Clamp(targetOf(current));
        return ExtendSelectionAsync(target.Row - current.Row, target.Column - current.Column);
    }

    private async Task SelectUsedRangeAsync()
    {
        if (Worksheet is null || !await AcceptAsync())
        {
            return;
        }

        Worksheet.Selection.Select(new RangeRef(new CellRef(0, 0), GetUsedEnd()));
        await ScrollToAsync(new CellRef(0, 0));
    }

    // The bottom-right corner of the populated range, used by Ctrl+End/Ctrl+A/End. Empty sheet -> A1.
    private CellRef GetUsedEnd()
    {
        var maxRow = 0;
        var maxColumn = 0;

        if (Worksheet is not null)
        {
            foreach (var cell in Worksheet.Cells.GetPopulatedCells())
            {
                if (cell.Address.Row > maxRow)
                {
                    maxRow = cell.Address.Row;
                }

                if (cell.Address.Column > maxColumn)
                {
                    maxColumn = cell.Address.Column;
                }
            }
        }

        return new CellRef(maxRow, maxColumn);
    }

    // Excel-style Ctrl+Arrow edge jump from 'from' in direction (dRow, dColumn), each +/-1 or 0:
    //   - on a data block: jump to the last contiguous populated cell;
    //   - otherwise: skip blanks to the next populated cell, or to the sheet edge if none.
    private CellRef FindEdge(CellRef from, int dRow, int dColumn)
    {
        if (Worksheet is null)
        {
            return from;
        }

        var rows = Worksheet.RowCount;
        var columns = Worksheet.ColumnCount;
        var r = from.Row;
        var c = from.Column;

        bool InBounds(int row, int column) => row >= 0 && row < rows && column >= 0 && column < columns;
        bool Has(int row, int column) => Worksheet.Cells.HasCell(row, column);

        if (!InBounds(r + dRow, c + dColumn))
        {
            return from;
        }

        if (Has(r, c) && Has(r + dRow, c + dColumn))
        {
            while (InBounds(r + dRow, c + dColumn) && Has(r + dRow, c + dColumn))
            {
                r += dRow;
                c += dColumn;
            }
        }
        else
        {
            // Skip blanks to the next populated cell; if there is none, this walks to the sheet edge.
            while (InBounds(r + dRow, c + dColumn))
            {
                r += dRow;
                c += dColumn;

                if (Has(r, c))
                {
                    break;
                }
            }
        }

        return new CellRef(r, c);
    }

    // Ctrl+D fills the multi-cell selection down from its top row; Ctrl+R fills right from its first column.
    private async Task FillAsync(bool down)
    {
        if (Worksheet is null)
        {
            return;
        }

        var range = Worksheet.Selection.Range;
        if (range.Collapsed)
        {
            return;
        }

        RangeRef source;
        AutofillDirection direction;

        if (down)
        {
            source = new RangeRef(range.Start, new CellRef(range.Start.Row, range.End.Column));
            direction = AutofillDirection.Down;
        }
        else
        {
            source = new RangeRef(range.Start, new CellRef(range.End.Row, range.Start.Column));
            direction = AutofillDirection.Right;
        }

        await ExecuteAsync(new AutofillCommand(Worksheet, source, range, direction));
    }

    private async Task SelectColumnAsync()
    {
        if (Worksheet is null || !await AcceptAsync())
        {
            return;
        }

        Worksheet.Selection.Select(new ColumnRef(Worksheet.Selection.Cell.Column));
        await ScrollToAsync(Worksheet.Selection.Cell);
    }

    private async Task SelectRowAsync()
    {
        if (Worksheet is null || !await AcceptAsync())
        {
            return;
        }

        Worksheet.Selection.Select(new RowRef(Worksheet.Selection.Cell.Row));
        await ScrollToAsync(Worksheet.Selection.Cell);
    }

    // Shift+F10 / the ContextMenu key open the cell context menu at the active cell. JS locates the
    // cell element and re-dispatches OnCellContextMenuAsync with a synthetic pointer at its position.
    private async Task OpenContextMenuAtActiveCellAsync()
    {
        if (Worksheet is null || jsRef is null || Worksheet.Selection.Cell == CellRef.Invalid)
        {
            return;
        }

        var cell = Worksheet.Selection.Cell;
        await jsRef.InvokeVoidAsync("openCellContextMenu", cell.Row, cell.Column);
    }

    // ── Image/chart keyboard layer (Excel object model) ─────────────────

    // Ctrl+Alt+5 selects the first drawing, or cycles when already in the layer.
    private Task EnterDrawingLayerAsync()
    {
        if (Worksheet is null)
        {
            return Task.CompletedTask;
        }

        if (Worksheet.SelectedImage is null && Worksheet.SelectedChart is null)
        {
            if (Worksheet.Images.Count > 0)
            {
                Worksheet.SelectedImage = Worksheet.Images[0];
            }
            else if (Worksheet.Charts.Count > 0)
            {
                Worksheet.SelectedChart = Worksheet.Charts[0];
            }

            AnnounceDrawing();
        }
        else
        {
            CycleDrawing(1);
        }

        return Task.CompletedTask;
    }

    private async Task<bool> TryHandleDrawingKeyAsync(KeyboardEventArgs args)
    {
        const double step = 8;
        const double fine = 1;

        switch (TranslateShortcut(args))
        {
            case "Escape": DeselectDrawing(); return true;
            case "Tab": CycleDrawing(1); return true;
            case "Shift+Tab": CycleDrawing(-1); return true;
            case "Enter": return true; // consume so it does not move the underlying cell
            case "Shift+F10":
            case "ContextMenu": await OpenDrawingSizeDialogAsync(); return true;
            case "ArrowUp": await MoveDrawingAsync(0, -step); return true;
            case "ArrowDown": await MoveDrawingAsync(0, step); return true;
            case "ArrowLeft": await MoveDrawingAsync(-step, 0); return true;
            case "ArrowRight": await MoveDrawingAsync(step, 0); return true;
            case "Ctrl+ArrowUp": await MoveDrawingAsync(0, -fine); return true;
            case "Ctrl+ArrowDown": await MoveDrawingAsync(0, fine); return true;
            case "Ctrl+ArrowLeft": await MoveDrawingAsync(-fine, 0); return true;
            case "Ctrl+ArrowRight": await MoveDrawingAsync(fine, 0); return true;
            default: return false;
        }
    }

    private void DeselectDrawing()
    {
        if (Worksheet is null)
        {
            return;
        }

        Worksheet.SelectedImage = null;
        Worksheet.SelectedChart = null;
    }

    private void CycleDrawing(int direction)
    {
        if (Worksheet is null)
        {
            return;
        }

        var images = Worksheet.Images;
        var charts = Worksheet.Charts;
        var total = images.Count + charts.Count;

        if (total == 0)
        {
            return;
        }

        int current;
        if (Worksheet.SelectedImage is SheetImage selectedImage)
        {
            current = IndexOf(images, selectedImage);
        }
        else if (Worksheet.SelectedChart is SheetChart selectedChart)
        {
            current = images.Count + IndexOf(charts, selectedChart);
        }
        else
        {
            current = -1;
        }

        var next = (current + direction + total) % total;

        if (next < images.Count)
        {
            Worksheet.SelectedImage = images[next];
            Worksheet.SelectedChart = null;
        }
        else
        {
            Worksheet.SelectedChart = charts[next - images.Count];
            Worksheet.SelectedImage = null;
        }

        AnnounceDrawing();
    }

    private async Task MoveDrawingAsync(double dx, double dy)
    {
        if (Worksheet is null)
        {
            return;
        }

        if (Worksheet.SelectedImage is SheetImage image)
        {
            var (from, to) = OffsetAnchors(image.From, image.To, dx, dy);
            await ExecuteAsync(new MoveAnchoredCommand<SheetImage>(image, from, to, SpreadsheetFeature.Images));
        }
        else if (Worksheet.SelectedChart is SheetChart chart)
        {
            var (from, to) = OffsetAnchors(chart.From, chart.To, dx, dy);
            await ExecuteAsync(new MoveAnchoredCommand<SheetChart>(chart, from, to, SpreadsheetFeature.Charts));
        }
    }

    private (CellAnchor From, CellAnchor? To) OffsetAnchors(CellAnchor from, CellAnchor? to, double dx, double dy)
    {
        var newFrom = from.Clone();
        newFrom.ColumnOffset += dx;
        newFrom.RowOffset += dy;
        NormalizeAnchor(newFrom);

        CellAnchor? newTo = null;
        if (to is not null)
        {
            newTo = to.Clone();
            newTo.ColumnOffset += dx;
            newTo.RowOffset += dy;
            NormalizeAnchor(newTo);
        }

        return (newFrom, newTo);
    }

    private void AnnounceDrawing()
    {
        if (Worksheet is null || accessibility is null)
        {
            return;
        }

        if (Worksheet.SelectedImage is SheetImage image)
        {
            accessibility.AnnounceAction(
                $"{Localize(nameof(RadzenStrings.Spreadsheet_A11yImageSelected))}, {new CellRef(image.From.Row, image.From.Column)}");
        }
        else if (Worksheet.SelectedChart is SheetChart chart)
        {
            accessibility.AnnounceAction(
                $"{Localize(nameof(RadzenStrings.Spreadsheet_A11yChartSelected))}, {new CellRef(chart.From.Row, chart.From.Column)}");
        }
    }

    // Keyboard resize: a Size dialog (Shift+F10 in the drawing layer) - the non-drag 2.5.7 alternative.
    private async Task OpenDrawingSizeDialogAsync()
    {
        if (Worksheet is null)
        {
            return;
        }

        double width;
        double height;

        if (Worksheet.SelectedImage is SheetImage selectedImage)
        {
            width = selectedImage.Width;
            height = selectedImage.Height;
        }
        else if (Worksheet.SelectedChart is SheetChart selectedChart)
        {
            width = selectedChart.Width;
            height = selectedChart.Height;
        }
        else
        {
            return;
        }

        var parameters = new Dictionary<string, object?>
        {
            { nameof(Spreadsheet.DrawingSizeDialog.Width), width },
            { nameof(Spreadsheet.DrawingSizeDialog.Height), height },
            { nameof(Spreadsheet.DrawingSizeDialog.WidthLabel), Localize(nameof(RadzenStrings.Spreadsheet_SizeWidth)) },
            { nameof(Spreadsheet.DrawingSizeDialog.HeightLabel), Localize(nameof(RadzenStrings.Spreadsheet_SizeHeight)) },
            { nameof(Spreadsheet.DrawingSizeDialog.OkText), Localize(nameof(RadzenStrings.Spreadsheet_OK)) },
            { nameof(Spreadsheet.DrawingSizeDialog.CancelText), Localize(nameof(RadzenStrings.Spreadsheet_Cancel)) },
        };

        var result = await OpenDialogAsync<Spreadsheet.DrawingSizeDialog>(
            Localize(nameof(RadzenStrings.Spreadsheet_SizeTitle)), parameters, new DialogOptions { Width = "340px" });

        if (result is Spreadsheet.DrawingSizeDialog.Size size)
        {
            if (Worksheet.SelectedImage is SheetImage image)
            {
                await ExecuteAsync(new ResizeAnchoredCommand<SheetImage>(image, size.Width, size.Height, SpreadsheetFeature.Images));
            }
            else if (Worksheet.SelectedChart is SheetChart chart)
            {
                await ExecuteAsync(new ResizeAnchoredCommand<SheetChart>(chart, size.Width, size.Height, SpreadsheetFeature.Charts));
            }
        }
    }

    // Announces discrete structural/data actions; the per-cell nav announcer covers cell edits.
    private string? DescribeCommand(ICommand command) => command switch
    {
        SortCommand or MultiKeySortCommand => Localize(nameof(RadzenStrings.Spreadsheet_A11ySorted)),
        FilterCommand or TableFilterCommand or SheetAutoFilterCommand or RemoveFilterCommand
            => Localize(nameof(RadzenStrings.Spreadsheet_A11yFilterApplied)),
        InsertRowCommand => Localize(nameof(RadzenStrings.Spreadsheet_A11yRowInserted)),
        InsertColumnCommand => Localize(nameof(RadzenStrings.Spreadsheet_A11yColumnInserted)),
        DeleteRowsCommand => Localize(nameof(RadzenStrings.Spreadsheet_A11yRowDeleted)),
        DeleteColumnsCommand => Localize(nameof(RadzenStrings.Spreadsheet_A11yColumnDeleted)),
        _ => null
    };

    private static int IndexOf<T>(IReadOnlyList<T> list, T item) where T : class
    {
        for (var i = 0; i < list.Count; i++)
        {
            if (ReferenceEquals(list[i], item))
            {
                return i;
            }
        }

        return -1;
    }

    private Task CancelEditAsync()
    {
        if (Editor?.Mode != EditMode.None)
        {
            Editor?.Cancel();
        }

        return Task.CompletedTask;
    }

    // A keyboard shortcut: the action to run, and whether it acts regardless of focus context. Global
    // shortcuts (F6 region escape, undo/redo, help) fire from anywhere; the rest are grid-only.
    private sealed record Shortcut(Func<KeyboardEventArgs, Task> Action, bool Global = false);

    private readonly Dictionary<string, Shortcut> shortcuts = [];

    private void Bind(string key, Func<KeyboardEventArgs, Task> action, bool global = false)
        => shortcuts.Add(key, new Shortcut(action, global));

    // Approximate page size (rows) for PageUp/PageDown - a screenful without coupling to the viewport.
    private const int PageRows = 20;

    private readonly SpreadsheetClipboard clipboard = new();

    /// <inheritdoc/>
    protected override void OnInitialized()
    {
        // Assigns UniqueID so GetId() is non-empty - the a11y instructions span id and its
        // aria-describedby reference derive from it and must be unique per spreadsheet on the page.
        base.OnInitialized();
        workbook = Workbook;
        Bind("Enter", _ => CycleSelectionAsync(1, 0));
        Bind("Escape", _ => CancelEditAsync());
        Bind("Tab", _ => CycleSelectionAsync(0, 1));
        Bind("ArrowUp", _ => MoveSelectionAsync(-1, 0));
        Bind("ArrowDown", _ => MoveSelectionAsync(1, 0));
        Bind("ArrowLeft", _ => MoveSelectionAsync(0, -1));
        Bind("ArrowRight", _ => MoveSelectionAsync(0, 1));
        Bind("Shift+Tab", _ => CycleSelectionAsync(0, -1));
        Bind("Shift+Enter", _ => CycleSelectionAsync(-1, 0));
        Bind("Shift+ArrowUp", _ => ExtendSelectionAsync(-1, 0));
        Bind("Shift+ArrowDown", _ => ExtendSelectionAsync(1, 0));
        Bind("Shift+ArrowLeft", _ => ExtendSelectionAsync(0, -1));
        Bind("Shift+ArrowRight", _ => ExtendSelectionAsync(0, 1));
        Bind("Home", _ => MoveToAsync(c => new CellRef(c.Row, 0)));
        Bind("End", _ => MoveToAsync(c => new CellRef(c.Row, GetUsedEnd().Column)));
        Bind("Ctrl+Home", _ => MoveToAsync(_ => new CellRef(0, 0)));
        Bind("Ctrl+End", _ => MoveToAsync(_ => GetUsedEnd()));
        Bind("Ctrl+A", _ => SelectUsedRangeAsync());
        Bind("Shift+Home", _ => ExtendToAsync(c => new CellRef(c.Row, 0)));
        Bind("Shift+End", _ => ExtendToAsync(c => new CellRef(c.Row, GetUsedEnd().Column)));
        Bind("Ctrl+Shift+Home", _ => ExtendToAsync(_ => new CellRef(0, 0)));
        Bind("Ctrl+Shift+End", _ => ExtendToAsync(_ => GetUsedEnd()));
        Bind("Ctrl+ArrowUp", _ => MoveToAsync(c => FindEdge(c, -1, 0)));
        Bind("Ctrl+ArrowDown", _ => MoveToAsync(c => FindEdge(c, 1, 0)));
        Bind("Ctrl+ArrowLeft", _ => MoveToAsync(c => FindEdge(c, 0, -1)));
        Bind("Ctrl+ArrowRight", _ => MoveToAsync(c => FindEdge(c, 0, 1)));
        Bind("Ctrl+Shift+ArrowUp", _ => ExtendToAsync(c => FindEdge(c, -1, 0)));
        Bind("Ctrl+Shift+ArrowDown", _ => ExtendToAsync(c => FindEdge(c, 1, 0)));
        Bind("Ctrl+Shift+ArrowLeft", _ => ExtendToAsync(c => FindEdge(c, 0, -1)));
        Bind("Ctrl+Shift+ArrowRight", _ => ExtendToAsync(c => FindEdge(c, 0, 1)));
        Bind("Ctrl+Space", _ => SelectColumnAsync());
        Bind("Shift+Space", _ => SelectRowAsync());
        Bind("Shift+F10", _ => OpenContextMenuAtActiveCellAsync());
        Bind("ContextMenu", _ => OpenContextMenuAtActiveCellAsync());
        Bind("Ctrl+Alt+5", _ => EnterDrawingLayerAsync());
        Bind("PageDown", _ => MoveSelectionAsync(PageRows, 0));
        Bind("PageUp", _ => MoveSelectionAsync(-PageRows, 0));
        Bind("Shift+PageDown", _ => ExtendSelectionAsync(PageRows, 0));
        Bind("Shift+PageUp", _ => ExtendSelectionAsync(-PageRows, 0));
        Bind("Ctrl+D", _ => FillAsync(down: true));
        Bind("Ctrl+R", _ => FillAsync(down: false));
        Bind("Ctrl+C", _ => CopySelectionAsync());
        Bind("Ctrl+X", _ => CutSelectionAsync());
        Bind("Delete", _ => DeleteSelectedAsync());
        Bind("Backspace", _ => DeleteSelectedAsync());
        Bind("F2", _ => StartEditActiveCellAsync());

        // Global: act from any focused element so F6 can always cycle/return, and undo/redo and the
        // shortcut help still work from the toolbar or formula bar.
        Bind("F6", _ => FocusRegionAsync(true), global: true);
        Bind("Shift+F6", _ => FocusRegionAsync(false), global: true);
        Bind("Alt+Slash", _ => OpenShortcutsHelpAsync(), global: true);
        Bind("Ctrl+Z", _ => UndoAsync(), global: true);
        Bind("Ctrl+Shift+Z", _ => RedoAsync(), global: true);
        Bind("Ctrl+Y", _ => RedoAsync(), global: true);
    }

    // One documented shortcut row: the key chords to display (split on " / " into badges by the
    // dialog), the resx description key, and the registered binding keys it covers. The Bindings let
    // a unit test assert every registered shortcut is documented, so the help cannot silently drift.
    private readonly record struct HelpEntry(string Keys, string DescriptionKey, string[] Bindings);

    // Ordered by priority - the most common navigation and editing keys first, with F6 region
    // movement near the top as the primary keyboard/accessibility escape.
    private static readonly HelpEntry[] ShortcutHelp =
    [
        new("Arrow keys", nameof(RadzenStrings.Spreadsheet_HelpMove), ["ArrowUp", "ArrowDown", "ArrowLeft", "ArrowRight"]),
        new("Tab / Shift+Tab", nameof(RadzenStrings.Spreadsheet_HelpNextCell), ["Tab", "Shift+Tab"]),
        new("Enter / Shift+Enter", nameof(RadzenStrings.Spreadsheet_HelpConfirmMove), ["Enter", "Shift+Enter"]),
        new("F6 / Shift+F6", nameof(RadzenStrings.Spreadsheet_HelpRegions), ["F6", "Shift+F6"]),
        new("F2", nameof(RadzenStrings.Spreadsheet_HelpEdit), ["F2"]),
        new("Escape", nameof(RadzenStrings.Spreadsheet_HelpCancel), ["Escape"]),
        new("Shift+Arrow keys", nameof(RadzenStrings.Spreadsheet_HelpExtend), ["Shift+ArrowUp", "Shift+ArrowDown", "Shift+ArrowLeft", "Shift+ArrowRight"]),
        new("Ctrl+Arrow keys", nameof(RadzenStrings.Spreadsheet_HelpEdge), ["Ctrl+ArrowUp", "Ctrl+ArrowDown", "Ctrl+ArrowLeft", "Ctrl+ArrowRight"]),
        new("Ctrl+Shift+Arrow keys", nameof(RadzenStrings.Spreadsheet_HelpExtendEdge), ["Ctrl+Shift+ArrowUp", "Ctrl+Shift+ArrowDown", "Ctrl+Shift+ArrowLeft", "Ctrl+Shift+ArrowRight"]),
        new("Home / Ctrl+Home", nameof(RadzenStrings.Spreadsheet_HelpRowStart), ["Home", "Ctrl+Home"]),
        new("End / Ctrl+End", nameof(RadzenStrings.Spreadsheet_HelpLastCell), ["End", "Ctrl+End"]),
        new("Shift+Home / Shift+End", nameof(RadzenStrings.Spreadsheet_HelpExtendRow), ["Shift+Home", "Shift+End"]),
        new("Ctrl+Shift+Home / Ctrl+Shift+End", nameof(RadzenStrings.Spreadsheet_HelpExtendSheet), ["Ctrl+Shift+Home", "Ctrl+Shift+End"]),
        new("Page Up / Page Down", nameof(RadzenStrings.Spreadsheet_HelpPage), ["PageUp", "PageDown"]),
        new("Shift+Page Up / Shift+Page Down", nameof(RadzenStrings.Spreadsheet_HelpExtendPage), ["Shift+PageUp", "Shift+PageDown"]),
        new("Ctrl+A", nameof(RadzenStrings.Spreadsheet_HelpSelectAll), ["Ctrl+A"]),
        new("Ctrl+Space", nameof(RadzenStrings.Spreadsheet_HelpSelectColumn), ["Ctrl+Space"]),
        new("Shift+Space", nameof(RadzenStrings.Spreadsheet_HelpSelectRow), ["Shift+Space"]),
        new("Ctrl+C / Ctrl+X / Ctrl+V", nameof(RadzenStrings.Spreadsheet_HelpClipboard), ["Ctrl+C", "Ctrl+X"]),
        new("Ctrl+Z / Ctrl+Y", nameof(RadzenStrings.Spreadsheet_HelpUndoRedo), ["Ctrl+Z", "Ctrl+Shift+Z", "Ctrl+Y"]),
        new("Ctrl+D / Ctrl+R", nameof(RadzenStrings.Spreadsheet_HelpFill), ["Ctrl+D", "Ctrl+R"]),
        new("Delete", nameof(RadzenStrings.Spreadsheet_HelpClear), ["Delete", "Backspace"]),
        new("Shift+F10", nameof(RadzenStrings.Spreadsheet_HelpContextMenu), ["Shift+F10", "ContextMenu"]),
        new("Ctrl+Alt+5", nameof(RadzenStrings.Spreadsheet_HelpDrawings), ["Ctrl+Alt+5"]),
        new("Alt+/", nameof(RadzenStrings.Spreadsheet_HelpShowShortcuts), ["Alt+Slash"]),
    ];

    // The keys actually bound, and the keys the help documents - compared by a drift-guard test.
    internal IEnumerable<string> RegisteredShortcutKeys => shortcuts.Keys;
    internal static IEnumerable<string> DocumentedShortcutKeys => ShortcutHelp.SelectMany(e => e.Bindings);

    private async Task OpenShortcutsHelpAsync()
    {
        var rows = ShortcutHelp
            .Select(e => new Spreadsheet.SpreadsheetShortcutsDialog.Shortcut(e.Keys, Localize(e.DescriptionKey)))
            .ToList();

        var parameters = new Dictionary<string, object?>
        {
            { nameof(Spreadsheet.SpreadsheetShortcutsDialog.Shortcuts), rows },
            { nameof(Spreadsheet.SpreadsheetShortcutsDialog.ShortcutColumn), Localize(nameof(RadzenStrings.Spreadsheet_HelpShortcutColumn)) },
            { nameof(Spreadsheet.SpreadsheetShortcutsDialog.ActionColumn), Localize(nameof(RadzenStrings.Spreadsheet_HelpActionColumn)) },
            { nameof(Spreadsheet.SpreadsheetShortcutsDialog.FilterPlaceholder), Localize(nameof(RadzenStrings.Spreadsheet_HelpFilterPlaceholder)) },
            { nameof(Spreadsheet.SpreadsheetShortcutsDialog.NoResultsText), Localize(nameof(RadzenStrings.Spreadsheet_HelpNoResults)) },
        };

        await OpenDialogAsync<Spreadsheet.SpreadsheetShortcutsDialog>(
            Localize(nameof(RadzenStrings.Spreadsheet_HelpTitle)), parameters, new DialogOptions { Width = "600px" });
    }

    // Opens a dialog and returns focus to the grid after it closes (WCAG 2.4.3 focus order).
    private async Task<dynamic?> OpenDialogAsync<[System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All)] T>(
        string title, Dictionary<string, object?>? parameters = null, DialogOptions? options = null) where T : ComponentBase
    {
        var result = await DialogService.OpenAsync<T>(title, parameters, options);
        await Element.FocusAsync();
        return result;
    }

    private async Task FocusRegionAsync(bool forward)
    {
        if (jsRef is not null)
        {
            await jsRef.InvokeVoidAsync("focusAdjacent", forward);
        }
    }

    private async Task StartEditActiveCellAsync()
    {
        if (Worksheet is null || Editor is null || !IsFeatureAllowed(SpreadsheetFeature.Editing))
        {
            return;
        }

        var address = Worksheet.MergedCells.GetMergedRangeStartOrSelf(Worksheet.Selection.Cell);
        var cell = Worksheet.Cells[address];

        if (cell != null)
        {
            await ScrollToAsync(address);
            Editor.StartEdit(address, cell.GetValue());
        }
    }

    private async Task DeleteSelectedAsync()
    {
        if (Worksheet?.SelectedImage != null)
        {
            await ExecuteAsync(new DeleteImageCommand(Worksheet, Worksheet.SelectedImage));
        }
        else if (Worksheet?.SelectedChart != null)
        {
            await ExecuteAsync(new DeleteChartCommand(Worksheet, Worksheet.SelectedChart));
        }
        else if (Worksheet != null && Worksheet.Selection.Range != RangeRef.Invalid)
        {
            await ExecuteAsync(new ClearContentsCommand(Worksheet, Worksheet.Selection.Range));
        }
    }

    private Task UndoAsync()
    {
        Undo();

        return Task.CompletedTask;
    }

    private Task RedoAsync()
    {
        Redo();

        return Task.CompletedTask;
    }

    private async Task CopySelectionAsync()
    {
        if (!IsFeatureAllowed(SpreadsheetFeature.Clipboard))
        {
            return;
        }

        if (Worksheet is not null)
        {
            var text = Worksheet.GetDelimitedString(Worksheet.Selection.Range);
            clipboard.Copy(Worksheet);

            if (jsRef is not null)
            {
                await jsRef.InvokeVoidAsync("copyToClipboard", text);
            }
        }
    }

    internal async Task CutSelectionAsync()
    {
        if (!IsFeatureAllowed(SpreadsheetFeature.Clipboard) || !IsFeatureAllowed(SpreadsheetFeature.Editing))
        {
            return;
        }

        if (Worksheet is not null)
        {
            // reject the cut if the range contains any locked cells on a protected sheet
            if (!Worksheet.IsRangeEditable(Worksheet.Selection.Range))
            {
                return;
            }

            var text = Worksheet.GetDelimitedString(Worksheet.Selection.Range);
            clipboard.Cut(Worksheet);

            if (jsRef is not null)
            {
                await jsRef.InvokeVoidAsync("copyToClipboard", text);
            }
        }
    }

    /// <summary>
    /// Invoked by JS interop to copy the current selection to the clipboard.
    /// </summary>
    /// <returns></returns>
    [JSInvokable]
    public Task OnCopyAsync() => CopySelectionAsync();

    /// <summary>
    /// Invoked by JS interop to paste text from the clipboard into the current selection.
    /// </summary>
    [JSInvokable]
    public async Task OnPasteAsync(string text)
    {
        if (!IsFeatureAllowed(SpreadsheetFeature.Clipboard) || !IsFeatureAllowed(SpreadsheetFeature.Editing))
        {
            return;
        }

        if (Worksheet is not null && Worksheet.IsCellEditable(Worksheet.Selection.Cell))
        {
            await ExecuteAsync(new PasteCommand(clipboard, Worksheet, Worksheet.Selection.Range, text));
        }
    }

    /// <summary>
    /// Invoked by JS interop when a cell is right-clicked.
    /// </summary>
    [JSInvokable]
    public async Task OnCellContextMenuAsync(CellEventArgs args)
    {
        ArgumentNullException.ThrowIfNull(args);

        if (Worksheet is null)
        {
            return;
        }

        await AcceptAsync();

        Worksheet.Selection.Select(new CellRef(args.Row, args.Column));

        var row = args.Row;
        var column = args.Column;

        var menuItems = new List<ContextMenuItem>();

        var canCutPaste = IsFeatureAllowed(SpreadsheetFeature.Clipboard) && IsFeatureAllowed(SpreadsheetFeature.Editing);
        if (canCutPaste)
        {
            menuItems.Add(new() { Text = Localize(nameof(RadzenStrings.Spreadsheet_Cut)), Value = "cut", Icon = "content_cut" });
        }
        if (IsFeatureAllowed(SpreadsheetFeature.Clipboard))
        {
            if (IsFeatureAllowed(SpreadsheetFeature.Clipboard))
        {
            menuItems.Add(new() { Text = Localize(nameof(RadzenStrings.Spreadsheet_Copy)), Value = "copy", Icon = "content_copy" });
        }
        }
        if (canCutPaste)
        {
            menuItems.Add(new() { Text = Localize(nameof(RadzenStrings.Spreadsheet_Paste)), Value = "paste", Icon = "content_paste" });
        }
        if (IsFeatureAllowed(SpreadsheetFeature.Editing))
        {
            menuItems.Add(new() { Text = Localize(nameof(RadzenStrings.Spreadsheet_ClearContents)), Value = "clear", Icon = "clear" });
        }
        if (IsFeatureAllowed(SpreadsheetFeature.Sorting))
        {
            menuItems.Add(new() { Text = Localize(nameof(RadzenStrings.Spreadsheet_ContextSortAscending)), Value = "sort-ascending", Icon = "arrow_upward" });
            menuItems.Add(new() { Text = Localize(nameof(RadzenStrings.Spreadsheet_ContextSortDescending)), Value = "sort-descending", Icon = "arrow_downward" });
        }

        if (FindTableAt(row, column) is not null && IsFeatureAllowed(SpreadsheetFeature.Tables))
        {
            menuItems.Add(new() { Text = Localize(nameof(RadzenStrings.Spreadsheet_ConvertTableToRange)),
                Value = "convert-table-to-range", Icon = "grid_off" });
            menuItems.Add(new() { Text = Localize(nameof(RadzenStrings.Spreadsheet_DeleteTable)),
                Value = "delete-table", Icon = "delete" });
        }

        ContextMenuService.Open(args.Pointer, menuItems,
            menuArgs => _ = OnContextMenuItemClick(menuArgs, row, column));
    }

    private Table? FindTableAt(int row, int column)
    {
        if (Worksheet is null)
        {
            return null;
        }

        foreach (var t in Worksheet.Tables)
        {
            if (t.Range.Contains(row, column))
            {
                return t;
            }
        }
        return null;
    }

    /// <summary>
    /// Invoked by JS interop when a row header is right-clicked.
    /// </summary>
    [JSInvokable]
    public async Task OnRowContextMenuAsync(CellEventArgs args)
    {
        ArgumentNullException.ThrowIfNull(args);

        if (Worksheet is null)
        {
            return;
        }

        await AcceptAsync();

        Worksheet.Selection.Select(new RowRef(args.Row));

        var row = args.Row;
        var menuItems = new List<ContextMenuItem>();

        var canCutPaste = IsFeatureAllowed(SpreadsheetFeature.Clipboard) && IsFeatureAllowed(SpreadsheetFeature.Editing);
        if (canCutPaste)
        {
            menuItems.Add(new() { Text = Localize(nameof(RadzenStrings.Spreadsheet_Cut)), Value = "cut", Icon = "content_cut" });
        }
        if (IsFeatureAllowed(SpreadsheetFeature.Clipboard))
        {
            menuItems.Add(new() { Text = Localize(nameof(RadzenStrings.Spreadsheet_Copy)), Value = "copy", Icon = "content_copy" });
        }
        if (canCutPaste)
        {
            menuItems.Add(new() { Text = Localize(nameof(RadzenStrings.Spreadsheet_Paste)), Value = "paste", Icon = "content_paste" });
        }
        if (IsFeatureAllowed(SpreadsheetFeature.Editing))
        {
            menuItems.Add(new() { Text = Localize(nameof(RadzenStrings.Spreadsheet_InsertRowAbove)), Value = "insert-row-before", Icon = "north" });
            menuItems.Add(new() { Text = Localize(nameof(RadzenStrings.Spreadsheet_InsertRowBelow)), Value = "insert-row-after", Icon = "south" });
            menuItems.Add(new() { Text = Localize(nameof(RadzenStrings.Spreadsheet_DeleteRow)), Value = "delete-row", Icon = "delete" });
        }
        if (IsFeatureAllowed(SpreadsheetFeature.Resizing))
        {
            menuItems.Add(new() { Text = Localize(nameof(RadzenStrings.Spreadsheet_HideRow)), Value = "hide-row", Icon = "visibility_off" });
            menuItems.Add(new() { Text = Localize(nameof(RadzenStrings.Spreadsheet_UnhideRow)), Value = "unhide-row", Icon = "visibility" });
        }

        ContextMenuService.Open(args.Pointer, menuItems, menuArgs => _ = OnRowContextMenuItemClick(menuArgs, row));
    }

    /// <summary>
    /// Invoked by JS interop when a column header is right-clicked.
    /// </summary>
    [JSInvokable]
    public async Task OnColumnContextMenuAsync(CellEventArgs args)
    {
        ArgumentNullException.ThrowIfNull(args);

        if (Worksheet is null)
        {
            return;
        }

        await AcceptAsync();

        Worksheet.Selection.Select(new ColumnRef(args.Column));

        var column = args.Column;
        var menuItems = new List<ContextMenuItem>();

        var canCutPaste = IsFeatureAllowed(SpreadsheetFeature.Clipboard) && IsFeatureAllowed(SpreadsheetFeature.Editing);
        if (canCutPaste)
        {
            menuItems.Add(new() { Text = Localize(nameof(RadzenStrings.Spreadsheet_Cut)), Value = "cut", Icon = "content_cut" });
        }
        if (IsFeatureAllowed(SpreadsheetFeature.Clipboard))
        {
            menuItems.Add(new() { Text = Localize(nameof(RadzenStrings.Spreadsheet_Copy)), Value = "copy", Icon = "content_copy" });
        }
        if (canCutPaste)
        {
            menuItems.Add(new() { Text = Localize(nameof(RadzenStrings.Spreadsheet_Paste)), Value = "paste", Icon = "content_paste" });
        }
        if (IsFeatureAllowed(SpreadsheetFeature.Editing))
        {
            menuItems.Add(new() { Text = Localize(nameof(RadzenStrings.Spreadsheet_InsertColumnBefore)), Value = "insert-column-before", Icon = "west" });
            menuItems.Add(new() { Text = Localize(nameof(RadzenStrings.Spreadsheet_InsertColumnAfter)), Value = "insert-column-after", Icon = "east" });
            menuItems.Add(new() { Text = Localize(nameof(RadzenStrings.Spreadsheet_DeleteColumn)), Value = "delete-column", Icon = "delete" });
        }
        if (IsFeatureAllowed(SpreadsheetFeature.Resizing))
        {
            menuItems.Add(new() { Text = Localize(nameof(RadzenStrings.Spreadsheet_HideColumn)), Value = "hide-column", Icon = "visibility_off" });
            menuItems.Add(new() { Text = Localize(nameof(RadzenStrings.Spreadsheet_UnhideColumn)), Value = "unhide-column", Icon = "visibility" });
        }

        ContextMenuService.Open(args.Pointer, menuItems, menuArgs => _ = OnColumnContextMenuItemClick(menuArgs, column));
    }

    private bool HandleCommonContextMenuAction(string? action)
    {
        switch (action)
        {
            case "cut":
                _ = InvokeAsync(CutSelectionAsync);
                return true;
            case "copy":
                _ = InvokeAsync(CopySelectionAsync);
                return true;
            case "paste":
                _ = InvokeAsync(PasteFromClipboardAsync);
                return true;
            default:
                return false;
        }
    }

    private async Task OnContextMenuItemClick(MenuItemEventArgs args, int row, int column)
    {
        if (Worksheet is null)
        {
            return;
        }

        ContextMenuService.Close();

        var action = args.Value?.ToString();

        if (!HandleCommonContextMenuAction(action))
        {
            switch (action)
            {
                case "clear":
                    await ExecuteAsync(new ClearContentsCommand(Worksheet, Worksheet.Selection.Range));
                    break;
                case "sort-ascending":
                    await ExecuteAsync(BuildSortCommand(row, column, SortOrder.Ascending));
                    break;
                case "sort-descending":
                    await ExecuteAsync(BuildSortCommand(row, column, SortOrder.Descending));
                    break;
                case "convert-table-to-range":
                case "delete-table":
                    {
                        var table = FindTableAt(row, column);
                        if (table is not null)
                        {
                            await ExecuteAsync(new RemoveTableCommand(Worksheet, table));
                        }
                    }
                    break;
            }
        }
    }

    private async Task OnRowContextMenuItemClick(MenuItemEventArgs args, int row)
    {
        if (Worksheet is null)
        {
            return;
        }

        ContextMenuService.Close();

        var action = args.Value?.ToString();

        if (!HandleCommonContextMenuAction(action))
        {
            switch (action)
            {
                case "insert-row-before":
                    await ExecuteAsync(new InsertRowCommand(Worksheet, row));
                    break;
                case "insert-row-after":
                    await ExecuteAsync(new InsertRowCommand(Worksheet, row + 1));
                    break;
                case "delete-row":
                    await ExecuteAsync(new DeleteRowsCommand(Worksheet, row, row));
                    break;
                case "hide-row":
                    if (IsFeatureAllowed(SpreadsheetFeature.Resizing))
                    {
                        Worksheet.Rows.Hide(row);
                    }
                    break;
                case "unhide-row":
                    if (IsFeatureAllowed(SpreadsheetFeature.Resizing))
                    {
                        Worksheet.Rows.Show(row);
                    }
                    break;
            }
        }
    }

    private async Task OnColumnContextMenuItemClick(MenuItemEventArgs args, int column)
    {
        if (Worksheet is null)
        {
            return;
        }

        ContextMenuService.Close();

        var action = args.Value?.ToString();

        if (!HandleCommonContextMenuAction(action))
        {
            switch (action)
            {
                case "insert-column-before":
                    await ExecuteAsync(new InsertColumnCommand(Worksheet, column));
                    break;
                case "insert-column-after":
                    await ExecuteAsync(new InsertColumnCommand(Worksheet, column + 1));
                    break;
                case "delete-column":
                    await ExecuteAsync(new DeleteColumnsCommand(Worksheet, column, column));
                    break;
                case "hide-column":
                    if (IsFeatureAllowed(SpreadsheetFeature.Resizing))
                    {
                        Worksheet.Columns.Hide(column);
                    }
                    break;
                case "unhide-column":
                    if (IsFeatureAllowed(SpreadsheetFeature.Resizing))
                    {
                        Worksheet.Columns.Show(column);
                    }
                    break;
            }
        }
    }

    private async Task PasteFromClipboardAsync()
    {
        if (!IsFeatureAllowed(SpreadsheetFeature.Clipboard) || !IsFeatureAllowed(SpreadsheetFeature.Editing))
        {
            return;
        }

        if (Worksheet is null || !Worksheet.IsCellEditable(Worksheet.Selection.Cell))
        {
            return;
        }

        string? text = null;

        if (jsRef is not null)
        {
            text = await jsRef.InvokeAsync<string?>("readClipboardText");
        }

        await ExecuteAsync(new PasteCommand(clipboard, Worksheet, Worksheet.Selection.Range, text));
    }

    private IJSObjectReference? jsRef;
    private DotNetObjectReference<RadzenSpreadsheet>? dotNetRef;

    /// <inheritdoc/>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && JSRuntime != null)
        {
            dotNetRef = DotNetObjectReference.Create(this);
            // Serialize one map of key -> isGlobal; the JS gate needs nothing more than that.
            jsRef = await JSRuntime.InvokeAsync<IJSObjectReference>("Radzen.createSpreadsheet", Element, dotNetRef,
                shortcuts.ToDictionary(s => s.Key, s => s.Value.Global));
        }
    }

    /// <summary>
    /// Invoked by JS interop when a cell is clicked with the pointer.
    /// </summary>
    [JSInvokable]
    public async Task<bool> OnCellPointerDownAsync(CellEventArgs args)
    {
        ArgumentNullException.ThrowIfNull(args);
        var result = await AcceptAsync();

        if (result)
        {
            // Clear image/chart selection when clicking a cell
            if (Worksheet != null)
            {
                Worksheet.SelectedImage = null;
                Worksheet.SelectedChart = null;
            }

            var address = new CellRef(args.Row, args.Column);

            if (args.Pointer.ShiftKey)
            {
                Worksheet?.Selection.Merge(address);
            }
            else
            {
                Worksheet?.Selection.Select(address);

            }

            if (grid is not null)
            {
                var capture = new PointerCapture
                {
                    ScrollTop = grid.ScrollTop,
                    ScrollLeft = grid.ScrollLeft,
                    Row = args.Row,
                    Column = args.Column,
                    Pointer = args.Pointer
                };

                activeCapture = capture;
            }
        }

        return result;
    }

    private object? activeCapture;

    /// <summary>
    /// Invoked by JS interop when the pointer moves over a cell.
    /// </summary>
    [JSInvokable]
    public async Task OnCellPointerMoveAsync(PointerEventArgs args)
    {
        ArgumentNullException.ThrowIfNull(args);
        if (activeCapture is PointerCapture capture)
        {
            await OnCellPointerMoveAsync(capture, args);
        }
    }

    class PointerCapture
    {
        public double ScrollTop { get; set; }
        public double ScrollLeft { get; set; }
        public int Row { get; set; }
        public int Column { get; set; }
        public PointerEventArgs Pointer { get; set; } = default!;
    }

    private async Task OnCellPointerMoveAsync(PointerCapture capture, PointerEventArgs pointer)
    {
        var address = GetDeltaCell(capture, pointer);

        if (address != CellRef.Invalid)
        {
            Worksheet?.Selection.Merge(address);

            await ScrollToAsync(address);
        }
    }

    /// <summary>
    /// Invoked by JS interop when the user releases the pointer after a cell, row, or
    /// column selection gesture. Fires <see cref="Selection.RangeFinalized"/> so subscribers
    /// (such as the range picker) can commit the user's pick.
    /// </summary>
    [JSInvokable]
    public Task OnSelectionPointerUpAsync()
    {
        activeCapture = null;
        Worksheet?.Selection.FinalizeRange();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Opens the visual range picker bar. The dialog calling this should already be closed.
    /// The returned task resolves with the picked range as a sheet-qualified absolute formula
    /// (e.g. <c>Sheet1!$A$1:$C$5</c>) when the user finishes a selection on the sheet, or
    /// <c>null</c> if the pick is cancelled.
    /// </summary>
    internal Task<string?> BeginRangePickAsync(string initialValue)
    {
        if (rangePickerBar is null || Worksheet is null)
        {
            return Task.FromResult<string?>(null);
        }

        return rangePickerBar.ShowAsync(initialValue, Worksheet);
    }

    private CellRef GetDeltaCell(PointerCapture capture, PointerEventArgs args)
    {
        if (grid is not null)
        {
            var deltaX = args.ClientX - capture.Pointer.ClientX + capture.Pointer.OffsetX;

            deltaX += grid.ScrollLeft - capture.ScrollLeft;

            var deltaY = args.ClientY - capture.Pointer.ClientY + capture.Pointer.OffsetY;

            deltaY += grid.ScrollTop - capture.ScrollTop;

            var columnPixelRange = grid.View.GetColumnPixelRange(capture.Column, capture.Column);

            var columnIndex = grid.View.GetColumnRange(columnPixelRange.Start + deltaX, columnPixelRange.Start + deltaX, true);

            var rowPixelRange = grid.View.GetRowPixelRange(capture.Row, capture.Row);

            var rowIndex = grid.View.GetRowRange(rowPixelRange.Start + deltaY, rowPixelRange.Start + deltaY, true);

            return new CellRef(rowIndex.Start, columnIndex.Start);
        }

        return CellRef.Invalid;
    }

    // ── Autofill ──────────────────────────────────────────────────────────

    /// <summary>
    /// Invoked by JS interop when the autofill handle is pressed.
    /// </summary>
    [JSInvokable]
    public void OnAutofillPointerDownAsync(PointerEventArgs args)
    {
        ArgumentNullException.ThrowIfNull(args);

        if (Worksheet is null || !IsFeatureAllowed(SpreadsheetFeature.Autofill))
        {
            return;
        }

        autofillSource = Worksheet.Selection.Range;
        autofillStartPointer = args;
    }

    /// <summary>
    /// Invoked by JS interop when the pointer moves during an autofill drag.
    /// </summary>
    [JSInvokable]
    public void OnAutofillPointerMoveAsync(PointerEventArgs args)
    {
        ArgumentNullException.ThrowIfNull(args);

        if (autofillSource == RangeRef.Invalid || autofillStartPointer is null || grid is null || Worksheet is null)
        {
            return;
        }

        var cell = GetAutofillTarget(args);

        if (cell == CellRef.Invalid)
        {
            return;
        }

        var fillRange = Spreadsheet.AutofillCommand.ComputeRange(autofillSource, cell);

        if (fillRange != RangeRef.Invalid)
        {
            Worksheet.AutofillPreview = fillRange;
        }
    }

    /// <summary>
    /// Invoked by JS interop when the pointer is released after an autofill drag.
    /// </summary>
    [JSInvokable]
    public async Task OnAutofillPointerUpAsync(PointerEventArgs args)
    {
        ArgumentNullException.ThrowIfNull(args);

        var fillRange = Worksheet?.AutofillPreview;

        if (Worksheet is not null)
        {
            Worksheet.AutofillPreview = null;
        }

        if (autofillSource != RangeRef.Invalid && Worksheet is not null && fillRange is not null && fillRange.Value != autofillSource)
        {
            var direction = Spreadsheet.AutofillCommand.GetDirection(autofillSource, fillRange.Value);
            var command = new Spreadsheet.AutofillCommand(Worksheet, autofillSource, fillRange.Value, direction);
            await ExecuteAsync(command);

            Worksheet.Selection.Select(fillRange.Value);
        }

        autofillSource = RangeRef.Invalid;
        autofillStartPointer = null;
    }

    private CellRef GetAutofillTarget(PointerEventArgs args)
    {
        if (grid is null || autofillStartPointer is null)
        {
            return CellRef.Invalid;
        }

        var anchorRow = autofillSource.End.Row;
        var anchorColumn = autofillSource.End.Column;

        var deltaX = args.ClientX - autofillStartPointer.ClientX;
        var deltaY = args.ClientY - autofillStartPointer.ClientY;

        var columnPixelRange = grid.View.GetColumnPixelRange(anchorColumn, anchorColumn);
        var rowPixelRange = grid.View.GetRowPixelRange(anchorRow, anchorRow);

        var targetX = columnPixelRange.End + deltaX;
        var targetY = rowPixelRange.End + deltaY;

        var columnIndex = grid.View.GetColumnRange(targetX, targetX, true);
        var rowIndex = grid.View.GetRowRange(targetY, targetY, true);

        return new CellRef(rowIndex.Start, columnIndex.Start);
    }

    // ── Row pointer ──────────────────────────────────────────────────────

    /// <summary>
    /// Invoked by JS interop when a row header is clicked with the pointer.
    /// </summary>
    [JSInvokable]
    public async Task<bool> OnRowPointerDownAsync(CellEventArgs args)
    {
        ArgumentNullException.ThrowIfNull(args);
        var result = await AcceptAsync();

        if (result)
        {
            if (args.Pointer.ShiftKey)
            {
                Worksheet?.Selection.Merge(new CellRef(args.Row, Worksheet.ColumnCount - 1));
            }
            else
            {
                Worksheet?.Selection.Select(new RowRef(args.Row));
            }

            if (grid is not null)
            {
                var capture = new PointerCapture
                {
                    ScrollTop = grid.ScrollTop,
                    ScrollLeft = grid.ScrollLeft,
                    Row = args.Row,
                    Pointer = args.Pointer
                };

                activeCapture = capture;
            }
        }

        return result;
    }

    /// <summary>
    /// Invoked by JS interop when the pointer moves over a row header.
    /// </summary>
    [JSInvokable]
    public async Task OnRowPointerMoveAsync(PointerEventArgs args)
    {
        ArgumentNullException.ThrowIfNull(args);
        if (activeCapture is PointerCapture capture)
        {
            await OnRowPointerMoveAsync(capture, args);
        }
    }

    private async Task OnRowPointerMoveAsync(PointerCapture capture, PointerEventArgs pointer)
    {
        var address = GetDeltaCell(capture, pointer);

        if (address != CellRef.Invalid)
        {
            Worksheet?.Selection.Merge(new CellRef(address.Row, Worksheet.ColumnCount - 1));

            await ScrollToAsync(address);
        }
    }

    /// <summary>
    /// Invoked by JS interop when a column header is clicked with the pointer.
    /// </summary>
    [JSInvokable]
    public async Task<bool> OnColumnPointerDownAsync(CellEventArgs args)
    {
        ArgumentNullException.ThrowIfNull(args);
        var result = await AcceptAsync();

        if (result)
        {
            if (args.Pointer.ShiftKey)
            {
                Worksheet?.Selection.Merge(new CellRef(Worksheet.RowCount - 1, args.Column));
            }
            else
            {
                Worksheet?.Selection.Select(new ColumnRef(args.Column));
            }

            if (grid is not null)
            {
                var capture = new PointerCapture
                {
                    ScrollTop = grid.ScrollTop,
                    ScrollLeft = grid.ScrollLeft,
                    Column = args.Column,
                    Pointer = args.Pointer
                };

                activeCapture = capture;
            }
        }

        return result;
    }

    /// <summary>
    /// Invoked by JS interop when the pointer moves over a column header.
    /// </summary>
    [JSInvokable]
    public async Task OnColumnPointerMoveAsync(PointerEventArgs args)
    {
        ArgumentNullException.ThrowIfNull(args);
        if (activeCapture is PointerCapture capture)
        {
            await OnColumnPointerMoveAsync(capture, args);
        }
    }

    private async Task OnColumnPointerMoveAsync(PointerCapture capture, PointerEventArgs pointer)
    {
        var address = GetDeltaCell(capture, pointer);

        if (address != CellRef.Invalid)
        {
            Worksheet?.Selection.Merge(new CellRef(Worksheet.RowCount - 1, address.Column));

            await ScrollToAsync(address);
        }
    }

    /// <summary>
    /// Invoked by JS interop when a cell is double-clicked with the pointer.
    /// </summary>
    [JSInvokable]
    public async Task OnCellDoubleClickAsync(CellEventArgs args)
    {
        ArgumentNullException.ThrowIfNull(args);

        if (!IsFeatureAllowed(SpreadsheetFeature.Editing))
        {
            return;
        }

        if (Worksheet != null)
        {
            var address = Worksheet.MergedCells.GetMergedRangeStartOrSelf(new CellRef(args.Row, args.Column));

            var cell = Worksheet.Cells[address];

            if (cell != null)
            {
                await ScrollToAsync(address);

                Editor!.StartEdit(address, cell.GetValue());
            }
        }
    }

    /// <summary>
    /// Scrolls the spreadsheet to the specified cell address.
    /// </summary>
    public async Task ScrollToAsync(CellRef address)
    {
        if (grid is not null)
        {
            await grid.ScrollToAsync(address.Row, address.Column);
        }
    }

    private static string TranslateShortcut(KeyboardEventArgs args)
    {
        var key = new StringBuilder();

        if (args.CtrlKey || args.MetaKey)
        {
            key.Append("Ctrl+");
        }

        if (args.AltKey)
        {
            key.Append("Alt+");
        }

        if (args.ShiftKey)
        {
            key.Append("Shift+");
        }

        key.Append(args.Code
            .Replace("Key", "", StringComparison.Ordinal)
            .Replace("Digit", "", StringComparison.Ordinal)
            .Replace("Numpad", "", StringComparison.Ordinal));

        return key.ToString();
    }

    /// <summary>
    /// Invoked by JS interop when a key is pressed down.
    /// </summary>
    [JSInvokable]
    public async Task OnKeyDownAsync(KeyboardEventArgs args, bool isGridContext)
    {
        ArgumentNullException.ThrowIfNull(args);

        // When an image/chart is selected, the grid enters the drawing layer: arrows move it, Tab
        // cycles, Esc returns to the grid. Keys the layer doesn't claim fall through to the cell keys.
        if (isGridContext && Worksheet is not null &&
            (Worksheet.SelectedImage is not null || Worksheet.SelectedChart is not null) &&
            await TryHandleDrawingKeyAsync(args))
        {
            return;
        }

        var shortcut = TranslateShortcut(args);
        if (shortcuts.TryGetValue(shortcut, out var entry))
        {
            // Grid-only shortcuts run only when focus is in the grid or its editor; global ones
            // (F6 region escape, undo/redo) run from anywhere so the chrome stays operable.
            if (isGridContext || entry.Global)
            {
                await entry.Action(args);
            }

            return;
        }

        // Type-to-edit only applies inside the grid - never when a toolbar/chrome control has focus.
        if (!isGridContext)
        {
            return;
        }

        if (args.CtrlKey || args.MetaKey || args.AltKey || args.Key == "Shift")
        {
            return;
        }

        if (!IsFeatureAllowed(SpreadsheetFeature.Editing))
        {
            return;
        }

        // Only single-character keys are printable. This guards IME composition keys (e.g. "Process")
        // and named keys like "Home"/"Enter" - the latter previously typed their first letter ("H").
        if (args.Key.Length != 1)
        {
            return;
        }

        var ch = args.Key[0];

        if (char.IsLetterOrDigit(ch) || char.IsPunctuation(ch) || char.IsSymbol(ch) || char.IsSeparator(ch))
        {
            Editor?.StartEdit(Worksheet!.Selection.Cell, ch.ToString());
        }
    }

    /// <summary>
    /// Invoked by JS interop when the column resize handle is pressed.
    /// </summary>
    [JSInvokable]
    public async Task<bool> OnColumnResizePointerDownAsync(CellEventArgs args)
    {
        ArgumentNullException.ThrowIfNull(args);

        if (!IsFeatureAllowed(SpreadsheetFeature.Resizing))
        {
            return false;
        }

        var result = await AcceptAsync();

        if (result)
        {
            if (grid is not null)
            {
                var capture = new ColumnResizeCapture
                {
                    ScrollTop = grid.ScrollTop,
                    ScrollLeft = grid.ScrollLeft,
                    Column = args.Column,
                    StartX = args.Pointer.ClientX,
                    StartWidth = Worksheet?.Columns[args.Column] ?? 100
                };

                activeCapture = capture;
            }
        }

        return result;
    }

    /// <summary>
    /// Invoked by JS interop when the row resize handle is pressed.
    /// </summary>
    [JSInvokable]
    public async Task<bool> OnRowResizePointerDownAsync(CellEventArgs args)
    {
        ArgumentNullException.ThrowIfNull(args);

        if (!IsFeatureAllowed(SpreadsheetFeature.Resizing))
        {
            return false;
        }

        var result = await AcceptAsync();

        if (result)
        {
            if (grid is not null)
            {
                var capture = new RowResizeCapture
                {
                    ScrollTop = grid.ScrollTop,
                    ScrollLeft = grid.ScrollLeft,
                    Row = args.Row,
                    StartY = args.Pointer.ClientY,
                    StartHeight = Worksheet?.Rows[args.Row] ?? 20
                };

                activeCapture = capture;
            }
        }

        return result;
    }

    /// <summary>
    /// Invoked by JS interop when the pointer moves while resizing a column.
    /// </summary>
    [JSInvokable]
    public Task OnColumnResizePointerMoveAsync(PointerEventArgs args)
    {
        ArgumentNullException.ThrowIfNull(args);
        if (activeCapture is ColumnResizeCapture capture)
        {
            return OnColumnResizePointerMoveAsync(capture, args);
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// Invoked by JS interop when the pointer moves while resizing a row.
    /// </summary>
    [JSInvokable]
    public Task OnRowResizePointerMoveAsync(PointerEventArgs args)
    {
        ArgumentNullException.ThrowIfNull(args);
        if (activeCapture is RowResizeCapture capture)
        {
            return OnRowResizePointerMoveAsync(capture, args);
        }
        return Task.CompletedTask;
    }

    internal const double MinColumnWidth = 24;
    internal const double MinRowHeight = 16;

    private Task OnColumnResizePointerMoveAsync(ColumnResizeCapture capture, PointerEventArgs pointer)
    {
        if (Worksheet != null && capture.Column >= 0 && capture.Column < Worksheet.Columns.Count)
        {
            var delta = pointer.ClientX - capture.StartX;
            var newWidth = Math.Max(MinColumnWidth, capture.StartWidth + delta);
            Worksheet.Columns[capture.Column] = newWidth;
        }

        return Task.CompletedTask;
    }

    private Task OnRowResizePointerMoveAsync(RowResizeCapture capture, PointerEventArgs pointer)
    {
        if (Worksheet != null && capture.Row >= 0 && capture.Row < Worksheet.Rows.Count)
        {
            var delta = pointer.ClientY - capture.StartY;
            var newHeight = Math.Max(MinRowHeight, capture.StartHeight + delta);
            Worksheet.Rows[capture.Row] = newHeight;
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Invoked by JS interop when the pointer is released after resizing a column.
    /// </summary>
    [JSInvokable]
    public async Task OnColumnResizePointerUpAsync(PointerEventArgs args)
    {
        // The drag set the width live (non-undoable). On release, revert and re-apply via a command
        // so the resize joins the undo stack as a single step.
        if (activeCapture is ColumnResizeCapture capture && Worksheet != null &&
            capture.Column >= 0 && capture.Column < Worksheet.Columns.Count)
        {
            var finalWidth = Worksheet.Columns[capture.Column];
            if (finalWidth != capture.StartWidth)
            {
                Worksheet.Columns[capture.Column] = capture.StartWidth;
                await ExecuteAsync(new ResizeColumnCommand(Worksheet, capture.Column, capture.StartWidth, finalWidth));
            }
        }

        activeCapture = null;
    }

    /// <summary>
    /// Invoked by JS interop when the pointer is released after resizing a row.
    /// </summary>
    [JSInvokable]
    public async Task OnRowResizePointerUpAsync(PointerEventArgs args)
    {
        if (activeCapture is RowResizeCapture capture && Worksheet != null &&
            capture.Row >= 0 && capture.Row < Worksheet.Rows.Count)
        {
            var finalHeight = Worksheet.Rows[capture.Row];
            if (finalHeight != capture.StartHeight)
            {
                Worksheet.Rows[capture.Row] = capture.StartHeight;
                await ExecuteAsync(new ResizeRowCommand(Worksheet, capture.Row, capture.StartHeight, finalHeight));
            }
        }

        activeCapture = null;
    }

    class ColumnResizeCapture
    {
        public double ScrollTop { get; set; }
        public double ScrollLeft { get; set; }
        public int Column { get; set; }
        public double StartX { get; set; }
        public double StartWidth { get; set; }
    }

    class RowResizeCapture
    {
        public double ScrollTop { get; set; }
        public double ScrollLeft { get; set; }
        public int Row { get; set; }
        public double StartY { get; set; }
        public double StartHeight { get; set; }
    }

    class DrawingResizeCapture
    {
        public SheetImage? Image { get; set; }
        public SheetChart? Chart { get; set; }
        public IAnchoredDrawing? Drawing => Image ?? (IAnchoredDrawing?)Chart;
        public string Direction { get; set; } = "";
        public double StartX { get; set; }
        public double StartY { get; set; }
        public double OriginalWidth { get; set; }
        public double OriginalHeight { get; set; }
        public CellAnchor OriginalFrom { get; set; } = default!;
    }

    class DrawingMoveCapture
    {
        public SheetImage? Image { get; set; }
        public SheetChart? Chart { get; set; }
        public IAnchoredDrawing? Drawing => Image ?? (IAnchoredDrawing?)Chart;
        public double StartX { get; set; }
        public double StartY { get; set; }
        public CellAnchor OriginalFrom { get; set; } = default!;
        public CellAnchor? OriginalTo { get; set; }
    }

    /// <summary>
    /// Invoked by JS interop when a drawing resize handle is pressed.
    /// </summary>
    [JSInvokable]
    public async Task<bool> OnDrawingResizePointerDownAsync(ImageResizeEventArgs args)
    {
        ArgumentNullException.ThrowIfNull(args);
        var result = await AcceptAsync();

        if (!result || Worksheet is null)
        {
            return result;
        }

        DrawingResizeCapture? capture = null;

        if (Worksheet.SelectedImage is SheetImage image)
        {
            capture = new DrawingResizeCapture { Image = image, OriginalWidth = image.Width, OriginalHeight = image.Height, OriginalFrom = image.From.Clone() };
        }
        else if (Worksheet.SelectedChart is SheetChart chart)
        {
            capture = new DrawingResizeCapture { Chart = chart, OriginalWidth = chart.Width, OriginalHeight = chart.Height, OriginalFrom = chart.From.Clone() };
        }

        if (capture is not null)
        {
            capture.Direction = args.Direction;
            capture.StartX = args.Pointer.ClientX;
            capture.StartY = args.Pointer.ClientY;
            activeCapture = capture;
        }

        return result;
    }

    /// <summary>
    /// Invoked by JS interop when the pointer moves while resizing a drawing.
    /// </summary>
    [JSInvokable]
    public Task OnDrawingResizePointerMoveAsync(PointerEventArgs args)
    {
        ArgumentNullException.ThrowIfNull(args);

        if (activeCapture is not DrawingResizeCapture capture || Worksheet is null)
        {
            return Task.CompletedTask;
        }

        var (newWidth, newHeight, newFrom) = CalculateResize(
            capture.Direction, args.ClientX - capture.StartX, args.ClientY - capture.StartY,
            capture.OriginalWidth, capture.OriginalHeight, capture.OriginalFrom);

        if (capture.Image is not null)
        {
            capture.Image.Width = newWidth;
            capture.Image.Height = newHeight;
            capture.Image.From = newFrom;
            NormalizeAnchor(capture.Image.From);
        }
        else if (capture.Chart is not null)
        {
            capture.Chart.Width = newWidth;
            capture.Chart.Height = newHeight;
            capture.Chart.From = newFrom;
            NormalizeAnchor(capture.Chart.From);
        }

        if (capture.Drawing is not null)
        {
            Worksheet.OnDrawingGeometryChanged(capture.Drawing);
        }

        return Task.CompletedTask;
    }

    private const double MinDrawingSize = 10;

    private static (double width, double height, CellAnchor from) CalculateResize(
        string direction, double deltaX, double deltaY,
        double originalWidth, double originalHeight, CellAnchor originalFrom)
    {
        var from = originalFrom.Clone();
        var newWidth = originalWidth;
        var newHeight = originalHeight;

        if (direction.Contains('e', StringComparison.Ordinal))
        {
            newWidth = Math.Max(MinDrawingSize, originalWidth + deltaX);
        }
        else if (direction.Contains('w', StringComparison.Ordinal))
        {
            var clampedDeltaX = Math.Min(deltaX, originalWidth - MinDrawingSize);
            newWidth = originalWidth - clampedDeltaX;
            from.ColumnOffset = originalFrom.ColumnOffset + clampedDeltaX;
        }

        if (direction.Contains('s', StringComparison.Ordinal))
        {
            newHeight = Math.Max(MinDrawingSize, originalHeight + deltaY);
        }
        else if (direction.Contains('n', StringComparison.Ordinal))
        {
            var clampedDeltaY = Math.Min(deltaY, originalHeight - MinDrawingSize);
            newHeight = originalHeight - clampedDeltaY;
            from.RowOffset = originalFrom.RowOffset + clampedDeltaY;
        }

        return (newWidth, newHeight, from);
    }

    /// <summary>
    /// Invoked by JS interop when the pointer is released after resizing a drawing.
    /// </summary>
    [JSInvokable]
    public async Task OnDrawingResizePointerUpAsync(PointerEventArgs args)
    {
        if (activeCapture is not DrawingResizeCapture capture || Worksheet is null)
        {
            return;
        }

        if (capture.Image is SheetImage image)
        {
            var finalWidth = image.Width;
            var finalHeight = image.Height;
            var finalFrom = image.From.Clone();

            image.Width = capture.OriginalWidth;
            image.Height = capture.OriginalHeight;
            image.From = capture.OriginalFrom.Clone();

            await ExecuteAsync(new ResizeAnchoredCommand<SheetImage>(image, finalWidth, finalHeight, SpreadsheetFeature.Images, finalFrom));
        }
        else if (capture.Chart is SheetChart chart)
        {
            var finalWidth = chart.Width;
            var finalHeight = chart.Height;
            var finalFrom = chart.From.Clone();

            chart.Width = capture.OriginalWidth;
            chart.Height = capture.OriginalHeight;
            chart.From = capture.OriginalFrom.Clone();

            await ExecuteAsync(new ResizeAnchoredCommand<SheetChart>(chart, finalWidth, finalHeight, SpreadsheetFeature.Charts, finalFrom));
        }

        activeCapture = null;
    }

    /// <summary>
    /// Invoked by JS interop when a drawing body is pressed to start a move.
    /// </summary>
    [JSInvokable]
    public Task OnDrawingMovePointerDownAsync(PointerEventArgs args)
    {
        ArgumentNullException.ThrowIfNull(args);

        if (Worksheet is null)
        {
            return Task.CompletedTask;
        }

        DrawingMoveCapture? capture = null;

        if (Worksheet.SelectedImage is SheetImage image)
        {
            capture = new DrawingMoveCapture { Image = image, OriginalFrom = image.From.Clone(), OriginalTo = image.To?.Clone() };
        }
        else if (Worksheet.SelectedChart is SheetChart chart)
        {
            capture = new DrawingMoveCapture { Chart = chart, OriginalFrom = chart.From.Clone(), OriginalTo = chart.To?.Clone() };
        }

        if (capture is not null)
        {
            capture.StartX = args.ClientX;
            capture.StartY = args.ClientY;
            activeCapture = capture;
        }

        return Task.CompletedTask;
    }

    private void NormalizeAnchor(CellAnchor anchor)
    {
        if (Worksheet is null)
        {
            return;
        }

        while (anchor.ColumnOffset < 0 && anchor.Column > 0)
        {
            anchor.Column--;
            anchor.ColumnOffset += Worksheet.Columns[anchor.Column];
        }

        while (anchor.Column < Worksheet.ColumnCount - 1)
        {
            var colWidth = Worksheet.Columns[anchor.Column];
            if (anchor.ColumnOffset < colWidth)
            {
                break;
            }

            anchor.ColumnOffset -= colWidth;
            anchor.Column++;
        }

        while (anchor.RowOffset < 0 && anchor.Row > 0)
        {
            anchor.Row--;
            anchor.RowOffset += Worksheet.Rows[anchor.Row];
        }

        while (anchor.Row < Worksheet.RowCount - 1)
        {
            var rowHeight = Worksheet.Rows[anchor.Row];
            if (anchor.RowOffset < rowHeight)
            {
                break;
            }

            anchor.RowOffset -= rowHeight;
            anchor.Row++;
        }

        if (anchor.ColumnOffset < 0)
        {
            anchor.ColumnOffset = 0;
        }

        if (anchor.RowOffset < 0)
        {
            anchor.RowOffset = 0;
        }
    }

    /// <summary>
    /// Invoked by JS interop when the pointer moves while dragging a drawing.
    /// </summary>
    [JSInvokable]
    public Task OnDrawingMovePointerMoveAsync(PointerEventArgs args)
    {
        ArgumentNullException.ThrowIfNull(args);

        if (activeCapture is not DrawingMoveCapture capture || Worksheet is null)
        {
            return Task.CompletedTask;
        }

        var deltaX = args.ClientX - capture.StartX;
        var deltaY = args.ClientY - capture.StartY;

        CellAnchor? from = null;
        CellAnchor? to = null;

        if (capture.Image is SheetImage image)
        {
            from = image.From;
            to = image.To;
        }
        else if (capture.Chart is SheetChart chart)
        {
            from = chart.From;
            to = chart.To;
        }

        if (from is not null)
        {
            from.Column = capture.OriginalFrom.Column;
            from.ColumnOffset = capture.OriginalFrom.ColumnOffset + deltaX;
            from.Row = capture.OriginalFrom.Row;
            from.RowOffset = capture.OriginalFrom.RowOffset + deltaY;
            NormalizeAnchor(from);
        }

        if (to is not null && capture.OriginalTo is not null)
        {
            to.Column = capture.OriginalTo.Column;
            to.ColumnOffset = capture.OriginalTo.ColumnOffset + deltaX;
            to.Row = capture.OriginalTo.Row;
            to.RowOffset = capture.OriginalTo.RowOffset + deltaY;
            NormalizeAnchor(to);
        }

        if (capture.Drawing is not null)
        {
            Worksheet.OnDrawingGeometryChanged(capture.Drawing);
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// Invoked by JS interop when the pointer is released after moving a drawing.
    /// </summary>
    [JSInvokable]
    public async Task OnDrawingMovePointerUpAsync(PointerEventArgs args)
    {
        if (activeCapture is not DrawingMoveCapture capture || Worksheet is null)
        {
            return;
        }

        if (capture.Image is SheetImage image)
        {
            var finalFrom = image.From.Clone();
            var finalTo = image.To?.Clone();

            image.From = capture.OriginalFrom.Clone();
            if (capture.OriginalTo is not null)
            {
                image.To = capture.OriginalTo.Clone();
            }

            await ExecuteAsync(new MoveAnchoredCommand<SheetImage>(image, finalFrom, finalTo, SpreadsheetFeature.Images));
        }
        else if (capture.Chart is SheetChart chart)
        {
            var finalFrom = chart.From.Clone();
            var finalTo = chart.To?.Clone();

            chart.From = capture.OriginalFrom.Clone();
            if (capture.OriginalTo is not null)
            {
                chart.To = capture.OriginalTo.Clone();
            }

            await ExecuteAsync(new MoveAnchoredCommand<SheetChart>(chart, finalFrom, finalTo, SpreadsheetFeature.Charts));
        }

        if (capture.Drawing is not null)
        {
            Worksheet.OnDrawingGeometryChanged(capture.Drawing);
        }

        activeCapture = null;
    }
    /// <summary>
    /// Invoked by JS interop when a column resize handle is double-clicked. Auto fits the column width to its displayed content.
    /// </summary>
    /// <param name="args">The column event arguments containing the target column index.</param>
    [JSInvokable]
    public async Task OnColumnResizeDoubleClickAsync(CellEventArgs args)
    {
        ArgumentNullException.ThrowIfNull(args);

        if (!IsFeatureAllowed(SpreadsheetFeature.Resizing) || Worksheet == null || jsRef == null ||
            args.Column < 0 || args.Column >= Worksheet.Columns.Count || Worksheet.Columns.IsHidden(args.Column))
        {
            return;
        }

        if (!await AcceptAsync())
        {
            return;
        }

        var items = GetAutoFitMeasureItems(Worksheet, args.Column);

        if (items.Count == 0)
        {
            return;
        }

        double[]? widths;

        try
        {
            widths = await jsRef.InvokeAsync<double[]>("measureTexts", items);
        }
        catch (Exception ex) when (ex is JSException or JSDisconnectedException or ObjectDisposedException or TaskCanceledException)
        {
            return;
        }

        if (widths is not { Length: > 0 })
        {
            return;
        }

        var maxWidth = widths.Max();

        if (maxWidth <= 0)
        {
            return;
        }

        var targetWidth = Math.Clamp(maxWidth, MinColumnWidth, ColumnWidthConversion.MaxWidthInPixels);
        var startWidth = Worksheet.Columns[args.Column];

        if (startWidth != targetWidth || !Worksheet.Columns.IsAutoFit(args.Column))
        {
            await ExecuteAsync(new ResizeColumnCommand(Worksheet, args.Column, startWidth, targetWidth, isAutoFit: true));
        }
    }

    private const int AutoFitMaxTextLength = 1000;
    private const int AutoFitCandidatesPerFontGroup = 20;
    private const int AutoFitMaxMeasureItems = 200;

    private static List<TextMeasureItem> GetAutoFitMeasureItems(Worksheet sheet, int column)
    {
        // Snapshot: computing effective formats can populate cells in the store.
        var cells = sheet.Cells.GetPopulatedCells().Where(cell => cell.Address.Column == column).ToList();

        var conditionalRanges = sheet.ConditionalFormats.Ranges
            .Where(range => range.Start.Column <= column && column <= range.End.Column)
            .ToList();

        var seen = new HashSet<(string, bool, bool, double?, string?)>();
        var candidates = new List<(Cell Cell, string Text, (bool Bold, bool Italic, double? FontSize, string? FontFamily) Font)>();

        foreach (var cell in cells)
        {
            if (sheet.MergedCells.Contains(cell.Address))
            {
                continue;
            }

            string? text;

            try
            {
                text = cell.GetDisplayText();
            }
            catch (ArgumentOutOfRangeException)
            {
                continue;
            }

            if (string.IsNullOrEmpty(text))
            {
                continue;
            }

            var format = cell.FormatOrNull;

            text = ExcelTextMetrics.DisplayLine(text, format?.WrapText == true);

            if (text.Length > AutoFitMaxTextLength)
            {
                text = text[..AutoFitMaxTextLength];
            }

            var font = (format?.Bold == true, format?.Italic == true, format?.FontSize, format?.FontFamily);

            // Conditionally formatted cells are exempt from dedupe - an unstyled twin must not replace them.
            var conditional = conditionalRanges.Any(range => range.Contains(cell.Address));

            if (!conditional && !seen.Add((text, font.Item1, font.Item2, font.Item3, font.Item4)))
            {
                continue;
            }

            candidates.Add((cell, text, font));
        }

        var survivors = candidates
            .GroupBy(candidate => candidate.Font)
            .SelectMany(group => group.OrderByDescending(candidate => candidate.Text.Length).Take(AutoFitCandidatesPerFontGroup));

        var items = new List<TextMeasureItem>();

        foreach (var (cell, text, font) in survivors)
        {
            var (bold, italic, fontSize, fontFamily) = font;

            if (conditionalRanges.Count > 0)
            {
                Format? effective;

                try
                {
                    effective = cell.GetEffectiveFormat();
                }
                catch (ArgumentOutOfRangeException)
                {
                    continue;
                }

                bold = effective?.Bold == true;
                italic = effective?.Italic == true;
                fontSize = effective?.FontSize;
                fontFamily = effective?.FontFamily;
            }

            if (fontSize is { } size && !double.IsFinite(size))
            {
                fontSize = null;
            }

            items.Add(new TextMeasureItem
            {
                Text = text,
                Bold = bold,
                Italic = italic,
                FontSize = fontSize,
                FontFamily = fontFamily
            });
        }

        if (items.Count > AutoFitMaxMeasureItems)
        {
            items = items.OrderByDescending(item => item.Text.Length).Take(AutoFitMaxMeasureItems).ToList();
        }

        return items;
    }

    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        activeCapture = null;

        if (jsRef is not null)
        {
            try
            {
                await jsRef.InvokeVoidAsync("dispose");
                await jsRef.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
            }
        }

        dotNetRef?.Dispose();
    }
}