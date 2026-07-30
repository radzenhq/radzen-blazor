using System;
using System.Collections.Generic;
using Radzen.Documents.Geometry;

namespace Radzen.Documents.Layout;


internal static class TablePaginator
{
    public static IReadOnlyList<LaidOutTableSlice> Paginate(
        LaidOutTable layout,
        Table source,
        double availableHeight,
        LayoutCaptureContext capture)
        => Paginate(layout, source, availableHeight, availableHeight, capture);

    public static IReadOnlyList<LaidOutTableSlice> Paginate(
        LaidOutTable layout,
        Table source,
        double firstAvailable,
        double subsequentAvailable,
        LayoutCaptureContext capture)
    {
        var (headers, bodies, headerHeight) = SplitRows(layout, source);

        var reach = BuildReach(layout, source.Rows.Count);

        List<LaidOutTableSlice> fragments = [];
        var body = 0;

        var onFirst = true;
        while (true)
        {
            var available = onFirst ? firstAvailable : subsequentAvailable;
            var running = headerHeight;
            List<int> placed = [];
            var deferred = false;
            while (body < bodies.Count)
            {
                var (last, groupHeight) = NextGroup(layout, bodies, reach, body);

                var fits = running + groupHeight <= available + LayoutTolerance.Epsilon;

                if (placed.Count == 0 && !fits && onFirst && available + LayoutTolerance.Epsilon < subsequentAvailable
                    && headerHeight + groupHeight <= subsequentAvailable + LayoutTolerance.Epsilon
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

                if (fragments.Count == 0)
                {
                    fragments.Add(BuildFragment(1, layout, headers, placed, capture));
                }

                break;
            }

            fragments.Add(BuildFragment(fragments.Count + 1, layout, headers, placed, capture));
            onFirst = false;

            if (body >= bodies.Count)
            {
                break;
            }
        }

        return fragments;
    }

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
                reach[r] = Math.Max(reach[r], end);
            }
        }

        return reach;
    }

    private static (List<int> Headers, List<int> Bodies, double HeaderHeight) SplitRows(LaidOutTable layout, Table source)
    {
        List<int> headers = [];
        List<int> bodies = [];
        double headerHeight = 0;
        for (var i = 0; i < source.Rows.Count; i++)
        {
            if (source.Rows[i].RepeatOnEveryPage)
            {
                headers.Add(i);
                headerHeight += layout.RowHeights[i];
            }
            else
            {
                bodies.Add(i);
            }
        }

        return (headers, bodies, headerHeight);
    }

    private static (int Last, double GroupHeight) NextGroup(
        LaidOutTable layout, List<int> bodies, int[] reach, int start)
    {
        var last = start;
        var groupEnd = reach[bodies[start]];
        var groupHeight = layout.RowHeights[bodies[start]];
        while (last + 1 < bodies.Count && bodies[last + 1] <= groupEnd)
        {
            last++;
            groupEnd = Math.Max(groupEnd, reach[bodies[last]]);
            groupHeight += layout.RowHeights[bodies[last]];
        }

        return (last, groupHeight);
    }

    internal static double FirstBodyGroupHeight(LaidOutTable layout, Table source)
    {
        var (_, bodies, headerHeight) = SplitRows(layout, source);
        if (bodies.Count == 0)
        {
            return headerHeight;
        }

        var (_, groupHeight) = NextGroup(layout, bodies, BuildReach(layout, source.Rows.Count), 0);
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

    private static LaidOutTableSlice BuildFragment(
        int number,
        LaidOutTable layout,
        List<int> headers,
        List<int> bodyRows,
        LayoutCaptureContext capture)
    {
        List<LaidOutFragmentRow> rows = [];
        double y = 0;
        foreach (var h in headers)
        {
            var height = layout.RowHeights[h];
            rows.Add(new LaidOutFragmentRow { SourceRow = h, IsHeader = true, Y = y, Height = height });
            y += height;
        }

        foreach (var b in bodyRows)
        {
            var height = layout.RowHeights[b];
            rows.Add(new LaidOutFragmentRow { SourceRow = b, IsHeader = false, Y = y, Height = height });
            y += height;
        }

        return new LaidOutTableSlice
        {
            Id = capture.Node(),
            Number = number,
            Rows = [.. rows],
            HeaderRowCount = headers.Count,
            ContainsRepeatedHeaders = number > 1 && headers.Count > 0,
            Height = y,
        };
    }
}
