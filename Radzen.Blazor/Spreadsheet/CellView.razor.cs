using System;
using System.Collections.Generic;
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

    /// <summary>
    /// Gets or sets the registered custom cell types.
    /// </summary>
    [CascadingParameter]
    public Dictionary<string, SpreadsheetCellType>? CellTypes { get; set; }

    private RadzenButton? cellMenuButton;
    private bool showCellMenu;
    private Type? customRendererType;

    private string Class => ClassList.Create("rz-spreadsheet-cell")
                                     .Add("rz-spreadsheet-frozen-row", FrozenState.HasFlag(FrozenState.Row))
                                     .Add("rz-spreadsheet-frozen-column", FrozenState.HasFlag(FrozenState.Column))
                                     .Add($"rz-spreadsheet-cell-{cell.ValueType.ToString().ToLowerInvariant()}", cell is not null)
                                     .Add("rz-spreadsheet-cell-invalid", cell?.HasValidationErrors == true)
                                     .ToString();

    private Cell cell = default!;
    private string? numberFormatColor;

    private string? GetDisplayValue()
    {
        if (cell is null) return null;
        var (formatted, color) = NumberFormat.ApplyWithColor(cell.Format?.NumberFormat, cell.Value, cell.ValueType);
        numberFormatColor = color;
        return formatted ?? cell.Value?.ToString();
    }

    private Type? ResolveRendererType()
    {
        if (CellTypes is not { Count: > 0 } || Worksheet is null)
        {
            return null;
        }

        var typeName = Worksheet.Cells.GetCustomType(Row, Column);

        if (typeName is not null && CellTypes.TryGetValue(typeName, out var cellType))
        {
            return cellType.RendererType;
        }

        return null;
    }

    private Dictionary<string, object> GetRendererParameters()
    {
        var displayValue = GetDisplayValue();
        var context = new SpreadsheetCellRenderContext(displayValue, cell!, Worksheet);

        return new Dictionary<string, object>
        {
            { "Context", context }
        };
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
        if (Worksheet is null) return false;

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
        if (Worksheet?.Tables is not null)
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

        if (Worksheet?.AutoFilter.Range is not null)
        {
            var autoFilterRange = Worksheet.AutoFilter.Range.Value;
            if (Column >= autoFilterRange.Start.Column && Column <= autoFilterRange.End.Column)
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
        ArgumentNullException.ThrowIfNull(sb);
        base.AppendStyle(sb);

        // Table style first (defaults), then user format (which can override),
        // then number-format color directives. Last write wins in inline style.
        AppendTableStyle(sb);
        cell.ApplyFormat(sb);

        if (numberFormatColor is not null)
        {
            sb.Append("color: ");
            sb.Append(numberFormatColor);
            sb.Append(';');
        }
    }

    private void AppendTableStyle(StringBuilder sb)
    {
        if (Worksheet?.Tables is null || Worksheet.Tables.Count == 0) return;

        Documents.Spreadsheet.Table? table = null;
        foreach (var t in Worksheet.Tables)
        {
            if (t.Range.Contains(Row, Column)) { table = t; break; }
        }
        if (table is null) return;

        var preset = TableStylePresets.Get(table.TableStyle);

        var isHeader = table.ShowHeaderRow && Row == table.Range.Start.Row;
        var isTotals = table.ShowTotalsRow && Row == table.Range.End.Row;
        var dataStart = table.ShowHeaderRow ? table.Range.Start.Row + 1 : table.Range.Start.Row;
        var dataEnd = table.ShowTotalsRow ? table.Range.End.Row - 1 : table.Range.End.Row;
        var isData = !isHeader && !isTotals && Row >= dataStart && Row <= dataEnd;
        var isFirstCol = Column == table.Range.Start.Column;
        var isLastCol = Column == table.Range.End.Column;

        string? bg = null;
        string? color = null;
        var bold = false;

        if (isHeader)
        {
            bg = preset.HeaderBackground;
            color = preset.HeaderColor;
            bold = preset.BoldHeader;
        }
        else if (isTotals)
        {
            bg = preset.TotalsBackground;
            color = preset.TotalsColor;
            bold = preset.BoldTotals;
        }
        else if (isData)
        {
            // Excel parity (verified via COM probe of $tbl.DisplayFormat.Interior.Color):
            //   - Rows only       → odd data-row offset gets the alt color, all columns.
            //   - Columns only    → odd data-col offset gets the alt color, all rows.
            //   - Both on         → alt color appears only when BOTH row AND col offsets
            //                       are odd. Other intersections keep the base color.
            var rowOdd = ((Row - dataStart) & 1) == 1;
            var colOdd = ((Column - table.Range.Start.Column) & 1) == 1;

            if (table.ShowBandedRows && table.ShowBandedColumns)
            {
                bg = rowOdd && colOdd ? preset.AltRowBackground : preset.RowBackground;
            }
            else if (table.ShowBandedRows)
            {
                bg = rowOdd ? preset.AltRowBackground : preset.RowBackground;
            }
            else if (table.ShowBandedColumns)
            {
                bg = colOdd ? preset.AltRowBackground : preset.RowBackground;
            }

            if (table.HighlightFirstColumn && isFirstCol && preset.BoldFirstColumn) bold = true;
            if (table.HighlightLastColumn && isLastCol && preset.BoldLastColumn) bold = true;
        }

        if (bg is not null)
        {
            sb.Append("background:");
            sb.Append(bg);
            sb.Append(';');
        }
        if (color is not null)
        {
            sb.Append("color:");
            sb.Append(color);
            sb.Append(';');
        }
        if (bold)
        {
            sb.Append("font-weight:600;");
        }

        // Outer table outline + role separators. Borders are drawn on the
        // outermost cells so the entire table reads as a framed block.
        if (preset.BorderColor is not null)
        {
            var isTopRow = Row == table.Range.Start.Row;
            var isBottomRow = Row == table.Range.End.Row;
            var isLeftCol = isFirstCol;
            var isRightCol = isLastCol;

            if (isTopRow)
            {
                sb.Append("border-top:1px solid ");
                sb.Append(preset.BorderColor);
                sb.Append(';');
            }
            if (isBottomRow)
            {
                sb.Append("border-bottom:1px solid ");
                sb.Append(preset.BorderColor);
                sb.Append(';');
            }
            if (isLeftCol)
            {
                sb.Append("border-left:1px solid ");
                sb.Append(preset.BorderColor);
                sb.Append(';');
            }
            if (isRightCol)
            {
                sb.Append("border-right:1px solid ");
                sb.Append(preset.BorderColor);
                sb.Append(';');
            }

            // Internal separator: header row gets a thicker bottom edge to demarcate
            // the data body. (Skip when header is hidden — the role doesn't exist.)
            if (isHeader && !isBottomRow)
            {
                sb.Append("border-bottom:1px solid ");
                sb.Append(preset.BorderColor);
                sb.Append(';');
            }
            // Totals row gets a thicker top edge for the same reason.
            if (isTotals && !isTopRow)
            {
                sb.Append("border-top:2px solid ");
                sb.Append(preset.BorderColor);
                sb.Append(';');
            }
        }
    }

    private async Task OnToggleAsync()
    {
        if (cellMenuButton is not null)
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

        if (didSheetChange && Worksheet is not null)
        {
            Worksheet.AutoFilterChanged -= OnAutoFilterChanged;
        }

        await base.SetParametersAsync(parameters);

        if (didRowChange || didColumnChange || didSheetChange)
        {
            if (cell is not null)
            {
                cell.Changed -= OnCellChanged;
            }

            if (Worksheet is not null)
            {
                cell = Worksheet.Cells[Row, Column];
                cell.Changed += OnCellChanged;
            }

            showCellMenu = ShouldShowCellMenu();
            customRendererType = ResolveRendererType();
        }

        if (didSheetChange && Worksheet is not null)
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
            return;
        }

        // Table style toggles (ShowBandedRows, TableStyle, ShowHeaderRow, etc.) all
        // fire AutoFilterChanged. Re-render when the cell is inside a table so the
        // table-style overrides in AppendStyle pick up the new values.
        if (IsInsideAnyTable())
        {
            StateHasChanged();
        }
    }

    private bool IsInsideAnyTable()
    {
        if (Worksheet?.Tables is null) return false;
        foreach (var t in Worksheet.Tables)
        {
            if (t.Range.Contains(Row, Column)) return true;
        }
        return false;
    }

    void IDisposable.Dispose()
    {
        if (cell is not null)
        {
            cell.Changed -= OnCellChanged;
        }

        if (Worksheet is not null)
        {
            Worksheet.AutoFilterChanged -= OnAutoFilterChanged;
        }
    }
}