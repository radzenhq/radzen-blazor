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
    private bool showCellMenu;

    private string Class => ClassList.Create("rz-spreadsheet-cell")
                                     .Add("rz-spreadsheet-frozen-row", FrozenState.HasFlag(FrozenState.Row))
                                     .Add("rz-spreadsheet-frozen-column", FrozenState.HasFlag(FrozenState.Column))
                                     .Add($"rz-spreadsheet-cell-{cell.ValueType.ToString().ToLowerInvariant()}", cell != null)
                                     .ToString();

    private Cell cell = default!;

    private bool ShowCellMenu()
    {
        if (Sheet?.Tables != null)
        {
            foreach (var table in Sheet.Tables)
            {
                if (Column >= table.Range.Start.Column &&
                    Column <= table.Range.End.Column)
                {
                    return Row == table.Start.Row && table.ShowFilterButton;
                }
            }
        }

        if (Sheet?.AutoFilter != null)
        {
            if (Column >= Sheet.AutoFilter.Range.Start.Column && Column <= Sheet.AutoFilter.Range.End.Column)
            {
                return Row == Sheet.AutoFilter.Start.Row;
            }
        }

        return false;
    }

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

        if (Sheet != null)
        {
            Sheet.AutoFilterChanged -= OnAutoFilterChanged;
        }

        await base.SetParametersAsync(parameters);

        if (didRowChange || didColumnChange || didSheetChange)
        {
            if (cell != null)
            {
                cell.Changed -= OnCellChanged;
            }

            if (Sheet != null)
            {
                cell = Sheet.Cells[Row, Column];
                cell.Changed += OnCellChanged;
            }

            showCellMenu = ShowCellMenu();
        }

        if (Sheet != null)
        {
            Sheet.AutoFilterChanged += OnAutoFilterChanged;
        }
    }

    private void OnCellChanged(Cell cell)
    {
        //Console.WriteLine($"Cell {Row}:{Column} changed: {cell.Value}");
        StateHasChanged();
    }

    private void OnAutoFilterChanged()
    {
        var show = ShowCellMenu();
        if (showCellMenu != show)
        {
            showCellMenu = show;
            StateHasChanged();
        }
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
        
        if (Sheet != null)
        {
            Sheet.AutoFilterChanged -= OnAutoFilterChanged;
        }
    }
}