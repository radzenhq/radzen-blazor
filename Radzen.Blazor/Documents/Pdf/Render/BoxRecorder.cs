using System;
using Radzen.Documents.LaidOut;
using Radzen.Documents.Pdf.Geometry;

namespace Radzen.Documents.Pdf.Render;

internal enum BoxContentOrigin
{
    Parent,
    Box,
}

internal sealed class BoxRecorder(StructureTreeBuilder structureTree)
{
    public void EmitBox(
        PageRenderContext context,
        LaidOutBox box,
        BoxContentOrigin origin,
        StructureElement? element,
        double left,
        double contentTop,
        double delta,
        SemanticArtifactKind? inheritedArtifact)
    {
        var plan = context.Plan;
        var mark = plan.Mark();
        var bounds = BottomUpSpace.Bounds(left, contentTop, box.Bounds, delta);
        var artifact = inheritedArtifact ?? structureTree.ArtifactOf(box.Source);

        if (box.Transform is not null && box.Style.Shadow is not null)
        {
            throw new NotSupportedException(
                "A rotated box cannot preserve a box shadow; remove the shadow or the rotation.");
        }

        var opacity = box.Opacity;
        BoxDecorationRecorder.Paint(
            plan,
            bounds,
            opacity,
            box.Style,
            SemanticArtifacts.ForDecoration(artifact));

        var radius = BoxStyle.ClampRadius(box.Style.CornerRadius, bounds.Width, bounds.Height);
        var innerWidth = Math.Max(0, box.Bounds.Width - box.Padding.Horizontal);
        var parentOrigin = origin == BoxContentOrigin.Parent;

        BoxContentRecorder.EmitBoxContent(
            context,
            box.Content,
            innerWidth,
            parentOrigin ? box.Bounds.X : 0,
            parentOrigin ? box.Bounds.X + box.Bounds.Width : box.Bounds.Width,
            bounds, radius, opacity, element,
            parentOrigin ? left : left + box.Bounds.X,
            contentTop, delta + box.Bounds.Y, artifact);

        if (box.Transform is { } transform)
        {
            PageDrawTransformer.ApplyTransform(
                plan,
                BottomUpSpace.FlipVertical(transform, plan.Size.Height.Point),
                mark);
        }
    }
}
