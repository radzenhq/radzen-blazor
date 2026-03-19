using System;
using System.Collections.Generic;
using System.Linq;

namespace Radzen.Blazor.Spreadsheet;

#nullable enable

/// <summary>
/// Base class for conditional formatting rules.
/// </summary>
public abstract class ConditionalFormatBase
{
    /// <summary>
    /// Calculates the conditional <see cref="Format"/> for the provided <see cref="Cell"/>.
    /// Return null when the rule does not apply.
    /// </summary>
    public abstract Format? GetFormat(Cell cell);
}

/// <summary>
/// Store for conditional formatting rules per ranges.
/// </summary>
public class ConditionalFormatStore
{
    private readonly Dictionary<RangeRef, List<ConditionalFormatBase>> formats = [];

    /// <summary>
    /// Adds conditional formatting rules for a specific range.
    /// </summary>
    public void Add(RangeRef range, params ConditionalFormatBase[] rules)
    {
        ArgumentNullException.ThrowIfNull(rules);
        foreach (var rule in rules)
        {
            Add(range, rule);
        }
    }

    /// <summary>
    /// Adds a conditional formatting rule for a specific range.
    /// </summary>
    public void Add(RangeRef range, ConditionalFormatBase rule)
    {
        ArgumentNullException.ThrowIfNull(rule);
        if (!formats.TryGetValue(range, out var list))
        {
            list = [];
            formats[range] = list;
        }

        list.Add(rule);
    }

    /// <summary>
    /// Removes a conditional formatting rule from a specific range.
    /// </summary>
    public bool Remove(RangeRef range, ConditionalFormatBase rule)
    {
        ArgumentNullException.ThrowIfNull(rule);
        if (formats.TryGetValue(range, out var list))
        {
            var removed = list.Remove(rule);
            if (list.Count == 0)
            {
                formats.Remove(range);
            }
            return removed;
        }
        return false;
    }

    /// <summary>
    /// Gets all rules for a specific range.
    /// </summary>
    public IReadOnlyList<ConditionalFormatBase> GetRules(RangeRef range)
    {
        return formats.TryGetValue(range, out var list) ? list : [];
    }

    /// <summary>
    /// Gets all ranges that have conditional formatting rules.
    /// </summary>
    public IEnumerable<RangeRef> Ranges => formats.Keys;

    /// <summary>
    /// Calculates the merged conditional overlay format for a cell from all matching rules.
    /// </summary>
    /// <remarks>
    /// Rules are applied in the order they were added, later rules can override earlier ones.
    /// </remarks>
    public Format? Calculate(Cell cell)
    {
        ArgumentNullException.ThrowIfNull(cell);
        Format? overlay = null;

        foreach (var kvp in formats)
        {
            if (!kvp.Key.Contains(cell.Address))
            {
                continue;
            }

            foreach (var rule in kvp.Value)
            {
                var candidate = rule.GetFormat(cell);
                if (candidate == null)
                {
                    continue;
                }

                overlay = overlay == null ? candidate.Clone() : overlay.Merge(candidate);
            }
        }

        return overlay;
    }
}
