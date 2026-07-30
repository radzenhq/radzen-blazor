using System;
using Radzen.Documents.LaidOut;

namespace Radzen.Documents.Pdf.Render;

internal sealed class BoxPlanner(TablePlanner tables)
{
    public void EmitBox(EmitContext context, in LaidOutBox box, double left, double contentTop)
    {
        var plan = context.Plan;
        var mark = plan.Mark();
        var bounds = PageSpace.Bounds(left, contentTop, box.Bounds);
        var artifact = tables.ArtifactOf(box.Source);

        if (box.Transform is not null && box.Style.Shadow is not null)
        {
            throw new NotSupportedException(
                "A rotated box cannot preserve a box shadow; remove the shadow or the rotation.");
        }

        var opacity = box.Opacity;
        BoxDecorationPlanner.Paint(
            plan,
            bounds,
            opacity,
            box.Style,
            SemanticArtifacts.ForDecoration(artifact));

        var radius = BoxStyle.ClampRadius(box.Style.CornerRadius, bounds.Width, bounds.Height);
        var innerWidth = Math.Max(0, box.Bounds.Width - box.Padding.Horizontal);

        tables.EmitBoxContent(
            context,
            box.Content,
            innerWidth, box.Bounds.X, box.Bounds.X + box.Bounds.Width,
            bounds, radius, opacity, null,
            left, contentTop, box.Bounds.Y, artifact);

        if (box.Transform is { } transform)
        {
            plan.ApplyTransform(PageSpace.Flip(transform, plan.Size.Height.Point), mark);
        }
    }
}
