using System.Collections.Generic;

namespace Radzen.Documents.Pdf;

#nullable enable

internal sealed class FragmentRow
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

        List<TableFragment> fragments = [];
        var body = 0;
        while (true)
        {
            var startedNew = fragments.Count == 0 && body == 0;
            var available = fragments.Count == 0 ? firstAvailable : subsequentAvailable;
            var running = headerHeight;
            List<int> placed = [];
            while (body < bodies.Count)
            {
                var rowHeight = layout.RowHeights[bodies[body]];
                if (placed.Count == 0 || running + rowHeight <= available + 1e-6)
                {
                    placed.Add(bodies[body]);
                    running += rowHeight;
                    body++;
                }
                else
                {
                    break;
                }
            }

            if (placed.Count == 0 && !startedNew)
            {
                break;
            }

            fragments.Add(BuildFragment(fragments.Count + 1, layout, headers, placed));

            if (body >= bodies.Count)
            {
                break;
            }
        }

        return fragments;
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
