using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Radzen.Documents.Geometry;

namespace Radzen.Documents.Layout;

internal static class TableFragmentJoin
{
    public static PositionedTableFragment Join(
        SourceId source, LaidOutTable layout, TableFragment fragment, double y, int order)
    {
        var index = RowIndex(layout);
        var rows = ImmutableArray.CreateBuilder<PlacedRow>(fragment.Rows.Length);
        var top = double.MaxValue;
        var bottom = double.MinValue;

        foreach (var row in fragment.Rows)
        {
            top = Math.Min(top, row.Y);
            bottom = Math.Max(bottom, row.Y + row.Height);

            var cells = index.Length > row.SourceRow ? index[row.SourceRow] : null;
            var placed = ImmutableArray.CreateBuilder<PlacedCell>(cells?.Count ?? 0);
            if (cells is not null)
            {
                foreach (var cell in cells)
                {
                    placed.Add(new PlacedCell { Cell = cell, Delta = y + row.Y - cell.Bounds.Y });
                }
            }

            rows.Add(new PlacedRow
            {
                SourceRow = row.SourceRow,
                IsHeader = row.IsHeader,
                Y = y + row.Y,
                Height = row.Height,
                Background = layout.Decoration.RowBackground(row.SourceRow),
                Cells = placed.MoveToImmutable(),
            });
        }

        return new PositionedTableFragment
        {
            Source = source,
            Layout = layout,
            Fragment = fragment,
            Rows = rows.MoveToImmutable(),
            Bounds = bottom > top
                ? new Rect(layout.Decoration.LeftIndent, y + top, layout.Width, bottom - top)
                : default,
            Y = y,
            Order = order,
        };
    }

    public static PositionedTableFragment Rejoin(in PositionedTableFragment positioned, LaidOutTable layout)
        => Join(positioned.Source, layout, positioned.Fragment, positioned.Y, positioned.Order);

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
