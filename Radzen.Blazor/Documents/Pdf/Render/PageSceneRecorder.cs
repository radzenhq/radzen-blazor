using System;
using System.Collections.Generic;
using Radzen.Documents.LaidOut;
using Radzen.Documents.Scene;
using Radzen.Documents.Pdf.Geometry;
using Radzen.Documents.Core;

namespace Radzen.Documents.Pdf.Render;

internal sealed class PageSceneRecorder(
    PageRenderContext context,
    StructureTreeBuilder structureTree,
    LaidOutPage page,
    double height) : ISceneVisitor
{
    private sealed class ContentScope
    {
        public required StructureElement? Element { get; init; }

        public required SemanticArtifactKind? Artifact { get; init; }

        public required double Opacity { get; init; }

        public required PdfRect Clip { get; init; }

        public required double Radius { get; init; }

        public required PlanMarks ContentMark { get; init; }

        public required PlanMarks BoxMark { get; init; }

        public required SceneContentBounds Bounds { get; init; }

        public required int FirstText { get; init; }

        public bool TextOverflows { get; set; }

        public int FirstImage { get; set; }

        public int FirstFill { get; set; }

        public int FirstCodeSymbolText { get; set; }

        public bool ContentOverflows { get; set; }
    }

    private sealed class TableScope
    {
        public required StructureElement? Element { get; init; }

        public required SemanticArtifactKind? Artifact { get; init; }

        public required double Width { get; init; }

        public required PdfRect Bounds { get; init; }

        public required double Radius { get; init; }

        public required PlanMarks Mark { get; init; }

        public SemanticArtifactKind? CellArtifact { get; set; }
    }

    private readonly List<ContentScope> scopes = [];
    private readonly List<TableScope> tables = [];
    private readonly List<StackMark> items = [];
    private double top;
    private bool body;

    private ContentScope? Scope => scopes.Count > 0 ? scopes[^1] : null;

    private TableScope Table => tables[^1];

    private PagePlan Plan => context.Plan;

    void ISceneVisitor.EnterLayer(SceneLayerKind kind)
    {
        context.Layer = (int)kind;
        body = kind == SceneLayerKind.Body;
        top = BottomUpSpace.FromTop(height, SceneWalk.LayerTop(page, kind));
    }

    void ISceneVisitor.BeginItem(int zOrder) => items.Add(context.BeginStack(zOrder));

    void ISceneVisitor.EndItem()
    {
        var mark = items[^1];
        items.RemoveAt(items.Count - 1);
        context.EndStack(mark);
    }

    void ISceneVisitor.Line(in LaidOutLine line, in SceneFrame frame)
    {
        if (Scope is not { } scope)
        {
            context.Text.EmitLines(
                context, [line],
                frame.Left, top, frame.Delta,
                opacity: 1, inherited: null, resolveStructure: body,
                overflowThreshold: double.PositiveInfinity,
                artifact: body ? null : SemanticArtifactKind.Pagination);
            return;
        }

        scope.TextOverflows |= context.Text.EmitLines(
            context, [line],
            frame.Left, top, frame.Delta,
            scope.Opacity, scope.Element, resolveStructure: scope.Artifact is null,
            overflowThreshold: scope.Bounds.Width,
            artifact: scope.Artifact);
    }

    void ISceneVisitor.Image(in LaidOutImage image, in SceneFrame frame)
    {
        var scope = Scope;
        context.Images.EmitImage(
            context, image, frame.Left, top, frame.Delta,
            scope?.Opacity ?? 1, scope?.Element, scope?.Artifact);

        Overflow(scope, image.X, image.Width);
    }

    void ISceneVisitor.CodeSymbol(in LaidOutCodeSymbol codeSymbol, in SceneFrame frame)
    {
        var scope = Scope;
        context.CodeSymbols.EmitCodeSymbolModules(
            context, codeSymbol.Source, codeSymbol.Modules, codeSymbol.Foreground,
            frame.Left + codeSymbol.X,
            BottomUpSpace.FromTop(top, codeSymbol.Y + frame.Delta),
            codeSymbol.Caption,
            scope?.Artifact);

        Overflow(scope, codeSymbol.X, codeSymbol.Width);
    }

    private static void Overflow(ContentScope? scope, double left, double width)
    {
        if (scope is null)
        {
            return;
        }

        scope.ContentOverflows |= left < scope.Bounds.Left - 0.01 || left + width > scope.Bounds.Right + 0.01;
    }

    void ISceneVisitor.AfterLines()
    {
        var scope = scopes[^1];
        if (scope.TextOverflows)
        {
            for (var i = scope.FirstText; i < Plan.Texts.Count; i++)
            {
                Plan.Texts[i] = Plan.Texts[i] with { Clip = scope.Clip };
            }
        }

        scope.FirstImage = Plan.Images.Count;
        scope.FirstFill = Plan.Fills.Count;
        scope.FirstCodeSymbolText = Plan.Texts.Count;
    }

    void ISceneVisitor.AfterInline()
    {
        var scope = scopes[^1];
        if (!scope.ContentOverflows)
        {
            return;
        }

        for (var i = scope.FirstImage; i < Plan.Images.Count; i++)
        {
            Plan.Images[i] = Plan.Images[i] with { Clip = scope.Clip };
        }

        for (var i = scope.FirstFill; i < Plan.Fills.Count; i++)
        {
            Plan.Fills[i] = Plan.Fills[i] with { Clip = scope.Clip };
        }

        for (var i = scope.FirstCodeSymbolText; i < Plan.Texts.Count; i++)
        {
            Plan.Texts[i] = Plan.Texts[i] with { Clip = scope.Clip };
        }
    }

    void ISceneVisitor.EnterBox(LaidOutBox box, in SceneFrame frame, in SceneContentBounds bounds)
    {
        var parent = Scope;
        var mark = Plan.Mark();
        var rect = BottomUpSpace.Bounds(frame.Left, top, box.Bounds, frame.Delta);
        var artifact = parent?.Artifact ?? structureTree.ArtifactOf(box.Source);

        if (box.Transform is not null && box.Style.Shadow is not null)
        {
            throw new NotSupportedException(
                "A rotated box cannot preserve a box shadow; remove the shadow or the rotation.");
        }

        BoxDecorationRecorder.Paint(
            Plan,
            rect,
            box.Opacity,
            box.Style,
            SemanticArtifacts.ForDecoration(artifact));

        var radius = BoxStyle.ClampRadius(box.Style.CornerRadius, rect.Width, rect.Height);

        scopes.Add(new ContentScope
        {
            Element = parent?.Element,
            Artifact = artifact,
            Opacity = box.Opacity,
            Clip = rect,
            Radius = radius,
            ContentMark = radius > 0 ? Plan.Mark() : default,
            BoxMark = mark,
            Bounds = bounds,
            FirstText = Plan.Texts.Count,
        });
    }

    void ISceneVisitor.LeaveBox(LaidOutBox box, in SceneFrame frame)
    {
        var scope = scopes[^1];
        scopes.RemoveAt(scopes.Count - 1);

        if (scope.Radius > 0)
        {
            PageDrawTransformer.ApplyRoundedClip(Plan, scope.Clip, scope.Radius, scope.ContentMark);
        }

        if (box.Transform is { } transform)
        {
            PageDrawTransformer.ApplyTransform(
                Plan,
                BottomUpSpace.FlipVertical(transform, height),
                scope.BoxMark);
        }
    }

    void ISceneVisitor.EnterFragment(in LaidOutTableFragment fragment, in SceneFrame frame)
    {
        var decoration = fragment.Layout.Decoration;
        var artifact = structureTree.ArtifactOf(fragment.Source);
        var rect = default(PdfRect);
        var radius = 0.0;
        var mark = default(PlanMarks);
        if (decoration.CornerRadius > 0)
        {
            var bounds = fragment.Bounds;
            rect = bounds.Width > 0 || bounds.Height > 0
                ? PdfRect.FromSize(
                    frame.Left,
                    BottomUpSpace.Bottom(top, bounds.Y, bounds.Height),
                    bounds.Width,
                    bounds.Height)
                : default;
            radius = BoxStyle.ClampRadius(decoration.CornerRadius, rect.Width, rect.Height);
            mark = Plan.Mark();
        }

        tables.Add(new TableScope
        {
            Element = null,
            Artifact = artifact,
            Width = fragment.Layout.Width,
            Bounds = rect,
            Radius = radius,
            Mark = mark,
            CellArtifact = artifact,
        });
    }

    void ISceneVisitor.Row(in LaidOutRow row, in SceneFrame frame)
    {
        var table = Table;
        table.CellArtifact = row.Artifact ?? table.Artifact;
        PaintRowBackground(
            row.Background,
            frame.Left,
            BottomUpSpace.FromTop(top, row.Y + row.Height),
            table.Width,
            row.Height,
            SemanticArtifacts.ForDecoration(table.CellArtifact));
    }

    void ISceneVisitor.LeaveFragment(in LaidOutTableFragment fragment, in SceneFrame frame)
    {
        var table = Table;
        tables.RemoveAt(tables.Count - 1);
        if (table.Radius > 0)
        {
            EmitTableFrame(
                fragment.Layout.Decoration.Frame,
                table.Bounds,
                table.Radius,
                table.Mark,
                SemanticArtifacts.ForDecoration(table.Artifact));
        }
    }

    void ISceneVisitor.EnterTable(in LaidOutTablePlacement placement, in SceneFrame frame)
    {
        var parent = Scope;
        var decoration = placement.Layout.Decoration;
        var artifact = parent?.Artifact ?? structureTree.ArtifactOf(placement.Layout.Source);
        var rect = default(PdfRect);
        var radius = 0.0;
        var mark = default(PlanMarks);
        if (decoration.CornerRadius > 0)
        {
            rect = PdfRect.FromSize(
                frame.Left,
                BottomUpSpace.Bottom(top, frame.Delta, placement.Layout.Height),
                placement.Layout.Width,
                placement.Layout.Height);
            radius = BoxStyle.ClampRadius(decoration.CornerRadius, rect.Width, rect.Height);
            mark = Plan.Mark();
        }

        var rowTop = 0.0;
        var rowHeights = placement.Layout.RowHeights;
        for (var r = 0; r < rowHeights.Length && r < decoration.RowBackgrounds.Length; r++)
        {
            PaintRowBackground(
                decoration.RowBackgrounds[r],
                frame.Left,
                BottomUpSpace.BottomFromTop(
                    BottomUpSpace.FromTop(top, frame.Delta),
                    rowTop + rowHeights[r]),
                placement.Layout.Width,
                rowHeights[r],
                SemanticArtifacts.ForDecoration(artifact));
            rowTop += rowHeights[r];
        }

        tables.Add(new TableScope
        {
            Element = parent?.Element,
            Artifact = artifact,
            Width = placement.Layout.Width,
            Bounds = rect,
            Radius = radius,
            Mark = mark,
            CellArtifact = artifact,
        });
    }

    void ISceneVisitor.LeaveTable(in LaidOutTablePlacement placement, in SceneFrame frame)
    {
        var table = Table;
        tables.RemoveAt(tables.Count - 1);
        if (table.Radius > 0)
        {
            EmitTableFrame(
                placement.Layout.Decoration.Frame,
                table.Bounds,
                table.Radius,
                table.Mark,
                SemanticArtifacts.ForDecoration(table.Artifact));
        }
    }

    void ISceneVisitor.EnterCell(LaidOutCell cell, in SceneFrame frame, in SceneContentBounds bounds)
    {
        var table = Table;
        var artifact = table.CellArtifact ?? structureTree.ArtifactOf(cell.Source);
        var element = artifact is null ? structureTree.ElementOf(cell.Source) ?? table.Element : null;
        var rect = BottomUpSpace.Bounds(frame.Left, top, cell.Bounds, frame.Delta);

        BoxDecorationRecorder.Paint(
            Plan,
            rect,
            cell.Opacity,
            cell.Decoration,
            SemanticArtifacts.ForDecoration(artifact));

        scopes.Add(new ContentScope
        {
            Element = element,
            Artifact = artifact,
            Opacity = cell.Opacity,
            Clip = rect,
            Radius = 0,
            ContentMark = default,
            BoxMark = default,
            Bounds = bounds,
            FirstText = Plan.Texts.Count,
        });
    }

    void ISceneVisitor.LeaveCell(LaidOutCell cell, in SceneFrame frame)
        => scopes.RemoveAt(scopes.Count - 1);

    private void PaintRowBackground(
        Color? rowBackground,
        double x,
        double bottom,
        double width,
        double rowHeight,
        SemanticArtifactKind artifact)
    {
        if (rowBackground is { } background)
        {
            Plan.Fills.Add(new FillDraw
            {
                X = x,
                Y = bottom,
                Width = width,
                Height = rowHeight,
                Color = background,
                Artifact = artifact,
            });
        }
    }

    private void EmitTableFrame(
        in BoxStyle style,
        in PdfRect bounds,
        double radius,
        PlanMarks mark,
        SemanticArtifactKind artifact)
    {
        PageDrawTransformer.ApplyRoundedClip(Plan, bounds, radius, mark);

        if (style.TryGetUniform(out var edge))
        {
            Plan.RoundedStrokes.Add(new RoundedStrokeDraw
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
}
