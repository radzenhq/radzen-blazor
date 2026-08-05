using System.Collections.Generic;
using System;
using Radzen.Documents.Fonts;
using Radzen.Documents.LaidOut;
using Radzen.Documents.Core;

namespace Radzen.Documents.Layout;


internal static class TableLayout
{
    private sealed class MeasuredCell
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
        LoweringResult resolution,
        LayoutCaptureContext capture,
        double additionalLeftIndent = 0)
    {
        var placement = resolution.TablePlacement(table);
        var columnWidths = ResolveColumnWidths(table, placement.ColumnCount, availableWidth);
        var columnX = Prefix(columnWidths);

        var nRows = table.Rows.Count;
        var placed = new List<MeasuredCell>(placement.Cells.Count);

        foreach (var placedCell in placement.Cells)
        {
            var cell = placedCell.Cell;
            double cellWidth = 0;
            for (var column = placedCell.Column; column < placedCell.Column + placedCell.ColumnSpan; column++)
            {
                cellWidth += columnWidths[column];
            }

            var contentWidth = cellWidth - cell.EffectivePaddingLeft.Point - cell.EffectivePaddingRight.Point;
            var align = cell.Alignment
                ?? resolution.CellAlignment(cell)
                ?? ColumnAlignment(table, placedCell.Column)
                ?? table.Rows[placedCell.Row].Alignment;
            var content = BoxContentLayout.Measure(
                cell.Blocks,
                contentWidth,
                align,
                fonts,
                resolution,
                capture);

            placed.Add(new MeasuredCell
            {
                Cell = cell,
                Row = placedCell.Row,
                Column = placedCell.Column,
                ColumnSpan = placedCell.ColumnSpan,
                RowSpan = placedCell.RowSpan,
                ContentWidth = contentWidth,
                Align = align,
                Content = content,
            });
        }

        var rowHeights = new double[nRows];
        foreach (var p in placed)
        {
            if (p.RowSpan != 1)
            {
                continue;
            }

            var h = p.ContentHeight + p.Cell.EffectivePaddingTop.Point + p.Cell.EffectivePaddingBottom.Point;
            if (h > rowHeights[p.Row])
            {
                rowHeights[p.Row] = h;
            }
        }

        foreach (var p in placed)
        {
            if (p.RowSpan <= 1)
            {
                continue;
            }

            var needed = p.ContentHeight + p.Cell.EffectivePaddingTop.Point + p.Cell.EffectivePaddingBottom.Point;
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
            var padLeft = p.Cell.EffectivePaddingLeft.Point;
            var padTop = p.Cell.EffectivePaddingTop.Point;
            var bounds = new Rect(x, y, width, height);
            var contentBox = new Rect(
                x + padLeft,
                y + padTop,
                width - padLeft - p.Cell.EffectivePaddingRight.Point,
                height - padTop - p.Cell.EffectivePaddingBottom.Point);

            var cellAlignment = p.Align ?? HorizontalAlignment.Left;
            var content = BoxContentLayout.Position(p.Content, contentBox, cellAlignment, p.Cell.VerticalAlignment);

            cells.Add(new LaidOutCell
            {
                Source = capture.Source(p.Cell),
                Decoration = CellDecoration(table, p.Cell, p.Row),
                Opacity = resolution.Opacities.CellOpacity(p.Cell),
                Row = p.Row,
                Column = p.Column,
                ColumnSpan = p.ColumnSpan,
                RowSpan = p.RowSpan,
                Bounds = bounds,
                ContentBox = contentBox,
                Lines = content.Lines,
                Images = content.Images,
                CodeSymbols = content.CodeSymbols,
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
            Source = capture.Source(table),
            Decoration = GeometryCapture.Table(table, additionalLeftIndent),
            ColumnWidths = [.. columnWidths],
            RowHeights = [.. rowHeights],
            Width = totalWidth,
            Height = totalHeight,
            Cells = [.. cells],
        };
    }

    private static BoxStyle CellDecoration(Table table, Cell cell, int row)
    {
        var cellBorders = cell.Borders;
        var sourceRow = row < table.Rows.Count ? table.Rows[row] : null;
        var rowBorders = sourceRow?.Borders;
        var tableBorders = table.Borders;

        return new BoxStyle
        {
            Background = cell.Background ?? sourceRow?.Background,
            Top = GeometryCapture.Edge(CascadeEdge(cellBorders.Top, rowBorders?.Top, tableBorders.Top)),
            Right = GeometryCapture.Edge(CascadeEdge(cellBorders.Right, rowBorders?.Right, tableBorders.Right)),
            Bottom = GeometryCapture.Edge(CascadeEdge(cellBorders.Bottom, rowBorders?.Bottom, tableBorders.Bottom)),
            Left = GeometryCapture.Edge(CascadeEdge(cellBorders.Left, rowBorders?.Left, tableBorders.Left)),
        };
    }

    private static Border CascadeEdge(Border cellEdge, Border? rowEdge, Border? tableEdge)
    {
        if (!cellEdge.IsSet)
        {
            if (rowEdge?.IsSet == true)
            {
                return rowEdge;
            }

            if (tableEdge is not null)
            {
                return tableEdge;
            }
        }

        return cellEdge;
    }

    private static HorizontalAlignment? ColumnAlignment(Table table, int column)
        => column < table.Columns.Count ? table.Columns[column].Alignment : null;

    private static double[] ResolveColumnWidths(Table table, int count, double availableWidth)
    {
        if (table.Columns.Count == 0)
        {
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
