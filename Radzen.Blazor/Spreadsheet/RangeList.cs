#nullable enable

using System.Collections.Generic;

namespace Radzen.Blazor.Spreadsheet;

/// <summary>
/// Represents a list of cell data originating from a rectangular range, carrying its row/column dimensions.
/// </summary>
public class RangeList : List<CellData>
{
    /// <summary>
    /// Number of rows in the range.
    /// </summary>
    public int Rows { get; }

    /// <summary>
    /// Number of columns in the range.
    /// </summary>
    public int Columns { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="RangeList"/> class with specified dimensions.
    /// </summary>
    /// <param name="rows">Number of rows in the range.</param>
    /// <param name="columns">Number of columns in the range.</param>
    public RangeList(int rows, int columns)
    {
        Rows = rows;
        Columns = columns;
    }
}


