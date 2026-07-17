namespace Radzen.Documents.Pdf.Emit;

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
