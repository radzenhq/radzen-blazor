using Radzen.Documents.Fonts.Sfnt;

namespace Radzen.Documents.Fonts;

internal readonly struct PositionedGlyph(ushort glyphId, double advance, int cluster, SfntFont face)
{
    public ushort GlyphId { get; } = glyphId;

    public double Advance { get; } = advance;

    public int Cluster { get; } = cluster;

    public SfntFont Face { get; } = face;
}
