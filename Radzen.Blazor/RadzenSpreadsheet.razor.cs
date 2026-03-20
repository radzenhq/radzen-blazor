using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Radzen.Blazor.Spreadsheet;
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
    /// </summary>
    [Parameter]
    public string ExportFileName { get; set; } = "sheet.xlsx";

    /// <summary>
    /// Event callback that is invoked when the workbook changes.
    /// </summary>
    [Parameter]
    public EventCallback<Workbook?> WorkbookChanged { get; set; }

    /// <inheritdoc/>
    protected override string GetComponentCssClass() => "rz-spreadsheet";

    private VirtualGrid? grid;
    private Popup? cellMenuPopup;
    private Popup? validationListPopup;
    private Spreadsheet.InputPrompt? inputPrompt;
    private int cellMenuRow = -1;
    private int cellMenuColumn = -1;
    private int validationListRow = -1;
    private int validationListColumn = -1;
    private IReadOnlyList<string> validationListItems = [];

    /// <inheritdoc/>
    public override async Task SetParametersAsync(ParameterView parameters)
    {
        var didWorkbookChange = parameters.DidParameterChange(nameof(Workbook), Workbook);

        await base.SetParametersAsync(parameters);

        if (didWorkbookChange)
        {
            workbook = Workbook;
        }
    }

    private const int sheetIndex = 0;

    private Sheet? Sheet => workbook?.Sheets[sheetIndex];

    private async Task OnWorkbookChangedAsync(Workbook? value)
    {
        workbook = value;

        await WorkbookChanged.InvokeAsync(value);
    }

    private async Task OnCellToggleAsync(CellMenuToggleEventArgs args)
    {
        ArgumentNullException.ThrowIfNull(args);

        // Check if this cell has list validation
        if (Sheet != null)
        {
            var validators = Sheet.Validation.GetValidatorsForCell(new Spreadsheet.CellRef(args.Row, args.Column));
            foreach (var v in validators)
            {
                if (v is Spreadsheet.DataValidationRule rule && rule.Type == Spreadsheet.DataValidationType.List)
                {
                    validationListRow = args.Row;
                    validationListColumn = args.Column;
                    validationListItems = rule.GetListItems(Sheet);
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

    private async Task OnValidationListValueSelectedAsync(string value)
    {
        if (Sheet != null && validationListRow >= 0 && validationListColumn >= 0)
        {
            var address = new Spreadsheet.CellRef(validationListRow, validationListColumn);
            Sheet.Editor.StartEdit(address, value);
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
        if (filter != null && Sheet != null)
        {
            var command = new FilterCommand(Sheet, filter);
            Sheet.Commands.Execute(command);
        }

        if (cellMenuPopup != null)
        {
            await cellMenuPopup.CloseAsync();
        }
    }

    private async Task OnCellMenuSortAscendingAsync()
    {
        if (Sheet != null)
        {
            // Check if we're in a data table
            foreach (var table in Sheet.Tables)
            {
                if (table.Range.Contains(cellMenuRow, cellMenuColumn))
                {
                    var command = new SortCommand(Sheet, table.Range, SortOrder.Ascending, cellMenuColumn);
                    Sheet.Commands.Execute(command);
                    break;
                }
            }

            // Check if we're in an auto filter
            if (Sheet.AutoFilter != null && Sheet.AutoFilter.Range.Contains(cellMenuRow, cellMenuColumn))
            {
                var command = new SortCommand(Sheet, Sheet.AutoFilter.Range, SortOrder.Ascending, cellMenuColumn, skipHeaderRow: true);
                Sheet.Commands.Execute(command);
            }
        }

        if (cellMenuPopup != null)
        {
            await cellMenuPopup.CloseAsync();
        }
    }

    private async Task OnCellMenuSortDescendingAsync()
    {
        if (Sheet != null)
        {
            // Check if we're in a data table
            foreach (var table in Sheet.Tables)
            {
                if (table.Range.Contains(cellMenuRow, cellMenuColumn))
                {
                    var command = new SortCommand(Sheet, table.Range, SortOrder.Descending, cellMenuColumn);
                    Sheet.Commands.Execute(command);
                    break;
                }
            }

            // Check if we're in an auto filter
            if (Sheet.AutoFilter != null && Sheet.AutoFilter.Range.Contains(cellMenuRow, cellMenuColumn))
            {
                var command = new SortCommand(Sheet, Sheet.AutoFilter.Range, SortOrder.Descending, cellMenuColumn, skipHeaderRow: true);
                Sheet.Commands.Execute(command);
            }
        }

        if (cellMenuPopup != null)
        {
            await cellMenuPopup.CloseAsync();
        }
    }

    private async Task OnCellMenuClearAsync()
    {
        if (Sheet != null)
        {
            // Remove all filters that affect the current column
            var filtersToRemove = new List<SheetFilter>();

            foreach (var filter in Sheet.Filters)
            {
                if (filter.Range.Contains(cellMenuRow, cellMenuColumn))
                {
                    filtersToRemove.Add(filter);
                }
            }

            // Execute remove commands for each filter
            foreach (var filter in filtersToRemove)
            {
                var command = new RemoveFilterCommand(Sheet, filter);
                Sheet.Commands.Execute(command);
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

        if (Sheet != null)
        {
            FilterCriterion? existingFilter = Sheet.Filters.FirstOrDefault(f => f.Range.Contains(cellMenuRow, cellMenuColumn))?.Criterion;


            var parameters = new Dictionary<string, object?>
            {
                { nameof(FilterDialog.Sheet), Sheet },
                { nameof(FilterDialog.Column), cellMenuColumn },
                { nameof(FilterDialog.Row), cellMenuRow }
            };

            if (existingFilter != null)
            {
                parameters.Add(nameof(FilterDialog.Filter), existingFilter);
            }

            var result = await DialogService.OpenAsync<FilterDialog>("Custom Filter", parameters, new DialogOptions
            {
                Width = "600px",
            });

            if (result is SheetFilter filter)
            {
                var command = new FilterCommand(Sheet, filter);
                Sheet.Commands.Execute(command);
            }
        }
    }

    private async Task OnFormatCellsAsync()
    {
        if (Sheet != null && Sheet.Selection.Cell != CellRef.Invalid &&
            Sheet.Cells.TryGet(Sheet.Selection.Cell.Row, Sheet.Selection.Cell.Column, out var cell))
        {
            var parameters = new Dictionary<string, object?>
            {
                { nameof(FormatCellsDialog.CurrentFormat), cell.Format.NumberFormat },
                { nameof(FormatCellsDialog.SampleValue), cell.Value ?? 1234.5 },
                { nameof(FormatCellsDialog.ValueType), cell.ValueType }
            };

            var result = await DialogService.OpenAsync<FormatCellsDialog>(
                "Format Cells", parameters, new DialogOptions { Width = "600px", Height = "480px" });

            if (result is string formatCode)
            {
                var format = string.Equals(formatCode, "General", StringComparison.OrdinalIgnoreCase) ? null : formatCode;
                var command = new FormatCommand(Sheet, Sheet.Selection.Range, cell.Format.WithNumberFormat(format));
                Sheet.Commands.Execute(command);
            }
        }
    }

    private async Task MoveSelectionAsync(int rowOffset, int columnOffset)
    {
        if (Sheet is not null)
        {
            var address = Sheet.Selection.Move(rowOffset, columnOffset);
            inputPrompt?.Show(address);
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

        if (Sheet is not null)
        {
            if (Sheet.Editor.HasChanges)
            {
                var command = new AcceptEditCommand(Sheet);

                var valid = Sheet.Commands.Execute(command);

                if (!valid && Sheet.Editor.Cell is not null)
                {
                    var error = string.Join(Environment.NewLine, Sheet.Editor.Cell.ValidationErrors);
                    var errorStyle = Sheet.Validation.GetErrorStyleForCell(Sheet.Editor.Cell.Address);

                    switch (errorStyle)
                    {
                        case Spreadsheet.DataValidationErrorStyle.Information:
                            await DialogService.Alert(error, "Information");
                            // Accept the value despite validation failure
                            Sheet.Editor.Cell.ClearValidationErrors();
                            break;

                        case Spreadsheet.DataValidationErrorStyle.Warning:
                            var confirmed = await DialogService.Confirm(
                                error + Environment.NewLine + Environment.NewLine + "Do you want to continue?",
                                "Warning");

                            if (confirmed == true)
                            {
                                Sheet.Editor.Cell.ClearValidationErrors();
                            }
                            else
                            {
                                command.Unexecute();
                                Sheet.Editor.Cancel();
                                result = false;
                            }
                            break;

                        default: // Stop
                            await DialogService.Alert(error, "Invalid Value");
                            command.Unexecute();
                            Sheet.Editor.Cancel();
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
                Sheet.Editor.EndEdit();
            }
        }

        isAccepting = false;

        return result;
    }

    private async Task CycleSelectionAsync(int rowOffset, int columnOffset)
    {
        if (await AcceptAsync())
        {
            if (Sheet is not null)
            {
                var address = Sheet.Selection.Cycle(rowOffset, columnOffset);

                await ScrollToAsync(address);
                inputPrompt?.Show(address);
            }
        }
    }

    private async Task ExtendSelectionAsync(int rowOffset, int columnOffset)
    {
        if (Sheet is not null)
        {
            var address = Sheet.Selection.Extend(rowOffset, columnOffset);

            await ScrollToAsync(address);
        }
    }

    private Task CancelEditAsync()
    {
        if (Sheet?.Editor.Mode != EditMode.None)
        {
            Sheet?.Editor.Cancel();
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
        if (Sheet?.SelectedImage != null)
        {
            var command = new Spreadsheet.DeleteImageCommand(Sheet, Sheet.SelectedImage);
            Sheet.Commands.Execute(command);
            return Task.CompletedTask;
        }

        return Task.CompletedTask;
    }

    private Task UndoAsync()
    {
        Sheet?.Commands.Undo();

        return Task.CompletedTask;
    }

    private Task RedoAsync()
    {
        Sheet?.Commands.Redo();

        return Task.CompletedTask;
    }

    private async Task CopySelectionAsync()
    {
        if (Sheet is not null)
        {
            var text = Sheet.GetDelimitedString(Sheet.Selection.Range);
            clipboard.Copy(Sheet);

            if (jsRef is not null)
            {
                await jsRef.InvokeVoidAsync("copyToClipboard", text);
            }
        }
    }

    private async Task CutSelectionAsync()
    {
        if (Sheet is not null)
        {
            var text = Sheet.GetDelimitedString(Sheet.Selection.Range);
            clipboard.Cut(Sheet);

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
        if (Sheet is not null)
        {
            clipboard.Paste(Sheet, Sheet.Selection.Cell, text);
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

        if (Sheet is null)
        {
            return;
        }

        await AcceptAsync();

        Sheet.Selection.Select(new CellRef(args.Row, args.Column));

        var row = args.Row;
        var column = args.Column;

        ContextMenuService.Open(args.Pointer, new List<ContextMenuItem>
        {
            new() { Text = "Cut", Value = "cut", Icon = "content_cut" },
            new() { Text = "Copy", Value = "copy", Icon = "content_copy" },
            new() { Text = "Paste", Value = "paste", Icon = "content_paste" },
            new() { Text = "Clear Contents", Value = "clear", Icon = "clear" },
            new() { Text = "Sort Ascending", Value = "sort-ascending", Icon = "arrow_upward" },
            new() { Text = "Sort Descending", Value = "sort-descending", Icon = "arrow_downward" },
        }, menuArgs => OnContextMenuItemClick(menuArgs, row, column));
    }

    /// <summary>
    /// Invoked by JS interop when a row header is right-clicked.
    /// </summary>
    [JSInvokable]
    public async Task OnRowContextMenuAsync(CellEventArgs args)
    {
        ArgumentNullException.ThrowIfNull(args);

        if (Sheet is null)
        {
            return;
        }

        await AcceptAsync();

        Sheet.Selection.Select(new RowRef(args.Row));

        var row = args.Row;

        ContextMenuService.Open(args.Pointer, new List<ContextMenuItem>
        {
            new() { Text = "Cut", Value = "cut", Icon = "content_cut" },
            new() { Text = "Copy", Value = "copy", Icon = "content_copy" },
            new() { Text = "Paste", Value = "paste", Icon = "content_paste" },
            new() { Text = "Insert Row Above", Value = "insert-row-before", Icon = "north" },
            new() { Text = "Insert Row Below", Value = "insert-row-after", Icon = "south" },
            new() { Text = "Delete Row", Value = "delete-row", Icon = "delete" },
            new() { Text = "Hide Row", Value = "hide-row", Icon = "visibility_off" },
            new() { Text = "Unhide Row", Value = "unhide-row", Icon = "visibility" },
        }, menuArgs => OnRowContextMenuItemClick(menuArgs, row));
    }

    /// <summary>
    /// Invoked by JS interop when a column header is right-clicked.
    /// </summary>
    [JSInvokable]
    public async Task OnColumnContextMenuAsync(CellEventArgs args)
    {
        ArgumentNullException.ThrowIfNull(args);

        if (Sheet is null)
        {
            return;
        }

        await AcceptAsync();

        Sheet.Selection.Select(new ColumnRef(args.Column));

        var column = args.Column;

        ContextMenuService.Open(args.Pointer, new List<ContextMenuItem>
        {
            new() { Text = "Cut", Value = "cut", Icon = "content_cut" },
            new() { Text = "Copy", Value = "copy", Icon = "content_copy" },
            new() { Text = "Paste", Value = "paste", Icon = "content_paste" },
            new() { Text = "Insert Column Before", Value = "insert-column-before", Icon = "west" },
            new() { Text = "Insert Column After", Value = "insert-column-after", Icon = "east" },
            new() { Text = "Delete Column", Value = "delete-column", Icon = "delete" },
            new() { Text = "Hide Column", Value = "hide-column", Icon = "visibility_off" },
            new() { Text = "Unhide Column", Value = "unhide-column", Icon = "visibility" },
        }, menuArgs => OnColumnContextMenuItemClick(menuArgs, column));
    }

    private void OnContextMenuItemClick(MenuItemEventArgs args, int row, int column)
    {
        if (Sheet is null)
        {
            return;
        }

        ContextMenuService.Close();

        switch (args.Value?.ToString())
        {
            case "cut":
                _ = CutSelectionAsync();
                break;
            case "copy":
                _ = CopySelectionAsync();
                break;
            case "paste":
                _ = PasteFromClipboardAsync();
                break;
            case "clear":
                Sheet.Commands.Execute(new ClearContentsCommand(Sheet, Sheet.Selection.Range));
                break;
            case "sort-ascending":
                Sheet.Commands.Execute(new SortCommand(Sheet, new RangeRef(new CellRef(0, 0), new CellRef(Sheet.RowCount - 1, Sheet.ColumnCount - 1)), SortOrder.Ascending, column));
                break;
            case "sort-descending":
                Sheet.Commands.Execute(new SortCommand(Sheet, new RangeRef(new CellRef(0, 0), new CellRef(Sheet.RowCount - 1, Sheet.ColumnCount - 1)), SortOrder.Descending, column));
                break;
        }

        StateHasChanged();
    }

    private void OnRowContextMenuItemClick(MenuItemEventArgs args, int row)
    {
        if (Sheet is null)
        {
            return;
        }

        ContextMenuService.Close();

        switch (args.Value?.ToString())
        {
            case "cut":
                _ = CutSelectionAsync();
                break;
            case "copy":
                _ = CopySelectionAsync();
                break;
            case "paste":
                _ = PasteFromClipboardAsync();
                break;
            case "insert-row-before":
                Sheet.Commands.Execute(new InsertRowBeforeCommand(Sheet, row));
                break;
            case "insert-row-after":
                Sheet.Commands.Execute(new InsertRowAfterCommand(Sheet, row));
                break;
            case "delete-row":
                Sheet.Commands.Execute(new DeleteRowsCommand(Sheet, row, row));
                break;
            case "hide-row":
                Sheet.Rows.Hide(row);
                break;
            case "unhide-row":
                Sheet.Rows.Show(row);
                break;
        }

        StateHasChanged();
    }

    private void OnColumnContextMenuItemClick(MenuItemEventArgs args, int column)
    {
        if (Sheet is null)
        {
            return;
        }

        ContextMenuService.Close();

        switch (args.Value?.ToString())
        {
            case "cut":
                _ = CutSelectionAsync();
                break;
            case "copy":
                _ = CopySelectionAsync();
                break;
            case "paste":
                _ = PasteFromClipboardAsync();
                break;
            case "insert-column-before":
                Sheet.Commands.Execute(new InsertColumnBeforeCommand(Sheet, column));
                break;
            case "insert-column-after":
                Sheet.Commands.Execute(new InsertColumnAfterCommand(Sheet, column));
                break;
            case "delete-column":
                Sheet.Commands.Execute(new DeleteColumnsCommand(Sheet, column, column));
                break;
            case "hide-column":
                Sheet.Columns.Hide(column);
                break;
            case "unhide-column":
                Sheet.Columns.Show(column);
                break;
        }

        StateHasChanged();
    }

    private async Task PasteFromClipboardAsync()
    {
        if (Sheet is null)
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
            clipboard.Paste(Sheet, Sheet.Selection.Cell, text);
        }
        else
        {
            clipboard.Paste(Sheet, Sheet.Selection.Cell);
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
            // Clear image selection when clicking a cell
            if (Sheet != null)
            {
                Sheet.SelectedImage = null;
            }

            var address = new CellRef(args.Row, args.Column);

            if (args.Pointer.ShiftKey)
            {
                Sheet?.Selection.Merge(address);
            }
            else
            {
                Sheet?.Selection.Select(address);

            }

            inputPrompt?.Show(address);

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

                onCellPointerMoveAsync = pointer => OnCellPointerMoveAsync(capture, pointer);
            }
        }

        return result;
    }

    private Func<PointerEventArgs, Task>? onCellPointerMoveAsync;

    /// <summary>
    /// Invoked by JS interop when the pointer moves over a cell.
    /// </summary>
    [JSInvokable]
    public async Task OnCellPointerMoveAsync(PointerEventArgs args)
    {
        ArgumentNullException.ThrowIfNull(args);
        if (onCellPointerMoveAsync is not null)
        {
            await onCellPointerMoveAsync(args);
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
            Sheet?.Selection.Merge(address);

            await ScrollToAsync(address);
        }
    }

    private CellRef GetDeltaCell(PointerCapture capture, PointerEventArgs args)
    {
        if (grid is not null)
        {
            var deltaX = args.ClientX - capture.Pointer.ClientX + capture.Pointer.OffsetX;

            deltaX += grid.ScrollLeft - capture.ScrollLeft;

            var deltaY = args.ClientY - capture.Pointer.ClientY + capture.Pointer.OffsetY;

            deltaY += grid.ScrollTop - capture.ScrollTop;

            var columnPixelRange = grid.Columns.GetPixelRange(capture.Column, capture.Column);

            var columnIndex = grid.Columns.GetIndexRange(columnPixelRange.Start + deltaX, columnPixelRange.Start + deltaX, true);

            var rowPixelRange = grid.Rows.GetPixelRange(capture.Row, capture.Row);

            var rowIndex = grid.Rows.GetIndexRange(rowPixelRange.Start + deltaY, rowPixelRange.Start + deltaY, true);

            return new CellRef(rowIndex.Start, columnIndex.Start);
        }

        return CellRef.Invalid;
    }

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
                Sheet?.Selection.Merge(new CellRef(args.Row, Sheet.ColumnCount - 1));
            }
            else
            {
                Sheet?.Selection.Select(new RowRef(args.Row));
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

                onRowPointerMoveAsync = pointer => OnRowPointerMoveAsync(capture, pointer);
            }
        }

        return result;
    }

    private Func<PointerEventArgs, Task>? onRowPointerMoveAsync;

    /// <summary>
    /// Invoked by JS interop when the pointer moves over a row header.
    /// </summary>
    [JSInvokable]
    public async Task OnRowPointerMoveAsync(PointerEventArgs args)
    {
        ArgumentNullException.ThrowIfNull(args);
        if (onRowPointerMoveAsync is not null)
        {
            await onRowPointerMoveAsync(args);
        }
    }

    private async Task OnRowPointerMoveAsync(PointerCapture capture, PointerEventArgs pointer)
    {
        var address = GetDeltaCell(capture, pointer);

        if (address != CellRef.Invalid)
        {
            Sheet?.Selection.Merge(new CellRef(address.Row, Sheet.ColumnCount - 1));

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
                Sheet?.Selection.Merge(new CellRef(Sheet.RowCount - 1, args.Column));
            }
            else
            {
                Sheet?.Selection.Select(new ColumnRef(args.Column));
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

                onColumnPointerMoveAsync = pointer => OnColumnPointerMoveAsync(capture, pointer);
            }
        }

        return result;
    }

    private Func<PointerEventArgs, Task>? onColumnPointerMoveAsync;

    /// <summary>
    /// Invoked by JS interop when the pointer moves over a column header.
    /// </summary>
    [JSInvokable]
    public async Task OnColumnPointerMoveAsync(PointerEventArgs args)
    {
        ArgumentNullException.ThrowIfNull(args);
        if (onColumnPointerMoveAsync is not null)
        {
            await onColumnPointerMoveAsync(args);
        }
    }

    private async Task OnColumnPointerMoveAsync(PointerCapture capture, PointerEventArgs pointer)
    {
        var address = GetDeltaCell(capture, pointer);

        if (address != CellRef.Invalid)
        {
            Sheet?.Selection.Merge(new CellRef(Sheet.RowCount - 1, address.Column));

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
        if (Sheet != null)
        {
            var address = Sheet.MergedCells.GetMergedRangeStartOrSelf(new CellRef(args.Row, args.Column));

            var cell = Sheet.Cells[address];

            if (cell != null)
            {
                await ScrollToAsync(address);

                inputPrompt?.Hide();

                Sheet.Editor.StartEdit(address, cell.GetValue());
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
            inputPrompt?.Hide();
            Sheet?.Editor.StartEdit(Sheet.Selection.Cell, ch.ToString());
        }
    }

    private Func<PointerEventArgs, Task>? onColumnResizePointerMoveAsync;
    private Func<PointerEventArgs, Task>? onRowResizePointerMoveAsync;
    private Func<PointerEventArgs, Task>? onImageResizePointerMoveAsync;
    private ImageResizeCapture? imageResizeCapture;

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
                    StartWidth = Sheet?.Columns[args.Column] ?? 100
                };

                onColumnResizePointerMoveAsync = pointer => OnColumnResizePointerMoveAsync(capture, pointer);
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
                    StartHeight = Sheet?.Rows[args.Row] ?? 20
                };

                onRowResizePointerMoveAsync = pointer => OnRowResizePointerMoveAsync(capture, pointer);
            }
        }

        return result;
    }

    /// <summary>
    /// Invoked by JS interop when the pointer moves while resizing a column.
    /// </summary>
    [JSInvokable]
    public async Task OnColumnResizePointerMoveAsync(PointerEventArgs args)
    {
        ArgumentNullException.ThrowIfNull(args);
        if (onColumnResizePointerMoveAsync is not null)
        {
            await onColumnResizePointerMoveAsync(args);
        }
    }

    /// <summary>
    /// Invoked by JS interop when the pointer moves while resizing a row.
    /// </summary>
    [JSInvokable]
    public async Task OnRowResizePointerMoveAsync(PointerEventArgs args)
    {
        ArgumentNullException.ThrowIfNull(args);
        if (onRowResizePointerMoveAsync is not null)
        {
            await onRowResizePointerMoveAsync(args);
        }
    }

    private Task OnColumnResizePointerMoveAsync(ColumnResizeCapture capture, PointerEventArgs pointer)
    {
        if (Sheet != null && capture.Column >= 0 && capture.Column < Sheet.Columns.Count)
        {
            var delta = pointer.ClientX - capture.StartX;
            var newWidth = Math.Max(24, capture.StartWidth + delta);
            Sheet.Columns[capture.Column] = newWidth;
            StateHasChanged();
        }

        return Task.CompletedTask;
    }

    private Task OnRowResizePointerMoveAsync(RowResizeCapture capture, PointerEventArgs pointer)
    {
        if (Sheet != null && capture.Row >= 0 && capture.Row < Sheet.Rows.Count)
        {
            var delta = pointer.ClientY - capture.StartY;
            var newHeight = Math.Max(16, capture.StartHeight + delta);
            Sheet.Rows[capture.Row] = newHeight;
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
        onColumnResizePointerMoveAsync = null;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Invoked by JS interop when the pointer is released after resizing a row.
    /// </summary>
    [JSInvokable]
    public Task OnRowResizePointerUpAsync(PointerEventArgs args)
    {
        onRowResizePointerMoveAsync = null;
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

    class ImageResizeCapture
    {
        public SheetImage Image { get; set; } = default!;
        public string Direction { get; set; } = "";
        public double StartX { get; set; }
        public double StartY { get; set; }
        public long OriginalWidth { get; set; }
        public long OriginalHeight { get; set; }
    }

    /// <summary>
    /// Invoked by JS interop when an image resize handle is pressed.
    /// </summary>
    [JSInvokable]
    public async Task<bool> OnImageResizePointerDownAsync(ImageResizeEventArgs args)
    {
        ArgumentNullException.ThrowIfNull(args);
        var result = await AcceptAsync();

        if (result && Sheet?.SelectedImage is SheetImage image)
        {
            var capture = new ImageResizeCapture
            {
                Image = image,
                Direction = args.Direction,
                StartX = args.Pointer.ClientX,
                StartY = args.Pointer.ClientY,
                OriginalWidth = image.Width,
                OriginalHeight = image.Height
            };

            imageResizeCapture = capture;
            onImageResizePointerMoveAsync = pointer => OnImageResizePointerMoveAsync(capture, pointer);
        }

        return result;
    }

    /// <summary>
    /// Invoked by JS interop when the pointer moves while resizing an image.
    /// </summary>
    [JSInvokable]
    public async Task OnImageResizePointerMoveAsync(PointerEventArgs args)
    {
        ArgumentNullException.ThrowIfNull(args);
        if (onImageResizePointerMoveAsync is not null)
        {
            await onImageResizePointerMoveAsync(args);
        }
    }

    private const double EmuPerPixel = 9525.0;

    private Task OnImageResizePointerMoveAsync(ImageResizeCapture capture, PointerEventArgs pointer)
    {
        var deltaX = pointer.ClientX - capture.StartX;
        var deltaY = pointer.ClientY - capture.StartY;

        long newWidth = capture.OriginalWidth;
        long newHeight = capture.OriginalHeight;

        switch (capture.Direction)
        {
            case "se":
                newWidth = capture.OriginalWidth + (long)(deltaX * EmuPerPixel);
                newHeight = capture.OriginalHeight + (long)(deltaY * EmuPerPixel);
                break;
            case "sw":
                newWidth = capture.OriginalWidth - (long)(deltaX * EmuPerPixel);
                newHeight = capture.OriginalHeight + (long)(deltaY * EmuPerPixel);
                break;
            case "ne":
                newWidth = capture.OriginalWidth + (long)(deltaX * EmuPerPixel);
                newHeight = capture.OriginalHeight - (long)(deltaY * EmuPerPixel);
                break;
            case "nw":
                newWidth = capture.OriginalWidth - (long)(deltaX * EmuPerPixel);
                newHeight = capture.OriginalHeight - (long)(deltaY * EmuPerPixel);
                break;
            case "n":
                newHeight = capture.OriginalHeight - (long)(deltaY * EmuPerPixel);
                break;
            case "s":
                newHeight = capture.OriginalHeight + (long)(deltaY * EmuPerPixel);
                break;
            case "e":
                newWidth = capture.OriginalWidth + (long)(deltaX * EmuPerPixel);
                break;
            case "w":
                newWidth = capture.OriginalWidth - (long)(deltaX * EmuPerPixel);
                break;
        }

        const long minSize = 95250; // ~10px
        newWidth = Math.Max(minSize, newWidth);
        newHeight = Math.Max(minSize, newHeight);

        capture.Image.Width = newWidth;
        capture.Image.Height = newHeight;
        StateHasChanged();

        return Task.CompletedTask;
    }

    /// <summary>
    /// Invoked by JS interop when the pointer is released after resizing an image.
    /// </summary>
    [JSInvokable]
    public Task OnImageResizePointerUpAsync(PointerEventArgs args)
    {
        if (imageResizeCapture is not null && Sheet is not null)
        {
            var capture = imageResizeCapture;
            var finalWidth = capture.Image.Width;
            var finalHeight = capture.Image.Height;

            // Restore original dimensions so ResizeImageCommand captures correct old values
            capture.Image.Width = capture.OriginalWidth;
            capture.Image.Height = capture.OriginalHeight;

            Sheet.Commands.Execute(new Spreadsheet.ResizeImageCommand(capture.Image, finalWidth, finalHeight));

            imageResizeCapture = null;
            onImageResizePointerMoveAsync = null;
        }

        return Task.CompletedTask;
    }

    async ValueTask IAsyncDisposable.DisposeAsync()
    {
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