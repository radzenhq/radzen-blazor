using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Radzen.Documents.Geometry;

namespace Radzen.Documents.Layout;

internal static class TableFragmentJoin
{
    public static LaidOutTableFragment Join(
        LaidOutNodeId id,
        SourceId source,
        LaidOutTable layout,
        LaidOutTableSlice fragment,
        double y,
        int order)
    {
        var index = RowIndex(layout);
        var rows = ImmutableArray.CreateBuilder<LaidOutRow>(fragment.Rows.Length);
        var top = double.MaxValue;
        var bottom = double.MinValue;

        foreach (var row in fragment.Rows)
        {
            top = Math.Min(top, row.Y);
            bottom = Math.Max(bottom, row.Y + row.Height);

            var cells = index.Length > row.SourceRow ? index[row.SourceRow] : null;
            var placed = ImmutableArray.CreateBuilder<LaidOutCellPlacement>(cells?.Count ?? 0);
            if (cells is not null)
            {
                foreach (var cell in cells)
                {
                    placed.Add(new LaidOutCellPlacement { Cell = cell, Delta = y + row.Y - cell.Bounds.Y });
                }
            }

            rows.Add(new LaidOutRow
            {
                SourceRow = row.SourceRow,
                IsHeader = row.IsHeader,
                Y = y + row.Y,
                Height = row.Height,
                Background = layout.Decoration.RowBackground(row.SourceRow),
                Cells = placed.MoveToImmutable(),
            });
        }

        return new LaidOutTableFragment
        {
            Id = id,
            Source = source,
            Layout = layout,
            Fragment = fragment,
            Rows = rows.MoveToImmutable(),
            Bounds = bottom > top
                ? new Rect(layout.Decoration.LeftIndent, y + top, layout.Width, bottom - top)
                : new Rect(layout.Decoration.LeftIndent, y, 0, 0),
            Order = order,
        };
    }

    public static LaidOutTableFragment Rejoin(in LaidOutTableFragment positioned, LaidOutTable layout)
        => Join(
            positioned.Id,
            positioned.Source,
            layout,
            positioned.Fragment,
            positioned.Bounds.Y,
            positioned.Order);

    private static List<LaidOutCell>[] RowIndex(LaidOutTable layout)
    {
        var rows = new List<LaidOutCell>[layout.RowHeights.Length];
        foreach (var cell in layout.Cells)
        {
            if (cell.Row < rows.Length)
            {
                (rows[cell.Row] ??= []).Add(cell);
            }
        }

        return rows;
    }
}
