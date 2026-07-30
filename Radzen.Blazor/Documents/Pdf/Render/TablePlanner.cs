using System;
using Radzen.Documents.Geometry;

namespace Radzen.Documents.Pdf.Render;

internal sealed class TablePlanner(ImageStore imageStore, StructureTreeBuilder structureTree)
{
    internal SemanticArtifactKind? ArtifactOf(SourceId source) => structureTree.ArtifactOf(source);

    public void EmitFragment(EmitContext context, LaidOutTableFragment positioned, double left, double contentTop)
    {
        var plan = context.Plan;
        var layout = positioned.Layout;
        var decoration = layout.Decoration;
        var x = left + decoration.LeftIndent;
        var tableRadius = 0.0;
        var tableBounds = default(PdfRect);
        var tableMark = default(PlanMarks);
        var tableArtifact = structureTree.ArtifactOf(positioned.Source);
        if (decoration.CornerRadius > 0)
        {
            var bounds = positioned.Bounds;
            tableBounds = bounds.Width > 0 || bounds.Height > 0
                ? PdfRect.FromSize(
                    x,
                    PageSpace.Bottom(contentTop, bounds.Y, bounds.Height),
                    bounds.Width,
                    bounds.Height)
                : default;
            tableRadius = BoxStyle.ClampRadius(decoration.CornerRadius, tableBounds.Width, tableBounds.Height);
            tableMark = plan.Mark();
        }

        foreach (var row in positioned.Rows)
        {
            var rowArtifact = row.Artifact ?? tableArtifact;
            PaintRowBackground(
                plan,
                row.Background,
                x,
                PageSpace.FromTop(contentTop, row.Y + row.Height),
                layout.Width,
                row.Height,
                SemanticArtifacts.ForDecoration(rowArtifact));

            foreach (var placed in row.Cells)
            {
                EmitCell(
                    context,
                    placed.Cell,
                    x,
                    contentTop,
                    placed.Delta,
                    null,
                    rowArtifact);
            }
        }

        if (tableRadius > 0)
        {
            EmitTableFrame(
                plan,
                decoration.Frame,
                tableBounds,
                tableRadius,
                tableMark,
                SemanticArtifacts.ForDecoration(tableArtifact));
        }
    }

