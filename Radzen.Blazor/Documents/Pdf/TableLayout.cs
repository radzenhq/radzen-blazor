using System.Collections.Generic;

namespace Radzen.Documents.Pdf;

#nullable enable

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
    private readonly struct CellItem
    {
        public LineBox? Line { get; init; }
        public Block? Source { get; init; }
        public Image? Image { get; init; }
        public Block? Code { get; init; }
        public LaidOutTable? Table { get; init; }
        public double Width { get; init; }
        public required double Height { get; init; }
    }

    private sealed class Placed
    {
        public required Cell Cell { get; init; }
        public required int Row { get; init; }
        public required int Column { get; init; }
        public required int ColumnSpan { get; init; }
        public required int RowSpan { get; init; }
        public required double ContentWidth { get; init; }
        public required double ContentHeight { get; init; }
        public required List<CellItem> Items { get; init; }
    }

    public static LaidOutTable Layout(
        Table table,
        double availableWidth,
        FontCollection fonts,
        System.Func<Image, double, (double Width, double Height)>? measureImage = null)
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

                var span = System.Math.Min(cell.ColumnSpan, nCols - c);
                var rowSpan = System.Math.Min(cell.RowSpan, nRows - r);
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
                var (items, contentHeight) = LayoutContent(cell, contentWidth, align, fonts, measureImage);

                placed.Add(new Placed
                {
                    Cell = cell,
                    Row = r,
                    Column = c,
                    ColumnSpan = span,
                    RowSpan = rowSpan,
                    ContentWidth = contentWidth,
                    ContentHeight = contentHeight,
                    Items = items,
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
            var lastRow = System.Math.Min(nRows, p.Row + p.RowSpan);
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

            var factor = p.Cell.VerticalAlignment switch
            {
                VerticalAlignment.Middle => 0.5,
                VerticalAlignment.Bottom => 1.0,
                _ => 0.0,
            };
            var offset = (contentBox.Height - p.ContentHeight) * factor;

            var cellAlignment = p.Cell.AlignmentValue
                ?? ColumnAlignment(table, p.Column)
                ?? table.Rows[p.Row].AlignmentValue
                ?? HorizontalAlignment.Left;

            var lines = new List<LaidOutLine>();
            var laidImages = new List<LaidOutImage>();
            var laidCodes = new List<LaidOutCode>();
            var nestedTables = new List<LaidOutNestedTable>();
            var cursorY = contentBox.Top + offset;
            foreach (var item in p.Items)
            {
                if (item.Line is { } line && item.Source is { } source)
                {
                    lines.Add(new LaidOutLine
                    {
                        Line = line,
                        Source = source,
                        X = contentBox.Left,
                        Y = cursorY,
                    });
                }
                else if (item.Image is { } image)
                {
                    laidImages.Add(new LaidOutImage
                    {
                        Source = image,
                        X = contentBox.Left + ((contentBox.Width - item.Width) * AlignFactor(image.Alignment, cellAlignment)),
                        Y = cursorY,
                        Width = item.Width,
                        Height = item.Height,
                    });
                }
                else if (item.Code is { } code)
                {
                    laidCodes.Add(new LaidOutCode
                    {
                        Source = code,
                        X = contentBox.Left + ((contentBox.Width - item.Width) * AlignFactor(BlockAlignment(code), cellAlignment)),
                        Y = cursorY,
                    });
                }
                else if (item.Table is { } nested)
                {
                    nestedTables.Add(new LaidOutNestedTable
                    {
                        Layout = nested,
                        X = contentBox.Left,
                        Y = cursorY,
                    });
                }

                cursorY += item.Height;
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
                Images = laidImages,
                Codes = laidCodes,
                Tables = nestedTables,
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

    private static HorizontalAlignment BlockAlignment(Block code) => code switch
    {
        QrCode qr => qr.Alignment,
        Barcode barcode => barcode.Alignment,
        _ => HorizontalAlignment.Left,
    };

    // Non-text content honors its OWN alignment, falling back to the cell's only when the
    // block leaves it at the default Left. The factor matches how text resolves alignment
    // (Right/End flush right, Center centered, Left/Start/Justify flush left).
    private static double AlignFactor(HorizontalAlignment blockAlignment, HorizontalAlignment cellAlignment)
        => (blockAlignment == HorizontalAlignment.Left ? cellAlignment : blockAlignment) switch
        {
            HorizontalAlignment.Right or HorizontalAlignment.End => 1.0,
            HorizontalAlignment.Center => 0.5,
            _ => 0.0,
        };

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
                    cells += System.Math.Max(1, cell.ColumnSpan);
                }

                count = System.Math.Max(count, cells);
            }

            if (count == 0)
            {
                return [];
            }

            var total = table.Width?.Point ?? availableWidth;
            var derived = new double[count];
            var share = System.Math.Max(0, total / count);
            for (var i = 0; i < count; i++)
            {
                derived[i] = share;
            }

            return derived;
        }

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

        var remaining = table.Width?.Point ?? availableWidth;
        var each = System.Math.Max(0, (remaining - fixedSum) / autoCount);
        for (var i = 0; i < count; i++)
        {
            if (table.Columns[i].Width is null)
            {
                widths[i] = each;
            }
        }

        return widths;
    }

    private static (List<CellItem> Items, double Height) LayoutContent(
        Cell cell,
        double contentWidth,
        HorizontalAlignment? align,
        FontCollection fonts,
        System.Func<Image, double, (double Width, double Height)>? measureImage)
    {
        var items = new List<CellItem>();
        double height = 0;
        // Lists expand to marker paragraphs exactly as in section content.
        foreach (var block in Paginator.ExpandBlocks(cell.Blocks))
        {
            if (block is Paragraph paragraph)
            {
                var spacingBefore = paragraph.SpacingBefore.Point;
                if (spacingBefore > 0)
                {
                    items.Add(new CellItem { Height = spacingBefore });
                    height += spacingBefore;
                }

                foreach (var line in LineBreaker.Break(paragraph, contentWidth, fonts, align))
                {
                    items.Add(new CellItem { Line = line, Source = block, Height = line.Height });
                    height += line.Height;
                }

                var spacingAfter = paragraph.SpacingAfter.Point;
                if (spacingAfter > 0)
                {
                    items.Add(new CellItem { Height = spacingAfter });
                    height += spacingAfter;
                }
            }
            else if (block is Image image)
            {
                var (imageWidth, imageHeight) = measureImage is null
                    ? ImageDecoder.Measure(image, ImageDecoder.Decode(image.Data), contentWidth)
                    : measureImage(image, contentWidth);
                items.Add(new CellItem { Image = image, Width = imageWidth, Height = imageHeight });
                height += imageHeight;
            }
            else if (block is QrCode or Barcode)
            {
                var (codeWidth, codeHeight) = Paginator.MeasureCode(block);
                items.Add(new CellItem { Code = block, Width = codeWidth, Height = codeHeight });
                height += codeHeight;
            }
            else if (block is Table nested)
            {
                var layout = Layout(nested, System.Math.Max(0, contentWidth - nested.LeftIndent.Point), fonts, measureImage);
                items.Add(new CellItem { Table = layout, Height = layout.Height });
                height += layout.Height;
            }
            else if (block is PageBreak)
            {
                // A cell cannot break across pages by itself, so a page break inside one is a no-op.
            }
            else
            {
                throw new System.NotSupportedException($"Block type '{block.GetType().Name}' is not supported inside a table cell.");
            }
        }

        return (items, height);
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
