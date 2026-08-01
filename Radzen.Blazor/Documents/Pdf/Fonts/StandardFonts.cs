using System;
using Radzen.Documents.Fonts;

namespace Radzen.Documents.Pdf.Fonts;

internal static class StandardFonts
{
    // PDF 32000-1:2008, 9.6.2.2: the standard 14 fonts a conforming reader shall support without
    // embedding, named by their PostScript names.
    public static string PostScriptName(in CapturedBuiltInFace face)
        => face.Family switch
        {
            BuiltInFontFamily.Sans => Styled(face, "Helvetica", "-Bold", "-Oblique", "-BoldOblique"),
            BuiltInFontFamily.Monospace => Styled(face, "Courier", "-Bold", "-Oblique", "-BoldOblique"),
            BuiltInFontFamily.Serif => face.Bold && face.Italic ? "Times-BoldItalic"
                : face.Bold ? "Times-Bold"
                : face.Italic ? "Times-Italic"
                : "Times-Roman",
            BuiltInFontFamily.Symbol => "Symbol",
            BuiltInFontFamily.ZapfDingbats => "ZapfDingbats",
            _ => throw new ArgumentOutOfRangeException(nameof(face)),
        };

    private static string Styled(
        in CapturedBuiltInFace face,
        string family,
        string bold,
        string italic,
        string boldItalic)
        => face.Bold && face.Italic ? family + boldItalic
            : face.Bold ? family + bold
            : face.Italic ? family + italic
            : family;
}
