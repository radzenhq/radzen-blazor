using Radzen.Documents.Pdf.Fonts;
using Radzen.Documents.Pdf.Fonts.Sfnt;
using Radzen.Documents.Pdf.Objects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;

namespace Radzen.Documents.Pdf;

/// <summary>
/// Registers embeddable fonts, resolves font families to faces, and measures text.
/// </summary>
public sealed class FontCollection
{
    private const uint TtcTag = 0x74746366;
    private const int SignatureWindow = 64 * 1024;

    private readonly Dictionary<(string Family, bool Bold, bool Italic), SfntFont> registered = [];

    private readonly List<(string Family, bool Bold, bool Italic)> registrationOrder = [];

    private readonly List<string> fallback = [];

    private SimpleShaper? shaper;
    private bool shaperKerning;

    /// <summary>
    /// Gets or sets whether pair kerning is applied when measuring and drawing text.
    /// When <see langword="true"/> consecutive same-face glyphs are tightened by the
    /// font's kern data (sfnt <c>kern</c> table, base-14 AFM pairs). Defaults to
    /// <see langword="false"/> so output stays byte identical unless opted in.
    /// </summary>
    public bool EnableKerning { get; set; }

    /// <summary>
    /// Gets or sets whether a font whose OS/2 fsType marks it as Restricted License
    /// Embedding may still be embedded. Defaults to <see langword="false"/>, so
    /// registering such a font throws unless the caller explicitly opts in.
    /// </summary>
    public bool AllowRestrictedEmbedding { get; set; }

    /// <summary>
    /// Gets or sets whether a font that would render degraded - a variable font
    /// (embedded only at its default instance) or a color font (COLR/sbix/SVG,
    /// rendered monochrome) - may still be registered. Defaults to
    /// <see langword="false"/>, so registering such a font fails loudly rather
    /// than silently producing wrong output.
    /// </summary>
    public bool AllowDegradedFonts { get; set; }

    private static readonly Dictionary<(ulong Hash, int Length), WeakReference<ParsedSource>> parseCache = [];
    private static readonly ConditionalWeakTable<SfntFont, ParsedSource> faceRetention = [];

    private sealed class ParsedSource(byte[] data, bool isCollection, IReadOnlyList<SfntFont> faces)
    {
        public byte[] Data { get; } = data;

        public bool IsCollection { get; } = isCollection;

        public IReadOnlyList<SfntFont> Faces { get; } = faces;
    }

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

        var parsed = ParseSource(font);
        var face = parsed.IsCollection
            ? SelectCollectionFace(parsed.Faces, family, bold, italic)
            : parsed.Faces[0];

        // ISO 32000-1 9.9 / OS/2 fsType: a Restricted License Embedding font must not be embedded without a license.
        face.EnsureEmbeddable(AllowRestrictedEmbedding);
        face.EnsureRenderable(AllowDegradedFonts);
        var key = (family, bold, italic);
        if (!registered.ContainsKey(key))
        {
            registrationOrder.Add(key);
        }

