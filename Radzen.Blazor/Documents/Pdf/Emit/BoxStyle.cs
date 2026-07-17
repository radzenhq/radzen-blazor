namespace Radzen.Documents.Pdf.Emit;

internal readonly record struct ResolvedEdge(Color Color, double Width, BorderStyle Style);

internal readonly struct BoxStyle
{
    public Color? Background { get; init; }

    public GradientBrush? BackgroundGradient { get; init; }
    public required Border Top { get; init; }
    public required Border Right { get; init; }
    public required Border Bottom { get; init; }
    public required Border Left { get; init; }
    public Unit CornerRadius { get; init; }
    public string? ExtGState { get; init; }

    public BoxShadow? Shadow { get; init; }

    public BlendMode? Blend { get; init; }
    public bool? OverprintStroke { get; init; }
    public bool? OverprintFill { get; init; }
    public int? OverprintMode { get; init; }
    public RenderingIntent? Intent { get; init; }

    public bool HasGraphicsStateOptions
        => Blend is not null || OverprintStroke is not null || OverprintFill is not null
            || OverprintMode is not null || Intent is not null;

    public static BoxStyle FromContainer(Container container) => new()
    {
        Background = container.Background,
        BackgroundGradient = container.BackgroundGradient,
        Top = container.Borders.Top,
        Right = container.Borders.Right,
        Bottom = container.Borders.Bottom,
        Left = container.Borders.Left,
        CornerRadius = container.CornerRadius,
        Shadow = container.Shadow,
        Blend = container.BlendMode,
        OverprintStroke = container.OverprintStroke,
        OverprintFill = container.OverprintFill,
        OverprintMode = container.OverprintMode,
        Intent = container.RenderingIntent,
    };

    public static ResolvedEdge? Resolve(Border edge)
    {
        var style = edge.Style;
        if (style == BorderStyle.None && edge.Width > 0)
        {
            style = BorderStyle.Solid;
        }

        if (style == BorderStyle.None)
        {
            return null;
        }

        return new ResolvedEdge(edge.Color, edge.Width > 0 ? edge.Width : 0.5, style);
    }

    public bool TryGetUniform(out ResolvedEdge uniform)
    {
        if (Resolve(Top) is { } edge
            && Resolve(Right) == edge
            && Resolve(Bottom) == edge
            && Resolve(Left) == edge)
        {
            uniform = edge;
            return true;
        }

        uniform = default;
        return false;
    }
}
