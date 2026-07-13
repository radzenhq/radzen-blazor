using System.Collections.Generic;

namespace Radzen.Documents.Pdf;


internal readonly struct FragmentRow
{
    public required int SourceRow { get; init; }

    public required bool IsHeader { get; init; }

    public required double Y { get; init; }

    public required double Height { get; init; }
}

internal sealed class TableFragment
{
    public required int Number { get; init; }

    public required IReadOnlyList<FragmentRow> Rows { get; init; }

    public required int HeaderRowCount { get; init; }

    public required double Height { get; init; }
}

internal static class TablePaginator
{
    public static IReadOnlyList<TableFragment> Paginate(LaidOutTable layout, Table source, double availableHeight)
        => Paginate(layout, source, availableHeight, availableHeight);

    // The first fragment may get less room than the rest (it starts at the flow cursor);
    // subsequent fragments start at the top of a fresh page.
    public static IReadOnlyList<TableFragment> Paginate(
        LaidOutTable layout, Table source, double firstAvailable, double subsequentAvailable)
    {
        List<int> headers = [];
        List<int> bodies = [];
        for (var i = 0; i < source.Rows.Count; i++)
        {
            if (source.Rows[i].IsHeader)
            {
                headers.Add(i);
            }
            else
            {
                bodies.Add(i);
            }
        }

        double headerHeight = 0;
        foreach (var h in headers)
        {
            headerHeight += layout.RowHeights[h];
        }

        // Rows covered by a RowSpan > 1 cell move between fragments as one group.
        var reach = BuildReach(layout, source.Rows.Count);

        List<TableFragment> fragments = [];
        var body = 0;

        // The first fragment starts at the flow cursor (firstAvailable); once anything is
        // emitted every later fragment starts fresh at subsequentAvailable.
        var onFirst = true;
        while (true)
        {
            var available = onFirst ? firstAvailable : subsequentAvailable;
            var running = headerHeight;
            List<int> placed = [];
            var deferred = false;
            while (body < bodies.Count)
            {
                var last = body;
                var groupEnd = reach[bodies[body]];
                double groupHeight = layout.RowHeights[bodies[body]];
                while (last + 1 < bodies.Count && bodies[last + 1] <= groupEnd)
                {
                    last++;
                    groupEnd = System.Math.Max(groupEnd, reach[bodies[last]]);
                    groupHeight += layout.RowHeights[bodies[last]];
                }

                var fits = running + groupHeight <= available + 1e-6;

                // A KeepTogether group that overflows the partial first fragment but fits a
                // fresh page is pushed there whole instead of being force-split at the break.
                if (placed.Count == 0 && !fits && onFirst && available + 1e-6 < subsequentAvailable
                    && headerHeight + groupHeight <= subsequentAvailable + 1e-6
                    && GroupKeepTogether(source, bodies, body, last))
                {
                    deferred = true;
                    break;
                }

                if (placed.Count == 0 || fits)
                {
                    for (var g = body; g <= last; g++)
                    {
                        placed.Add(bodies[g]);
                    }

                    running += groupHeight;
                    body = last + 1;
                }
                else
                {
                    break;
                }
            }

            if (placed.Count == 0)
            {
                if (deferred)
                {
                    onFirst = false;
                    continue;
                }

                // A header-only (or empty) table still yields a single fragment.
                if (fragments.Count == 0)
                {
                    fragments.Add(BuildFragment(1, layout, headers, placed));
                }

                break;
            }

            fragments.Add(BuildFragment(fragments.Count + 1, layout, headers, placed));
            onFirst = false;

            if (body >= bodies.Count)
            {
                break;
            }
        }

        return fragments;
    }

    // reach[r] is the last row the group starting at r must include: every row spanned by a
    // RowSpan > 1 cell moves between fragments as one unit.
    private static int[] BuildReach(LaidOutTable layout, int rowCount)
    {
        var reach = new int[rowCount];
        for (var i = 0; i < reach.Length; i++)
        {
            reach[i] = i;
        }

        foreach (var cell in layout.Cells)
        {
            if (cell.RowSpan <= 1)
            {
                continue;
            }

            var end = cell.Row + cell.RowSpan - 1;
            for (var r = cell.Row; r <= end && r < reach.Length; r++)
            {
                reach[r] = System.Math.Max(reach[r], end);
            }
        }

        return reach;
    }

    // Height of the header rows plus the first body row group - the rowspan closure Paginate
    // force-places as one unit. Paginator's page-flush check needs the identical measurement,
    // so both consume this one helper instead of duplicating the reach/group computation.
    internal static double FirstBodyGroupHeight(LaidOutTable layout, Table source)
    {
        double headerHeight = 0;
        List<int> bodies = [];
        for (var r = 0; r < source.Rows.Count; r++)
        {
            if (source.Rows[r].IsHeader)
            {
                headerHeight += layout.RowHeights[r];
            }
            else
            {
                bodies.Add(r);
            }
        }

        if (bodies.Count == 0)
        {
            return headerHeight;
        }

        var reach = BuildReach(layout, source.Rows.Count);
        var last = 0;
        var groupEnd = reach[bodies[0]];
        var groupHeight = layout.RowHeights[bodies[0]];
        while (last + 1 < bodies.Count && bodies[last + 1] <= groupEnd)
        {
            last++;
            groupEnd = System.Math.Max(groupEnd, reach[bodies[last]]);
            groupHeight += layout.RowHeights[bodies[last]];
        }

        return headerHeight + groupHeight;
    }

    private static bool GroupKeepTogether(Table source, List<int> bodies, int first, int last)
    {
        for (var g = first; g <= last; g++)
        {
            if (source.Rows[bodies[g]].KeepTogether)
            {
                return true;
            }
        }

        return false;
    }

    private static TableFragment BuildFragment(int number, LaidOutTable layout, List<int> headers, List<int> bodyRows)
    {
        List<FragmentRow> rows = [];
        double y = 0;
        foreach (var h in headers)
        {
            var height = layout.RowHeights[h];
            rows.Add(new FragmentRow { SourceRow = h, IsHeader = true, Y = y, Height = height });
            y += height;
        }

        foreach (var b in bodyRows)
        {
            var height = layout.RowHeights[b];
            rows.Add(new FragmentRow { SourceRow = b, IsHeader = false, Y = y, Height = height });
            y += height;
        }

        return new TableFragment
        {
            Number = number,
            Rows = rows,
            HeaderRowCount = headers.Count,
            Height = y,
        };
    }
}
