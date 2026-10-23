#nullable enable

using System;
using System.Collections.Generic;

namespace Radzen.Documents.Spreadsheet;

static class RangeSearch
{
    // matchMode: 0 = exact; -1 = exact or next-smaller (largest value <= lookup); 1 = exact or next-larger (smallest value >= lookup).
    public static int Find(IReadOnlyList<CellData> values, CellData lookup, int matchMode, bool wildcards, bool acrossTypes, out CellData? error)
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

            if (!acrossTypes && !IsSameKind(cell, lookup))
            {
                continue;
            }

            if (IsBetterCandidate(cell, lookup, bestIndex == -1 ? null : values[bestIndex], matchMode, acrossTypes))
            {
                bestIndex = i;
            }
        }

        return matchMode == 0 ? -1 : bestIndex;
    }

    public static bool IsBetterCandidate(CellData cell, CellData lookup, CellData? best, int matchMode, bool acrossTypes)
    {
        int Compare(CellData left, CellData right) => acrossTypes ? CompareAcrossTypes(left, right) : left.CompareTo(right);

        return matchMode switch
        {
            -1 => Compare(cell, lookup) < 0 && (best is null || Compare(cell, best) > 0),
            1 => Compare(cell, lookup) > 0 && (best is null || Compare(cell, best) < 0),
            _ => false
        };
    }

    public static int CompareAcrossTypes(CellData left, CellData right)
    {
        var rank = TypeRank(left).CompareTo(TypeRank(right));

        if (rank != 0 || left.IsEmpty || left.Type == CellDataType.Boolean)
        {
            return rank;
        }

        return left.CompareTo(right);
    }

    private static int TypeRank(CellData value) => value.Type switch
    {
        CellDataType.Number or CellDataType.Date => 0,
        CellDataType.String => 1,
        CellDataType.Boolean => value.GetValueOrDefault<bool>() ? 3 : 2,
        CellDataType.Empty => 4,
        _ => 5
    };

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

    public static bool IsExactMatch(CellData cell, CellData lookup, bool wildcards)
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
