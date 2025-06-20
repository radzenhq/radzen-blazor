using System;
using System.Collections.Generic;

namespace Radzen.Blazor.Spreadsheet;
#nullable enable

public partial class Sheet
{
    /// <summary>
    /// Sorts the specified range of cells in the sheet based on the specified order and key index.
    /// </summary>
    /// <param name="range"></param>
    /// <param name="order"></param>
    /// <param name="keyIndex"></param>
    public void Sort(RangeRef range, SortOrder order, int keyIndex = 0)
    {
        if (range != RangeRef.Invalid)
        {
            var rows = new List<(object? key, List<Cell> cells)>();

            for (var row = range.Start.Row; row <= range.End.Row; row++)
            {
                var cells = Cells.GetRow(row, range.Start.Column, range.End.Column);

                var key = cells[keyIndex - range.Start.Column]?.Value;

                rows.Add((key, cells));
            }

            if (order == SortOrder.Ascending)
            {
                rows.Sort(Compare);
            }
            else
            {
                rows.Sort(CompareDescending);
            }

            for (var row = 0; row < rows.Count; row++)
            {
                var (key, cells) = rows[row];
                var targetRow = range.Start.Row + row;

                for (var column = 0; column < cells.Count; column++)
                {
                    var targetColumn = range.Start.Column + column;
                    var targetCell = Cells[targetRow, targetColumn];
                    var sourceCell = cells[column];

                    targetCell.CopyFrom(sourceCell);
                }
            }
        }
    }

    private static int Compare((object? key, List<Cell> cells) x, (object? key, List<Cell> cells) y) => Compare(x.key, y.key);

    private static int CompareDescending((object? key, List<Cell> cells) x, (object? key, List<Cell> cells) y)
    {
        if (x.key == null && y.key == null) return 0;
        if (x.key == null) return 1;
        if (y.key == null) return -1;
        return Compare(y.key, x.key);
    }

    private static int Compare(object? x, object? y)
    {
        if (x == null && y == null) return 0;
        if (x == null) return 1;
        if (y == null) return -1;
        if (x is double dx && y is double dy) return dx.CompareTo(dy);
        if (x is string sx && y is string sy) return string.Compare(sx, sy, StringComparison.OrdinalIgnoreCase);
        return string.Compare(x.ToString(), y.ToString(), StringComparison.OrdinalIgnoreCase);
    }
}