    private static void EmitTableFrame(
        PagePlan plan,
        in BoxStyle style,
        in PdfRect bounds,
        double radius,
        PlanMarks mark,
        SemanticArtifactKind artifact)
    {
        plan.ApplyRoundedClip(bounds, radius, mark);

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
                Artifact = artifact,
            });
        }
    }

    private static void PaintRowBackground(
        PagePlan plan,
        Color? rowBackground,
        double x,
        double bottom,
        double width,
        double height,
        SemanticArtifactKind artifact)
    {
        if (rowBackground is { } background)
        {
            plan.Fills.Add(new FillDraw
            {
                X = x,
                Y = bottom,
                Width = width,
                Height = height,
                Color = background,
                Artifact = artifact,
            });
        }
    }

    private void EmitCell(
        EmitContext context,
        LaidOutCell cell,
        double left,
        double contentTop,
        double delta,
        StructureElement? inherited,
        SemanticArtifactKind? artifact = null)
    {
        var plan = context.Plan;
        artifact ??= structureTree.ArtifactOf(cell.Source);
        var element = artifact is null ? structureTree.ElementOf(cell.Source) ?? inherited : null;
        var opacity = cell.Opacity;
        var bounds = PageSpace.Bounds(left, contentTop, cell.Bounds, delta);
        BoxDecorationPlanner.Paint(
            plan,
            bounds,
            opacity,
            cell.Decoration,
            SemanticArtifacts.ForDecoration(artifact));

        EmitBoxContent(
            context,
            new LaidOutBoxContent
            {
                Height = 0,
                Lines = cell.Lines,
                Images = cell.Images,
                CodeSymbols = cell.CodeSymbols,
                Tables = cell.Tables,
                Boxes = cell.Boxes,
            },
            cell.ContentBox.Width, cell.Bounds.X, cell.Bounds.X + cell.Bounds.Width,
            bounds, 0, opacity, element,
            left, contentTop, delta, artifact);
    }

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
        double delta,
        SemanticArtifactKind? artifact = null)
    {
        var images = content.Images;
        var codeSymbols = content.CodeSymbols;
        var tables = content.Tables;
        var boxes = content.Boxes;
        var plan = context.Plan;

        var contentMark = radius > 0 ? plan.Mark() : default;

        var firstText = plan.Texts.Count;
        var overflows = context.Text.EmitLines(
            context, content.Lines,
            left, contentTop, delta,
            opacity, element, resolveStructure: artifact is null,
            overflowThreshold: contentWidth,
            artifact: artifact);

        var cellClip = clip;
        if (overflows)
        {
            for (var t = firstText; t < plan.Texts.Count; t++)
            {
                plan.Texts[t] = plan.Texts[t] with { Clip = cellClip };
            }
        }

        var contentOverflows = false;
        var firstImage = plan.Images.Count;
        var firstFill = plan.Fills.Count;
        var firstCodeSymbolText = plan.Texts.Count;

        foreach (var image in images)
        {
            contentOverflows |= image.X < boundsLeft - 0.01 || image.X + image.Width > boundsRight + 0.01;
            var xobject = imageStore.DecodeApplied(image.Source, image.Paint);
            var alpha = image.Paint.Opacity * opacity;
            var imageElement = artifact is null ? structureTree.ElementOf(image.Source) ?? element : null;
            plan.Images.Add(new ImageDraw
            {
                X = left + image.X,
                Y = PageSpace.Bottom(contentTop, image.Y + delta, image.Height),
                Width = image.Width,
                Height = image.Height,
                Image = xobject,
                Sequence = plan.NextSequence(),
                Element = imageElement,
                Artifact = imageElement is null ? artifact ?? structureTree.ArtifactOf(image.Source) : null,
                ExtGState = alpha < 1 ? plan.RegisterExtGState(alpha, alpha) : null,
            });
            plan.UsedImages.Add(xobject);
        }

        foreach (var codeSymbol in codeSymbols)
        {
            contentOverflows |= codeSymbol.X < boundsLeft - 0.01 || codeSymbol.X + codeSymbol.Width > boundsRight + 0.01;
            context.CodeSymbols.EmitCodeSymbolModules(
                context, codeSymbol.Source, codeSymbol.Modules,
                left + codeSymbol.X,
                PageSpace.FromTop(contentTop, codeSymbol.Y + delta),
                codeSymbol.Caption,
                artifact);
        }

        if (contentOverflows)
        {
            for (var im = firstImage; im < plan.Images.Count; im++)
            {
                plan.Images[im] = plan.Images[im] with { Clip = cellClip };
            }

            for (var f = firstFill; f < plan.Fills.Count; f++)
            {
                plan.Fills[f] = plan.Fills[f] with { Clip = cellClip };
            }

            for (var t = firstCodeSymbolText; t < plan.Texts.Count; t++)
            {
                plan.Texts[t] = plan.Texts[t] with { Clip = cellClip };
            }
        }

        OrderedMerge.VisitByOrder(
            tables,
            static table => table.ZOrder,
            boxes,
            static box => box.ZOrder,
            table => EmitNestedTable(context, table, element, left, contentTop, delta, artifact),
            box => EmitNestedBox(context, box, element, left, contentTop, delta, artifact));

        if (radius > 0)
        {
            plan.ApplyRoundedClip(cellClip, radius, contentMark);
        }
    }

    private void EmitNestedTable(
        EmitContext context,
        in LaidOutTablePlacement nested,
        StructureElement? element,
        double left,
        double contentTop,
        double delta,
        SemanticArtifactKind? inheritedArtifact)
    {
        var plan = context.Plan;
        var nestedDecoration = nested.Layout.Decoration;
        var artifact = inheritedArtifact ?? structureTree.ArtifactOf(nested.Layout.Source);
        var nestedLeft = left + nested.X + nestedDecoration.LeftIndent;
        var nestedRadius = 0.0;
        var nestedBounds = default(PdfRect);
        var nestedMark = default(PlanMarks);
        if (nestedDecoration.CornerRadius > 0)
        {
            nestedBounds = PdfRect.FromSize(
                nestedLeft,
                PageSpace.Bottom(contentTop, delta + nested.Y, nested.Layout.Height),
                nested.Layout.Width,
                nested.Layout.Height);
            nestedRadius = BoxStyle.ClampRadius(nestedDecoration.CornerRadius, nestedBounds.Width, nestedBounds.Height);
            nestedMark = plan.Mark();
        }

        var rowTop = 0.0;
        var rowHeights = nested.Layout.RowHeights;
        for (var r = 0; r < rowHeights.Length && r < nestedDecoration.RowBackgrounds.Length; r++)
        {
            PaintRowBackground(
                plan, nestedDecoration.RowBackgrounds[r], nestedLeft,
                PageSpace.FromTop(
                    PageSpace.FromTop(contentTop, delta + nested.Y),
                    rowTop + rowHeights[r]),
                nested.Layout.Width,
                rowHeights[r],
                SemanticArtifacts.ForDecoration(artifact));
            rowTop += rowHeights[r];
        }

        foreach (var nestedCell in nested.Layout.Cells)
        {
            EmitCell(context, nestedCell, nestedLeft, contentTop, delta + nested.Y, element, artifact);
        }

        if (nestedRadius > 0)
        {
            EmitTableFrame(
                plan,
                nestedDecoration.Frame,
                nestedBounds,
                nestedRadius,
                nestedMark,
                SemanticArtifacts.ForDecoration(artifact));
        }
    }

    private void EmitNestedBox(
        EmitContext context,
        LaidOutBox box,
        StructureElement? element,
        double left,
        double contentTop,
        double delta,
        SemanticArtifactKind? inheritedArtifact)
    {
        var plan = context.Plan;
        var bounds = PageSpace.Bounds(left, contentTop, box.Bounds, delta);
        var opacity = box.Opacity;
        var artifact = inheritedArtifact ?? structureTree.ArtifactOf(box.Source);
        BoxDecorationPlanner.Paint(
            plan,
            bounds,
            opacity,
            box.Style,
            SemanticArtifacts.ForDecoration(artifact));

        var innerWidth = Math.Max(0, box.Bounds.Width - box.Padding.Horizontal);
        EmitBoxContent(
            context,
            box.Content,
            innerWidth, 0, box.Bounds.Width,
            bounds, BoxStyle.ClampRadius(box.Style.CornerRadius, bounds.Width, bounds.Height), opacity, element,
            left + box.Bounds.X, contentTop, delta + box.Bounds.Y, artifact);
    }
}
