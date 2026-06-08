#nullable enable

using System.Collections.Generic;

namespace Radzen.Documents.Spreadsheet;

/// <summary>
/// Shared matcher for functions with repeating (range, criteria) pairs (COUNTIFS, SUMIFS, AVERAGEIFS,
/// MAXIFS, MINIFS).
/// </summary>
static class CriteriaPairs
{
    /// <summary>
    /// Parses alternating (range, criteria) groups and returns the indices where ALL criteria match.
    /// Returns false with <paramref name="error"/> set on a structural problem (odd/empty groups or
    /// mismatched range sizes).
    /// </summary>
    public static bool TryMatch(List<List<CellData>>? groups, out List<int> matched, out CellData? error)
    {
        matched = new List<int>();
        error = null;

        if (groups is null || groups.Count == 0 || groups.Count % 2 != 0)
        {
            error = CellData.FromError(CellError.Value);
            return false;
        }

        var pairCount = groups.Count / 2;
        var ranges = new List<CellData>[pairCount];
        var criterias = new CellData[pairCount];

        for (var p = 0; p < pairCount; p++)
        {
            ranges[p] = groups[p * 2];
            var criteriaGroup = groups[p * 2 + 1];

            if (criteriaGroup.Count == 0)
            {
                error = CellData.FromError(CellError.Value);
                return false;
            }

            criterias[p] = criteriaGroup[0];
        }

        var count = ranges[0].Count;

        for (var p = 1; p < pairCount; p++)
        {
            if (ranges[p].Count != count)
            {
                error = CellData.FromError(CellError.Value);
                return false;
            }
        }

        for (var i = 0; i < count; i++)
        {
            var allMatch = true;

            for (var p = 0; p < pairCount; p++)
            {
                if (!ranges[p][i].MatchesCriteria(criterias[p]))
                {
                    allMatch = false;
                    break;
                }
            }

            if (allMatch)
            {
                matched.Add(i);
            }
        }

        return true;
    }
}
