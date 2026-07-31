using System;
using System.Collections.Immutable;

namespace Radzen.Documents.LaidOut;

internal readonly record struct ResolvedEdge(Color Color, double Width, BorderStyle Style);

internal enum GradientPaintKind
{
    Linear,
    Radial,
}

internal readonly record struct GradientStopPaint(double Offset, Color Color);

internal readonly record struct GradientPaint(
    LaidOutNodeId Identity,
    GradientPaintKind Kind,
    double X0,
    double Y0,
    double R0,
    double X1,
    double Y1,
    double R1,
    ImmutableArray<GradientStopPaint> Stops);

internal readonly record struct BoxShadowPaint(
    Color Color,
    double BlurRadius,
    double OffsetX,
    double OffsetY,
    double Spread);

internal readonly struct BoxStyle
{
    public Color? Background { get; init; }

    public GradientPaint? BackgroundGradient { get; init; }
    public required ResolvedEdge? Top { get; init; }
    public required ResolvedEdge? Right { get; init; }
    public required ResolvedEdge? Bottom { get; init; }
    public required ResolvedEdge? Left { get; init; }
    public double CornerRadius { get; init; }

    public BoxShadowPaint? Shadow { get; init; }

    public BlendMode? Blend { get; init; }

    public bool HasGraphicsStateOptions => Blend is not null;

    public bool TryGetUniform(out ResolvedEdge uniform)
    {
        if (Top is { } edge
            && Right == edge
            && Bottom == edge
            && Left == edge)
        {
            uniform = edge;
            return true;
        }

        uniform = default;
        return false;
    }

    public static double ClampRadius(double radius, double width, double height)
    {
        if (radius <= 0 || width <= 0 || height <= 0)
        {
            return 0;
        }

        return Math.Min(radius, Math.Min(width, height) / 2);
    }
}
