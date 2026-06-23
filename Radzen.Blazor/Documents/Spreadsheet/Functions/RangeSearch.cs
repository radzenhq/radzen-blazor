#nullable enable

using System;
using System.Collections.Generic;

namespace Radzen.Documents.Spreadsheet;

static class RangeSearch
{
    // matchMode: 0 = exact; -1 = exact or next-smaller (largest value <= lookup); 1 = exact or next-larger (smallest value >= lookup).
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
