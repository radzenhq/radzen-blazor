using System;
using System.Collections.Generic;

namespace Radzen.Documents.Fonts;

internal static class MissingGlyphMetrics
{
    private const int MaxReported = 8;

    public static bool IsReportable(int codepoint)
        => codepoint > 0xFFFF || !char.IsControl((char)codepoint);

    public static int Substituted(int codepoint) => IsReportable(codepoint) ? codepoint : '?';

    public static string Describe(int codepoint)
        => $"'{char.ConvertFromUtf32(codepoint)}' (U+{codepoint:X4})";

    public static InvalidOperationException Error(string family, IReadOnlyList<int> codepoints)
    {
        var offenders = new List<string>(Math.Min(codepoints.Count, MaxReported));
        for (var i = 0; i < codepoints.Count && i < MaxReported; i++)
        {
            offenders.Add(Describe(codepoints[i]));
        }

        return new InvalidOperationException(
            $"The built-in metrics font '{family}' has no glyph metrics for {string.Join(", ", offenders)}. "
            + $"Register a font that covers these characters with {nameof(FontCollection)}.{nameof(FontCollection.Register)}, "
            + $"or add such a font to the {nameof(FontCollection.SetFallback)} chain.");
    }
}
