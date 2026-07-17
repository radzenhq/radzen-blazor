using Radzen.Documents.Pdf.Fonts.Sfnt;

namespace Radzen.Documents.Pdf;

internal readonly struct PositionedGlyph(ushort glyphId, double advance, int cluster, SfntFont face)
{
    public ushort GlyphId { get; } = glyphId;

    public double Advance { get; } = advance;

    public int Cluster { get; } = cluster;

    public SfntFont Face { get; } = face;
}
