namespace Radzen.Blazor.Spreadsheet;

using System;
using System.Collections.Generic;
using System.Globalization;

#nullable enable

/// <summary>
/// Represents a filter applied to a sheet, which includes a filter criterion and the range of rows that the filter applies to.
/// </summary>
public class SheetFilter(FilterCriterion criterion, RangeRef range)
{
    /// <summary>
    /// Gets or sets the filter criterion used to filter rows in the sheet.
    /// </summary>
    public FilterCriterion Criterion { get; } = criterion;

    /// <summary>
    /// Gets or sets the range of rows that the filter applies to.
    /// </summary>
    public RangeRef Range { get; } = range;
}

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

    /// <summary>
    /// Accepts a visitor that can perform operations on this filter criterion.
    /// </summary>
    public abstract void Accept(IFilterCriterionVisitor visitor);
}

/// <summary>
/// Defines a visitor interface for filter criteria, allowing different operations to be performed on various types of filter criteria.
/// </summary>
public interface IFilterCriterionVisitor
{
    /// <summary>
    /// Visits an OrCriterion.
    /// </summary>
    void Visit(OrCriterion criterion);

    /// <summary>
    /// Visits an AndCriterion.
    /// </summary>
    void Visit(AndCriterion criterion);

    /// <summary>
    /// Visits an EqualsCriterion.
    /// </summary>
    void Visit(EqualsCriterion criterion);

    /// <summary>
    /// Visits a GreaterThanCriterion.
    /// </summary>
    void Visit(GreaterThanCriterion criterion);

    /// <summary>
    /// Visits an InListCriterion.
    /// </summary>
    void Visit(InListCriterion criterion);

    /// <summary>
    /// Visits an IsNullCriterion.
    /// </summary>
    void Visit(IsNullCriterion criterion);
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

    /// <inheritdoc/>
    public override void Accept(IFilterCriterionVisitor visitor) => visitor.Visit(this);
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

    /// <inheritdoc/>
    public override void Accept(IFilterCriterionVisitor visitor) => visitor.Visit(this);
}

/// <summary>
/// Represents a base class for filter criteria that operate on a specific column in a spreadsheet.
/// </summary>
public abstract class FilterCriterionLeaf : FilterCriterion
{
    /// <summary>
    /// Gets or sets the index of the column that this filter criterion applies to.
    /// </summary>
    public int Column { get; init; }

    /// <inheritdoc/>
    public override bool Matches(Sheet sheet, int row)
    {
        var cell = sheet.Cells[row, Column];
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

    /// <inheritdoc/>
    public override void Accept(IFilterCriterionVisitor visitor) => visitor.Visit(this);
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

    /// <inheritdoc/>
    public override void Accept(IFilterCriterionVisitor visitor) => visitor.Visit(this);
}

/// <summary>
/// Represents a filter criterion that checks if the value is in a predefined list of values.
/// </summary>
public class InListCriterion : FilterCriterionLeaf
{
    /// <summary>
    /// Gets or sets the list of values that this filter criterion checks against.
    /// </summary>
    public object?[] Values { get; init; } = [];

    /// <inheritdoc/>
    public override bool Matches(object? value)
    {
        // Check for exact matches first (including null)
        foreach (var listValue in Values)
        {
            if (Equals(value, listValue))
            {
                return true;
            }
        }

        // Check for numeric coercion matches (only if value is not null)
        if (value != null && TryCoerce(value, out var numericValue))
        {
            foreach (var listValue in Values)
            {
                if (listValue != null && TryCoerce(listValue, out var numericListValue))
                {
                    if (numericValue == numericListValue)
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    /// <inheritdoc/>
    public override void Accept(IFilterCriterionVisitor visitor) => visitor.Visit(this);
}

/// <summary>
/// Represents a filter criterion that matches only if the value is null.
/// </summary>
public class IsNullCriterion : FilterCriterionLeaf
{
    /// <inheritdoc/>
    public override bool Matches(object? value)
    {
        return value == null;
    }

    /// <inheritdoc/>
    public override void Accept(IFilterCriterionVisitor visitor) => visitor.Visit(this);
}

public partial class Sheet
{
    private readonly List<SheetFilter> filters = [];

    /// <summary>
    /// Gets the list of filters applied to the sheet.
    /// </summary>
    public IReadOnlyList<SheetFilter> Filters => filters;

    /// <summary>
    /// Adds a filter to the sheet.
    /// </summary>
    public void AddFilter(SheetFilter filter)
    {
        if (filter == null)
        {
            throw new ArgumentNullException(nameof(filter));
        }

        filters.Add(filter);

        ApplyFilters();
    }

    private void ApplyFilters()
    {
        Rows.BeginUpdate();

        foreach (var filter in filters)
        {
            Filter(filter.Range, filter.Criterion);
        }

        Rows.EndUpdate();
    }

    internal void Filter(RangeRef range, FilterCriterion criterion)
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