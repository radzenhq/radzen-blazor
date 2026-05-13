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
    /// Executes a command through the active view's undo/redo stack.
    /// </summary>
    bool Execute(ICommand command);

    /// <summary>
    /// Undoes the last command.
    /// </summary>
    void Undo();

    /// <summary>
    /// Redoes the last undone command.
    /// </summary>
    void Redo();

    /// <summary>
    /// Gets whether there is a command to undo.
    /// </summary>
    bool CanUndo { get; }

    /// <summary>
    /// Gets whether there is a command to redo.
    /// </summary>
    bool CanRedo { get; }

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

    /// <inheritdoc/>
    protected override string GetComponentCssClass() => "rz-spreadsheet";

    private VirtualGrid? grid;
    private Popup? cellMenuPopup;
    private Popup? validationListPopup;
    private RangePickerBar? rangePickerBar;
    private int cellMenuRow = -1;
    private int cellMenuColumn = -1;
    private int validationListRow = -1;
    private int validationListColumn = -1;
    private IReadOnlyList<string> validationListItems = [];
    private RangeRef autofillSource = RangeRef.Invalid;
    private PointerEventArgs? autofillStartPointer;
    private RangeRef? autofillPreviewRange;

    /// <inheritdoc/>
    public override async Task SetParametersAsync(ParameterView parameters)
    {
        var didWorkbookChange = parameters.DidParameterChange(nameof(Workbook), Workbook);

        await base.SetParametersAsync(parameters);

        if (didWorkbookChange)
        {
            workbook = Workbook;
            workbookView = null;
            sheetIndex = 0;
        }

        // Always reconcile our worksheet event subscriptions so the contextual
        // "Table Design" tab can flip its Visible flag whenever the active cell
        // moves into or out of a table.
        SubscribeToWorksheetEvents(Worksheet);
    }

    private int sheetIndex;

    private Worksheet? Worksheet => workbook != null && sheetIndex < workbook.Sheets.Count ? workbook.Sheets[sheetIndex] : null;

    /// <summary>
    /// True when the active cell sits inside a Table — drives visibility of the contextual
    /// "Table Design" tab. Maintained as a flag rather than a computed property because
    /// RadzenTabsItem.Visible is captured at parameter-set time; we need an explicit
    /// StateHasChanged() each time the value flips.
    /// </summary>
    private bool hasTableSelection;
    private Worksheet? subscribedWorksheet;

    private void SubscribeToWorksheetEvents(Worksheet? next)
    {
        if (ReferenceEquals(subscribedWorksheet, next)) return;

        if (subscribedWorksheet is not null)
        {
            subscribedWorksheet.Selection.Changed -= OnTableSelectionMaybeChanged;
            subscribedWorksheet.AutoFilterChanged -= OnTableSelectionMaybeChanged;
        }

        subscribedWorksheet = next;

        if (subscribedWorksheet is not null)
        {
            subscribedWorksheet.Selection.Changed += OnTableSelectionMaybeChanged;
            subscribedWorksheet.AutoFilterChanged += OnTableSelectionMaybeChanged;
        }

        UpdateHasTableSelection();
    }

    private void OnTableSelectionMaybeChanged() => UpdateHasTableSelection();

    private void UpdateHasTableSelection()
    {
        var next = ComputeHasTableSelection();
        if (next != hasTableSelection)
        {
            hasTableSelection = next;
            StateHasChanged();
        }
    }

    private bool ComputeHasTableSelection()
    {
        if (Worksheet is null) return false;
        var sel = Worksheet.Selection.Cell;
        if (sel == CellRef.Invalid) return false;
        foreach (var t in Worksheet.Tables)
        {
            if (t.Range.Contains(sel.Row, sel.Column)) return true;
        }
        return false;
    }

    private WorkbookView? workbookView;

    private SheetView? ActiveView => Worksheet != null ? (workbookView ??= new WorkbookView(workbook!)).GetView(Worksheet) : null;

    private Editor? Editor => ActiveView?.Editor;

    /// <inheritdoc/>
    public bool Execute(ICommand command)
    {
        if (command is Spreadsheet.IProtectedCommand pc && Worksheet?.Protection.IsActionBlocked(pc.RequiredAction) == true)
        {
            return false;
        }

        return ActiveView?.Commands.Execute(command) ?? false;
    }

    /// <inheritdoc/>
    public void Undo() => ActiveView?.Commands.Undo();

    /// <inheritdoc/>
    public void Redo() => ActiveView?.Commands.Redo();

    /// <inheritdoc/>
    public bool CanUndo => ActiveView?.Commands.CanUndo ?? false;

    /// <inheritdoc/>
    public bool CanRedo => ActiveView?.Commands.CanRedo ?? false;

    private async Task OnWorkbookChangedAsync(Workbook? value)
    {
        workbook = value;

        await WorkbookChanged.InvokeAsync(value);
    }

    private async Task OnSheetTabChanged(int index)
    {
        await AcceptAsync();

        if (cellMenuPopup != null)
        {
            await cellMenuPopup.CloseAsync();
        }

        if (validationListPopup != null)
        {
            await validationListPopup.CloseAsync();
        }

        sheetIndex = index;
    }

    private async Task OnAddSheetAsync()
    {
        await AcceptAsync();

        if (workbook is null || workbook.Protection.LockStructure)
        {
            return;
        }

        var name = GenerateSheetName();
        workbook.AddSheet(name, 100, 26);
        sheetIndex = workbook.Sheets.Count - 1;
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
                OnMoveSheetLeft(sheet);
                break;
            case "move-right":
                OnMoveSheetRight(sheet);
                break;
        }
    }

    private async Task OnRemoveSheetAsync(Worksheet sheet)
    {
        await AcceptAsync();

        if (workbook is null || workbook.Sheets.Count <= 1 || workbook.Protection.LockStructure)
        {
            return;
        }

        var removedIndex = workbook.IndexOf(sheet);
        workbook.RemoveSheet(sheet);
        workbookView?.Remove(sheet);

        if (sheetIndex >= workbook.Sheets.Count)
        {
            sheetIndex = workbook.Sheets.Count - 1;
        }
        else if (removedIndex < sheetIndex)
        {
            sheetIndex--;
        }
    }

    private async Task OnRenameSheetAsync(Worksheet sheet)
    {
        if (workbook?.Protection.LockStructure == true)
        {
            return;
        }

        var existingNames = workbook!.Sheets
            .Where(s => s != sheet)
            .Select(s => s.Name)
            .ToList();

        var name = await DialogService.OpenAsync<Spreadsheet.RenameSheetDialog>(Localize(nameof(RadzenStrings.Spreadsheet_RenameSheetTitle)),
            new Dictionary<string, object?> { { "Name", sheet.Name }, { "ExistingNames", existingNames } },
            new DialogOptions { Width = "300px" });

        if (name is string newName && !string.IsNullOrWhiteSpace(newName))
        {
            sheet.Name = newName;
        }
    }

    private void OnMoveSheetLeft(Worksheet sheet)
    {
        if (workbook is null || workbook.Protection.LockStructure)
        {
            return;
        }

        var index = workbook.IndexOf(sheet);

        if (index > 0)
        {
            workbook.MoveSheet(index, index - 1);

            if (sheetIndex == index)
            {
                sheetIndex--;
            }
            else if (sheetIndex == index - 1)
            {
                sheetIndex++;
            }
        }
    }

    private void OnMoveSheetRight(Worksheet sheet)
    {
        if (workbook is null || workbook.Protection.LockStructure)
        {
            return;
        }

        var index = workbook.IndexOf(sheet);

        if (index < workbook.Sheets.Count - 1)
        {
            workbook.MoveSheet(index, index + 1);

            if (sheetIndex == index)
            {
                sheetIndex++;
            }
            else if (sheetIndex == index + 1)
            {
                sheetIndex--;
            }
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
            Execute(command);
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
            // Check if we're in a data table
            foreach (var table in Worksheet.Tables)
            {
                if (table.Range.Contains(cellMenuRow, cellMenuColumn))
                {
                    var command = new SortCommand(Worksheet, table.Range, order, cellMenuColumn);
                    Execute(command);
                    break;
                }
            }

            // Check if we're in an auto filter
            if (Worksheet.AutoFilter.Range is not null && Worksheet.AutoFilter.Range.Value.Contains(cellMenuRow, cellMenuColumn))
            {
                var command = new SortCommand(Worksheet, Worksheet.AutoFilter.Range.Value, order, cellMenuColumn, skipHeaderRow: true);
                Execute(command);
            }
        }

        if (cellMenuPopup != null)
        {
            await cellMenuPopup.CloseAsync();
        }
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
                Execute(command);
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

            var result = await DialogService.OpenAsync<FilterDialog>(Localize(nameof(RadzenStrings.Spreadsheet_CustomFilterTitle)), parameters, new DialogOptions
            {
                Width = "600px",
            });

            if (result is SheetFilter filter)
            {
                var command = new FilterCommand(Worksheet, filter);
                Execute(command);
            }
        }
    }

    private async Task OnCellMenuTop10FilterAsync()
    {
        if (cellMenuPopup != null) await cellMenuPopup.CloseAsync();
        if (Worksheet is null) return;

        var range = ResolveColumnFilterRange();
        if (range == RangeRef.Invalid) return;

        var result = await DialogService.OpenAsync<Spreadsheet.Top10FilterDialog>(
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
            Execute(new FilterCommand(Worksheet, new SheetFilter(criterion, sliceRange)));
        }
    }

    private void OnCellMenuDynamicFilterAsync(DynamicFilterType type)
    {
        if (Worksheet is null) return;
        _ = cellMenuPopup?.CloseAsync();

        var range = ResolveColumnFilterRange();
        if (range == RangeRef.Invalid) return;

        var sliceRange = new RangeRef(
            new CellRef(range.Start.Row, cellMenuColumn),
            new CellRef(range.End.Row, cellMenuColumn));
        var criterion = new DynamicFilterCriterion { Column = cellMenuColumn, Type = type };
        Execute(new FilterCommand(Worksheet, new SheetFilter(criterion, sliceRange)));
    }

    private RangeRef ResolveColumnFilterRange()
    {
        if (Worksheet is null) return RangeRef.Invalid;

        foreach (var t in Worksheet.Tables)
        {
            if (t.Range.Contains(cellMenuRow, cellMenuColumn)) return t.Range;
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

            var result = await DialogService.OpenAsync<FormatCellsDialog>(
                Localize(nameof(RadzenStrings.Spreadsheet_FormatCellsTitle)), parameters, new DialogOptions { Width = "600px", Height = "480px" });

            if (result is string formatCode)
            {
                var format = string.Equals(formatCode, "General", StringComparison.OrdinalIgnoreCase) ? null : formatCode;
                var command = new FormatCommand(Worksheet, Worksheet.Selection.Range, cell.Format.WithNumberFormat(format));
                Execute(command);
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

                var valid = Execute(command);

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
        if (await AcceptAsync())
        {
            if (Worksheet is not null)
            {
                var address = Worksheet.Selection.Cycle(rowOffset, columnOffset);

                await ScrollToAsync(address);
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

    private Task CancelEditAsync()
    {
        if (Editor?.Mode != EditMode.None)
        {
            Editor?.Cancel();
        }

        return Task.CompletedTask;
    }

    private readonly Dictionary<string, Func<KeyboardEventArgs, Task>> shortcuts = [];
    private readonly SpreadsheetClipboard clipboard = new();

    /// <inheritdoc/>
    protected override void OnInitialized()
    {
        workbook = Workbook;
        shortcuts.Add("Enter", _ => CycleSelectionAsync(1, 0));
        shortcuts.Add("Escape", _ => CancelEditAsync());
        shortcuts.Add("Tab", _ => CycleSelectionAsync(0, 1));
        shortcuts.Add("ArrowUp", _ => MoveSelectionAsync(-1, 0));
        shortcuts.Add("ArrowDown", _ => MoveSelectionAsync(1, 0));
        shortcuts.Add("ArrowLeft", _ => MoveSelectionAsync(0, -1));
        shortcuts.Add("ArrowRight", _ => MoveSelectionAsync(0, 1));
        shortcuts.Add("Shift+Tab", _ => CycleSelectionAsync(0, -1));
        shortcuts.Add("Shift+Enter", _ => CycleSelectionAsync(-1, 0));
        shortcuts.Add("Shift+ArrowUp", _ => ExtendSelectionAsync(-1, 0));
        shortcuts.Add("Shift+ArrowDown", _ => ExtendSelectionAsync(1, 0));
        shortcuts.Add("Shift+ArrowLeft", _ => ExtendSelectionAsync(0, -1));
        shortcuts.Add("Shift+ArrowRight", _ => ExtendSelectionAsync(0, 1));
        shortcuts.Add("Ctrl+C", _ => CopySelectionAsync());
        shortcuts.Add("Ctrl+Z", _ => UndoAsync());
        shortcuts.Add("Ctrl+X", _ => CutSelectionAsync());
        shortcuts.Add("Ctrl+Shift+Z", _ => RedoAsync());
        shortcuts.Add("Delete", _ => DeleteSelectedAsync());
        shortcuts.Add("Backspace", _ => DeleteSelectedAsync());
    }

    private Task DeleteSelectedAsync()
    {
        if (Worksheet?.SelectedImage != null)
        {
            Execute(new DeleteImageCommand(Worksheet, Worksheet.SelectedImage));
        }
        else if (Worksheet?.SelectedChart != null)
        {
            Execute(new DeleteChartCommand(Worksheet, Worksheet.SelectedChart));
        }
        else if (Worksheet != null && Worksheet.Selection.Range != RangeRef.Invalid)
        {
            Execute(new ClearContentsCommand(Worksheet, Worksheet.Selection.Range));
        }

        return Task.CompletedTask;
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

    private async Task CutSelectionAsync()
    {
        if (Worksheet is not null)
        {
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
    public Task OnPasteAsync(string text)
    {
        if (Worksheet is not null && Worksheet.IsCellEditable(Worksheet.Selection.Cell))
        {
            clipboard.Paste(Worksheet, Worksheet.Selection.Cell, text);
        }

        return Task.CompletedTask;
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

        var menuItems = new List<ContextMenuItem>
        {
            new() { Text = Localize(nameof(RadzenStrings.Spreadsheet_Cut)), Value = "cut", Icon = "content_cut" },
            new() { Text = Localize(nameof(RadzenStrings.Spreadsheet_Copy)), Value = "copy", Icon = "content_copy" },
            new() { Text = Localize(nameof(RadzenStrings.Spreadsheet_Paste)), Value = "paste", Icon = "content_paste" },
            new() { Text = Localize(nameof(RadzenStrings.Spreadsheet_ClearContents)), Value = "clear", Icon = "clear" },
            new() { Text = Localize(nameof(RadzenStrings.Spreadsheet_ContextSortAscending)), Value = "sort-ascending", Icon = "arrow_upward" },
            new() { Text = Localize(nameof(RadzenStrings.Spreadsheet_ContextSortDescending)), Value = "sort-descending", Icon = "arrow_downward" },
        };

        // If the active cell is inside a Table, add table-management actions.
        if (FindTableAt(row, column) is not null)
        {
            menuItems.Add(new() { Text = Localize(nameof(RadzenStrings.Spreadsheet_ConvertTableToRange)),
                Value = "convert-table-to-range", Icon = "grid_off" });
            menuItems.Add(new() { Text = Localize(nameof(RadzenStrings.Spreadsheet_DeleteTable)),
                Value = "delete-table", Icon = "delete" });
        }

        ContextMenuService.Open(args.Pointer, menuItems,
            menuArgs => OnContextMenuItemClick(menuArgs, row, column));
    }

    private Table? FindTableAt(int row, int column)
    {
        if (Worksheet is null) return null;
        foreach (var t in Worksheet.Tables)
        {
            if (t.Range.Contains(row, column)) return t;
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

        ContextMenuService.Open(args.Pointer, new List<ContextMenuItem>
        {
            new() { Text = Localize(nameof(RadzenStrings.Spreadsheet_Cut)), Value = "cut", Icon = "content_cut" },
            new() { Text = Localize(nameof(RadzenStrings.Spreadsheet_Copy)), Value = "copy", Icon = "content_copy" },
            new() { Text = Localize(nameof(RadzenStrings.Spreadsheet_Paste)), Value = "paste", Icon = "content_paste" },
            new() { Text = Localize(nameof(RadzenStrings.Spreadsheet_InsertRowAbove)), Value = "insert-row-before", Icon = "north" },
            new() { Text = Localize(nameof(RadzenStrings.Spreadsheet_InsertRowBelow)), Value = "insert-row-after", Icon = "south" },
            new() { Text = Localize(nameof(RadzenStrings.Spreadsheet_DeleteRow)), Value = "delete-row", Icon = "delete" },
            new() { Text = Localize(nameof(RadzenStrings.Spreadsheet_HideRow)), Value = "hide-row", Icon = "visibility_off" },
            new() { Text = Localize(nameof(RadzenStrings.Spreadsheet_UnhideRow)), Value = "unhide-row", Icon = "visibility" },
        }, menuArgs => OnRowContextMenuItemClick(menuArgs, row));
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

        ContextMenuService.Open(args.Pointer, new List<ContextMenuItem>
        {
            new() { Text = Localize(nameof(RadzenStrings.Spreadsheet_Cut)), Value = "cut", Icon = "content_cut" },
            new() { Text = Localize(nameof(RadzenStrings.Spreadsheet_Copy)), Value = "copy", Icon = "content_copy" },
            new() { Text = Localize(nameof(RadzenStrings.Spreadsheet_Paste)), Value = "paste", Icon = "content_paste" },
            new() { Text = Localize(nameof(RadzenStrings.Spreadsheet_InsertColumnBefore)), Value = "insert-column-before", Icon = "west" },
            new() { Text = Localize(nameof(RadzenStrings.Spreadsheet_InsertColumnAfter)), Value = "insert-column-after", Icon = "east" },
            new() { Text = Localize(nameof(RadzenStrings.Spreadsheet_DeleteColumn)), Value = "delete-column", Icon = "delete" },
            new() { Text = Localize(nameof(RadzenStrings.Spreadsheet_HideColumn)), Value = "hide-column", Icon = "visibility_off" },
            new() { Text = Localize(nameof(RadzenStrings.Spreadsheet_UnhideColumn)), Value = "unhide-column", Icon = "visibility" },
        }, menuArgs => OnColumnContextMenuItemClick(menuArgs, column));
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

    private void OnContextMenuItemClick(MenuItemEventArgs args, int row, int column)
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
                    Execute(new ClearContentsCommand(Worksheet, Worksheet.Selection.Range));
                    break;
                case "sort-ascending":
                    Execute(new SortCommand(Worksheet, new RangeRef(new CellRef(0, 0), new CellRef(Worksheet.RowCount - 1, Worksheet.ColumnCount - 1)), SortOrder.Ascending, column));
                    break;
                case "sort-descending":
                    Execute(new SortCommand(Worksheet, new RangeRef(new CellRef(0, 0), new CellRef(Worksheet.RowCount - 1, Worksheet.ColumnCount - 1)), SortOrder.Descending, column));
                    break;
                case "convert-table-to-range":
                case "delete-table":
                    {
                        var table = FindTableAt(row, column);
                        if (table is not null)
                        {
                            Execute(new RemoveTableCommand(Worksheet, table));
                        }
                    }
                    break;
            }
        }

        StateHasChanged();
    }

    private void OnRowContextMenuItemClick(MenuItemEventArgs args, int row)
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
                    Execute(new InsertRowBeforeCommand(Worksheet, row));
                    break;
                case "insert-row-after":
                    Execute(new InsertRowAfterCommand(Worksheet, row));
                    break;
                case "delete-row":
                    Execute(new DeleteRowsCommand(Worksheet, row, row));
                    break;
                case "hide-row":
                    Worksheet.Rows.Hide(row);
                    break;
                case "unhide-row":
                    Worksheet.Rows.Show(row);
                    break;
            }
        }

        StateHasChanged();
    }

    private void OnColumnContextMenuItemClick(MenuItemEventArgs args, int column)
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
                    Execute(new InsertColumnBeforeCommand(Worksheet, column));
                    break;
                case "insert-column-after":
                    Execute(new InsertColumnAfterCommand(Worksheet, column));
                    break;
                case "delete-column":
                    Execute(new DeleteColumnsCommand(Worksheet, column, column));
                    break;
                case "hide-column":
                    Worksheet.Columns.Hide(column);
                    break;
                case "unhide-column":
                    Worksheet.Columns.Show(column);
                    break;
            }
        }

        StateHasChanged();
    }

    private async Task PasteFromClipboardAsync()
    {
        if (Worksheet is null || !Worksheet.IsCellEditable(Worksheet.Selection.Cell))
        {
            return;
        }

        string? text = null;

        if (jsRef is not null)
        {
            text = await jsRef.InvokeAsync<string?>("readClipboardText");
        }

        if (!string.IsNullOrEmpty(text))
        {
            clipboard.Paste(Worksheet, Worksheet.Selection.Cell, text);
        }
        else
        {
            clipboard.Paste(Worksheet, Worksheet.Selection.Cell);
        }
    }

    private IJSObjectReference? jsRef;
    private DotNetObjectReference<RadzenSpreadsheet>? dotNetRef;

    /// <inheritdoc/>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && JSRuntime != null)
        {
            dotNetRef = DotNetObjectReference.Create(this);
            jsRef = await JSRuntime.InvokeAsync<IJSObjectReference>("Radzen.createSpreadsheet", new { Element, dotNetRef, shortcuts = shortcuts.Keys });
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

        if (Worksheet is null)
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
            autofillPreviewRange = fillRange;
            StateHasChanged();
        }
    }

    /// <summary>
    /// Invoked by JS interop when the pointer is released after an autofill drag.
    /// </summary>
    [JSInvokable]
    public void OnAutofillPointerUpAsync(PointerEventArgs args)
    {
        ArgumentNullException.ThrowIfNull(args);

        var fillRange = autofillPreviewRange;

        autofillPreviewRange = null;

        if (autofillSource != RangeRef.Invalid && Worksheet is not null && fillRange is not null && fillRange.Value != autofillSource)
        {
            var direction = Spreadsheet.AutofillCommand.GetDirection(autofillSource, fillRange.Value);
            var command = new Spreadsheet.AutofillCommand(Worksheet, autofillSource, fillRange.Value, direction);
            Execute(command);

            Worksheet.Selection.Select(fillRange.Value);
        }

        autofillSource = RangeRef.Invalid;
        autofillStartPointer = null;

        StateHasChanged();
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
    public async Task OnKeyDownAsync(KeyboardEventArgs args)
    {
        ArgumentNullException.ThrowIfNull(args);
        if (shortcuts.TryGetValue(TranslateShortcut(args), out var action))
        {
            await action(args);

            return;
        }

        if (args.CtrlKey || args.MetaKey || args.AltKey || args.Key == "Shift")
        {
            return;
        }

        var ch = args.Key == "Space" ? ' ' : args.Key[0];

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

    private Task OnColumnResizePointerMoveAsync(ColumnResizeCapture capture, PointerEventArgs pointer)
    {
        if (Worksheet != null && capture.Column >= 0 && capture.Column < Worksheet.Columns.Count)
        {
            var delta = pointer.ClientX - capture.StartX;
            var newWidth = Math.Max(24, capture.StartWidth + delta);
            Worksheet.Columns[capture.Column] = newWidth;
            StateHasChanged();
        }

        return Task.CompletedTask;
    }

    private Task OnRowResizePointerMoveAsync(RowResizeCapture capture, PointerEventArgs pointer)
    {
        if (Worksheet != null && capture.Row >= 0 && capture.Row < Worksheet.Rows.Count)
        {
            var delta = pointer.ClientY - capture.StartY;
            var newHeight = Math.Max(16, capture.StartHeight + delta);
            Worksheet.Rows[capture.Row] = newHeight;
            StateHasChanged();
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Invoked by JS interop when the pointer is released after resizing a column.
    /// </summary>
    [JSInvokable]
    public Task OnColumnResizePointerUpAsync(PointerEventArgs args)
    {
        activeCapture = null;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Invoked by JS interop when the pointer is released after resizing a row.
    /// </summary>
    [JSInvokable]
    public Task OnRowResizePointerUpAsync(PointerEventArgs args)
    {
        activeCapture = null;
        return Task.CompletedTask;
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

        if (activeCapture is not DrawingResizeCapture capture)
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

        StateHasChanged();
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
    public Task OnDrawingResizePointerUpAsync(PointerEventArgs args)
    {
        if (activeCapture is not DrawingResizeCapture capture || Worksheet is null)
        {
            return Task.CompletedTask;
        }

        if (capture.Image is SheetImage image)
        {
            var finalWidth = image.Width;
            var finalHeight = image.Height;
            var finalFrom = image.From.Clone();

            image.Width = capture.OriginalWidth;
            image.Height = capture.OriginalHeight;
            image.From = capture.OriginalFrom.Clone();

            Execute(new ResizeImageCommand(image, finalWidth, finalHeight, finalFrom));
        }
        else if (capture.Chart is SheetChart chart)
        {
            var finalWidth = chart.Width;
            var finalHeight = chart.Height;
            var finalFrom = chart.From.Clone();

            chart.Width = capture.OriginalWidth;
            chart.Height = capture.OriginalHeight;
            chart.From = capture.OriginalFrom.Clone();

            Execute(new ResizeChartCommand(chart, finalWidth, finalHeight, finalFrom));
        }

        activeCapture = null;
        return Task.CompletedTask;
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
        if (Worksheet is null) return;

        while (anchor.ColumnOffset < 0 && anchor.Column > 0)
        {
            anchor.Column--;
            anchor.ColumnOffset += Worksheet.Columns[anchor.Column];
        }

        while (anchor.Column < Worksheet.ColumnCount - 1)
        {
            var colWidth = Worksheet.Columns[anchor.Column];
            if (anchor.ColumnOffset < colWidth) break;
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
            if (anchor.RowOffset < rowHeight) break;
            anchor.RowOffset -= rowHeight;
            anchor.Row++;
        }

        if (anchor.ColumnOffset < 0) anchor.ColumnOffset = 0;
        if (anchor.RowOffset < 0) anchor.RowOffset = 0;
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

        StateHasChanged();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Invoked by JS interop when the pointer is released after moving a drawing.
    /// </summary>
    [JSInvokable]
    public Task OnDrawingMovePointerUpAsync(PointerEventArgs args)
    {
        if (activeCapture is not DrawingMoveCapture capture || Worksheet is null)
        {
            return Task.CompletedTask;
        }

        if (capture.Image is SheetImage image)
        {
            var finalFrom = image.From.Clone();
            var finalTo = image.To?.Clone();

            image.From = capture.OriginalFrom.Clone();
            if (capture.OriginalTo is not null) image.To = capture.OriginalTo.Clone();

            Execute(new MoveImageCommand(image, finalFrom, finalTo));
        }
        else if (capture.Chart is SheetChart chart)
        {
            var finalFrom = chart.From.Clone();
            var finalTo = chart.To?.Clone();

            chart.From = capture.OriginalFrom.Clone();
            if (capture.OriginalTo is not null) chart.To = capture.OriginalTo.Clone();

            Execute(new MoveChartCommand(chart, finalFrom, finalTo));
        }

        activeCapture = null;
        StateHasChanged();
        return Task.CompletedTask;
    }

    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        activeCapture = null;

        SubscribeToWorksheetEvents(null);

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