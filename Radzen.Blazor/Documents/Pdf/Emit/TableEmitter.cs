using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Radzen.Documents.Pdf.Emit;

// Emits a positioned table fragment, clipping content that overflows the cell box.
internal sealed class TableEmitter(ImageStore imageStore, StructureTreeBuilder structureTree, StyleResolution resolution, OpacityResolver opacities)
{
    private readonly Dictionary<LaidOutTable, List<LaidOutCell>[]> tableRows = [];

    public void EmitFragment(EmitContext context, PositionedTableFragment positioned, double left, double contentTop)
    {
        var plan = context.Plan;
        var layout = positioned.Layout;
        var x = left + (layout.Source?.LeftIndent.Point ?? 0);
        var tableRadius = 0.0;
        var tableBounds = default(PdfRect);
        var tableMark = default(PlanMarks);
        if (layout.Source is { } table && table.CornerRadius.Point > 0)
        {
            tableBounds = FragmentBounds(positioned, x, contentTop);
            tableRadius = BoxRenderer.ClampRadius(table.CornerRadius.Point, tableBounds.Width, tableBounds.Height);
            tableMark = plan.Mark();
        }

        var rowIndex = RowIndex(layout);
        foreach (var row in positioned.Fragment.Rows)
        {
            if (layout.Source is { } source && row.SourceRow < source.Rows.Count)
            {
                PaintRowBackground(plan, source.Rows[row.SourceRow], x, contentTop - (positioned.Y + row.Y + row.Height), layout.Width, row.Height);
            }

            var rowCells = row.SourceRow < rowIndex.Length ? rowIndex[row.SourceRow] : null;
            if (rowCells is null)
            {
                continue;
            }

            foreach (var cell in rowCells)
            {
                var delta = positioned.Y + row.Y - cell.Bounds.Y;
                EmitCell(context, layout, cell, x, contentTop, delta, null);
            }
        }

        if (tableRadius > 0)
        {
            EmitTableFrame(plan, layout.Source!, tableBounds, tableRadius, tableMark);
        }
    }

    // The page-space box of one on-page table fragment. A table that splits across pages
    // rounds each fragment to its own box, so every fragment gets four rounded corners.
    private static PdfRect FragmentBounds(in PositionedTableFragment positioned, double x, double contentTop)
    {
        var top = double.MaxValue;
        var bottom = double.MinValue;
        foreach (var row in positioned.Fragment.Rows)
        {
            top = Math.Min(top, row.Y);
            bottom = Math.Max(bottom, row.Y + row.Height);
        }

        if (bottom <= top)
        {
            return default;
        }

        return PdfRect.FromSize(x, contentTop - positioned.Y - bottom, positioned.Layout.Width, bottom - top);
    }

    // Draws the rounded frame of a whole table: clips everything the table emitted (corner
    // cell backgrounds and outer border edges included; interior grid lines are only touched
    // where they meet the rounded corners) and, when the table-level borders resolve to one
    // uniform edge, strokes a single rounded-rectangle border around the perimeter.
    private static void EmitTableFrame(PagePlan plan, Table source, in PdfRect bounds, double radius, PlanMarks mark)
    {
        plan.ApplyRoundedClip(bounds, radius, mark);

        var borders = source.Borders;
        var style = new BoxStyle
        {
            Top = borders.Top,
            Right = borders.Right,
            Bottom = borders.Bottom,
            Left = borders.Left,
        };
        if (style.TryGetUniform(out var edge))
        {
            plan.RoundedStrokes.Add(new RoundedStrokeDraw
            {
                X = bounds.Left,
                Y = bounds.Bottom,
                Width = bounds.Width,
                Height = bounds.Height,
                Radius = radius,
                LineWidth = edge.Width,
                Color = edge.Color,
                Style = edge.Style,
            });
        }
    }

    private static void PaintRowBackground(PagePlan plan, Row row, double x, double bottom, double width, double height)
    {
        if (row.Background is { } background)
        {
            plan.Fills.Add(new FillDraw
            {
                X = x,
                Y = bottom,
                Width = width,
                Height = height,
                Color = background,
            });
        }
    }

    // Groups a table's flat cell list by source row once (cached per layout) so a
    // multi-fragment table no longer rescans every cell for each row it emits.
    private List<LaidOutCell>[] RowIndex(LaidOutTable layout)
    {
        if (tableRows.TryGetValue(layout, out var cached))
        {
            return cached;
        }

        var rows = new List<LaidOutCell>[layout.RowHeights.Count];
        foreach (var cell in layout.Cells)
        {
            if (cell.Row < rows.Length)
            {
                (rows[cell.Row] ??= []).Add(cell);
            }
        }

        tableRows[layout] = rows;
        return rows;
    }

