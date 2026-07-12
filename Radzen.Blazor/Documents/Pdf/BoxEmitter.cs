namespace Radzen.Documents.Pdf;

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

        // The container's OWN opacity (times any ancestor product), not recovered
        // through a child block like the lowered-table path.
        var opacity = opacities.ContainerOpacity(box.Source);
        var extGState = opacity < 1 || box.Style.HasGraphicsStateOptions
            ? plan.RegisterExtGState(
                opacity, opacity,
                box.Style.Blend, box.Style.OverprintStroke, box.Style.OverprintFill,
                box.Style.OverprintMode, box.Style.Intent)
            : null;
        var style = box.Style with { ExtGState = extGState };
        BoxRenderer.Paint(plan, bounds, style);

        var radius = BoxRenderer.ClampRadius(box.Style.CornerRadius.Point, bounds.Width, bounds.Height);
        var innerWidth = System.Math.Max(0, box.Bounds.Width - (2 * box.Source.Padding.Point));

        // Container children are untagged (StructureTreeBuilder does not descend into
        // containers), matching the lowered path where the synthetic cell had no element.
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
