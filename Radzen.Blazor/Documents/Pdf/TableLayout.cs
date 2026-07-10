using System.Collections.Generic;

namespace Radzen.Documents.Pdf;

#nullable enable

internal sealed class LaidOutLine
{
    public required LineBox Line { get; init; }

    public required Block Source { get; init; }

    public required double X { get; init; }

    public required double Y { get; init; }
}

internal sealed class LaidOutCell
{
    public required Cell Cell { get; init; }

    public required int Row { get; init; }

    public required int Column { get; init; }

    public required int ColumnSpan { get; init; }

    public required int RowSpan { get; init; }

    public required Rect Bounds { get; init; }

    public required Rect ContentBox { get; init; }

    public required IReadOnlyList<LaidOutLine> Lines { get; init; }
}

internal sealed class LaidOutTable
{
    public required IReadOnlyList<double> ColumnWidths { get; init; }

    public required IReadOnlyList<double> RowHeights { get; init; }

    public required double Width { get; init; }

    public required double Height { get; init; }

    public required IReadOnlyList<LaidOutCell> Cells { get; init; }
}

internal static class TableLayout
{
    private sealed class Placed
    {
        public required Cell Cell { get; init; }
        public required int Row { get; init; }
        public required int Column { get; init; }
        public required int ColumnSpan { get; init; }
        public required int RowSpan { get; init; }
        public required double ContentWidth { get; init; }
        public required double ContentHeight { get; init; }
        public required List<(LineBox Line, Block Source)> Lines { get; init; }
    }

    public static LaidOutTable Layout(Table table, double availableWidth, FontCollection fonts)
    {
        var columnWidths = ResolveColumnWidths(table, availableWidth);
        var columnX = Prefix(columnWidths);

        var nRows = table.Rows.Count;
        var nCols = columnWidths.Length;
        var occupied = new bool[nRows, nCols];
        var placed = new List<Placed>();

        for (var r = 0; r < nRows; r++)
        {
            var c = 0;
            foreach (var cell in table.Rows[r].Cells)
            {
                while (c < nCols && occupied[r, c])
                {
                    c++;
                }

                if (c >= nCols)
                {
                    break;
                }

                var span = cell.ColumnSpan;
                if (c + span > nCols)
                {
                    continue;
                }

                var rowSpan = cell.RowSpan;
                var lastRow = System.Math.Min(nRows, r + rowSpan);
                for (var rr = r; rr < lastRow; rr++)
                {
                    for (var cc = c; cc < c + span; cc++)
                    {
                        occupied[rr, cc] = true;
                    }
                }

                double cellWidth = 0;
                for (var cc = c; cc < c + span; cc++)
                {
                    cellWidth += columnWidths[cc];
                }

                var padding = cell.Padding.Point;
                var contentWidth = cellWidth - (2 * padding);
                var align = table.Columns[c].Alignment ?? cell.Alignment;
                var (lines, contentHeight) = LayoutContent(cell, contentWidth, align, fonts);

                placed.Add(new Placed
                {
                    Cell = cell,
                    Row = r,
                    Column = c,
                    ColumnSpan = span,
                    RowSpan = rowSpan,
                    ContentWidth = contentWidth,
                    ContentHeight = contentHeight,
                    Lines = lines,
                });

                c += span;
            }
        }

        var rowHeights = new double[nRows];
        foreach (var p in placed)
        {
            if (p.RowSpan != 1)
            {
                continue;
            }

            var h = p.ContentHeight + (2 * p.Cell.Padding.Point);
            if (h > rowHeights[p.Row])
            {
                rowHeights[p.Row] = h;
            }
        }

        var rowY = Prefix(rowHeights);

        var cells = new List<LaidOutCell>(placed.Count);
        foreach (var p in placed)
        {
            double width = 0;
            for (var cc = p.Column; cc < p.Column + p.ColumnSpan; cc++)
            {
                width += columnWidths[cc];
            }

            double height = 0;
            var lastRow = System.Math.Min(nRows, p.Row + p.RowSpan);
            for (var rr = p.Row; rr < lastRow; rr++)
            {
                height += rowHeights[rr];
            }

            var x = columnX[p.Column];
            var y = rowY[p.Row];
            var padding = p.Cell.Padding.Point;
            var bounds = new Rect(x, y, width, height);
            var contentBox = new Rect(
                x + padding,
                y + padding,
                width - (2 * padding),
                height - (2 * padding));

            var factor = p.Cell.VerticalAlignment switch
            {
                VerticalAlignment.Middle => 0.5,
                VerticalAlignment.Bottom => 1.0,
                _ => 0.0,
            };
            var offset = (contentBox.Height - p.ContentHeight) * factor;

            var lines = new List<LaidOutLine>(p.Lines.Count);
            var cursorY = contentBox.Top + offset;
            foreach (var (line, source) in p.Lines)
            {
                lines.Add(new LaidOutLine
                {
                    Line = line,
                    Source = source,
                    X = contentBox.Left,
                    Y = cursorY,
                });
                cursorY += line.Height;
            }

            cells.Add(new LaidOutCell
            {
                Cell = p.Cell,
                Row = p.Row,
                Column = p.Column,
                ColumnSpan = p.ColumnSpan,
                RowSpan = p.RowSpan,
                Bounds = bounds,
                ContentBox = contentBox,
                Lines = lines,
            });
        }

        double totalWidth = 0;
        foreach (var w in columnWidths)
        {
            totalWidth += w;
        }

        double totalHeight = 0;
        foreach (var h in rowHeights)
        {
            totalHeight += h;
        }

        return new LaidOutTable
        {
            ColumnWidths = columnWidths,
            RowHeights = rowHeights,
            Width = totalWidth,
            Height = totalHeight,
            Cells = cells,
        };
    }

    private static double[] ResolveColumnWidths(Table table, double availableWidth)
    {
        var count = table.Columns.Count;
        var widths = new double[count];
        double fixedSum = 0;
        var autoCount = 0;
        for (var i = 0; i < count; i++)
        {
            if (table.Columns[i].Width is { } w)
            {
                widths[i] = w.Point;
                fixedSum += w.Point;
            }
            else
            {
                autoCount++;
            }
        }

        if (autoCount == 0)
        {
            return widths;
        }

        var total = table.Width?.Point ?? availableWidth;
        var each = (total - fixedSum) / autoCount;
        for (var i = 0; i < count; i++)
        {
            if (table.Columns[i].Width is null)
            {
                widths[i] = each;
            }
        }

        return widths;
    }

    private static (List<(LineBox, Block)> Lines, double Height) LayoutContent(
        Cell cell, double contentWidth, HorizontalAlignment align, FontCollection fonts)
    {
        var lines = new List<(LineBox, Block)>();
        double height = 0;
        foreach (var block in cell.Blocks)
        {
            if (block is not Paragraph paragraph)
            {
                continue;
            }

            var original = paragraph.Alignment;
            paragraph.Alignment = align;
            try
            {
                foreach (var line in LineBreaker.Break(paragraph, contentWidth, fonts))
                {
                    lines.Add((line, block));
                    height += line.Height;
                }
            }
            finally
            {
                paragraph.Alignment = original;
            }
        }

        return (lines, height);
    }

    private static double[] Prefix(double[] values)
    {
        var result = new double[values.Length];
        double sum = 0;
        for (var i = 0; i < values.Length; i++)
        {
            result[i] = sum;
            sum += values[i];
        }

        return result;
    }
}
