using System;

namespace Radzen.Documents.Pdf.Emit;

internal static class BoxRenderer
{
    public static void Paint(PagePlan plan, PdfRect bounds, in BoxStyle style)
    {
        var radius = ClampRadius(style.CornerRadius.Point, bounds.Width, bounds.Height);

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
                ExtGState = style.ExtGState,
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
                ExtGState = style.ExtGState,
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
                ExtGState = style.ExtGState,
            });
            return;
        }

        EmitEdge(plan, style.Top, x, top, right, top, style.ExtGState);
        EmitEdge(plan, style.Right, right, bottom, right, top, style.ExtGState);
        EmitEdge(plan, style.Bottom, x, bottom, right, bottom, style.ExtGState);
        EmitEdge(plan, style.Left, x, bottom, x, top, style.ExtGState);
    }

    public static double ClampRadius(double radius, double width, double height)
    {
        if (radius <= 0 || width <= 0 || height <= 0)
        {
            return 0;
        }

        return Math.Min(radius, Math.Min(width, height) / 2);
    }

    private static void EmitEdge(PagePlan plan, Border border, double x1, double y1, double x2, double y2, string? extGState)
    {
        if (BoxStyle.Resolve(border) is not { } edge)
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
