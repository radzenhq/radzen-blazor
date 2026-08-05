using System;
using System.Collections.Generic;
using System.Text;

namespace Radzen.Documents.Pdf;

/// <summary>
/// What rendering does when text contains a character that neither its font nor any
/// registered fallback can draw.
/// </summary>
public enum UnsupportedCharacterPolicy
{
    /// <summary>
    /// Rendering fails with an exception naming every uncovered character and the font that
    /// missed it. The default.
    /// </summary>
    Throw,

    /// <summary>
    /// Rendering succeeds: an embedded font draws its missing-glyph shape (<c>.notdef</c>)
    /// and a built-in font draws '?'. Each substituted character is reported through
    /// <see cref="DocumentRenderer.UnsupportedCharacterFound"/> when a callback is set,
    /// and stays extractable as U+FFFD.
    /// </summary>
    Substitute,
}

/// <summary>
/// A character that the fonts of a rendered document could not draw.
/// </summary>
/// <param name="Codepoint">The Unicode code point.</param>
/// <param name="FontFamily">The family of the font that was asked to draw it.</param>
public readonly record struct UnsupportedCharacter(int Codepoint, string FontFamily)
{
    /// <summary>Gets the character as a string, or U+FFFD when the code point is not scalar.</summary>
    public string Character => Codepoint is >= 0 and <= 0x10FFFF and not (>= 0xD800 and <= 0xDFFF)
        ? char.ConvertFromUtf32(Codepoint)
        : "�";
}

internal sealed class UnsupportedCharacterLog
{
    private const int MaxReported = 8;

    private readonly List<UnsupportedCharacter> entries = [];
    private readonly HashSet<UnsupportedCharacter> seen = [];

    public IReadOnlyList<UnsupportedCharacter> Entries => entries;

    public void Record(int codepoint, string fontFamily)
    {
        var entry = new UnsupportedCharacter(codepoint, fontFamily);
        if (seen.Add(entry))
        {
            entries.Add(entry);
        }
    }

    public void ThrowIfAny()
    {
        if (entries.Count == 0)
        {
            return;
        }

        var message = new StringBuilder("The document uses characters its fonts cannot draw: ");
        for (var i = 0; i < entries.Count && i < MaxReported; i++)
        {
            var entry = entries[i];
            if (i > 0)
            {
                message.Append(", ");
            }

            message.Append(System.Globalization.CultureInfo.InvariantCulture,
                $"'{entry.Character}' (U+{entry.Codepoint:X4}) in font '{entry.FontFamily}'");
        }

        if (entries.Count > MaxReported)
        {
            message.Append(System.Globalization.CultureInfo.InvariantCulture,
                $" and {entries.Count - MaxReported} more");
        }

        message.Append(". Register a font that covers these characters with "
            + $"{nameof(Radzen.Documents.Fonts.FontCollection)}.{nameof(Radzen.Documents.Fonts.FontCollection.Register)}, add one to the "
            + $"{nameof(Radzen.Documents.Fonts.FontCollection)}.{nameof(Radzen.Documents.Fonts.FontCollection.SetFallback)} chain, or set "
            + $"{nameof(DocumentRenderer)}.{nameof(DocumentRenderer.UnsupportedCharacters)} to "
            + $"{nameof(UnsupportedCharacterPolicy)}.{nameof(UnsupportedCharacterPolicy.Substitute)} to draw "
            + "a substitute in their place.");

        throw new InvalidOperationException(message.ToString());
    }
}
