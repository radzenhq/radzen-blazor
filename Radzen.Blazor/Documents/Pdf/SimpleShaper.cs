using System;
using System.Collections.Generic;

namespace Radzen.Documents.Pdf;

/// <summary>
/// A minimal left-to-right, horizontal text shaper: identity codepoint-to-glyph mapping via the
/// resolved font's cmap with per-character fallback, and hmtx-based advances. No kerning.
/// </summary>
/// <param name="fonts">The font collection used to resolve families and glyphs.</param>
public sealed class SimpleShaper(FontCollection fonts) : ITextShaper
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
        for (var i = 0; i < text.Length; i++)
        {
            var (face, glyph) = fonts.ResolveGlyph(primary, text[i]);
            var advance = face.GetAdvanceWidth(glyph) * font.Size / face.UnitsPerEm;
            glyphs.Add(new PositionedGlyph(glyph, advance, i));
            total += advance;
        }

        return new GlyphRun(glyphs, font, total);
    }
}
