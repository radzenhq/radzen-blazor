using Microsoft.AspNetCore.Components;
using System.Collections.Generic;

namespace Radzen.Blazor.Spreadsheet;

/// <summary>
/// Represents a component that displays the selection of a cell in a spreadsheet.
/// </summary>
public partial class CellSelection
{
    /// <summary>
    /// Gets or sets the cell reference for which the selection is displayed.
    /// </summary>
    [Parameter]
    public CellRef Cell { get; set; }

    /// <summary>
    /// Gets or sets the sheet that contains the cell for which the selection is displayed.
    /// </summary>
    [Parameter]
    public Sheet Sheet { get; set; }

    /// <summary>
    /// Gets or sets the virtual grid context that provides information about the grid's state.
    /// </summary>
    [Parameter]
    public IVirtualGridContext Context { get; set; }

    private IEnumerable<RangeInfo> GetRanges()
    {
        var mergedRange = Sheet.MergedCells.GetMergedRange(Cell);
        var range = mergedRange != RangeRef.Invalid ? mergedRange : new RangeRef(Cell, Cell);

        return Sheet.GetRanges(range);
    }
}