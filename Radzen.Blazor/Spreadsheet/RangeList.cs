#nullable enable

using System.Collections.Generic;
using System;

namespace Radzen.Blazor.Spreadsheet;

/// <summary>
/// Represents a list of cell data originating from a rectangular range, carrying its row/column dimensions.
/// </summary>
public class RangeList : List<CellData>
{
    private readonly Sheet sheet;
    internal Sheet Sheet => sheet;
    /// <summary>
    /// Number of rows in the range.
    /// </summary>
    public int Rows { get; }

    /// <summary>
    /// Number of columns in the range.
    /// </summary>
    public int Columns { get; }

    /// <summary>
    /// Starting row (absolute) of the range in the sheet.
    /// </summary>
    public int StartRow { get; }

    /// <summary>
    /// Starting column (absolute) of the range in the sheet.
    /// </summary>
    public int StartColumn { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="RangeList"/> class with specified dimensions.
    /// </summary>
    /// <param name="rows">Number of rows in the range.</param>
    /// <param name="columns">Number of columns in the range.</param>
    /// <param name="startRow">Starting row index.</param>
    /// <param name="startColumn">Starting column index.</param>
    /// <param name="sheet">The sheet instance providing row visibility information.</param>
    public RangeList(int rows, int columns, int startRow, int startColumn, Sheet sheet)
    {
        Rows = rows;
        Columns = columns;
        StartRow = startRow;
        StartColumn = startColumn;
        this.sheet = sheet;
    }

    /// <summary>
    /// Returns true if the row corresponding to the specified item index is hidden.
    /// </summary>
    public bool IsRowHiddenAt(int itemIndex)
    {
        var rowOffset = itemIndex / Math.Max(1, Columns);
        return sheet.Rows.IsHidden(StartRow + rowOffset);
    }
}