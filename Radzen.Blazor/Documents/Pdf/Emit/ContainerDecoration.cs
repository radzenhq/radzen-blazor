using Radzen.Documents.Geometry;

namespace Radzen.Documents.Pdf.Emit;

internal static class ContainerDecoration
{
    public static void Paint(PagePlan plan, in PdfRect bounds, double opacity, in BoxStyle style)
    {
        var extGState = opacity < 1 || style.HasGraphicsStateOptions
            ? plan.RegisterExtGState(opacity, opacity, style.Blend)
            : null;
        BoxRenderer.Paint(plan, bounds, style, extGState);
    }
}
