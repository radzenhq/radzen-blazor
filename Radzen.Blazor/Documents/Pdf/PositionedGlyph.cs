using Radzen.Documents.Pdf.Fonts.Sfnt;

namespace Radzen.Documents.Pdf;

// Advance is in points. Face is carried so emission can group glyphs by their owning
// embedded subset without re-resolving the cmap.
internal readonly struct PositionedGlyph(ushort glyphId, double advance, int cluster, SfntFont face)
{
    public ushort GlyphId { get; } = glyphId;

    public double Advance { get; } = advance;

    public int Cluster { get; } = cluster;

    public SfntFont Face { get; } = face;
}
