using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Radzen.Blazor.Rendering;

namespace Radzen.Blazor.Spreadsheet;

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
    /// Gets or sets the mouse event arguments associated with the cell event.
    /// </summary>
    public MouseEventArgs Mouse { get; set; } = default!;
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

    private string Class => ClassList.Create("rz-spreadsheet-cell")
                                     .Add("rz-spreadsheet-frozen-row", FrozenState.HasFlag(FrozenState.Row))
                                     .Add("rz-spreadsheet-frozen-column", FrozenState.HasFlag(FrozenState.Column))
                                     .ToString();

    private Cell cell = default!;

    /// <inheritdoc/>
    protected override string Style => GetStyle();

    /// <inheritdoc/>
    protected override void AppendStyle(StringBuilder sb)
    {
        base.AppendStyle(sb);
        cell.Format?.AppendStyle(sb);
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