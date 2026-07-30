using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Radzen.Documents.Fonts;
using Radzen.Documents.Pdf.Fonts;
using Radzen.Documents.Geometry;

namespace Radzen.Documents.Pdf.Emit;

internal sealed class Base14GlyphEncoder(bool allowUnsupportedCharacters)
{
    public byte[] Encode(ImmutableArray<CapturedBuiltInGlyph> glyphs, Font font)
    {
        var bytes = new byte[glyphs.Length];
        List<int>? unsupported = null;
        for (var i = 0; i < glyphs.Length; i++)
        {
            var codepoint = glyphs[i].Codepoint;
            if (codepoint <= char.MaxValue && WinAnsiEncoding.TryGetCode((char)codepoint, out var code))
            {
                bytes[i] = code;
                continue;
            }

            if ((codepoint <= char.MaxValue && char.IsControl((char)codepoint))
                || allowUnsupportedCharacters)
            {
                WinAnsiEncoding.TryGetCode('?', out bytes[i]);
                continue;
            }

            unsupported ??= [];
            if (!unsupported.Contains(codepoint))
            {
                unsupported.Add(codepoint);
            }
        }

        if (unsupported is { Count: > 0 })
        {
            throw UnsupportedCharacters(font, unsupported);
        }

        return bytes;
    }

    private static InvalidOperationException UnsupportedCharacters(Font font, List<int> codepoints)
    {
        const int MaxReported = 8;
        var offenders = new List<string>(Math.Min(codepoints.Count, MaxReported));
        for (var i = 0; i < codepoints.Count && i < MaxReported; i++)
        {
            var codepoint = codepoints[i];
            offenders.Add($"'{UnicodeCodePoint.ToString(codepoint)}' (U+{codepoint:X4})");
        }

        return new InvalidOperationException(
            $"The built-in font '{font.EffectiveFamily}' cannot draw {string.Join(", ", offenders)}: a base-14 font is limited "
            + "to the WinAnsi character set. Register a font that covers these characters with "
            + $"{nameof(FontCollection)}.{nameof(FontCollection.Register)}, add such a font to the "
            + $"{nameof(FontCollection.SetFallback)} chain, or set "
            + $"{nameof(FontCollection)}.{nameof(FontCollection.AllowUnsupportedCharacters)} to true to draw '?' in their place.");
    }
}