        registered[key] = face;
    }

    private static ParsedSource ParseSource(Stream font)
    {
        if (font is MemoryStream ms)
        {
            if (ms.TryGetBuffer(out var segment) && segment.Array is { } array
                && segment.Offset == 0 && segment.Count == array.Length)
            {
                return FromBytes(array, sharedWithCaller: true);
            }

            var position = ms.Position;
            try
            {
                return FromMemoryStream(ms);
            }
            finally
            {
                ms.Position = position;
            }
        }

        return FromBytes(BufferStream(font, ReaderLimits.Default), sharedWithCaller: false);
    }

    internal static byte[] BufferStream(Stream font, ReaderLimits limits)
    {
        ArgumentNullException.ThrowIfNull(limits);
        return DocumentReader.ReadFully(font, limits.MaxFileBytes);
    }

    private static ParsedSource FromBytes(byte[] bytes, bool sharedWithCaller)
    {
        var length = bytes.Length;
        var window = Math.Min(length, SignatureWindow);
        var signature = (Signature(bytes.AsSpan(0, window), bytes.AsSpan(length - window)), length);

        lock (parseCache)
        {
            if (TryGetLive(signature, out var cached) && cached.Data.AsSpan().SequenceEqual(bytes))
            {
                return cached;
            }
        }

        return ParseAndStore(signature, sharedWithCaller ? [.. bytes] : bytes);
    }

    private static ParsedSource FromMemoryStream(MemoryStream ms)
    {
        var length = checked((int)ms.Length);
        var window = Math.Min(length, SignatureWindow);
        var buffer = new byte[window];

        ms.Position = 0;
        ms.ReadExactly(buffer);
        var hash = Signature(buffer, default);
        ms.Position = length - window;
        ms.ReadExactly(buffer);
        var signature = (Signature(default, buffer, hash), length);

        lock (parseCache)
        {
            if (TryGetLive(signature, out var cached) && ContentEquals(ms, cached.Data, buffer))
            {
                return cached;
            }
        }

        var bytes = new byte[length];
        ms.Position = 0;
        ms.ReadExactly(bytes);
        return ParseAndStore(signature, bytes);
    }

    private static ParsedSource ParseAndStore((ulong, int) signature, byte[] ownedBytes)
    {
        var parsed = ParseCopy(ownedBytes);

        lock (parseCache)
        {
            if (TryGetLive(signature, out var already) && already.Data.AsSpan().SequenceEqual(ownedBytes))
            {
                return already;
            }

            PruneDeadEntries();
            parseCache[signature] = new WeakReference<ParsedSource>(parsed);
            foreach (var face in parsed.Faces)
            {
                faceRetention.Add(face, parsed);
            }

            return parsed;
        }
    }

    private static bool TryGetLive((ulong, int) signature, out ParsedSource cached)
    {
        cached = null!;
        return parseCache.TryGetValue(signature, out var entry) && entry.TryGetTarget(out cached!);
    }

    private static bool ContentEquals(MemoryStream ms, byte[] data, byte[] buffer)
    {
        if (ms.Length != data.Length)
        {
            return false;
        }

        ms.Position = 0;
        var offset = 0;
        while (offset < data.Length)
        {
            var count = Math.Min(buffer.Length, data.Length - offset);
            ms.ReadExactly(buffer, 0, count);
            if (!buffer.AsSpan(0, count).SequenceEqual(data.AsSpan(offset, count)))
            {
                return false;
            }

            offset += count;
        }

        return true;
    }

    private static void PruneDeadEntries()
    {
        List<(ulong, int)>? dead = null;
        foreach (var pair in parseCache)
        {
            if (!pair.Value.TryGetTarget(out _))
            {
                (dead ??= []).Add(pair.Key);
            }
        }

        if (dead is not null)
        {
            foreach (var key in dead)
            {
                parseCache.Remove(key);
            }
        }
    }

    private static ulong Signature(ReadOnlySpan<byte> head, ReadOnlySpan<byte> tail, ulong hash = Fnv1a64.OffsetBasis)
        => Fnv1a64.Hash(tail, Fnv1a64.Hash(head, hash));

    private static ParsedSource ParseCopy(byte[] bytes)
        => new(bytes, IsCollection(bytes), SfntFont.ParseCollection(bytes));

    private static SfntFont SelectCollectionFace(IReadOnlyList<SfntFont> faces, string family, bool bold, bool italic)
    {
        SfntFont? named = null;
        foreach (var face in faces)
        {
            if (!string.Equals(face.FamilyName, family, StringComparison.Ordinal))
            {
                continue;
            }

            if (face.Bold == bold && face.Italic == italic)
            {
                return face;
            }

            named ??= face;
        }

        return named ?? throw new InvalidDataException($"No font face with family name '{family}' was found.");
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
    /// face and unmapped codepoints measured as the '?' substitute that emission draws. When
    /// <see cref="EnableKerning"/> is enabled, registered and base-14 fonts apply kerning.
    /// </summary>
    /// <param name="text">The text to measure.</param>
    /// <param name="font">The font to measure with.</param>
    /// <returns>The advance width in points.</returns>
    /// <exception cref="InvalidOperationException">The family is neither registered nor base-14.</exception>
    public double MeasureText(string text, Font font)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(font);
        SimpleShaper.EnsureNoComplexScript(text);

        if (TryResolvePrimary(font, out _))
        {
            return Shaper().MeasureAdvance(text, font);
        }

        var base14 = Base14Metrics.Resolve(font)
            ?? throw new InvalidOperationException($"No font is registered for family '{font.Name}'.");
        return MeasureBase14(text, font, base14);
    }

    internal SimpleShaper Shaper()
    {
        if (shaper is null || shaperKerning != EnableKerning)
        {
            shaper = new SimpleShaper(this, EnableKerning);
            shaperKerning = EnableKerning;
        }

        return shaper;
    }

    private double MeasureBase14(string text, Font font, Base14Metrics metrics)
    {
        WinAnsiEncoding.TryGetCode('?', out var question);
        double sum = 0;
        var i = 0;
        char? prevBase14 = null;
        while (i < text.Length)
        {
            var codepoint = CodePointAt(text, i);
            if (codepoint <= 0xFFFF && WinAnsiEncoding.TryGetCode((char)codepoint, out var code))
            {
                if (EnableKerning && prevBase14 is { } prev)
                {
                    sum += metrics.GetRunKerning(prev, (char)codepoint) * font.Size / 1000.0;
                }

                sum += metrics.GetWidth(code) * font.Size / 1000.0;
                prevBase14 = (char)codepoint;
            }
            else if (TryResolveFallbackGlyph(codepoint, out var face, out var glyph))
            {
                sum += face.GetAdvanceWidth(glyph) * font.Size / face.UnitsPerEm;
                prevBase14 = null;
            }
            else
            {
                sum += metrics.GetWidth(question) * font.Size / 1000.0;
                prevBase14 = null;
            }

            i += codepoint > 0xFFFF ? 2 : 1;
        }

        return sum;
    }

    internal static int CodePointAt(ReadOnlySpan<char> text, int index)
        => char.IsHighSurrogate(text[index]) && index + 1 < text.Length && char.IsLowSurrogate(text[index + 1])
            ? char.ConvertToUtf32(text[index], text[index + 1])
            : text[index];

    internal bool TryResolvePrimary(Font font, out SfntFont primary)
    {
        if (registered.TryGetValue((font.Name, font.Bold, font.Italic), out primary!))
        {
            return true;
        }

        return TryResolveFamily(font.Name, out primary);
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

        foreach (var key in registrationOrder)
        {
            if (string.Equals(key.Family, family, StringComparison.Ordinal))
            {
                face = registered[key];
                return true;
            }
        }

        return false;
    }

    private static bool IsCollection(byte[] data)
        => data.Length >= 4
            && ((uint)data[0] << 24 | (uint)data[1] << 16 | (uint)data[2] << 8 | data[3]) == TtcTag;
}
