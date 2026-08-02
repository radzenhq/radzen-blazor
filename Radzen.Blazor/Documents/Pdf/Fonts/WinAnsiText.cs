using System;
using System.Collections.Generic;

namespace Radzen.Documents.Pdf.Fonts;

internal enum OnUnencodable
{
    Substitute,

    Throw,
}

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
            }
        }

        return [.. bytes];
    }
}
