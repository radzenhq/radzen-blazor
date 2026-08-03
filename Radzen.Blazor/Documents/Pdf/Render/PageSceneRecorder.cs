using System;
using System.Collections.Generic;
using Radzen.Documents.LaidOut;
using Radzen.Documents.Scene;
using Radzen.Documents.Pdf.Geometry;
using Radzen.Documents.Pdf.Output;
using Radzen.Documents.Core;

namespace Radzen.Documents.Pdf.Render;

internal readonly record struct PlannedAnchor(int PageIndex, string Name, double Top);

internal sealed class PageSceneRecorder(
    StructureTreeBuilder structureTree,
    TextLineRecorder textRecorder,
    CodeSymbolRecorder codeSymbolRecorder,
    ImageRecorder imageRecorder,
    WatermarkRecorder watermarkRecorder) : ISceneVisitor
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

        public required PdfRect? LineClip { get; init; }

        public required PdfRect? InlineClip { get; init; }
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
    private PageRenderContext context = null!;
    private double height;
    private double top;
    private bool body;

    public List<PagePlan> Plans { get; } = [];

    public List<PlannedAnchor> Anchors { get; } = [];

    private ContentScope? Scope => scopes.Count > 0 ? scopes[^1] : null;

    private ContentScope? Parent => Scope;

    private TableScope Table => tables[^1];

    private PagePlan Plan => context.Plan;

    void ISceneVisitor.BeginPage(LaidOutPage page, int index)
    {
        height = page.Size.Height.Point;
        var plan = new PagePlan { Size = page.Size };
        context = new PageRenderContext(plan, textRecorder);
        Plans.Add(plan);
        scopes.Clear();
        tables.Clear();
        items.Clear();
    }

    void ISceneVisitor.EnterLayer(SceneLayerKind kind, double layerTop)
    {
        context.Layer = (int)kind;
        body = kind == SceneLayerKind.Body;
        top = BottomUpSpace.FromTop(height, layerTop);
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
            textRecorder.EmitLines(
                context, [line],
                frame.Left, top, frame.Delta,
                opacity: 1, inherited: null, resolveStructure: body,
                clip: null,
                artifact: body ? null : SemanticArtifactKind.Pagination);
            return;
        }

        textRecorder.EmitLines(
            context, [line],
            frame.Left, top, frame.Delta,
            scope.Opacity, scope.Element, resolveStructure: scope.Artifact is null,
            clip: scope.LineClip,
            artifact: scope.Artifact);
    }

    void ISceneVisitor.Image(in LaidOutImage image, in SceneFrame frame)
    {
        var scope = Scope;
        imageRecorder.EmitImage(
            context, image, frame.Left, top, frame.Delta,
            scope?.Opacity ?? 1, scope?.Element, scope?.Artifact, scope?.InlineClip);
    }

    void ISceneVisitor.CodeSymbol(in LaidOutCodeSymbol codeSymbol, in SceneFrame frame)
    {
        var scope = Scope;
        codeSymbolRecorder.EmitCodeSymbolModules(
            context, codeSymbol.Source, codeSymbol.Modules, codeSymbol.Foreground,
            frame.Left + codeSymbol.X,
            BottomUpSpace.FromTop(top, codeSymbol.Y + frame.Delta),
            codeSymbol.Caption,
            scope?.Artifact,
            scope?.InlineClip);
    }

    void ISceneVisitor.EnterBox(LaidOutBox box, in SceneFrame frame, in SceneClip clip)
    {
        var parent = Scope;
        var mark = Plan.Mark();
        var rect = ToPdfRect(clip.Bounds);
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
            LineClip = IntersectClips(parent?.LineClip, clip.ClipsLines ? rect : null),
            InlineClip = IntersectClips(parent?.InlineClip, clip.ClipsInline ? rect : null),
        });
    }

    private static PdfRect? IntersectClips(PdfRect? outer, PdfRect? inner)
        => (outer, inner) switch
        {
            (null, _) => inner,
            (_, null) => outer,
            ({ } a, { } b) => new PdfRect(
                Math.Max(a.Left, b.Left),
                Math.Max(a.Bottom, b.Bottom),
                Math.Min(a.Right, b.Right),
                Math.Min(a.Top, b.Top)),
        };

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

    void ISceneVisitor.EnterRow(in LaidOutRow row, in SceneFrame frame)
    {
        var table = Table;
        table.CellArtifact = row.Artifact ?? table.Artifact;
        PaintRowBackground(
            row.Background,
            frame.Left,
            BottomUpSpace.FromTop(BottomUpSpace.FromTop(top, frame.Delta), row.Y + row.Height),
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

    void ISceneVisitor.EnterCell(LaidOutCell cell, in SceneFrame frame, in SceneClip clip)
    {
        var table = Table;
        var artifact = table.CellArtifact ?? structureTree.ArtifactOf(cell.Source);
        var element = artifact is null ? structureTree.ElementOf(cell.Source) ?? table.Element : null;
        var rect = ToPdfRect(clip.Bounds);

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
            LineClip = IntersectClips(Parent?.LineClip, clip.ClipsLines ? rect : null),
            InlineClip = IntersectClips(Parent?.InlineClip, clip.ClipsInline ? rect : null),
        });
    }

    void ISceneVisitor.LeaveCell(LaidOutCell cell, in SceneFrame frame)
        => scopes.RemoveAt(scopes.Count - 1);

    void ISceneVisitor.Link(in LaidOutLink link)
        => Plan.Links.Add(new OutputLink(
            link.Left,
            BottomUpSpace.FromTop(height, link.Bottom),
            link.Right,
            BottomUpSpace.FromTop(height, link.Top),
            link.Uri,
            link.Anchor,
            structureTree.LinkElementOf(link.Source)?.Id,
            link.Text));

    void ISceneVisitor.Anchor(in LaidOutAnchor anchor)
        => Anchors.Add(new PlannedAnchor(
            Plans.Count - 1,
            anchor.Name,
            BottomUpSpace.FromTop(height, anchor.Top)));

    void ISceneVisitor.Watermark(LaidOutWatermark watermark) => watermarkRecorder.Plan(Plan, watermark);

    private PdfRect ToPdfRect(in Rect bounds)
        => PdfRect.FromSize(
            bounds.X,
            BottomUpSpace.Bottom(top, bounds.Y, bounds.Height),
            bounds.Width,
            bounds.Height);

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
