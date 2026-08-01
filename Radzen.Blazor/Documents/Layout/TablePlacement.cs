using System;
using System.Collections.Generic;

namespace Radzen.Documents.Layout;

internal readonly record struct PlacedTableCell(
    Cell Cell,
    int Row,
    int Column,
    int RowSpan,
    int ColumnSpan);

internal sealed class TablePlacement
{
    private TablePlacement(int columnCount, IReadOnlyList<PlacedTableCell> cells)
    {
        ColumnCount = columnCount;
        Cells = cells;
    }

    public int ColumnCount { get; }

    public IReadOnlyList<PlacedTableCell> Cells { get; }

    public static TablePlacement Create(Table table)
    {
        var rowCount = table.Rows.Count;
        var columnCount = ResolveColumnCount(table);
        var occupied = new bool[rowCount, columnCount];
        var placed = new List<PlacedTableCell>();

        for (var row = 0; row < rowCount; row++)
        {
            var column = 0;
            foreach (var cell in table.Rows[row].Cells)
            {
                while (column < columnCount && occupied[row, column])
                {
                    column++;
                }

                if (column >= columnCount)
                {
                    if (IsStructuralPlaceholder(cell))
                    {
                        continue;
                    }

                    throw new InvalidOperationException(
                        $"Table row {row} has more cells than the {columnCount}-column grid can place.");
                }

                var columnSpan = Math.Min(cell.ColumnSpan, columnCount - column);
                var rowSpan = Math.Min(cell.RowSpan, rowCount - row);
                for (var occupiedRow = row; occupiedRow < row + rowSpan; occupiedRow++)
                {
                    for (var occupiedColumn = column; occupiedColumn < column + columnSpan; occupiedColumn++)
                    {
                        occupied[occupiedRow, occupiedColumn] = true;
                    }
                }

                placed.Add(new PlacedTableCell(cell, row, column, rowSpan, columnSpan));
                column += columnSpan;
            }
        }

        return new TablePlacement(columnCount, placed);
    }

    private static int ResolveColumnCount(Table table)
    {
        if (table.Columns.Count > 0)
        {
            return table.Columns.Count;
        }

        var count = 0;
        foreach (var row in table.Rows)
        {
            var cells = 0;
            foreach (var cell in row.Cells)
            {
                cells += Math.Max(1, cell.ColumnSpan);
            }

            count = Math.Max(count, cells);
        }

        return count;
    }

    private static bool IsStructuralPlaceholder(Cell cell)
        => cell.Blocks.Count == 0
            && cell.ColumnSpan == 1
            && cell.RowSpan == 1
            && cell.Font.HasNoDeclaredValues
            && cell.Alignment is null
            && cell.VerticalAlignment == VerticalAlignment.Top
            && cell.Padding == default
            && cell.PaddingLeft is null
            && cell.PaddingRight is null
            && cell.PaddingTop is null
            && cell.PaddingBottom is null
            && cell.StyleName is null
            && cell.Background is null
            && !cell.Borders.Top.IsSet
            && !cell.Borders.Right.IsSet
            && !cell.Borders.Bottom.IsSet
            && !cell.Borders.Left.IsSet;
}
