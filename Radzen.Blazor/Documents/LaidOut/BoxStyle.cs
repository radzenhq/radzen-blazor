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

internal readonly struct GradientReference
{
    private readonly double width;
    private readonly double height;
    private readonly bool sized;

    private GradientReference(double width, double height)
    {
        this.width = width;
        this.height = height;
        sized = true;
    }

    public static GradientReference Box(double width, double height) => new(width, height);

    public static GradientReference None => default;

    public double X(Unit value) => Resolve(value, width);

    public double Y(Unit value) => Resolve(value, height);

    public double Radius(Unit value) => Resolve(value, width);

    private double Resolve(Unit value, double extent) => sized ? value.Resolve(extent) : value.Point;
}

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
