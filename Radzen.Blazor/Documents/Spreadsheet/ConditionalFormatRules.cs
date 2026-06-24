using System;
using System.Collections.Generic;
using System.Linq;

namespace Radzen.Documents.Spreadsheet;

#nullable enable

/// <summary>
/// Conditional format rule that applies when the cell value is greater than a threshold.
/// </summary>
public class GreaterThanRule : ConditionalFormatBase
{
    /// <summary>Gets or sets the threshold value.</summary>
    public double Value { get; set; }

    /// <summary>Gets or sets the format to apply when the rule matches.</summary>
    public Format Format { get; set; } = new();

    /// <inheritdoc/>
    public override Format? GetFormat(Cell cell)
    {
        ArgumentNullException.ThrowIfNull(cell);
        if (NumericCoercion.TryCoerceToDouble(cell.Value, out var number) && number > Value)
        {
            return Format;
        }
        return null;
    }
}

/// <summary>
/// Conditional format rule that applies when the cell value is less than a threshold.
/// </summary>
public class LessThanRule : ConditionalFormatBase
{
    /// <summary>Gets or sets the threshold value.</summary>
    public double Value { get; set; }

    /// <summary>Gets or sets the format to apply when the rule matches.</summary>
    public Format Format { get; set; } = new();

    /// <inheritdoc/>
    public override Format? GetFormat(Cell cell)
    {
        ArgumentNullException.ThrowIfNull(cell);
        if (NumericCoercion.TryCoerceToDouble(cell.Value, out var number) && number < Value)
        {
            return Format;
        }
        return null;
    }
}

/// <summary>
/// Conditional format rule that applies when the cell value is between two thresholds.
/// </summary>
public class BetweenRule : ConditionalFormatBase
{
    /// <summary>Gets or sets the minimum value.</summary>
    public double Minimum { get; set; }

    /// <summary>Gets or sets the maximum value.</summary>
    public double Maximum { get; set; }

    /// <summary>Gets or sets the format to apply when the rule matches.</summary>
    public Format Format { get; set; } = new();

    /// <inheritdoc/>
    public override Format? GetFormat(Cell cell)
    {
        ArgumentNullException.ThrowIfNull(cell);
        if (NumericCoercion.TryCoerceToDouble(cell.Value, out var number) && number >= Minimum && number <= Maximum)
        {
            return Format;
        }
        return null;
    }
}

/// <summary>
/// Conditional format rule that applies when the cell value equals a specific value.
/// </summary>
public class EqualToRule : ConditionalFormatBase
{
    /// <summary>Gets or sets the value to compare against.</summary>
    public object? Value { get; set; }

    /// <summary>Gets or sets the format to apply when the rule matches.</summary>
    public Format Format { get; set; } = new();

    /// <inheritdoc/>
    public override Format? GetFormat(Cell cell)
    {
        ArgumentNullException.ThrowIfNull(cell);
        if (cell.Value is not null && cell.Value.Equals(Value))
        {
            return Format;
        }
        if (NumericCoercion.TryCoerceToDouble(cell.Value, out var number) && Value is double dv && number == dv)
        {
            return Format;
        }
        return null;
    }
}

/// <summary>
/// Conditional format rule that applies when the cell value contains specified text.
/// </summary>
public class TextContainsRule : ConditionalFormatBase
{
    /// <summary>Gets or sets the text to search for.</summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>Gets or sets the format to apply when the rule matches.</summary>
    public Format Format { get; set; } = new();

    /// <inheritdoc/>
    public override Format? GetFormat(Cell cell)
    {
        ArgumentNullException.ThrowIfNull(cell);
        var cellText = cell.Value?.ToString();
        if (cellText is not null && cellText.Contains(Text, StringComparison.OrdinalIgnoreCase))
        {
            return Format;
        }
        return null;
    }
}

/// <summary>
/// Conditional format rule that applies when the cell value is in the top (or bottom) N distinct values of its range.
/// </summary>
/// <remarks>
/// Selection follows Excel semantics: it ranks the DISTINCT numeric values within the range and matches any cell
/// whose value is at or beyond the resulting threshold (so ties at the threshold are all included). Because the
/// rule needs the full range to compute the threshold, evaluation is performed by
/// <see cref="ConditionalFormatStore.Calculate(Cell)"/>; calling <see cref="GetFormat(Cell)"/> directly returns
/// <see langword="null"/>.
/// </remarks>
public class Top10Rule : ConditionalFormatBase
{
    /// <summary>Gets or sets how many top values to highlight.</summary>
    public int Count { get; set; } = 10;

    /// <summary>Gets or sets whether to use bottom N instead of top N.</summary>
    public bool Bottom { get; set; }

    /// <summary>Gets or sets the format to apply when the rule matches.</summary>
    public Format Format { get; set; } = new();

    /// <inheritdoc/>
    public override Format? GetFormat(Cell cell)
    {
        ArgumentNullException.ThrowIfNull(cell);
        return null;
    }

    internal Format? GetFormat(Cell cell, RangeRef range)
    {
        ArgumentNullException.ThrowIfNull(cell);

        if (Count <= 0)
        {
            return null;
        }

        if (!NumericCoercion.TryCoerceToDouble(cell.Value, out var cellValue))
        {
            return null;
        }

        var sheet = cell.Worksheet;
        var distinct = new HashSet<double>();
        foreach (var address in range.GetCells())
        {
            var other = sheet.Cells[address];
            if (NumericCoercion.TryCoerceToDouble(other.Value, out var v))
            {
                distinct.Add(v);
            }
        }

        if (distinct.Count == 0)
        {
            return null;
        }

        var ordered = Bottom
            ? distinct.OrderBy(v => v).ToList()
            : distinct.OrderByDescending(v => v).ToList();

        var index = Math.Min(Count, ordered.Count) - 1;
        var threshold = ordered[index];

        var matches = Bottom ? cellValue <= threshold : cellValue >= threshold;
        return matches ? Format : null;
    }
}
