using System.Collections.Generic;
using static Radzen.Documents.Pdf.Content.ContentOperands;
using Token = Radzen.Documents.Pdf.Content.ContentTokenizer.Token;

namespace Radzen.Documents.Pdf.Content;

internal static class GlyphMetrics
{
    public static double Advance(double widthEm, double fontSize, double charSpacing, double wordSpacing, bool isWordSpace)
        => widthEm * fontSize + charSpacing + (isWordSpace ? wordSpacing : 0.0);
}

internal struct TextSpacing()
{
    public double CharSpacing { get; private set; }

    public double WordSpacing { get; private set; }

    public double HorizontalScale { get; private set; } = 1.0;

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
