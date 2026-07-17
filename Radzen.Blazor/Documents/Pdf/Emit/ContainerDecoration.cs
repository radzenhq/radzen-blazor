namespace Radzen.Documents.Pdf.Emit;

// Paints a container box's resolved decoration - shadow, background (solid or gradient),
// border and corner radius - folding the container's own opacity and any blend/overprint/
// intent into one ExtGState. Shared by the top-level box path and the nested-box path so
// both honour every BoxStyle field identically. Returns the resolved opacity so the caller
// can carry it into content emission.
internal static class ContainerDecoration
{
    public static double Paint(PagePlan plan, OpacityResolver opacities, in PdfRect bounds, Container source, in BoxStyle style)
    {
        var opacity = opacities.ContainerOpacity(source);
        var extGState = opacity < 1 || style.HasGraphicsStateOptions
            ? plan.RegisterExtGState(
                opacity, opacity,
                style.Blend, style.OverprintStroke, style.OverprintFill,
                style.OverprintMode, style.Intent)
            : null;
        BoxRenderer.Paint(plan, bounds, style with { ExtGState = extGState });
        return opacity;
    }
}
