using System;

namespace Radzen.Documents.Pdf.Emit;

internal sealed class BoxEmitter(TableEmitter tables, OpacityResolver opacities)
{
    public void EmitBox(EmitContext context, in PositionedBox box, double left, double contentTop)
    {
        var plan = context.Plan;
        var mark = plan.Mark();
        var bounds = PdfRect.FromSize(
            left + box.Bounds.X,
            contentTop - box.Bounds.Y - box.Bounds.Height,
            box.Bounds.Width,
            box.Bounds.Height);

        if (box.Transform is not null && box.Style.Shadow is not null)
        {
            throw new NotSupportedException(
                "A rotated box cannot preserve a box shadow; remove the shadow or the rotation.");
        }

        var opacity = ContainerDecoration.Paint(plan, opacities, bounds, box.Source, box.Style);

        var radius = BoxRenderer.ClampRadius(box.Style.CornerRadius.Point, bounds.Width, bounds.Height);
        var innerWidth = Math.Max(0, box.Bounds.Width - (2 * box.Source.Padding.Point));

        tables.EmitBoxContent(
            context,
            box.Content,
            innerWidth, box.Bounds.X, box.Bounds.X + box.Bounds.Width,
            bounds, radius, opacity, null,
            left, contentTop, box.Y);

        if (box.Transform is { } transform)
        {
            plan.ApplyTransform(transform, mark);
        }
    }
}
