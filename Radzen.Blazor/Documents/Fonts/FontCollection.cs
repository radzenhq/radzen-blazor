using Radzen.Documents.Fonts.Sfnt;
using System;
using System.Collections.Immutable;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using Radzen.Documents.Geometry;

namespace Radzen.Documents.Fonts;

internal enum BuiltInGlyphKind
{
    BuiltIn,
    Fallback,
    Missing,
}

internal readonly record struct RegisteredFace(
    string Family,
    bool Bold,
    bool Italic,
    SfntFont Face,
    FontSourceData Source,
    int FaceIndex);

internal sealed class FontSourceData(byte[] bytes)
{
    internal ReadOnlyMemory<byte> Memory { get; } = bytes;
}

internal readonly record struct FontCollectionSnapshot(
    ImmutableArray<RegisteredFace> Faces,
    ImmutableArray<string> Fallback,
    bool EnableKerning,
    bool AllowRestrictedEmbedding,
    bool AllowDegradedFonts,
    bool AllowUnsupportedCharacters)
{
    internal bool HasFamily(string family)
    {
        foreach (var face in Faces)
        {
            if (string.Equals(face.Family, family, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}

/// <summary>
/// Registers embeddable fonts, resolves font families to faces, and measures text.
/// </summary>
public sealed class FontCollection
{
    private const int SignatureWindow = 64 * 1024;

    private readonly Dictionary<(string Family, bool Bold, bool Italic), SfntFont> registered = [];

    private readonly List<(string Family, bool Bold, bool Italic)> registrationOrder = [];

    private readonly List<string> fallback = [];

    private SimpleShaper? shaper;
    private bool shaperKerning;

    /// <summary>
    /// Gets or sets whether pair kerning is applied when measuring and drawing text.
    /// When <see langword="true"/> consecutive same-face glyphs are tightened by the
    /// font's kern data (sfnt <c>kern</c> table or built-in AFM pairs). Defaults to
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

    /// <summary>
    /// Gets or sets whether renderers may substitute '?' when a glyph captured from a built-in
    /// metrics font cannot be represented by the output format. Defaults to
    /// <see langword="false"/>, so the renderer throws and names the offending characters.
    /// </summary>
    public bool AllowUnsupportedCharacters { get; set; }

    private static readonly Dictionary<(ulong Hash, int Length), WeakReference<ParsedSource>> parseCache = [];
    private static readonly ConditionalWeakTable<SfntFont, ParsedSource> faceRetention = [];

    private sealed class ParsedSource(byte[] data, bool isCollection, IReadOnlyList<SfntFont> faces)
    {
        public byte[] Data { get; } = data;

        public FontSourceData Source { get; } = new(data);

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
            ? SfntFont.SelectFace(parsed.Faces, family, bold, italic)
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

        return FromBytes(BufferStream(font, ResourceLimits.Default), sharedWithCaller: false);
    }

    internal static byte[] BufferStream(Stream font, ResourceLimits limits)
    {
        ArgumentNullException.ThrowIfNull(limits);
        return StreamBytes.ReadFully(font, limits.MaxFileBytes);
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
    /// iterating Unicode codepoints. A registered family (matched by <see cref="Font.Family"/> and
    /// style) is measured from its hmtx advances with per-codepoint fallback; an unregistered
    /// built-in metrics family is measured from its AFM widths, with fallback-served codepoints
    /// measured by the fallback face. When <see cref="EnableKerning"/> is enabled, registered and
    /// built-in metrics fonts apply kerning.
    /// </summary>
    /// <param name="text">The text to measure.</param>
    /// <param name="font">The font to measure with.</param>
    /// <returns>The advance width in points.</returns>
    /// <exception cref="InvalidOperationException">The family is neither registered nor supplied by the built-in metrics.</exception>
    public double MeasureText(string text, Font font)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(font);
        var captured = Capture(font);
        return MeasureText(text, captured);
    }

    internal double MeasureText(string text, in FontPaint font)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (TryResolvePrimary(font, out _))
        {
            return Shaper().MeasureAdvance(text, font);
        }

        SimpleShaper.EnsureNoComplexScript(text);
        var builtIn = BuiltInFontMetrics.Resolve(font)
            ?? throw new InvalidOperationException($"No font is registered for family '{font.Family}'.");
        return MeasureBuiltIn(text, font, builtIn);
    }

    private static FontPaint Capture(Font font)
        => new(
            font.EffectiveFamily,
            font.EffectiveSize.Point,
            font.EffectiveBold,
            font.EffectiveItalic,
            font.EffectiveUnderline,
            font.EffectiveStrikethrough,
            font.EffectiveColor);

    internal SimpleShaper Shaper()
    {
        if (shaper is null || shaperKerning != EnableKerning)
        {
            shaper = new SimpleShaper(this, EnableKerning);
            shaperKerning = EnableKerning;
        }

        return shaper;
    }

    internal CapturedGlyphRun CaptureGlyphRun(
        string text,
        Font font,
        bool enableBuiltInKerning = true)
    {
        var captured = Capture(font);
        return CaptureGlyphRun(text, captured, enableBuiltInKerning);
    }

    internal CapturedGlyphRun CaptureGlyphRun(
        string text,
        in FontPaint font,
        bool enableBuiltInKerning = true)
    {
        if (text.Length == 0)
        {
            return CapturedGlyphRun.Empty(text);
        }

        return TryResolvePrimary(font, out _)
            ? CaptureSfntGlyphRun(text, font)
            : CaptureBuiltInGlyphRun(text, font, enableBuiltInKerning);
    }

    private CapturedGlyphRun CaptureSfntGlyphRun(string text, in FontPaint font)
    {
        var positioned = Shaper().Shape(text, font, out var totalAdvance);
        var spans = ImmutableArray.CreateBuilder<CapturedGlyphSpan>();
        var glyphs = new List<CapturedSfntGlyph>();
        SfntFont? face = null;
        var spanOffset = 0.0;

        foreach (var positionedGlyph in positioned)
        {
            if (face is not null && !ReferenceEquals(face, positionedGlyph.Face))
            {
                var advance = SumAdvance(glyphs);
                spans.Add(new CapturedGlyphSpan(
                    CapturedFontFace.FromSfnt(face),
                    ImmutableArray.CreateRange(glyphs),
                    [],
                    advance,
                    spanOffset));
                spanOffset += advance;
                glyphs.Clear();
            }

            face = positionedGlyph.Face;
            var codepoint = CodePointAt(text, positionedGlyph.Cluster);
            var trailing = SimpleShaper.TrailingKerning(
                face, positionedGlyph.GlyphId, positionedGlyph.Advance, font.Size);
            glyphs.Add(new CapturedSfntGlyph(
                positionedGlyph.GlyphId,
                positionedGlyph.Advance,
                0,
                0,
                -trailing,
                positionedGlyph.Cluster,
                codepoint));
        }

        if (face is not null)
        {
            var advance = SumAdvance(glyphs);
            spans.Add(new CapturedGlyphSpan(
                CapturedFontFace.FromSfnt(face),
                ImmutableArray.CreateRange(glyphs),
                [],
                advance,
                spanOffset));
        }

        return new CapturedGlyphRun(text, spans.ToImmutable(), totalAdvance);
    }

    private CapturedGlyphRun CaptureBuiltInGlyphRun(
        string text,
        in FontPaint font,
        bool enableKerning)
    {
        SimpleShaper.EnsureNoComplexScript(text);
        var metrics = BuiltInFontMetrics.Resolve(font)
            ?? throw new InvalidOperationException($"No font is registered for family '{font.Family}'.");

        var spans = ImmutableArray.CreateBuilder<CapturedGlyphSpan>();
        var builtInGlyphs = new List<CapturedBuiltInGlyph>();
        var sfntGlyphs = new List<CapturedSfntGlyph>();
        SfntFont? fallbackFace = null;
        var builtInDesignAdvance = 0.0;
        var builtInKernAdvance = 0.0;
        var sfntAdvance = 0.0;
        var totalAdvance = 0.0;
        var fontSize = font.Size;

        void FlushBuiltIn()
        {
            if (builtInGlyphs.Count == 0)
            {
                return;
            }

            var advance = FontMetric.Scale(builtInDesignAdvance, fontSize, 1000) + builtInKernAdvance;
            spans.Add(new CapturedGlyphSpan(
                CapturedFontFace.FromBuiltIn(metrics.PostScriptName),
                [],
                ImmutableArray.CreateRange(builtInGlyphs),
                advance,
                totalAdvance));
            totalAdvance += advance;
            builtInGlyphs.Clear();
            builtInDesignAdvance = 0;
            builtInKernAdvance = 0;
        }

        void FlushSfnt()
        {
            if (fallbackFace is null || sfntGlyphs.Count == 0)
            {
                return;
            }

            spans.Add(new CapturedGlyphSpan(
                CapturedFontFace.FromSfnt(fallbackFace),
                ImmutableArray.CreateRange(sfntGlyphs),
                [],
                sfntAdvance,
                totalAdvance));
            totalAdvance += sfntAdvance;
            sfntGlyphs.Clear();
            fallbackFace = null;
            sfntAdvance = 0;
        }

        var i = 0;
        while (i < text.Length)
        {
            var codepoint = CodePointAt(text, i, out var codePointLength);
            var kind = ClassifyBuiltInGlyph(metrics, codepoint, out var width, out var face, out var glyph);
            if (kind == BuiltInGlyphKind.Fallback)
            {
                FlushBuiltIn();
                if (fallbackFace is not null && !ReferenceEquals(fallbackFace, face))
                {
                    FlushSfnt();
                }

                fallbackFace = face;
                if (enableKerning && EnableKerning && sfntGlyphs.Count > 0)
                {
                    var previous = sfntGlyphs[^1];
                    var kern = SimpleShaper.PairKerning(
                        face!, previous.GlyphId, glyph, previous.Codepoint, codepoint, font.Size);
                    sfntAdvance += kern;
                    sfntGlyphs[^1] = previous with
                    {
                        Advance = previous.Advance + kern,
                        TextAdjustmentPoints = -kern,
                    };
                }

                sfntGlyphs.Add(new CapturedSfntGlyph(
                    glyph,
                    face!.AdvanceInUserSpace(glyph, font.Size),
                    0,
                    0,
                    0,
                    i,
                    codepoint));
                sfntAdvance += face.AdvanceInUserSpace(glyph, font.Size);
            }
            else if (kind == BuiltInGlyphKind.BuiltIn)
            {
                FlushSfnt();
                if (enableKerning && EnableKerning && builtInGlyphs.Count > 0)
                {
                    var previous = builtInGlyphs[^1];
                    var kern = metrics.GetRunKerning(
                        (char)MetricsCodepoint(previous.Codepoint),
                        (char)MetricsCodepoint(codepoint));
                    builtInKernAdvance += kern * font.Size / 1000.0;
                    builtInGlyphs[^1] = previous with
                    {
                        Advance = previous.Advance + FontMetric.Scale(kern, font.Size, 1000),
                        TextAdjustmentPoints = -FontMetric.Scale(kern, font.Size, 1000),
                    };
                }

                builtInGlyphs.Add(new CapturedBuiltInGlyph(
                    FontMetric.Scale(width, font.Size, 1000),
                    0,
                    i,
                    codepoint));
                builtInDesignAdvance += width;
            }
            else
            {
                throw MissingMetrics(text, font, metrics);
            }

            i += codePointLength;
        }

        FlushSfnt();
        FlushBuiltIn();
        return new CapturedGlyphRun(text, spans.ToImmutable(), totalAdvance);
    }

    private static double SumAdvance(List<CapturedSfntGlyph> glyphs)
    {
        var advance = 0.0;
        foreach (var glyph in glyphs)
        {
            advance += glyph.Advance;
        }

        return advance;
    }

    private double MeasureBuiltIn(string text, in FontPaint font, BuiltInFontMetrics metrics)
    {
        double sum = 0;
        var i = 0;
        char? previousBuiltIn = null;
        SfntFont? prevFallbackFace = null;
        ushort prevFallbackGlyph = 0;
        var prevFallbackCodepoint = 0;
        while (i < text.Length)
        {
            var codepoint = CodePointAt(text, i, out var codePointLength);
            switch (ClassifyBuiltInGlyph(metrics, codepoint, out var width, out var face, out var glyph))
            {
                case BuiltInGlyphKind.BuiltIn:
                    if (EnableKerning && previousBuiltIn is { } previous)
                    {
                        sum += FontMetric.Scale(
                            metrics.GetRunKerning(previous, (char)MetricsCodepoint(codepoint)),
                            font.Size,
                            1000);
                    }

                    sum += FontMetric.Scale(width, font.Size, 1000);
                    previousBuiltIn = (char)MetricsCodepoint(codepoint);
                    prevFallbackFace = null;
                    break;
                case BuiltInGlyphKind.Fallback:
                    if (EnableKerning && ReferenceEquals(prevFallbackFace, face))
                    {
                        sum += SimpleShaper.PairKerning(
                            face!, prevFallbackGlyph, glyph, prevFallbackCodepoint, codepoint, font.Size);
                    }

                    sum += face!.AdvanceInUserSpace(glyph, font.Size);
                    previousBuiltIn = null;
                    prevFallbackFace = face;
                    prevFallbackGlyph = glyph;
                    prevFallbackCodepoint = codepoint;
                    break;
                default:
                    throw MissingMetrics(text, font, metrics);
            }

            i += codePointLength;
        }

        return sum;
    }

    private InvalidOperationException MissingMetrics(
        string text,
        in FontPaint font,
        BuiltInFontMetrics metrics)
    {
        const int MaxReported = 8;
        var offenders = new List<string>();
        var seen = new HashSet<int>();
        var i = 0;
        while (i < text.Length && offenders.Count < MaxReported)
        {
            var codepoint = CodePointAt(text, i, out var length);
            if (IsReportable(codepoint)
                && ClassifyBuiltInGlyph(metrics, codepoint, out _, out _, out _) == BuiltInGlyphKind.Missing
                && seen.Add(codepoint))
            {
                offenders.Add(Describe(codepoint));
            }

            i += length;
        }

        return new InvalidOperationException(
            $"The built-in metrics font '{font.Family}' has no glyph metrics for {string.Join(", ", offenders)}. "
            + $"Register a font that covers these characters with {nameof(FontCollection)}.{nameof(Register)}, "
            + $"or add such a font to the {nameof(SetFallback)} chain.");
    }

    private static bool IsReportable(int codepoint)
        => codepoint > 0xFFFF || !char.IsControl((char)codepoint);

    private static int MetricsCodepoint(int codepoint) => IsReportable(codepoint) ? codepoint : '?';

    private static string Describe(int codepoint)
        => $"'{char.ConvertFromUtf32(codepoint)}' (U+{codepoint:X4})";

    internal BuiltInGlyphKind ClassifyBuiltInGlyph(
        BuiltInFontMetrics metrics,
        int codepoint,
        out double width,
        out SfntFont? fallbackFace,
        out ushort fallbackGlyph)
    {
        if (metrics.TryGetWidth(codepoint, out width))
        {
            fallbackFace = null;
            fallbackGlyph = 0;
            return BuiltInGlyphKind.BuiltIn;
        }

        if (TryResolveFallbackGlyph(codepoint, out var face, out fallbackGlyph))
        {
            width = 0;
            fallbackFace = face;
            return BuiltInGlyphKind.Fallback;
        }

        if (!IsReportable(codepoint) && metrics.TryGetWidth('?', out width))
        {
            fallbackFace = null;
            fallbackGlyph = 0;
            return BuiltInGlyphKind.BuiltIn;
        }

        width = 0;
        fallbackFace = null;
        fallbackGlyph = 0;
        return BuiltInGlyphKind.Missing;
    }

    internal static int CodePointAt(ReadOnlySpan<char> text, int index) => CodePointAt(text, index, out _);

    internal static int CodePointAt(ReadOnlySpan<char> text, int index, out int length)
    {
        if (char.IsHighSurrogate(text[index]) && index + 1 < text.Length && char.IsLowSurrogate(text[index + 1]))
        {
            length = 2;
            return char.ConvertToUtf32(text[index], text[index + 1]);
        }

        length = 1;
        return text[index];
    }

    internal IEnumerable<RegisteredFace> RegisteredFaces()
    {
        foreach (var key in registrationOrder)
        {
            var face = registered[key];
            var source = new FontSourceData([]);
            var index = 0;
            if (faceRetention.TryGetValue(face, out var parsed))
            {
                source = parsed.Source;
                for (var i = 0; i < parsed.Faces.Count; i++)
                {
                    if (ReferenceEquals(parsed.Faces[i], face))
                    {
                        index = i;
                        break;
                    }
                }
            }

            yield return new RegisteredFace(
                key.Family,
                key.Bold,
                key.Italic,
                face,
                source,
                index);
        }
    }

    internal FontCollectionSnapshot Snapshot()
    {
        var faces = ImmutableArray.CreateBuilder<RegisteredFace>(registrationOrder.Count);
        foreach (var registered in RegisteredFaces())
        {
            faces.Add(registered);
        }

        return new FontCollectionSnapshot(
            faces.MoveToImmutable(),
            ImmutableArray.CreateRange(fallback),
            EnableKerning,
            AllowRestrictedEmbedding,
            AllowDegradedFonts,
            AllowUnsupportedCharacters);
    }

    internal SfntFont? ResolveFace(Font font) => TryResolvePrimary(font, out var face) ? face : null;

    internal SfntFont? ResolveFace(in FontPaint font)
        => TryResolvePrimary(font, out var face) ? face : null;

    internal bool TryResolvePrimary(Font font, out SfntFont primary)
        => TryResolvePrimary(
            font.EffectiveFamily,
            font.EffectiveBold,
            font.EffectiveItalic,
            out primary);

    internal bool TryResolvePrimary(in FontPaint font, out SfntFont primary)
        => TryResolvePrimary(font.Family, font.Bold, font.Italic, out primary);

    private bool TryResolvePrimary(
        string family,
        bool bold,
        bool italic,
        out SfntFont primary)
    {
        if (registered.TryGetValue((family, bold, italic), out primary!))
        {
            return true;
        }

        return TryResolveFamily(family, out primary);
    }

    internal SfntFont ResolvePrimarySfnt(Font font)
        => TryResolvePrimary(font, out var primary)
            ? primary
            : throw new InvalidOperationException($"No font is registered for family '{font.EffectiveFamily}'.");

    internal SfntFont ResolvePrimarySfnt(string family, bool bold, bool italic)
        => TryResolvePrimary(family, bold, italic, out var primary)
            ? primary
            : throw new InvalidOperationException($"No font is registered for family '{family}'.");

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
        => SfntFont.IsCollection(data);
}
