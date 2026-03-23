namespace Radzen.Documents.Spreadsheet;

/// <summary>
/// Represents a data table in a spreadsheet, defined by a range.
/// </summary>
public class Table
{
    private bool showFilterButton = true;
    private AutoFilter filter = default!;

    /// <summary>
    /// Initializes a new instance of the <see cref="Table"/> class.
    /// </summary>
    public Table(Worksheet sheet, RangeRef range)
    {
        Worksheet = sheet;
        Range = range;
        Filter = new AutoFilter(sheet, range);
    }

    /// <summary>
    /// Gets the sheet to which the table is applied.
    /// </summary>
    public Worksheet Worksheet { get; }

    /// <summary>
    /// Gets the range of the table.
    /// </summary>
    public RangeRef Range { get; }

    /// <summary>
    /// Gets the auto filter for this table.
    /// </summary>
    public AutoFilter Filter
    {
        get => filter;
        private set => filter = value;
    }

    /// <summary>
    /// Gets the first visible cell reference in the range.
    /// </summary>
    public CellRef Start => Filter.Start;

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
                Worksheet.OnAutoFilterChanged();
            }
        }
    }

    /// <summary>
    /// Sorts the data table order based on the specified column index.
    /// </summary>
    public void Sort(SortOrder order, int column)
    {
        Worksheet.Sort(Range, order, column - Range.Start.Column);
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
    internal AutoFilter(Worksheet sheet)
    {
        Worksheet = sheet;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AutoFilter"/> class with a range.
    /// </summary>
    internal AutoFilter(Worksheet sheet, RangeRef range)
    {
        Worksheet = sheet;
        Range = range;
    }

    /// <summary>
    /// Gets the sheet to which the auto filter is applied.
    /// </summary>
    public Worksheet Worksheet { get; }

    /// <summary>
    /// Gets or sets the range of the filter. Set to null to disable the auto filter.
    /// </summary>
    public RangeRef? Range { get; set; }

    /// <summary>
    /// Gets the first visible cell reference in the range.
    /// </summary>
    public CellRef Start
    {
        get
        {
            if (Range is null)
            {
                return CellRef.Invalid;
            }

            var range = Range.Value;

            // Find the first visible row in the data table range
            for (int row = range.Start.Row; row <= range.End.Row; row++)
            {
                if (!Worksheet.Rows.IsHidden(row))
                {
                    return new CellRef(row, range.Start.Column);
                }
            }

            // If all rows are hidden, return the original start
            return range.Start;
        }
    }

    /// <summary>
    /// Clears all filter criteria while keeping the auto filter enabled.
    /// </summary>
    public void ShowAll()
    {
        if (Range is not null)
        {
            Worksheet.ClearFilters();
        }
    }
}