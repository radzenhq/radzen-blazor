using System.Collections.Generic;
using static Radzen.Documents.Pdf.Content.ContentOperands;
using Token = Radzen.Documents.Pdf.Content.ContentTokenizer.Token;

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

// The spacing operands feeding GlyphMetrics.Advance. Standalone Tc/Tw/Tz are not the only
// source: the " show operator carries its own aw/ac, so a walk that reads only Tc/Tw drifts
// out of step with one that does not, and their advances then disagree.
internal struct TextSpacing()
{
    public double CharSpacing { get; private set; }

    public double WordSpacing { get; private set; }

    public double HorizontalScale { get; private set; } = 1.0;

    // Applies op's spacing effect, if it has one. Call before showing " so its aw/ac reach
    // the string it shows.
    public bool Apply(string? op, List<Token> operands)
    {
        switch (op)
        {
            case "Tc":
                CharSpacing = LastNumber(operands);
                return true;
            case "Tw":
                WordSpacing = LastNumber(operands);
                return true;
            case "Tz":
                HorizontalScale = LastNumber(operands) / 100.0;
                return true;
            case "\"":
                WordSpacing = Number(operands, 0);
                CharSpacing = Number(operands, 1);
                return true;
            default:
                return false;
        }
    }
}
