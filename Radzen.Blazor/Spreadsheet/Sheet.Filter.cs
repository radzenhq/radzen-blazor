namespace Radzen.Blazor.Spreadsheet;

using System.Globalization;

#nullable enable

/// <summary>
/// Represents a base class for filter criteria used in filtering rows of a spreadsheet.
/// </summary>
public abstract class FilterCriterion
{
    /// <summary>
    /// Determines whether the specified row in the given sheet matches the filter criterion.
    /// </summary>
    /// <param name="sheet"></param>
    /// <param name="row"></param>
    /// <returns></returns>
    public abstract bool Matches(Sheet sheet, int row);
}

/// <summary>
/// Represents a filter criterion that combines multiple criteria using a logical OR operation.
/// </summary>
public class OrCriterion : FilterCriterion
{
    /// <summary>
    /// Gets or sets the array of filter criteria that will be combined using a logical OR operation.
    /// </summary>
    public FilterCriterion[] Criteria { get; init; } = [];

    /// <inheritdoc/>
    public override bool Matches(Sheet sheet, int row)
    {
        foreach (var criterion in Criteria)
        {
            if (criterion.Matches(sheet, row))
            {
                return true;
            }
        }

        return false;
    }
}

/// <summary>
/// Represents a filter criterion that combines multiple criteria using a logical AND operation.
/// </summary>
public class AndCriterion : FilterCriterion
{
    /// <summary>
    /// Gets or sets the array of filter criteria that will be combined using a logical AND operation.
    /// </summary>
    public FilterCriterion[] Criteria { get; init; } = [];

    /// <inheritdoc/>
    public override bool Matches(Sheet sheet, int row)
    {
        foreach (var criterion in Criteria)
        {
            if (!criterion.Matches(sheet, row))
            {
                return false;
            }
        }

        return true;
    }
}

/// <summary>
/// Represents a base class for filter criteria that operate on a specific column in a spreadsheet.
/// </summary>
public abstract class FilterCriterionLeaf : FilterCriterion
{
    /// <summary>
    /// Gets or sets the index of the column that this filter criterion applies to.
    /// </summary>
    public int ColumnIndex { get; init; }

    /// <inheritdoc/>
    public override bool Matches(Sheet sheet, int row)
    {
        var cell = sheet.Cells[row, ColumnIndex];
        return Matches(cell.Value);
    }

    /// <summary>
    /// Determines whether the specified value matches the filter criterion.
    /// </summary>
    public abstract bool Matches(object? value);

    /// <summary>
    /// Attempts to coerce the given value into a double for comparison purposes.
    /// </summary>
    /// <param name="value"></param>
    /// <param name="result"></param>
    /// <returns></returns>
    protected static bool TryCoerce(object? value, out double result)
    {
        switch (value)
        {
            case double d:
                result = d;
                return true;
            case int i:
                result = i;
                return true;
            case long l:
                result = l;
                return true;
            case float f:
                result = f;
                return true;
            case decimal dec:
                result = (double)dec;
                return true;
            case string str when double.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out var number):
                result = number;
                return true;
        }

        result = default;
        return false;
    }
}

/// <summary>
/// Represents a filter criterion that checks for equality with a specified value.
/// </summary>
public class EqualsCriterion : FilterCriterionLeaf
{
    /// <summary>
    /// Gets or sets the value that this filter criterion checks for equality against.
    /// </summary>
    public object? Value { get; init; }

    /// <inheritdoc/>
    public override bool Matches(object? value)
    {
        if (value == null || Value == null)
        {
            return false;
        }

        if (Equals(value, Value))
        {
            return true;
        }

        if (TryCoerce(value, out var numericValue) && TryCoerce(Value, out var numericCriterion))
        {
            return numericValue == numericCriterion;
        }

        return false;
    }
}

/// <summary>
/// Represents a filter criterion that checks for inequality with a specified value.
/// </summary>
public class GreaterThanCriterion : FilterCriterionLeaf
{
    /// <summary>
    /// Gets or sets the value that this filter criterion checks for being greater than.
    /// </summary>
    public object? Value { get; init; }

    /// <inheritdoc/>
    public override bool Matches(object? value)
    {
        if (value == null || Value == null)
        {
            return false;
        }

        if (TryCoerce(value, out var numericValue) && TryCoerce(Value, out var numericCriterion))
        {
            return numericValue > numericCriterion;
        }

        return false;
    }
}

public partial class Sheet
{
    /// <summary>
    /// Filters the rows in the specified range based on the provided filter criterion.
    /// </summary>
    /// <param name="range"></param>
    /// <param name="criterion"></param>
    public void Filter(RangeRef range, FilterCriterion criterion)
    {
        if (range == RangeRef.Invalid)
        {
            return;
        }

        Rows.BeginUpdate();

        for (var row = range.Start.Row; row <= range.End.Row; row++)
        {
            if (criterion.Matches(this, row))
            {
                Rows.Show(row);
            }
            else
            {
                Rows.Hide(row);
            }
        }

        Rows.EndUpdate();
    }
}