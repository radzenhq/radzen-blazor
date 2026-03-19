using System;
using System.Globalization;

namespace Radzen.Blazor.Spreadsheet;

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
        if (TryGetNumber(cell, out var number) && number > Value)
        {
            return Format;
        }
        return null;
    }

    internal static bool TryGetNumber(Cell cell, out double number)
    {
        number = 0;
        if (cell.Value is double d) { number = d; return true; }
        if (cell.Value is int i) { number = i; return true; }
        if (cell.Value is float f) { number = f; return true; }
        if (cell.Value is decimal dec) { number = (double)dec; return true; }
        if (cell.Value is long l) { number = l; return true; }
        if (cell.Value != null)
        {
            return double.TryParse(cell.Value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out number);
        }
        return false;
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
        if (GreaterThanRule.TryGetNumber(cell, out var number) && number < Value)
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
        if (GreaterThanRule.TryGetNumber(cell, out var number) && number >= Minimum && number <= Maximum)
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
        if (cell.Value != null && cell.Value.Equals(Value))
        {
            return Format;
        }
        if (GreaterThanRule.TryGetNumber(cell, out var number) && Value is double dv && number == dv)
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
        if (cellText != null && cellText.Contains(Text, StringComparison.OrdinalIgnoreCase))
        {
            return Format;
        }
        return null;
    }
}

/// <summary>
/// Conditional format rule that applies when the cell value is in the top N values.
/// </summary>
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
        if (GreaterThanRule.TryGetNumber(cell, out _))
        {
            return Format;
        }
        return null;
    }
}
