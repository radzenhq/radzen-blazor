using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Radzen.Blazor.Spreadsheet;
using Radzen.Blazor.Rendering;

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
    private int cellMenuRow = -1;
    private int cellMenuColumn = -1;

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

    private readonly int sheetIndex = 0;

    private Sheet? Sheet => workbook?.Sheets[sheetIndex];

    private async Task OnWorkbookChangedAsync(Workbook? value)
    {
        workbook = value;

        await WorkbookChanged.InvokeAsync(value);
    }

    private async Task OnCellToggleAsync(CellMenuToggleEventArgs args)
    {
        if (cellMenuPopup != null)
        {
            cellMenuRow = args.Row;
            cellMenuColumn = args.Column;
            await cellMenuPopup.ToggleAsync(args.Element);
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
            foreach (var dataTable in Sheet.DataTables)
            {
                if (dataTable.Range.Contains(cellMenuRow, cellMenuColumn))
                {
                    var command = new DataTableSortCommand(Sheet, dataTable.Range, SortOrder.Ascending, cellMenuColumn);
                    Sheet.Commands.Execute(command);
                    break;
                }
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
            foreach (var dataTable in Sheet.DataTables)
            {
                if (dataTable.Range.Contains(cellMenuRow, cellMenuColumn))
                {
                    var command = new DataTableSortCommand(Sheet, dataTable.Range, SortOrder.Descending, cellMenuColumn);
                    Sheet.Commands.Execute(command);
                    break;
                }
            }
        }

        if (cellMenuPopup != null)
        {
            await cellMenuPopup.CloseAsync();
        }
    }

    private async Task MoveSelectionAsync(int rowOffset, int columnOffset)
    {
        if (Sheet is not null)
        {
            var address = Sheet.Selection.Move(rowOffset, columnOffset);

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

        if (Sheet is not null && Sheet.Editor.Mode != EditMode.None)
        {
            var command = new AcceptEditCommand(Sheet);

            var valid = Sheet.Commands.Execute(command);

            if (!valid && Sheet.Editor.Cell is not null)
            {
                string error = string.Join("\n", Sheet.Editor.Cell.ValidationErrors);

                await JSRuntime.InvokeVoidAsync("alert", error);

                command.Unexecute();

                Sheet.Editor.Cancel();

                result = false;
            }
            else
            {
                await Element.FocusAsync();
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
    }

    private async Task CopySelectionAsync()
    {
        if (Sheet is not null && jsRef is not null)
        {
            var text = Sheet.GetDelimitedString(Sheet.Selection.Range);

            await jsRef.InvokeVoidAsync("copyToClipboard", text);
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
        Sheet?.InsertDelimitedString(Sheet.Selection.Cell, text);

        return Task.CompletedTask;
    }

    private IJSObjectReference? jsRef;
    private DotNetObjectReference<RadzenSpreadsheet>? dotNetRef;

    /// <inheritdoc/>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
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
        var result = await AcceptAsync();

        if (result)
        {
            var address = new CellRef(args.Row, args.Column);

            if (args.Pointer.ShiftKey)
            {
                Sheet?.Selection.Merge(address);
            }
            else
            {
                Sheet?.Selection.Select(address);

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
        var address = new CellRef(args.Row, args.Column);
        var cell = Sheet?.Cells[address];

        if (cell != null)
        {
            await ScrollToAsync(address);

            Sheet?.Editor.StartEdit(address, cell.GetValue());
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

        key.Append(args.Code.Replace("Key", "").Replace("Digit", "").Replace("Numpad", ""));

        return key.ToString();
    }

    /// <summary>
    /// Invoked by JS interop when a key is pressed down.
    /// </summary>
    [JSInvokable]
    public async Task OnKeyDownAsync(KeyboardEventArgs args)
    {
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
            Sheet?.Editor.StartEdit(Sheet.Selection.Cell, ch.ToString());
        }
    }

    private Func<PointerEventArgs, Task>? onColumnResizePointerMoveAsync;
    private Func<PointerEventArgs, Task>? onRowResizePointerMoveAsync;

    /// <summary>
    /// Invoked by JS interop when the column resize handle is pressed.
    /// </summary>
    [JSInvokable]
    public async Task<bool> OnColumnResizePointerDownAsync(CellEventArgs args)
    {
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