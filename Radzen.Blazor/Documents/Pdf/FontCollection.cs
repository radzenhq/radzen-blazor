using Radzen.Documents.Pdf.Fonts;
using Radzen.Documents.Pdf.Fonts.Sfnt;
using System;
using System.Collections.Generic;
using System.IO;

namespace Radzen.Documents.Pdf;

/// <summary>
/// Registers embeddable fonts, resolves font families to faces, and measures text.
/// </summary>
public sealed class FontCollection
{
    private const uint TtcTag = 0x74746366; // 'ttcf'

    private readonly Dictionary<(string Family, bool Bold, bool Italic), SfntFont> registered = [];
    private readonly List<string> fallback = [];

    /// <summary>
    /// Registers a font as the regular face of the given family. The stream is buffered fully so
    /// it may be closed immediately after. For a TrueType collection the face whose internal
    /// family name matches <paramref name="family"/> is selected; a plain font is registered
    /// under the key regardless of its internal name. Registering an existing face overwrites it.
    /// </summary>
    /// <param name="family">The family key to register the font under.</param>
    /// <param name="font">A stream containing the font data.</param>
    public void Register(string family, Stream font) => Register(family, font, bold: false, italic: false);

    /// <summary>
    /// Registers a font as the face of the given family with the specified style. Runs whose
    /// <see cref="Font.Bold"/>/<see cref="Font.Italic"/> flags match the registered style use this
    /// face; when no styled face exists the regular face of the family is used instead.
    /// </summary>
    /// <param name="family">The family key to register the font under.</param>
    /// <param name="font">A stream containing the font data.</param>
    /// <param name="bold">Whether this face is the bold face of the family.</param>
    /// <param name="italic">Whether this face is the italic face of the family.</param>
    public void Register(string family, Stream font, bool bold, bool italic)
    {
        ArgumentNullException.ThrowIfNull(family);
        ArgumentNullException.ThrowIfNull(font);

        var bytes = ReadFully(font);
        registered[(family, bold, italic)] = IsCollection(bytes)
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
    /// Measures the width of <paramref name="text"/> in points at <paramref name="font"/>'s size,
    /// iterating Unicode codepoints. A registered family (matched by <see cref="Font.Name"/> and
    /// style) is measured from its hmtx advances with per-codepoint fallback; a base-14 font is
    /// measured from its WinAnsi widths, with fallback-served codepoints measured by the fallback
    /// face and unmapped codepoints measured as the '?' substitute that emission draws. No
    /// kerning is applied.
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
            var i = 0;
            while (i < text.Length)
            {
                var codepoint = CodePointAt(text, i);
                var (face, glyph) = ResolveGlyph(primary, codepoint);
                sum += face.GetAdvanceWidth(glyph) * font.Size / face.UnitsPerEm;
                i += codepoint > 0xFFFF ? 2 : 1;
            }

            return sum;
        }

        var base14 = Base14Metrics.Resolve(font)
            ?? throw new InvalidOperationException($"No font is registered for family '{font.Name}'.");
        return MeasureBase14(text, font, base14);
    }

    // Mirrors DocumentGenerator.EmitBase14Fragment: WinAnsi codepoints use base-14
    // widths, non-WinAnsi codepoints served by the fallback chain use the fallback
    // face's advances, and anything else measures as the '?' substitute.
    private double MeasureBase14(string text, Font font, Base14Metrics metrics)
    {
        WinAnsiEncoding.TryGetCode('?', out var question);
        double sum = 0;
        var i = 0;
        while (i < text.Length)
        {
            var codepoint = CodePointAt(text, i);
            if (codepoint <= 0xFFFF && WinAnsiEncoding.TryGetCode((char)codepoint, out var code))
            {
                sum += metrics.GetWidth(code) * font.Size / 1000.0;
            }
            else if (TryResolveFallbackGlyph(codepoint, out var face, out var glyph))
            {
                sum += face.GetAdvanceWidth(glyph) * font.Size / face.UnitsPerEm;
            }
            else
            {
                sum += metrics.GetWidth(question) * font.Size / 1000.0;
            }

            i += codepoint > 0xFFFF ? 2 : 1;
        }

        return sum;
    }

    // A lone surrogate yields its own code unit so it resolves without throwing.
    internal static int CodePointAt(ReadOnlySpan<char> text, int index)
        => char.IsHighSurrogate(text[index]) && index + 1 < text.Length && char.IsLowSurrogate(text[index + 1])
            ? char.ConvertToUtf32(text[index], text[index + 1])
            : text[index];

    // Resolves style-aware: exact (family, bold, italic) face first, then the regular
    // face of the family, then any registered face of the family.
    internal bool TryResolvePrimary(Font font, out SfntFont primary)
    {
        if (registered.TryGetValue((font.Name, font.Bold, font.Italic), out primary!))
        {
            return true;
        }

        return (font.Bold || font.Italic) && TryResolveFamily(font.Name, out primary);
    }

    internal SfntFont ResolvePrimarySfnt(Font font)
        => TryResolvePrimary(font, out var primary)
            ? primary
            : throw new InvalidOperationException($"No font is registered for family '{font.Name}'.");

    internal (SfntFont Face, ushort GlyphId) ResolveGlyph(SfntFont primary, int c)
    {
        var glyph = primary.GetGlyphId(c);
        if (glyph != 0)
        {
            return (primary, glyph);
        }

        return TryResolveFallbackGlyph(c, out var face, out var candidate)
            ? (face, candidate)
            : (primary, (ushort)0);
    }

    // Walks the fallback chain only, returning the first face that maps the codepoint
    // to a non-notdef glyph.
    internal bool TryResolveFallbackGlyph(int c, out SfntFont face, out ushort glyph)
    {
        foreach (var name in fallback)
        {
            if (TryResolveFamily(name, out face!))
            {
                glyph = face.GetGlyphId(c);
                if (glyph != 0)
                {
                    return true;
                }
            }
        }

        face = null!;
        glyph = 0;
        return false;
    }

    private bool TryResolveFamily(string family, out SfntFont face)
    {
        if (registered.TryGetValue((family, false, false), out face!))
        {
            return true;
        }

        foreach (var pair in registered)
        {
            if (string.Equals(pair.Key.Family, family, StringComparison.Ordinal))
            {
                face = pair.Value;
                return true;
            }
        }

        return false;
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
