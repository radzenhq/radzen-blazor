using System.Collections.Generic;

namespace Radzen.Documents.Pdf;

// Emits a positioned table fragment: row backgrounds, then each cell's background,
// borders, text lines (with per-page field resolution), images, codes and nested
// tables - clipping content that overflows the cell box.
internal sealed class TableEmitter(ImageStore imageStore, StructureTreeBuilder structureTree, StyleResolution resolution, OpacityResolver opacities)
{
    private readonly Dictionary<LaidOutTable, List<LaidOutCell>[]> tableRows = [];

    public void EmitFragment(EmitContext context, PositionedTableFragment positioned, double left, double contentTop)
    {
        var plan = context.Plan;
        var layout = positioned.Layout;
        var x = left + (layout.Source?.LeftIndent.Point ?? 0);
        var rowIndex = RowIndex(layout);
        foreach (var row in positioned.Fragment.Rows)
        {
            if (layout.Source?.Rows[row.SourceRow].Background is { } background)
            {
                plan.Fills.Add(new FillDraw
                {
                    X = x,
                    Y = contentTop - (positioned.Y + row.Y + row.Height),
                    Width = layout.Width,
                    Height = row.Height,
                    Color = background,
                });
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
        var radius = CornerRadius(cell);
        if (cell.Cell.Background is { } background)
        {
            plan.Fills.Add(new FillDraw
            {
                X = left + cell.Bounds.X,
                Y = contentTop - (cell.Bounds.Y + delta) - cell.Bounds.Height,
                Width = cell.Bounds.Width,
                Height = cell.Bounds.Height,
                Color = background,
                Radius = radius,
                ExtGState = extGState,
            });
        }

        EmitBorders(plan, layout, cell, left, contentTop, delta, radius, extGState);

        var firstText = plan.Texts.Count;
        var overflows = false;
        var cellLines = cell.Lines;
        var li = 0;
        while (li < cellLines.Count)
        {
            var line = cellLines[li];
            // Fields (page number/count) in a band-table cell resolve per page here,
            // re-broken to the cell's content width, replacing the placeholder layout.
            if (line.Source is Paragraph paragraph && context.Fields.HasField(paragraph))
            {
                var y = line.Y;
                foreach (var box in context.Fields.ResolveFields(paragraph, cell.ContentBox.Width, pageNumber, pageCount, resolution.Alignment(paragraph)))
                {
                    context.Text.EmitLine(context, box, left + line.X, contentTop - (y + delta), element, opacity);
                    overflows |= box.Width > cell.ContentBox.Width + 0.01;
                    y += box.Height;
                }

                while (li < cellLines.Count && cellLines[li].Source == paragraph)
                {
                    li++;
                }
            }
            else
            {
                context.Text.EmitLine(context, line.Line, left + line.X, contentTop - (line.Y + delta), element, opacity);
                overflows |= line.Line.Width > cell.ContentBox.Width + 0.01;
                li++;
            }
        }

        // An unbreakable token or oversized image/code wider than the cell is clipped to the
        // cell box so it never overpaints the neighboring cell.
        var cellClip = new Rect(
            left + cell.Bounds.X,
            contentTop - (cell.Bounds.Y + delta) - cell.Bounds.Height,
            cell.Bounds.Width,
            cell.Bounds.Height);
        if (overflows)
        {
            var texts = System.Runtime.InteropServices.CollectionsMarshal.AsSpan(plan.Texts);
            for (var t = firstText; t < texts.Length; t++)
            {
                texts[t].Clip = cellClip;
            }
        }

        var boundsLeft = cell.Bounds.X;
        var boundsRight = cell.Bounds.X + cell.Bounds.Width;
        var contentOverflows = false;
        var firstImage = plan.Images.Count;
        var firstFill = plan.Fills.Count;
        var firstCodeText = plan.Texts.Count;

        foreach (var image in cell.Images)
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
                Element = element,
                ExtGState = alpha < 1 ? plan.RegisterExtGState(alpha, alpha) : null,
            });
            plan.UsedImages.Add(xobject);
        }

        foreach (var code in cell.Codes)
        {
            contentOverflows |= code.X < boundsLeft - 0.01 || code.X + CodeEmitter.CodeWidth(code.Source) > boundsRight + 0.01;
            context.Codes.EmitCodeBlock(context, code.Source, left + code.X, contentTop - (code.Y + delta));
        }

        if (contentOverflows)
        {
            var images = System.Runtime.InteropServices.CollectionsMarshal.AsSpan(plan.Images);
            for (var im = firstImage; im < images.Length; im++)
            {
                images[im].Clip = cellClip;
            }

            var fills = System.Runtime.InteropServices.CollectionsMarshal.AsSpan(plan.Fills);
            for (var f = firstFill; f < fills.Length; f++)
            {
                fills[f].Clip = cellClip;
            }

            var codeTexts = System.Runtime.InteropServices.CollectionsMarshal.AsSpan(plan.Texts);
            for (var t = firstCodeText; t < codeTexts.Length; t++)
            {
                codeTexts[t].Clip = cellClip;
            }
        }

