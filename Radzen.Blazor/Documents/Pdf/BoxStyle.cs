namespace Radzen.Documents.Pdf;

// A border edge resolved to the line it actually draws, or absent when nothing draws.
internal readonly record struct ResolvedEdge(Color Color, double Width, BorderStyle Style);

// A box's RESOLVED decoration: any border cascade (cell -> row -> table) has already
// picked one Border per edge, and opacity has already been registered upstream as a
// page ExtGState name.
internal readonly struct BoxStyle
{
    public Color? Background { get; init; }
    public required Border Top { get; init; }
    public required Border Right { get; init; }
    public required Border Bottom { get; init; }
    public required Border Left { get; init; }
    public Unit CornerRadius { get; init; }
    public string? ExtGState { get; init; }

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
