using Radzen.Documents.LaidOut;
using Radzen.Documents.Pdf.Geometry;

namespace Radzen.Documents.Pdf.Render;

internal sealed class TableRecorder(StructureTreeBuilder structureTree)
{
    public void EmitFragment(PageRenderContext context, LaidOutTableFragment positioned, double left, double contentTop)
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
                    BottomUpSpace.Bottom(contentTop, bounds.Y, bounds.Height),
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
                BottomUpSpace.FromTop(contentTop, row.Y + row.Height),
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
        PageDrawTransformer.ApplyRoundedClip(plan, bounds, radius, mark);

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
        PageRenderContext context,
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
        var bounds = BottomUpSpace.Bounds(left, contentTop, cell.Bounds, delta);
        BoxDecorationRecorder.Paint(
            plan,
            bounds,
            opacity,
            cell.Decoration,
            SemanticArtifacts.ForDecoration(artifact));

        BoxContentRecorder.EmitBoxContent(
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

    internal void EmitNestedTable(
        PageRenderContext context,
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
                BottomUpSpace.Bottom(contentTop, delta + nested.Y, nested.Layout.Height),
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
                BottomUpSpace.BottomFromTop(
                    BottomUpSpace.FromTop(contentTop, delta + nested.Y),
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
}
