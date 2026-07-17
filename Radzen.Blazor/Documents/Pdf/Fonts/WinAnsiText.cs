using System;
using System.Collections.Generic;

namespace Radzen.Documents.Pdf.Fonts;

/// <summary>
/// What <see cref="WinAnsiText.Encode"/> does with a character the WinAnsi encoding
/// cannot represent.
/// </summary>
internal enum OnUnencodable
{
    /// <summary>Emit the '?' glyph in its place.</summary>
    Substitute,

    /// <summary>Throw a <see cref="NotSupportedException"/> naming the character.</summary>
    Throw,

    /// <summary>Emit nothing for it. Only safe when the caller has already pre-filtered.</summary>
    Drop,
}

// The one WinAnsi encoder. The policy for an unencodable character is a parameter, so a
// caller must name the one it wants rather than reach for a helper that silently drops.
internal static class WinAnsiText
{
    public static bool CanEncode(string text)
    {
        foreach (var c in text)
        {
            if (!WinAnsiEncoding.CanEncode(c))
            {
                return false;
            }
        }

        return true;
    }

    // context names the feature in the OnUnencodable.Throw message.
    public static byte[] Encode(string text, OnUnencodable onUnencodable, string context = "Text")
    {
        var bytes = new List<byte>(text.Length);
        foreach (var c in text)
        {
            if (WinAnsiEncoding.TryGetCode(c, out var code))
            {
                bytes.Add(code);
                continue;
            }

            switch (onUnencodable)
            {
                case OnUnencodable.Substitute:
                    WinAnsiEncoding.TryGetCode('?', out var question);
                    bytes.Add(question);
                    break;
                case OnUnencodable.Throw:
                    throw new NotSupportedException(
                        $"{context} contains a character (U+{(int)c:X4}) not representable in the base-14 WinAnsi encoding; register a font that covers it.");
                case OnUnencodable.Drop:
                    break;
            }
        }

        return [.. bytes];
    }
}
