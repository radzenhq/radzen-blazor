using System.Collections.Immutable;
using Radzen.Documents.Fonts;
using Radzen.Documents.Pdf.Fonts;

namespace Radzen.Documents.Pdf.Render;

internal sealed class Base14GlyphEncoder(UnsupportedCharacterPolicy policy, UnsupportedCharacterLog unsupported)
{
    public byte[] Encode(
        ImmutableArray<CapturedGlyph> glyphs,
        CapturedBuiltInFace face)
    {
        var bytes = new byte[glyphs.Length];
        for (var i = 0; i < glyphs.Length; i++)
        {
            var codepoint = glyphs[i].Codepoint;
            if (codepoint <= char.MaxValue && WinAnsiEncoding.TryGetCode((char)codepoint, out var code))
            {
                bytes[i] = code;
                continue;
            }

            if (codepoint > char.MaxValue || !char.IsControl((char)codepoint))
            {
                unsupported.Record(codepoint, StandardFonts.PostScriptName(face));
                if (policy == UnsupportedCharacterPolicy.Throw)
                {
                    continue;
                }
            }

            WinAnsiEncoding.TryGetCode('?', out bytes[i]);
        }

        return bytes;
    }

}
