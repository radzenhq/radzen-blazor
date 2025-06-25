using System;

namespace Radzen.Blazor.Spreadsheet;

/// <summary>
/// Represents a data table in a spreadsheet, defined by a range.
/// Tables are used to organize and manage data within a specific range of cells.
/// A data table can be used to apply styles, formatting, and other operations on the specified range.
/// </summary>
public class DataTable(Sheet sheet, RangeRef range)
{
    /// <summary>
    /// Gets the range that defines the data table.
    /// </summary>
    public RangeRef Range { get; } = range;

    /// <summary>
    /// Sorts the data table in ascending order based on the specified column index.
    /// </summary>
    public void Sort(SortOrder order, int column)
    {
        sheet.Sort(Range, order, column - Range.Start.Column);
    }
}