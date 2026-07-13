namespace Radzen.Documents.Pdf.Emit;

// A border edge resolved to the line it actually draws, or absent when nothing draws.
internal readonly record struct ResolvedEdge(Color Color, double Width, BorderStyle Style);

// A box's RESOLVED decoration: any border cascade (cell -> row -> table) has already
// picked one Border per edge, and opacity has already been registered upstream as a
// page ExtGState name.
internal readonly struct BoxStyle
{
    public Color? Background { get; init; }

    // A gradient background wins over the solid Background when set; realized as a
    // shading pattern selected with /Pattern cs + scn (opt-in, default null).
    public GradientBrush? BackgroundGradient { get; init; }
    public required Border Top { get; init; }
    public required Border Right { get; init; }
    public required Border Bottom { get; init; }
    public required Border Left { get; init; }
    public Unit CornerRadius { get; init; }
    public string? ExtGState { get; init; }

    // An optional blurred drop shadow painted underneath the box (via a luminosity soft mask).
    // Null (the default) paints no shadow and leaves output unchanged.
    public BoxShadow? Shadow { get; init; }

    // Optional blend/overprint/rendering-intent, folded into the box ExtGState alongside
    // opacity at emit time. All null (the default) leaves output unchanged.
    public BlendMode? Blend { get; init; }
    public bool? OverprintStroke { get; init; }
    public bool? OverprintFill { get; init; }
    public int? OverprintMode { get; init; }
    public RenderingIntent? Intent { get; init; }

    public bool HasGraphicsStateOptions
        => Blend is not null || OverprintStroke is not null || OverprintFill is not null
            || OverprintMode is not null || Intent is not null;

    // A container's decoration has no border cascade - its four edges are its own.
    // Opacity is registered as an ExtGState later, at emit time, when a PagePlan exists.
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

    // MigraDoc semantics: a positive width alone makes the edge a visible solid line,
    // and a visible edge without a width draws at 0.5pt.
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

    // True when all four edges resolve to the same visible line (same width, color
    // and style), which lets a rounded box stroke one rounded-rectangle path.
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
