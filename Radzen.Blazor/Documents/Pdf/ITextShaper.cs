using System;

namespace Radzen.Documents.Pdf;

/// <summary>
/// Shapes runs of text into positioned glyphs for a given font and layout orientation.
/// </summary>
public interface ITextShaper
{
    /// <summary>
    /// Shapes <paramref name="text"/> with <paramref name="font"/>.
    /// </summary>
    /// <param name="text">The characters to shape.</param>
    /// <param name="font">The font to shape with.</param>
    /// <param name="direction">The base flow direction.</param>
    /// <param name="mode">The writing mode.</param>
    /// <returns>The shaped glyph run.</returns>
    GlyphRun Shape(ReadOnlySpan<char> text, Font font, FlowDirection direction, WritingMode mode);
}
