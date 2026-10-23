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
                continue;
            }

            if (IsExactMatch(cell, lookup, wildcards))
            {
                return i;
            }

            if (!IsSameKind(cell, lookup))
            {
                continue;
            }

            if (matchMode == -1 && cell.CompareTo(lookup) < 0 &&
                (bestIndex == -1 || cell.CompareTo(values[bestIndex]) > 0))
            {
                bestIndex = i;
            }
            else if (matchMode == 1 && cell.CompareTo(lookup) > 0 &&
                (bestIndex == -1 || cell.CompareTo(values[bestIndex]) < 0))
            {
                bestIndex = i;
            }
        }

        return matchMode == 0 ? -1 : bestIndex;
    }

    public static bool IsSameKind(CellData cell, CellData lookup)
    {
        if (cell.IsError || cell.IsEmpty || lookup.IsEmpty)
        {
            return false;
        }

        var cellIsNumber = cell.Type is CellDataType.Number or CellDataType.Date;
        var lookupIsNumber = lookup.Type is CellDataType.Number or CellDataType.Date;

        return cellIsNumber ? lookupIsNumber : cell.Type == lookup.Type;
    }

    private static bool IsExactMatch(CellData cell, CellData lookup, bool wildcards)
    {
        if (cell.IsEmpty || lookup.IsEmpty)
        {
            return cell.IsEmpty && lookup.IsEmpty;
        }

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
