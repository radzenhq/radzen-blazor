using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Radzen.Blazor.Spreadsheet;

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
    /// Invoked by JS interop when a cell is clicked with the mouse.
    /// </summary>
    [JSInvokable]
    public async Task<bool> OnCellMouseDownAsync(CellEventArgs args)
    {
        var result = await AcceptAsync();

        if (result)
        {
            var address = new CellRef(args.Row, args.Column);

            if (args.Mouse.ShiftKey)
            {
                Sheet?.Selection.Merge(address);
            }
            else
            {
                Sheet?.Selection.Select(address);

            }

            if (grid is not null)
            {
                var capture = new MouseCapture
                {
                    ScrollTop = grid.ScrollTop,
                    ScrollLeft = grid.ScrollLeft,
                    Row = args.Row,
                    Column = args.Column,
                    Mouse = args.Mouse
                };

                onCellMouseMoveAsync = mouse => OnCellMouseMoveAsync(capture, mouse);
            }
        }

        return result;
    }

    private Func<MouseEventArgs, Task>? onCellMouseMoveAsync;

    /// <summary>
    /// Invoked by JS interop when the mouse moves over a cell.
    /// </summary>
    [JSInvokable]
    public async Task OnCellMouseMoveAsync(MouseEventArgs args)
    {
        if (onCellMouseMoveAsync is not null)
        {
            await onCellMouseMoveAsync(args);
        }
    }

    class MouseCapture
    {
        public double ScrollTop { get; set; }
        public double ScrollLeft { get; set; }
        public int Row { get; set; }
        public int Column { get; set; }
        public MouseEventArgs Mouse { get; set; } = default!;
    }

    private async Task OnCellMouseMoveAsync(MouseCapture capture, MouseEventArgs mouse)
    {
        var address = GetDeltaCell(capture, mouse);

        if (address != CellRef.Invalid)
        {
            Sheet?.Selection.Merge(address);

            await ScrollToAsync(address);
        }
    }

    private CellRef GetDeltaCell(MouseCapture capture, MouseEventArgs args)
    {
        if (grid is not null)
        {
            var deltaX = args.ClientX - capture.Mouse.ClientX + capture.Mouse.OffsetX;

            deltaX += grid.ScrollLeft - capture.ScrollLeft;

            var deltaY = args.ClientY - capture.Mouse.ClientY + capture.Mouse.OffsetY;

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
    /// Invoked by JS interop when a row header is clicked with the mouse.
    /// </summary>
    [JSInvokable]
    public async Task<bool> OnRowMouseDownAsync(CellEventArgs args)
    {
        var result = await AcceptAsync();

        if (result)
        {
            if (args.Mouse.ShiftKey)
            {
                Sheet?.Selection.Merge(new CellRef(args.Row, Sheet.ColumnCount - 1));
            }
            else
            {
                Sheet?.Selection.Select(new RowRef(args.Row));
            }

            if (grid is not null)
            {
                var capture = new MouseCapture
                {
                    ScrollTop = grid.ScrollTop,
                    ScrollLeft = grid.ScrollLeft,
                    Row = args.Row,
                    Mouse = args.Mouse
                };

                onRowMouseMoveAsync = mouse => OnRowMouseMoveAsync(capture, mouse);
            }
        }

        return result;
    }

    private Func<MouseEventArgs, Task>? onRowMouseMoveAsync;

    /// <summary>
    /// Invoked by JS interop when the mouse moves over a row header.
    /// </summary>
    [JSInvokable]
    public async Task OnRowMouseMoveAsync(MouseEventArgs args)
    {
        if (onRowMouseMoveAsync is not null)
        {
            await onRowMouseMoveAsync(args);
        }
    }

    private async Task OnRowMouseMoveAsync(MouseCapture capture, MouseEventArgs mouse)
    {
        var address = GetDeltaCell(capture, mouse);

        if (address != CellRef.Invalid)
        {
            Sheet?.Selection.Merge(new CellRef(address.Row, Sheet.ColumnCount - 1));

            await ScrollToAsync(address);
        }
    }

    /// <summary>
    /// Invoked by JS interop when a column header is clicked with the mouse.
    /// </summary>
    [JSInvokable]
    public async Task<bool> OnColumnMouseDownAsync(CellEventArgs args)
    {
        var result = await AcceptAsync();

        if (result)
        {
            if (args.Mouse.ShiftKey)
            {
                Sheet?.Selection.Merge(new CellRef(Sheet.RowCount - 1, args.Column));
            }
            else
            {
                Sheet?.Selection.Select(new ColumnRef(args.Column));
            }

            if (grid is not null)
            {
                var capture = new MouseCapture
                {
                    ScrollTop = grid.ScrollTop,
                    ScrollLeft = grid.ScrollLeft,
                    Column = args.Column,
                    Mouse = args.Mouse
                };

                onColumnMouseMoveAsync = mouse => OnColumnMouseMoveAsync(capture, mouse);
            }
        }

        return result;
    }

    private Func<MouseEventArgs, Task>? onColumnMouseMoveAsync;

    /// <summary>
    /// Invoked by JS interop when the mouse moves over a column header.
    /// </summary>
    [JSInvokable]
    public async Task OnColumnMouseMoveAsync(MouseEventArgs args)
    {
        if (onColumnMouseMoveAsync is not null)
        {
            await onColumnMouseMoveAsync(args);
        }
    }

    private async Task OnColumnMouseMoveAsync(MouseCapture capture, MouseEventArgs mouse)
    {
        var address = GetDeltaCell(capture, mouse);

        if (address != CellRef.Invalid)
        {
            Sheet?.Selection.Merge(new CellRef(Sheet.RowCount - 1, address.Column));

            await ScrollToAsync(address);
        }
    }

    /// <summary>
    /// Invoked by JS interop when a cell is double-clicked with the mouse.
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

    private Func<MouseEventArgs, Task>? onColumnResizeMouseMoveAsync;
    private Func<MouseEventArgs, Task>? onRowResizeMouseMoveAsync;

    /// <summary>
    /// Invoked by JS interop when the column resize handle is pressed.
    /// </summary>
    [JSInvokable]
    public async Task<bool> OnColumnResizeMouseDownAsync(CellEventArgs args)
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
                    StartX = args.Mouse.ClientX,
                    StartWidth = Sheet?.Columns[args.Column] ?? 100
                };

                onColumnResizeMouseMoveAsync = mouse => OnColumnResizeMouseMoveAsync(capture, mouse);
            }
        }

        return result;
    }

    /// <summary>
    /// Invoked by JS interop when the row resize handle is pressed.
    /// </summary>
    [JSInvokable]
    public async Task<bool> OnRowResizeMouseDownAsync(CellEventArgs args)
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
                    StartY = args.Mouse.ClientY,
                    StartHeight = Sheet?.Rows[args.Row] ?? 20
                };

                onRowResizeMouseMoveAsync = mouse => OnRowResizeMouseMoveAsync(capture, mouse);
            }
        }

        return result;
    }

    /// <summary>
    /// Invoked by JS interop when the mouse moves while resizing a column.
    /// </summary>
    [JSInvokable]
    public async Task OnColumnResizeMouseMoveAsync(MouseEventArgs args)
    {
        if (onColumnResizeMouseMoveAsync is not null)
        {
            await onColumnResizeMouseMoveAsync(args);
        }
    }

    /// <summary>
    /// Invoked by JS interop when the mouse moves while resizing a row.
    /// </summary>
    [JSInvokable]
    public async Task OnRowResizeMouseMoveAsync(MouseEventArgs args)
    {
        if (onRowResizeMouseMoveAsync is not null)
        {
            await onRowResizeMouseMoveAsync(args);
        }
    }

    private Task OnColumnResizeMouseMoveAsync(ColumnResizeCapture capture, MouseEventArgs mouse)
    {
        if (Sheet != null && capture.Column >= 0 && capture.Column < Sheet.Columns.Count)
        {
            var delta = mouse.ClientX - capture.StartX;
            var newWidth = Math.Max(24, capture.StartWidth + delta);
            Sheet.Columns[capture.Column] = newWidth;
            StateHasChanged();
        }

        return Task.CompletedTask;
    }

    private Task OnRowResizeMouseMoveAsync(RowResizeCapture capture, MouseEventArgs mouse)
    {
        if (Sheet != null && capture.Row >= 0 && capture.Row < Sheet.Rows.Count)
        {
            var delta = mouse.ClientY - capture.StartY;
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