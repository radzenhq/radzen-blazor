using System;
using System.Collections.Generic;
using Radzen.Documents.Pdf.Fonts.Sfnt;

namespace Radzen.Documents.Pdf;

/// <summary>
/// A minimal left-to-right, horizontal text shaper: identity codepoint-to-glyph mapping via the
/// resolved font's cmap with per-character fallback, and hmtx-based advances. Optional legacy
/// 'kern'-table pair kerning is applied to adjacent glyphs of the same face; it is off by default
/// so shaped advances match <see cref="FontCollection.MeasureText"/> unless kerning is requested.
/// </summary>
/// <param name="fonts">The font collection used to resolve families and glyphs.</param>
/// <param name="enableKerning">Whether to apply legacy 'kern'-table pair kerning to adjacent glyphs.</param>
public sealed class SimpleShaper(FontCollection fonts, bool enableKerning = false) : ITextShaper
{
    /// <inheritdoc/>
    public GlyphRun Shape(ReadOnlySpan<char> text, Font font, FlowDirection direction, WritingMode mode)
    {
        ArgumentNullException.ThrowIfNull(font);

        if (direction != FlowDirection.LeftToRight || mode != WritingMode.HorizontalTopToBottom)
        {
            throw new NotSupportedException("SimpleShaper supports only left-to-right, horizontal text.");
        }

        var primary = fonts.ResolvePrimarySfnt(font);

        var glyphs = new List<PositionedGlyph>(text.Length);
        double total = 0;
        SfntFont? previousFace = null;
        ushort previousGlyph = 0;
        var i = 0;
        while (i < text.Length)
        {
            var codepoint = FontCollection.CodePointAt(text, i);
            var (face, glyph) = fonts.ResolveGlyph(primary, codepoint);
            var advance = face.GetAdvanceWidth(glyph) * font.Size / face.UnitsPerEm;

            // Kerning adjusts the space between the previous glyph and this one, so it is
            // folded into the previous glyph's advance. It applies only within a single face.
            if (enableKerning && ReferenceEquals(previousFace, face) && glyphs.Count > 0)
            {
                var kern = previousFace!.GetKerning(previousGlyph, glyph) * font.Size / previousFace.UnitsPerEm;
                if (kern != 0)
                {
                    var last = glyphs.Count - 1;
                    var previous = glyphs[last];
                    glyphs[last] = new PositionedGlyph(previous.GlyphId, previous.Advance + kern, previous.Cluster);
                    total += kern;
                }
            }

            glyphs.Add(new PositionedGlyph(glyph, advance, i));
            total += advance;
            previousFace = face;
            previousGlyph = glyph;
            i += codepoint > 0xFFFF ? 2 : 1;
        }

        return new GlyphRun(glyphs, font, total);
    }
}
