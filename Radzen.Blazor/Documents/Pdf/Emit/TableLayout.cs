using System;
using System.Collections.Generic;

namespace Radzen.Documents.Pdf.Emit;


internal readonly struct LaidOutLine
{
    public required LineBox Line { get; init; }

    public required Block Source { get; init; }

    public required double X { get; init; }

    public required double Y { get; init; }
}

internal readonly struct LaidOutImage
{
    public required Image Source { get; init; }

    public required double X { get; init; }

    public required double Y { get; init; }

    public required double Width { get; init; }

    public required double Height { get; init; }
}

internal readonly struct LaidOutCode
{
    public required Block Source { get; init; }

    public required double X { get; init; }

    public required double Y { get; init; }
}

internal readonly struct LaidOutNestedTable
{
    public required LaidOutTable Layout { get; init; }

    public required double X { get; init; }

    public required double Y { get; init; }

    /// <summary>
    /// Placement sequence within the parent box content, shared with
    /// <see cref="LaidOutNestedBox.Order"/> so emission interleaves nested tables and
    /// nested boxes in document order.
    /// </summary>
    public int Order { get; init; }
}

// Bounds is in the parent's content space (same space as LaidOutNestedTable.X/Y), Radius is
// already clamped to the box, and Style carries no ExtGState - opacity resolves per page at
// emit time.
internal readonly struct LaidOutNestedBox
{
    public required Container Source { get; init; }

    public required LaidOutBoxContent Content { get; init; }

    public required Rect Bounds { get; init; }

    public required BoxStyle Style { get; init; }

    public required double Radius { get; init; }

    public required double Opacity { get; init; }

    public int Order { get; init; }
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

    public IReadOnlyList<LaidOutImage> Images { get; init; } = [];

    public IReadOnlyList<LaidOutCode> Codes { get; init; } = [];

    public IReadOnlyList<LaidOutNestedTable> Tables { get; init; } = [];

    public IReadOnlyList<LaidOutNestedBox> Boxes { get; init; } = [];
}

internal sealed class LaidOutTable
{
    public required IReadOnlyList<double> ColumnWidths { get; init; }

    public required IReadOnlyList<double> RowHeights { get; init; }

    public required double Width { get; init; }

    public required double Height { get; init; }

    public required IReadOnlyList<LaidOutCell> Cells { get; init; }

    public Table? Source { get; init; }
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
        public required HorizontalAlignment? Align { get; init; }
        public required BoxContentLayout.Measured Content { get; init; }
        public double ContentHeight => Content.Height;
    }

    public static LaidOutTable Layout(
        Table table,
        double availableWidth,
        FontCollection fonts,
        Func<Image, double, (double Width, double Height)>? measureImage = null,
        StyleResolution? resolution = null)
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

                var span = Math.Min(cell.ColumnSpan, nCols - c);
                var rowSpan = Math.Min(cell.RowSpan, nRows - r);
                var lastRow = r + rowSpan;
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

                var contentWidth = cellWidth - cell.PaddingLeft.Point - cell.PaddingRight.Point;
                var align = cell.AlignmentValue ?? ColumnAlignment(table, c) ?? table.Rows[r].AlignmentValue;
                var content = BoxContentLayout.Measure(cell.Blocks, contentWidth, align, fonts, measureImage, resolution);

                placed.Add(new Placed
                {
                    Cell = cell,
                    Row = r,
                    Column = c,
                    ColumnSpan = span,
                    RowSpan = rowSpan,
                    ContentWidth = contentWidth,
                    Align = align,
                    Content = content,
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

            var h = p.ContentHeight + p.Cell.PaddingTop.Point + p.Cell.PaddingBottom.Point;
            if (h > rowHeights[p.Row])
            {
                rowHeights[p.Row] = h;
            }
        }

        // Rows covered by a spanning cell grow (last row absorbs the deficit) so the
        // spanned content always fits within the combined row heights.
        foreach (var p in placed)
        {
            if (p.RowSpan <= 1)
            {
                continue;
            }

            var needed = p.ContentHeight + p.Cell.PaddingTop.Point + p.Cell.PaddingBottom.Point;
            double covered = 0;
            var end = p.Row + p.RowSpan - 1;
            for (var rr = p.Row; rr <= end; rr++)
            {
                covered += rowHeights[rr];
            }

            if (needed > covered)
            {
                rowHeights[end] += needed - covered;
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
            var lastRow = Math.Min(nRows, p.Row + p.RowSpan);
            for (var rr = p.Row; rr < lastRow; rr++)
            {
                height += rowHeights[rr];
            }

            var x = columnX[p.Column];
            var y = rowY[p.Row];
            var padLeft = p.Cell.PaddingLeft.Point;
            var padTop = p.Cell.PaddingTop.Point;
            var bounds = new Rect(x, y, width, height);
            var contentBox = new Rect(
                x + padLeft,
                y + padTop,
                width - padLeft - p.Cell.PaddingRight.Point,
                height - padTop - p.Cell.PaddingBottom.Point);

            var cellAlignment = p.Align ?? HorizontalAlignment.Left;
            var content = BoxContentLayout.Position(p.Content, contentBox, cellAlignment, p.Cell.VerticalAlignment);

            cells.Add(new LaidOutCell
            {
                Cell = p.Cell,
                Row = p.Row,
                Column = p.Column,
                ColumnSpan = p.ColumnSpan,
                RowSpan = p.RowSpan,
                Bounds = bounds,
                ContentBox = contentBox,
                Lines = content.Lines,
                Images = content.Images,
                Codes = content.Codes,
                Tables = content.Tables,
                Boxes = content.Boxes,
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
            Source = table,
            ColumnWidths = columnWidths,
            RowHeights = rowHeights,
            Width = totalWidth,
            Height = totalHeight,
            Cells = cells,
        };
    }

    private static HorizontalAlignment? ColumnAlignment(Table table, int column)
        => column < table.Columns.Count ? table.Columns[column].Alignment : null;

    private static double[] ResolveColumnWidths(Table table, double availableWidth)
    {
        var count = table.Columns.Count;
        if (count == 0)
        {
            // No declared columns: derive them from the widest row so content is not
            // silently dropped.
            foreach (var row in table.Rows)
            {
                var cells = 0;
                foreach (var cell in row.Cells)
                {
                    cells += Math.Max(1, cell.ColumnSpan);
                }

                count = Math.Max(count, cells);
            }

            if (count == 0)
            {
                return [];
            }

            var total = table.Width?.Point ?? availableWidth;
            var derived = new double[count];
            var share = Math.Max(0, total / count);
            for (var i = 0; i < count; i++)
            {
                derived[i] = share;
            }

            return derived;
        }

        var widths = new double[count];
        double fixedSum = 0;
        double weightSum = 0;
        for (var i = 0; i < count; i++)
        {
            if (table.Columns[i].Width is { } w)
            {
                widths[i] = w.Point;
                fixedSum += w.Point;
            }
            else
            {
                weightSum += table.Columns[i].RelativeWidth ?? 1.0;
            }
        }

        if (weightSum == 0)
        {
            return widths;
        }

        var remaining = Math.Max(0, (table.Width?.Point ?? availableWidth) - fixedSum);
        for (var i = 0; i < count; i++)
        {
            if (table.Columns[i].Width is null)
            {
                widths[i] = remaining * (table.Columns[i].RelativeWidth ?? 1.0) / weightSum;
            }
        }

        return widths;
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
