using Radzen.Documents.Geometry;

namespace Radzen.Documents.Pdf.Render;

internal static class BoxRenderer
{
    public static void Paint(PagePlan plan, PdfRect bounds, in BoxStyle style, string? extGState)
    {
        var radius = BoxStyle.ClampRadius(style.CornerRadius, bounds.Width, bounds.Height);

        if (style.Shadow is { } shadow)
        {
            SoftMask.EmitBoxShadow(plan, bounds, radius, shadow);
        }

        if (style.BackgroundGradient is { } gradient)
        {
            plan.Fills.Add(new FillDraw
            {
                X = bounds.Left,
                Y = bounds.Bottom,
                Width = bounds.Width,
                Height = bounds.Height,
                Color = style.Background ?? Color.Black,
                Radius = radius,
                ExtGState = extGState,
                Gradient = gradient,
            });
        }
        else if (style.Background is { } background)
        {
            plan.Fills.Add(new FillDraw
            {
                X = bounds.Left,
                Y = bounds.Bottom,
                Width = bounds.Width,
                Height = bounds.Height,
                Color = background,
                Radius = radius,
                ExtGState = extGState,
            });
        }

        var x = bounds.Left;
        var bottom = bounds.Bottom;
        var right = x + bounds.Width;
        var top = bottom + bounds.Height;

        if (radius > 0 && style.TryGetUniform(out var uniform))
        {
            plan.RoundedStrokes.Add(new RoundedStrokeDraw
            {
                X = x,
                Y = bottom,
                Width = bounds.Width,
                Height = bounds.Height,
                Radius = radius,
                LineWidth = uniform.Width,
                Color = uniform.Color,
                Style = uniform.Style,
                ExtGState = extGState,
            });
            return;
        }

        EmitEdge(plan, style.Top, x, top, right, top, extGState);
        EmitEdge(plan, style.Right, right, bottom, right, top, extGState);
        EmitEdge(plan, style.Bottom, x, bottom, right, bottom, extGState);
        EmitEdge(plan, style.Left, x, bottom, x, top, extGState);
    }

    private static void EmitEdge(PagePlan plan, ResolvedEdge? border, double x1, double y1, double x2, double y2, string? extGState)
    {
        if (border is not { } edge)
        {
            return;
        }

        plan.Edges.Add(new EdgeDraw
        {
            X1 = x1,
            Y1 = y1,
            X2 = x2,
            Y2 = y2,
            LineWidth = edge.Width,
            Color = edge.Color,
            Style = edge.Style,
            ExtGState = extGState,
        });
    }
}
