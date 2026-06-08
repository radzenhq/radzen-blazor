#nullable enable

using System;
using System.Collections.Generic;

namespace Radzen.Documents.Spreadsheet;

/// <summary>
/// Shared lookup-position search for MATCH and XMATCH.
/// </summary>
static class RangeSearch
{
    /// <summary>
    /// Finds the position of <paramref name="lookup"/> within <paramref name="values"/>.
    /// </summary>
    /// <param name="values">The vector to search.</param>
    /// <param name="lookup">The value to find.</param>
    /// <param name="matchMode">0 = exact; -1 = exact or next-smaller (largest value &lt;= lookup);
    /// 1 = exact or next-larger (smallest value &gt;= lookup).</param>
    /// <param name="wildcards">When true, a string lookup containing <c>*</c> or <c>?</c> is matched as a pattern.</param>
    /// <param name="error">Set to the first error cell encountered, if any.</param>
    /// <returns>The 0-based index, or -1 if not found.</returns>
    public static int Find(IReadOnlyList<CellData> values, CellData lookup, int matchMode, bool wildcards, out CellData? error)
    {
        error = null;
        var bestIndex = -1;

        for (var i = 0; i < values.Count; i++)
        {
            var cell = values[i];

            if (cell.IsError)
            {
                error = cell;
                return -1;
            }

            if (IsExactMatch(cell, lookup, wildcards))
            {
                return i;
            }

            if (matchMode == -1 && cell.IsLessThan(lookup) &&
                (bestIndex == -1 || cell.IsGreaterThan(values[bestIndex])))
            {
                bestIndex = i;
            }
            else if (matchMode == 1 && cell.IsGreaterThan(lookup) &&
                (bestIndex == -1 || cell.IsLessThan(values[bestIndex])))
            {
                bestIndex = i;
            }
        }

        return matchMode == 0 ? -1 : bestIndex;
    }

    private static bool IsExactMatch(CellData cell, CellData lookup, bool wildcards)
    {
        if (wildcards && lookup.Type == CellDataType.String)
        {
            var pattern = lookup.GetValueOrDefault<string>() ?? "";

            if (pattern.Contains('*', StringComparison.Ordinal) || pattern.Contains('?', StringComparison.Ordinal))
            {
                return Wildcard.IsFullMatch(cell.ToString() ?? "", pattern);
            }
        }

        return cell.IsEqualTo(lookup);
    }
}
