using System.Collections.Generic;

namespace Radzen.Documents.Pdf;

/// <summary>
/// The result of shaping a run of text: the positioned glyphs, the font used and the total advance.
/// </summary>
/// <param name="glyphs">The shaped glyphs in visual order.</param>
/// <param name="font">The font the run was shaped with.</param>
/// <param name="advance">The total advance width in points.</param>
public sealed class GlyphRun(IReadOnlyList<PositionedGlyph> glyphs, Font font, double advance)
{
    /// <summary>Gets the shaped glyphs in visual order.</summary>
    public IReadOnlyList<PositionedGlyph> Glyphs { get; } = glyphs;

    /// <summary>Gets the font the run was shaped with.</summary>
    public Font Font { get; } = font;

    /// <summary>Gets the total advance width in points.</summary>
    public double Advance { get; } = advance;
}
