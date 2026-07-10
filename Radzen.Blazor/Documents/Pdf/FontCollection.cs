using System;
using System.Collections.Generic;
using System.IO;
using Radzen.Documents.Pdf.Fonts;
using Radzen.Documents.Pdf.Fonts.Sfnt;

namespace Radzen.Documents.Pdf;

/// <summary>
/// Registers embeddable fonts, resolves font families to faces, and measures text.
/// </summary>
public sealed class FontCollection
{
    private const uint TtcTag = 0x74746366; // 'ttcf'

    private readonly Dictionary<string, SfntFont> registered = new(StringComparer.Ordinal);
    private readonly List<string> fallback = [];

    /// <summary>
    /// Registers a font under the given family key. The stream is buffered fully so it may be
    /// closed immediately after. For a TrueType collection the face whose internal family name
    /// matches <paramref name="family"/> is selected; a plain font is registered under the key
    /// regardless of its internal name. Registering an existing family overwrites it.
    /// </summary>
    /// <param name="family">The family key to register the font under.</param>
    /// <param name="font">A stream containing the font data.</param>
    public void Register(string family, Stream font)
    {
        ArgumentNullException.ThrowIfNull(family);
        ArgumentNullException.ThrowIfNull(font);

        var bytes = ReadFully(font);
        registered[family] = IsCollection(bytes)
            ? SfntFont.Parse(bytes, family)
            : SfntFont.Parse(bytes);
    }

    /// <summary>
    /// Declares an ordered fallback chain of registered families. When the primary font lacks a
    /// glyph for a codepoint, the chain is walked and the first family that maps the codepoint to
    /// a non-notdef glyph is used.
    /// </summary>
    /// <param name="families">The registered family names, in fallback order.</param>
    public void SetFallback(params string[] families)
    {
        ArgumentNullException.ThrowIfNull(families);
        fallback.Clear();
        fallback.AddRange(families);
    }

    /// <summary>
    /// Measures the width of <paramref name="text"/> in points at <paramref name="font"/>'s size.
    /// A registered family (matched by exact <see cref="Font.Name"/>) is measured from its hmtx
    /// advances with per-character fallback; otherwise a base-14 font is measured. No kerning is
    /// applied.
    /// </summary>
    /// <param name="text">The text to measure.</param>
    /// <param name="font">The font to measure with.</param>
    /// <returns>The advance width in points.</returns>
    /// <exception cref="InvalidOperationException">The family is neither registered nor base-14.</exception>
    public double MeasureText(string text, Font font)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(font);

        if (TryResolvePrimary(font, out var primary))
        {
            double sum = 0;
            foreach (var c in text)
            {
                var (face, glyph) = ResolveGlyph(primary, c);
                sum += face.GetAdvanceWidth(glyph) * font.Size / face.UnitsPerEm;
            }

            return sum;
        }

        var base14 = Base14Metrics.Resolve(font);
        return base14 != null
            ? base14.MeasureString(text, font.Size)
            : throw new InvalidOperationException($"No font is registered for family '{font.Name}'.");
    }

    internal bool TryResolvePrimary(Font font, out SfntFont primary)
        => registered.TryGetValue(font.Name, out primary!);

    internal SfntFont ResolvePrimarySfnt(Font font)
        => TryResolvePrimary(font, out var primary)
            ? primary
            : throw new InvalidOperationException($"No font is registered for family '{font.Name}'.");

    internal (SfntFont Face, ushort GlyphId) ResolveGlyph(SfntFont primary, char c)
    {
        var glyph = primary.GetGlyphId(c);
        if (glyph != 0)
        {
            return (primary, glyph);
        }

        foreach (var name in fallback)
        {
            if (registered.TryGetValue(name, out var face))
            {
                var candidate = face.GetGlyphId(c);
                if (candidate != 0)
                {
                    return (face, candidate);
                }
            }
        }

        return (primary, 0);
    }

    private static bool IsCollection(byte[] data)
        => data.Length >= 4
            && ((uint)data[0] << 24 | (uint)data[1] << 16 | (uint)data[2] << 8 | data[3]) == TtcTag;

    private static byte[] ReadFully(Stream stream)
    {
        if (stream is MemoryStream ms)
        {
            return ms.ToArray();
        }

        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return buffer.ToArray();
    }
}
