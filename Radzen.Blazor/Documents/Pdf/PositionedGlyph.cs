using Radzen.Documents.Pdf.Fonts.Sfnt;

namespace Radzen.Documents.Pdf;

// A single shaped glyph produced by SimpleShaper: its glyph index within the owning
// face, horizontal advance in points, source cluster, and the physical face that
// supplies it (primary or a fallback face). Face is carried so emission groups glyphs
// by their owning embedded subset without re-resolving the cmap.
internal readonly struct PositionedGlyph(ushort glyphId, double advance, int cluster, SfntFont face)
{
    public ushort GlyphId { get; } = glyphId;

    public double Advance { get; } = advance;

    public int Cluster { get; } = cluster;

    public SfntFont Face { get; } = face;
}
