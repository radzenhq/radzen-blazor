using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Radzen.Blazor.Rendering;

using Radzen.Documents.Spreadsheet;
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
/// Represents the event arguments for image resize events in a spreadsheet.
/// </summary>
public class ImageResizeEventArgs : EventArgs
{
    /// <summary>
    /// Gets or sets the resize direction (nw, ne, sw, se).
    /// </summary>
    public string Direction { get; set; } = "";

    /// <summary>
    /// Gets or sets the pointer event arguments.
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
    [Parameter]
    public int Row { get; set; }

    /// <summary>
    /// Gets or sets the column index of the cell.
    /// </summary>
    [Parameter]
    public int Column { get; set; }

    /// <summary>
    /// Gets or sets the sheet that contains the cell.
    /// </summary>
    [Parameter]
    public Worksheet Worksheet { get; set; } = default!;

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
                                     .Add("rz-spreadsheet-cell-invalid", cell?.HasValidationErrors == true)
                                     .ToString();

    private Cell cell = default!;
    private string? numberFormatColor;

    private string? GetDisplayValue()
    {
        if (cell == null) return null;
        var (formatted, color) = NumberFormat.ApplyWithColor(cell.Format?.NumberFormat, cell.Value, cell.ValueType);
        numberFormatColor = color;
        return formatted ?? cell.Value?.ToString();
    }

    private string? GetValidationTooltip()
    {
        if (cell?.HasValidationErrors != true)
        {
            return null;
        }

        return string.Join("\n", cell.ValidationErrors);
    }

    private bool HasListValidation()
    {
        if (Worksheet == null) return false;

        var validators = Worksheet.Validation.GetValidatorsForCell(new CellRef(Row, Column));
        foreach (var v in validators)
        {
            if (v is DataValidationRule rule && rule.Type == DataValidationType.List)
            {
                return true;
            }
        }
        return false;
    }

    private bool ShouldShowCellMenu()
    {
        if (Worksheet?.Tables != null)
        {
            foreach (var table in Worksheet.Tables)
            {
                if (Column >= table.Range.Start.Column &&
                    Column <= table.Range.End.Column)
                {
                    if (Row == table.Start.Row && table.ShowFilterButton)
                    {
                        return ShouldShowMenuForMergedCell();
                    }
                }
            }
        }

        if (Worksheet?.AutoFilter != null)
        {
            if (Column >= Worksheet.AutoFilter.Range.Start.Column && Column <= Worksheet.AutoFilter.Range.End.Column)
            {
                if (Row == Worksheet.AutoFilter.Start.Row)
                {
                    return ShouldShowMenuForMergedCell();
                }
            }
        }

        if (HasListValidation())
        {
            return true;
        }

        return false;
    }

    private bool ShouldShowMenuForMergedCell()
    {
        // Check if the current cell is part of a merged range
        var mergedRange = Worksheet.MergedCells.GetMergedRange(new CellRef(Row, Column));

        if (mergedRange == RangeRef.Invalid)
        {
            // Not a merged cell, show the menu
            return true;
        }

        // If the merged range overlaps with frozen columns, we need to check which split region this cell belongs to
        if (mergedRange.Start.Column < Worksheet.Columns.Frozen && mergedRange.End.Column >= Worksheet.Columns.Frozen)
        {
            // The merged range is split horizontally by frozen columns
            // Only show the menu for the region that is after the frozen columns
            return Column >= Worksheet.Columns.Frozen;
        }

        // If the merged range doesn't overlap with frozen columns, show the menu
        return true;
    }

    /// <inheritdoc/>
    protected override void AppendStyle(StringBuilder sb)
    {
        base.AppendStyle(sb);
        cell.ApplyFormat(sb);

        if (numberFormatColor != null)
        {
            sb.Append("color: ");
            sb.Append(numberFormatColor);
            sb.Append(';');
        }
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
        var didSheetChange = parameters.TryGetValue<Worksheet>(nameof(Worksheet), out var sheet) && Worksheet != sheet;

        if (didSheetChange && Worksheet != null)
        {
            Worksheet.AutoFilterChanged -= OnAutoFilterChanged;
        }

        await base.SetParametersAsync(parameters);

        if (didRowChange || didColumnChange || didSheetChange)
        {
            if (cell != null)
            {
                cell.Changed -= OnCellChanged;
            }

            if (Worksheet != null)
            {
                cell = Worksheet.Cells[Row, Column];
                cell.Changed += OnCellChanged;
            }

            showCellMenu = ShouldShowCellMenu();
        }

        if (didSheetChange && Worksheet != null)
        {
            Worksheet.AutoFilterChanged += OnAutoFilterChanged;
        }
    }

    private void OnCellChanged(Cell cell)
    {
        StateHasChanged();
    }

    private void OnAutoFilterChanged()
    {
        var show = ShouldShowCellMenu();
        if (showCellMenu != show)
        {
            showCellMenu = show;
            StateHasChanged();
        }
    }

    void IDisposable.Dispose()
    {
        if (cell != null)
        {
            cell.Changed -= OnCellChanged;
        }

        if (Worksheet != null)
        {
            Worksheet.AutoFilterChanged -= OnAutoFilterChanged;
        }
    }
}