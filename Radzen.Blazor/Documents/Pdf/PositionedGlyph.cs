namespace Radzen.Documents.Pdf;

/// <summary>
/// A single shaped glyph with its advance and source cluster.
/// </summary>
/// <param name="glyphId">The glyph index within its font face.</param>
/// <param name="advance">The horizontal advance in points.</param>
/// <param name="cluster">The index of the source character that produced this glyph.</param>
public readonly struct PositionedGlyph(ushort glyphId, double advance, int cluster)
{
    /// <summary>Gets the glyph index within its font face.</summary>
    public ushort GlyphId { get; } = glyphId;

    /// <summary>Gets the horizontal advance in points.</summary>
    public double Advance { get; } = advance;

    /// <summary>Gets the index of the source character that produced this glyph.</summary>
    public int Cluster { get; } = cluster;
}