    private void EmitCell(EmitContext context, LaidOutTable layout, LaidOutCell cell, double left, double contentTop, double delta, StructureElement? inherited)
    {
        var plan = context.Plan;
        var pageNumber = context.PageNumber;
        var pageCount = context.PageCount;
        var element = structureTree.ElementOf(cell.Cell) ?? inherited;
        var opacity = opacities.CellOpacity(cell.Cell);
        var extGState = opacity < 1 ? plan.RegisterExtGState(opacity, opacity) : null;
        var bounds = PdfRect.FromSize(
            left + cell.Bounds.X,
            contentTop - (cell.Bounds.Y + delta) - cell.Bounds.Height,
            cell.Bounds.Width,
            cell.Bounds.Height);
        BoxRenderer.Paint(plan, bounds, ResolveStyle(layout, cell, extGState));

        // A grid cell never rounds its own corners against its neighbours, so its content is
        // never rounded-clipped (radius 0); only whole tables and containers round.
        // Height is unused by the emit path; the cell stores no content height to carry.
        EmitBoxContent(
            context,
            new LaidOutBoxContent
            {
                Height = 0,
                Lines = cell.Lines,
                Images = cell.Images,
                Codes = cell.Codes,
                Tables = cell.Tables,
                Boxes = cell.Boxes,
            },
            cell.ContentBox.Width, cell.Bounds.X, cell.Bounds.X + cell.Bounds.Width,
            bounds, 0, opacity, element,
            left, contentTop, delta);
    }

    // Shared by table cells and containers, so it takes already-resolved pieces rather than
    // a Cell/Table.
    internal void EmitBoxContent(
        EmitContext context,
        in LaidOutBoxContent content,
        double contentWidth,
        double boundsLeft,
        double boundsRight,
        in PdfRect clip,
        double radius,
        double opacity,
        StructureElement? element,
        double left,
        double contentTop,
        double delta)
    {
        var lines = content.Lines;
        var images = content.Images;
        var codes = content.Codes;
        var tables = content.Tables;
        var boxes = content.Boxes;
        var plan = context.Plan;
        var pageNumber = context.PageNumber;
        var pageCount = context.PageCount;

        // The mark sits after the cell's own rounded background and border so only CHILD
        // content gets clipped to the rounded box - the border stroke stays unclipped.
        var contentMark = radius > 0 ? plan.Mark() : default;

        var firstText = plan.Texts.Count;
        var overflows = false;
        var cellLines = lines;
        var li = 0;
        while (li < cellLines.Count)
        {
            var line = cellLines[li];
            // Fields (page number/count) in a band-table cell resolve per page here,
            // re-broken to the cell's content width, replacing the placeholder layout.
            // A container tags each child block into its own structure element; a table cell
            // leaves its blocks unmapped and inherits the cell's element (the passed element).
            var lineElement = structureTree.ElementOf(line.Source) ?? element;
            if (line.Source is Paragraph paragraph && context.Fields.HasField(paragraph))
            {
                var reserved = 0;
                while (li + reserved < cellLines.Count && cellLines[li + reserved].Source == paragraph)
                {
                    reserved++;
                }

                var y = line.Y;
                foreach (var box in context.Fields.ResolveFields(paragraph, contentWidth, pageNumber, pageCount, resolution.Alignment(paragraph), reserved))
                {
                    context.Text.EmitLine(context, box, left + line.X, contentTop - (y + delta), lineElement, opacity);
                    overflows |= box.Width > contentWidth + 0.01;
                    y += box.Height;
                }

                li += reserved;
            }
            else
            {
                context.Text.EmitLine(context, line.Line, left + line.X, contentTop - (line.Y + delta), lineElement, opacity);
                overflows |= line.Line.Width > contentWidth + 0.01;
                li++;
            }
        }

        // An unbreakable token or oversized image/code wider than the cell is clipped to the
        // cell box so it never overpaints the neighboring cell.
        var cellClip = clip;
        if (overflows)
        {
            var texts = CollectionsMarshal.AsSpan(plan.Texts);
            for (var t = firstText; t < texts.Length; t++)
            {
                texts[t].Clip = cellClip;
            }
        }

        var contentOverflows = false;
        var firstImage = plan.Images.Count;
        var firstFill = plan.Fills.Count;
        var firstCodeText = plan.Texts.Count;

        foreach (var image in images)
        {
            contentOverflows |= image.X < boundsLeft - 0.01 || image.X + image.Width > boundsRight + 0.01;
            var xobject = imageStore.Decode(image.Source);
            var alpha = image.Source.Opacity * opacity;
            plan.Images.Add(new ImageDraw
            {
                X = left + image.X,
                Y = contentTop - (image.Y + delta) - image.Height,
                Width = image.Width,
                Height = image.Height,
                Image = xobject,
                Element = structureTree.ElementOf(image.Source) ?? element,
                ExtGState = alpha < 1 ? plan.RegisterExtGState(alpha, alpha) : null,
            });
            plan.UsedImages.Add(xobject);
        }

        foreach (var code in codes)
        {
            contentOverflows |= code.X < boundsLeft - 0.01 || code.X + CodeEmitter.CodeWidth(code.Source) > boundsRight + 0.01;
            context.Codes.EmitCodeBlock(context, code.Source, left + code.X, contentTop - (code.Y + delta));
        }

        if (contentOverflows)
        {
            var imageDraws = CollectionsMarshal.AsSpan(plan.Images);
            for (var im = firstImage; im < imageDraws.Length; im++)
            {
                imageDraws[im].Clip = cellClip;
            }

            var fills = CollectionsMarshal.AsSpan(plan.Fills);
            for (var f = firstFill; f < fills.Length; f++)
            {
                fills[f].Clip = cellClip;
            }

            var codeTexts = CollectionsMarshal.AsSpan(plan.Texts);
            for (var t = firstCodeText; t < codeTexts.Length; t++)
            {
                codeTexts[t].Clip = cellClip;
            }
        }

        // Nested tables and nested boxes interleave in document order (the shared Order
        // sequence assigned by BoxContentLayout.Position).
        var cursor = OrderedMerge.ByOrder(tables, static t => t.Order, boxes, static b => b.Order);
        while (cursor.MoveNext())
        {
            if (cursor.IsTable)
            {
                EmitNestedTable(context, tables[cursor.TableIndex], element, left, contentTop, delta);
            }
            else
            {
                EmitNestedBox(context, boxes[cursor.BoxIndex], element, left, contentTop, delta);
            }
        }

        if (radius > 0)
        {
            plan.ApplyRoundedClip(cellClip, radius, contentMark);
        }
    }

