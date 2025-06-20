namespace Radzen.Blazor.Spreadsheet;

/// <summary>
/// Represents a data table in a spreadsheet, defined by a range.
/// Tables are used to organize and manage data within a specific range of cells.
/// A data table can be used to apply styles, formatting, and other operations on the specified range.
/// </summary>
/// <param name="range"></param>
public class DataTable(RangeRef range)
{
    /// <summary>
    /// Gets the range that defines the data table.
    /// </summary>
    public RangeRef Range { get; } = range;
}