using System;

namespace Radzen.Documents.Pdf.Emit;

// Emits a first-class section-level container box: paints the box decoration through
// BoxRenderer, then delegates the laid-out child content to TableEmitter.EmitBoxContent
// so a box's content renders exactly like a table cell's (field resolution, overflow
// clipping and the rounded content clip included).
internal sealed class BoxEmitter(TableEmitter tables, OpacityResolver opacities)
{
    public void EmitBox(EmitContext context, in PositionedBox box, double left, double contentTop)
    {
        var plan = context.Plan;
        var mark = plan.Mark();
        var bounds = new Rect(
            left + box.Bounds.X,
            contentTop - box.Bounds.Y - box.Bounds.Height,
            box.Bounds.Width,
            box.Bounds.Height);

        // The container's OWN opacity (times any ancestor product), read directly
        // off the container rather than recovered through a child block.
        var opacity = opacities.ContainerOpacity(box.Source);
        var extGState = opacity < 1 || box.Style.HasGraphicsStateOptions
            ? plan.RegisterExtGState(
                opacity, opacity,
                box.Style.Blend, box.Style.OverprintStroke, box.Style.OverprintFill,
                box.Style.OverprintMode, box.Style.Intent)
            : null;
        // A rotated box bakes a page-space transform into its draws below; the shadow's soft
        // mask cannot survive that (its masked fill would degrade to a stroked edge), so a
        // rotated container carrying a shadow fails loud rather than silently dropping it.
        if (box.Transform is not null && box.Style.Shadow is not null)
        {
            throw new NotSupportedException(
                "A rotated box cannot preserve a box shadow; remove the shadow or the rotation.");
        }

        var style = box.Style with { ExtGState = extGState };
        BoxRenderer.Paint(plan, bounds, style);

        var radius = BoxRenderer.ClampRadius(box.Style.CornerRadius.Point, bounds.Width, bounds.Height);
        var innerWidth = Math.Max(0, box.Bounds.Width - (2 * box.Source.Padding.Point));

        // Container children are untagged (StructureTreeBuilder does not descend into
        // containers), so no structure element is passed down.
        tables.EmitBoxContent(
            context,
            box.Content.Lines, box.Content.Images, box.Content.Codes, box.Content.Tables, box.Content.Boxes,
            innerWidth, box.Bounds.X, box.Bounds.X + box.Bounds.Width,
            bounds, radius, opacity, null,
            left, contentTop, box.Y);

        // A rotated box bakes its page-space rotation into every draw it produced,
        // exactly like DocumentGenerator wraps a transformed table fragment.
        if (box.Transform is { } transform)
        {
            plan.ApplyTransform(transform, mark);
        }
    }
}