    private void EmitNestedTable(EmitContext context, in LaidOutNestedTable nested, StructureElement? element, double left, double contentTop, double delta)
    {
        var plan = context.Plan;
        var nestedLeft = left + nested.X + (nested.Layout.Source?.LeftIndent.Point ?? 0);
        var nestedRadius = 0.0;
        var nestedBounds = default(PdfRect);
        var nestedMark = default(PlanMarks);
        if (nested.Layout.Source is { } nestedTable && nestedTable.CornerRadius.Point > 0)
        {
            nestedBounds = PdfRect.FromSize(
                nestedLeft,
                contentTop - (delta + nested.Y) - nested.Layout.Height,
                nested.Layout.Width,
                nested.Layout.Height);
            nestedRadius = BoxRenderer.ClampRadius(nestedTable.CornerRadius.Point, nestedBounds.Width, nestedBounds.Height);
            nestedMark = plan.Mark();
        }

        if (nested.Layout.Source is { } nestedSource)
        {
            var rowTop = 0.0;
            var rowHeights = nested.Layout.RowHeights;
            for (var r = 0; r < rowHeights.Count && r < nestedSource.Rows.Count; r++)
            {
                PaintRowBackground(
                    plan, nestedSource.Rows[r], nestedLeft,
                    contentTop - (delta + nested.Y) - (rowTop + rowHeights[r]),
                    nested.Layout.Width, rowHeights[r]);
                rowTop += rowHeights[r];
            }
        }

        foreach (var nestedCell in nested.Layout.Cells)
        {
            EmitCell(context, nested.Layout, nestedCell, nestedLeft, contentTop, delta + nested.Y, element);
        }

        if (nestedRadius > 0)
        {
            EmitTableFrame(plan, nested.Layout.Source!, nestedBounds, nestedRadius, nestedMark);
        }
    }

    private void EmitNestedBox(EmitContext context, in LaidOutNestedBox box, StructureElement? element, double left, double contentTop, double delta)
    {
        var plan = context.Plan;
        var bounds = PdfRect.FromSize(
            left + box.Bounds.X,
            contentTop - (box.Bounds.Y + delta) - box.Bounds.Height,
            box.Bounds.Width,
            box.Bounds.Height);
        var opacity = ContainerDecoration.Paint(plan, opacities, bounds, box.Source, box.Style);

        var innerWidth = Math.Max(0, box.Bounds.Width - (2 * box.Source.Padding.Point));
        EmitBoxContent(
            context,
            box.Content,
            innerWidth, 0, box.Bounds.Width,
            bounds, box.Radius, opacity, element,
            left + box.Bounds.X, contentTop, delta + box.Bounds.Y);
    }

    // Resolves the cell/row/table border cascade (an edge falls back cell -> row -> table)
    // into the box decoration BoxRenderer paints.
    private static BoxStyle ResolveStyle(LaidOutTable layout, LaidOutCell cell, string? extGState)
    {
        var cellBorders = cell.Cell.Borders;
        var rowBorders = layout.Source?.Rows[cell.Row].Borders;
        var tableBorders = layout.Source?.Borders;

        return new BoxStyle
        {
            Background = cell.Cell.Background,
            Top = CascadeEdge(cellBorders.Top, rowBorders?.Top, tableBorders?.Top),
            Right = CascadeEdge(cellBorders.Right, rowBorders?.Right, tableBorders?.Right),
            Bottom = CascadeEdge(cellBorders.Bottom, rowBorders?.Bottom, tableBorders?.Bottom),
            Left = CascadeEdge(cellBorders.Left, rowBorders?.Left, tableBorders?.Left),
            ExtGState = extGState,
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
}