        foreach (var nested in cell.Tables)
        {
            var nestedLeft = left + nested.X + (nested.Layout.Source?.LeftIndent.Point ?? 0);
            foreach (var nestedCell in nested.Layout.Cells)
            {
                EmitCell(context, nested.Layout, nestedCell, nestedLeft, contentTop, delta + nested.Y, element);
            }
        }
    }

    // The effective corner radius, clamped so opposite corners never overlap.
    private static double CornerRadius(LaidOutCell cell)
    {
        var radius = cell.Cell.CornerRadius.Point;
        if (radius <= 0)
        {
            return 0;
        }

        return System.Math.Min(radius, System.Math.Min(cell.Bounds.Width, cell.Bounds.Height) / 2);
    }

    // A rounded cell with a UNIFORM border (same width, color and style resolve on all four
    // edges) strokes one rounded-rectangle path. A non-uniform border falls back to the
    // existing four square edges - only the background fill stays rounded in that case.
    private static void EmitBorders(PagePlan plan, LaidOutTable layout, LaidOutCell cell, double left, double contentTop, double delta, double radius, string? extGState)
    {
        var cellBorders = cell.Cell.Borders;
        var rowBorders = layout.Source?.Rows[cell.Row].Borders;
        var tableBorders = layout.Source?.Borders;

        var x = left + cell.Bounds.X;
        var top = contentTop - (cell.Bounds.Y + delta);
        var right = x + cell.Bounds.Width;
        var bottom = top - cell.Bounds.Height;

        if (radius > 0)
        {
            var topEdge = ResolveEdge(cellBorders.Top, rowBorders?.Top, tableBorders?.Top);
            var rightEdge = ResolveEdge(cellBorders.Right, rowBorders?.Right, tableBorders?.Right);
            var bottomEdge = ResolveEdge(cellBorders.Bottom, rowBorders?.Bottom, tableBorders?.Bottom);
            var leftEdge = ResolveEdge(cellBorders.Left, rowBorders?.Left, tableBorders?.Left);
            if (topEdge is { } uniform && rightEdge == uniform && bottomEdge == uniform && leftEdge == uniform)
            {
                plan.RoundedStrokes.Add(new RoundedStrokeDraw
                {
                    X = x,
                    Y = bottom,
                    Width = cell.Bounds.Width,
                    Height = cell.Bounds.Height,
                    Radius = radius,
                    LineWidth = uniform.Width,
                    Color = uniform.Color,
                    Style = uniform.Style,
                    ExtGState = extGState,
                });
                return;
            }
        }

        EmitEdge(plan, cellBorders.Top, rowBorders?.Top, tableBorders?.Top, x, top, right, top, extGState);
        EmitEdge(plan, cellBorders.Right, rowBorders?.Right, tableBorders?.Right, right, bottom, right, top, extGState);
        EmitEdge(plan, cellBorders.Bottom, rowBorders?.Bottom, tableBorders?.Bottom, x, bottom, right, bottom, extGState);
        EmitEdge(plan, cellBorders.Left, rowBorders?.Left, tableBorders?.Left, x, bottom, x, top, extGState);
    }

    private readonly record struct ResolvedEdge(Color Color, double Width, BorderStyle Style);

    // Applies the cell/row/table cascade and returns the visible edge, or null when none draws.
    private static ResolvedEdge? ResolveEdge(Border cellEdge, Border? rowEdge, Border? tableEdge)
    {
        var edge = cellEdge;
        if (!cellEdge.IsSet)
        {
            if (rowEdge?.IsSet == true)
            {
                edge = rowEdge;
            }
            else if (tableEdge is not null)
            {
                edge = tableEdge;
            }
        }

        // MigraDoc semantics: a positive width alone makes the edge a visible solid line.
        var style = edge.Style;
        if (style == BorderStyle.None && edge.Width > 0)
        {
            style = BorderStyle.Solid;
        }

        if (style == BorderStyle.None)
        {
            return null;
        }

        return new ResolvedEdge(edge.Color, edge.Width > 0 ? edge.Width : 0.5, style);
    }

    private static void EmitEdge(
        PagePlan plan,
        Border cellEdge,
        Border? rowEdge,
        Border? tableEdge,
        double x1,
        double y1,
        double x2,
        double y2,
        string? extGState)
    {
        if (ResolveEdge(cellEdge, rowEdge, tableEdge) is not { } edge)
        {
            return;
        }

        plan.Edges.Add(new EdgeDraw
        {
            X1 = x1,
            Y1 = y1,
            X2 = x2,
            Y2 = y2,
            LineWidth = edge.Width,
            Color = edge.Color,
            Style = edge.Style,
            ExtGState = extGState,
        });
    }
}
