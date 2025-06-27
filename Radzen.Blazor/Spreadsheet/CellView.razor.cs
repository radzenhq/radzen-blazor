using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Radzen.Blazor.Rendering;

namespace Radzen.Blazor.Spreadsheet;

#nullable enable

/// <summary>
/// Represents the event arguments for cell-related events in a spreadsheet.
/// </summary>
public class CellEventArgs : EventArgs
{
    /// <summary>
    /// Gets or sets the row index of the cell.
    /// </summary>
    public int Row { get; set; }
    /// <summary>
    /// Gets or sets the column index of the cell.
    /// </summary>
    public int Column { get; set; }
    /// <summary>
    /// Gets or sets the pointer event arguments associated with the cell event.
    /// </summary>
    public PointerEventArgs Pointer { get; set; } = default!;
}

/// <summary>
/// Represents the event arguments for cell menu toggle events in a spreadsheet.
/// </summary>
public class CellMenuToggleEventArgs
{
    /// <summary>
    /// Gets or sets the element reference of the toggle button.
    /// </summary>
    public ElementReference Element { get; set; }
    
    /// <summary>
    /// Gets or sets the row index of the cell.
    /// </summary>
    public int Row { get; set; }
    
    /// <summary>
    /// Gets or sets the column index of the cell.
    /// </summary>
    public int Column { get; set; }
}

/// <summary>
/// Renders a cell in a spreadsheet.
/// </summary>
public partial class CellView : CellBase, IDisposable
{
    /// <summary>
    /// Gets or sets the row index of the cell.
    /// </summary>
    [Parameter, EditorRequired]
    public int Row { get; set; }

    /// <summary>
    /// Gets or sets the column index of the cell.
    /// </summary>
    [Parameter, EditorRequired]
    public int Column { get; set; }

    /// <summary>
    /// Gets or sets the sheet that contains the cell.
    /// </summary>
    [Parameter, EditorRequired]
    public Sheet Sheet { get; set; } = default!;

    /// <summary>
    /// Event callback that is invoked when the toggle button is clicked.
    /// </summary>
    [Parameter]
    public EventCallback<CellMenuToggleEventArgs> Toggle { get; set; }

    private RadzenButton? cellMenuButton;

    private string Class => ClassList.Create("rz-spreadsheet-cell")
                                     .Add("rz-spreadsheet-frozen-row", FrozenState.HasFlag(FrozenState.Row))
                                     .Add("rz-spreadsheet-frozen-column", FrozenState.HasFlag(FrozenState.Column))
                                     .ToString();

    private Cell cell = default!;

    /// <summary>
    /// Gets a value indicating whether the cell menu icon should be shown.
    /// </summary>
    protected bool ShouldShowCellMenu => IsInDataTableFirstVisibleRow || IsFiltered;

    /// <summary>
    /// Gets a value indicating whether the cell is in the first visible row of a data table.
    /// </summary>
    protected bool IsInDataTableFirstVisibleRow
    {
        get
        {
            if (Sheet?.DataTables == null) return false;
            
            foreach (var dataTable in Sheet.DataTables)
            {
                if (Column >= dataTable.Range.Start.Column && 
                    Column <= dataTable.Range.End.Column)
                {
                    return Row == dataTable.Start.Row;
                }
            }

            return false;
        }
    }

    /// <summary>
    /// Gets a value indicating whether the cell is part of a filter.
    /// </summary>
    protected bool IsFiltered
    {
        get
        {
            // TODO: Implement filter state tracking
            // For now, return false until filter state tracking is implemented
            return false;
        }
    }

    /// <inheritdoc/>
    protected override string Style => GetStyle();

    /// <inheritdoc/>
    protected override void AppendStyle(StringBuilder sb)
    {
        base.AppendStyle(sb);
        cell.Format?.AppendStyle(sb);
    }

    private async Task OnToggleAsync()
    {
        if (cellMenuButton != null)
        {
            var args = new CellMenuToggleEventArgs
            {
                Element = cellMenuButton.Element,
                Row = Row,
                Column = Column
            };
            await Toggle.InvokeAsync(args);
        }
    }

    /// <inheritdoc/>
    public override async Task SetParametersAsync(ParameterView parameters)
    {
        var didRowChange = parameters.TryGetValue<int>(nameof(Row), out var row) && Row != row;
        var didColumnChange = parameters.TryGetValue<int>(nameof(Column), out var column) && Column != column;
        var didSheetChange = parameters.TryGetValue<Sheet>(nameof(Sheet), out var sheet) && Sheet != sheet;

        await base.SetParametersAsync(parameters);

        if (didRowChange || didColumnChange || didSheetChange)
        {
            if (cell != null)
            {
                cell.Changed -= OnCellChanged;
            }

            cell = Sheet.Cells[Row, Column];
            cell.Changed += OnCellChanged;
        }
    }

    private void OnCellChanged(Cell cell)
    {
        //Console.WriteLine($"Cell {Row}:{Column} changed: {cell.Value}");
        StateHasChanged();
    }

    /// <inheritdoc/>
    protected override void OnAfterRender(bool firstRender)
    {
        base.OnAfterRender(firstRender);

        if (!firstRender)
        {
            //Console.WriteLine($"Cell {Row}:{Column} rendered with value: {cell.Value}");
        }
    }

    void IDisposable.Dispose()
    {
        if (cell != null)
        {
            cell.Changed -= OnCellChanged;
        }
    }
}