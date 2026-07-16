namespace Radzen.Documents.Pdf.Content;

internal static class GlyphMetrics
{
    // The text-space advance one glyph contributes, unscaled by Tz. Replacement and
    // redaction adjustments are differences against the advances search computed, so all
    // three must apply the same formula. The caller resolves the glyph width, because a
    // missing width is a hard error when re-encoding but only an estimate for search.
    public static double Advance(double widthEm, double fontSize, double charSpacing, double wordSpacing, bool isWordSpace)
        => widthEm * fontSize + charSpacing + (isWordSpace ? wordSpacing : 0.0);
}
