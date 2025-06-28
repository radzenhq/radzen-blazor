namespace Radzen.Blazor.Spreadsheet;

/// <summary>
/// Represents a data table in a spreadsheet, defined by a range.
/// </summary>
public class DataTable(Sheet sheet, RangeRef range) : AutoFilter(sheet, range)
{
    private bool showFilterButton = true;

    /// <summary>
    /// Gets or sets whether to show the filter button for this data table.
    /// </summary>
    public bool ShowFilterButton
    {
        get => showFilterButton;
        set
        {
            if (showFilterButton != value)
            {
                showFilterButton = value;
                Sheet.OnAutoFilterChanged();
            }
        }
    }

    /// <summary>
    /// Sorts the data table order based on the specified column index.
    /// </summary>
    public void Sort(SortOrder order, int column)
    {
        Sheet.Sort(Range, order, column - Range.Start.Column);
    }
}

/// <summary>
/// Represents an auto filter applied to a range in a spreadsheet.
/// </summary>
public class AutoFilter
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AutoFilter"/> class.
    /// </summary>
    public AutoFilter(Sheet sheet, RangeRef range)
    {
        Sheet = sheet;
        Range = range;
    }

    /// <summary>
    /// Gets the sheet to which the auto filter is applied.
    /// </summary>
    public Sheet Sheet { get; }

    /// <summary>
    /// Gets the range of the filter
    /// </summary>
    public RangeRef Range { get; }

    /// <summary>
    /// Gets the first visible cell reference in the range.
    /// </summary>
    public CellRef Start
    {
        get
        {
            // Find the first visible row in the data table range
            for (int row = Range.Start.Row; row <= Range.End.Row; row++)
            {
                if (!Sheet.Rows.IsHidden(row))
                {
                    return new CellRef(row, Range.Start.Column);
                }
            }
            
            // If all rows are hidden, return the original start
            return Range.Start;
        }
    }
}