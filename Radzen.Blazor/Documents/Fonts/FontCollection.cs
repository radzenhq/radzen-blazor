using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System;
using Radzen.Documents.Fonts.Sfnt;
using Radzen.Documents.Internal;

namespace Radzen.Documents.Fonts;

internal enum BuiltInGlyphKind
{
    BuiltIn,
    Fallback,
    Skip,
    Missing,
}

internal interface IFontProgramSource
{
    SfntFont Program { get; }
}

internal static class FontProgram
{
    public static SfntFont Of<TSource>(in TSource source)
        where TSource : struct, IFontProgramSource
        => source.Program;
}

internal readonly record struct RegisteredFace : IFontProgramSource
{
    private readonly SfntFont program;

    internal RegisteredFace(string family, bool bold, bool italic, SfntFont program, int faceIndex)
    {
        Family = family;
        Bold = bold;
        Italic = italic;
        FaceIndex = faceIndex;
        this.program = program;
    }

    public string Family { get; }

    public bool Bold { get; }

    public bool Italic { get; }

    public int FaceIndex { get; }

    public CapturedFaceMetrics Metrics => new(program.Ascent, program.Descent, program.UnitsPerEm);

    SfntFont IFontProgramSource.Program => program;
}

internal readonly record struct FontCollectionSnapshot(
    ImmutableArray<RegisteredFace> Faces,
    ImmutableArray<string> Fallback,
    bool EnableKerning)
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

    internal bool TryResolvePrimary(string family, bool bold, bool italic, out SfntFont primary)
    {
        foreach (var face in Faces)
        {
            if (face.Bold == bold && face.Italic == italic
                && string.Equals(face.Family, family, StringComparison.Ordinal))
            {
                primary = FontProgram.Of(face);
                return true;
            }
        }

        return TryResolveFamily(family, out primary);
    }

    internal SfntFont ResolvePrimary(string family, bool bold, bool italic)
        => TryResolvePrimary(family, bold, italic, out var primary)
            ? primary
            : throw new InvalidOperationException($"No font is registered for family '{family}'.");

    internal (SfntFont Face, ushort GlyphId) ResolveGlyph(SfntFont primary, int codepoint)
    {
        var glyph = primary.GetGlyphId(codepoint);
        if (glyph != 0)
        {
            return (primary, glyph);
        }

        return TryResolveFallbackGlyph(codepoint, out var face, out var candidate)
            ? (face, candidate)
            : (primary, (ushort)0);
    }

    internal bool TryResolveFallbackGlyph(int codepoint, out SfntFont face, out ushort glyph)
    {
        foreach (var name in Fallback)
        {
            if (TryResolveFamily(name, out face!))
            {
                glyph = face.GetGlyphId(codepoint);
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
        foreach (var candidate in Faces)
        {
            if (!candidate.Bold && !candidate.Italic
                && string.Equals(candidate.Family, family, StringComparison.Ordinal))
            {
                face = FontProgram.Of(candidate);
                return true;
            }
        }

        foreach (var candidate in Faces)
        {
            if (string.Equals(candidate.Family, family, StringComparison.Ordinal))
            {
                face = FontProgram.Of(candidate);
                return true;
            }
        }

        face = null!;
        return false;
    }
}

/// <summary>
/// Registers embeddable fonts, resolves font families to faces, and measures text.
/// </summary>
/// <remarks>
/// Configuration - the registered faces, the fallback chain and the behaviour flags - is frozen
/// into an immutable snapshot the first time text is measured, captured or resolved, and every
/// measurement reads only that snapshot. Registering a face, changing the fallback chain or
/// setting a flag afterwards discards the snapshot, so the next measurement re-freezes and
/// observes the new configuration whole; a measurement already in flight keeps the snapshot it
/// started with rather than seeing a half-applied change.
/// </remarks>
public sealed class FontCollection
{
    internal FontCollection()
    {
    }

    private const int SignatureWindow = 64 * 1024;

    private readonly Dictionary<(string Family, bool Bold, bool Italic), SfntFont> registered = [];

    private readonly List<(string Family, bool Bold, bool Italic)> registrationOrder = [];

    private readonly List<string> fallback = [];

    private readonly object gate = new();

    private FrozenConfiguration? frozen;

    private bool enableKerning;

    private sealed class FrozenConfiguration(FontCollectionSnapshot snapshot)
    {
        public FontCollectionSnapshot Snapshot { get; } = snapshot;

        public SimpleShaper Shaper { get; } = new SimpleShaper(snapshot);
    }

    /// <summary>
    /// Gets or sets whether pair kerning is applied when measuring and drawing text.
    /// When <see langword="true"/> consecutive same-face glyphs are tightened by the
    /// font's kern data (sfnt <c>kern</c> table or built-in AFM pairs). Defaults to
    /// <see langword="false"/> so output stays byte identical unless opted in.
    /// </summary>
    public bool EnableKerning
    {
        get => enableKerning;
        set => Configure(ref enableKerning, value);
    }

    private void Configure(ref bool field, bool value)
    {
        lock (gate)
        {
            field = value;
            Volatile.Write(ref frozen, null);
        }
    }

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
            ? SfntFont.SelectFace(parsed.Faces, family, bold, italic)
            : parsed.Faces[0];

        lock (gate)
        {
            var key = (family, bold, italic);
            if (!registered.ContainsKey(key))
            {
                registrationOrder.Add(key);
            }

            registered[key] = face;
            Volatile.Write(ref frozen, null);
        }
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

        return FromBytes(BufferStream(font, Core.ResourceLimits.Default), sharedWithCaller: false);
    }

    internal static byte[] BufferStream(Stream font, Core.ResourceLimits limits)
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
        lock (gate)
        {
            fallback.Clear();
            fallback.AddRange(families);
            Volatile.Write(ref frozen, null);
        }
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
        var captured = FontPaintCapture.Capture(font);
        return MeasureText(text, captured);
    }

    internal double MeasureText(string text, in FontPaint font)
    {
        ArgumentNullException.ThrowIfNull(text);
        var configuration = Freeze();
        if (configuration.Snapshot.TryResolvePrimary(font.Family, font.Bold, font.Italic, out _))
        {
            return configuration.Shaper.MeasureAdvance(text, font);
        }

        SimpleShaper.EnsureNoComplexScript(text);
        var builtIn = BuiltInFontMetrics.Resolve(font)
            ?? throw new InvalidOperationException($"No font is registered for family '{font.Family}'.");
        return MeasureBuiltIn(configuration.Snapshot, text, font, builtIn);
    }

    internal SimpleShaper Shaper() => Freeze().Shaper;

    private FrozenConfiguration Freeze()
    {
        var current = Volatile.Read(ref frozen);
        if (current is not null)
        {
            return current;
        }

        lock (gate)
        {
            if (frozen is { } existing)
            {
                return existing;
            }

            var created = new FrozenConfiguration(Build());
            Volatile.Write(ref frozen, created);
            return created;
        }
    }

    internal CapturedGlyphRun CaptureGlyphRun(string text, Font font)
        => CaptureGlyphRun(text, FontPaintCapture.Capture(font));

    internal CapturedGlyphRun CaptureGlyphRun(string text, in FontPaint font)
    {
        if (text.Length == 0)
        {
            return CapturedGlyphRun.Empty(text);
        }

        var configuration = Freeze();
        return configuration.Snapshot.TryResolvePrimary(font.Family, font.Bold, font.Italic, out _)
            ? CaptureSfntGlyphRun(configuration, text, font)
            : CaptureBuiltInGlyphRun(configuration.Snapshot, text, font);
    }

    private struct SpanAccumulator
    {
        private CapturedGlyphSpan first;
        private ImmutableArray<CapturedGlyphSpan>.Builder? rest;
        private int count;

        public void Add(in CapturedGlyphSpan span)
        {
            if (count == 0)
            {
                first = span;
            }
            else
            {
                if (rest is null)
                {
                    rest = ImmutableArray.CreateBuilder<CapturedGlyphSpan>();
                    rest.Add(first);
                }

                rest.Add(span);
            }

            count++;
        }

        public readonly ImmutableArray<CapturedGlyphSpan> ToImmutable()
            => count switch
            {
                0 => [],
                1 => ImmutableArray.Create(first),
                _ => rest!.ToImmutable(),
            };
    }

    private static CapturedGlyphRun CaptureSfntGlyphRun(
        FrozenConfiguration configuration,
        string text,
        in FontPaint font)
    {
        var positioned = configuration.Shaper.Shape(text, font, out var totalAdvance);
        if (positioned.Count == 0)
        {
            return new CapturedGlyphRun(text, [], totalAdvance);
        }

        if (SingleFace(positioned) is { } only)
        {
            return CaptureSingleFaceRun(positioned, only, text, font, totalAdvance);
        }

        var spans = default(SpanAccumulator);
        var glyphs = new List<CapturedGlyph>(text.Length);
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
                    advance,
                    spanOffset));
                spanOffset += advance;
                glyphs.Clear();
            }

            face = positionedGlyph.Face;
            var codepoint = CodePointAt(text, positionedGlyph.Cluster);
            var trailing = SimpleShaper.TrailingKerning(
                face, positionedGlyph.GlyphId, positionedGlyph.Advance, font.Size);
            glyphs.Add(new CapturedGlyph(
                positionedGlyph.Advance,
                trailing,
                positionedGlyph.Cluster,
                codepoint));
        }

        if (face is not null)
        {
            var advance = SumAdvance(glyphs);
            spans.Add(new CapturedGlyphSpan(
                CapturedFontFace.FromSfnt(face),
                ImmutableArray.CreateRange(glyphs),
                advance,
                spanOffset));
        }

        return new CapturedGlyphRun(text, spans.ToImmutable(), totalAdvance);
    }

    private static SfntFont? SingleFace(List<ShapedGlyph> positioned)
    {
        var face = positioned[0].Face;
        for (var i = 1; i < positioned.Count; i++)
        {
            if (!ReferenceEquals(face, positioned[i].Face))
            {
                return null;
            }
        }

        return face;
    }

    private static CapturedGlyphRun CaptureSingleFaceRun(
        List<ShapedGlyph> positioned,
        SfntFont face,
        string text,
        in FontPaint font,
        double totalAdvance)
    {
        var captured = new CapturedGlyph[positioned.Count];
        var advance = 0.0;
        for (var i = 0; i < positioned.Count; i++)
        {
            var positionedGlyph = positioned[i];
            captured[i] = new CapturedGlyph(
                positionedGlyph.Advance,
                SimpleShaper.TrailingKerning(face, positionedGlyph.GlyphId, positionedGlyph.Advance, font.Size),
                positionedGlyph.Cluster,
                CodePointAt(text, positionedGlyph.Cluster));
            advance += positionedGlyph.Advance;
        }

        return new CapturedGlyphRun(
            text,
            ImmutableArray.Create(new CapturedGlyphSpan(
                CapturedFontFace.FromSfnt(face),
                ImmutableCollectionsMarshal.AsImmutableArray(captured),
                advance,
                0)),
            totalAdvance);
    }

    private static CapturedGlyphRun CaptureBuiltInGlyphRun(
        in FontCollectionSnapshot snapshot,
        string text,
        in FontPaint font)
    {
        SimpleShaper.EnsureNoComplexScript(text);
        var metrics = BuiltInFontMetrics.Resolve(font)
            ?? throw new InvalidOperationException($"No font is registered for family '{font.Family}'.");

        var spans = default(SpanAccumulator);
        var builtInGlyphs = new List<CapturedGlyph>(text.Length);
        var sfntGlyphs = new List<CapturedGlyph>();
        SfntFont? fallbackFace = null;
        ushort previousFallbackGlyph = 0;
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

            var advance = FontMetric.ScaleAfm(builtInDesignAdvance, fontSize) + builtInKernAdvance;
            spans.Add(new CapturedGlyphSpan(
                CapturedFontFace.FromBuiltIn(metrics.Face()),
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
                sfntAdvance,
                totalAdvance));
            totalAdvance += sfntAdvance;
            sfntGlyphs.Clear();
            fallbackFace = null;
            previousFallbackGlyph = 0;
            sfntAdvance = 0;
        }

        var i = 0;
        while (i < text.Length)
        {
            var codepoint = CodePointAt(text, i, out var codePointLength);
            var kind = ClassifyBuiltInGlyph(snapshot, metrics, codepoint, out var width, out var face, out var glyph);
            if (kind == BuiltInGlyphKind.Fallback)
            {
                FlushBuiltIn();
                if (fallbackFace is not null && !ReferenceEquals(fallbackFace, face))
                {
                    FlushSfnt();
                }

                fallbackFace = face;
                if (snapshot.EnableKerning && sfntGlyphs.Count > 0)
                {
                    var previous = sfntGlyphs[^1];
                    var kern = SimpleShaper.PairKerning(
                        face!, previousFallbackGlyph, glyph, previous.Codepoint, codepoint, font.Size);
                    sfntAdvance += kern;
                    sfntGlyphs[^1] = previous with
                    {
                        Advance = previous.Advance + kern,
                        Kerning = kern,
                    };
                }

                sfntGlyphs.Add(new CapturedGlyph(
                    face!.AdvanceInUserSpace(glyph, font.Size),
                    0,
                    i,
                    codepoint));
                previousFallbackGlyph = glyph;
                sfntAdvance += face.AdvanceInUserSpace(glyph, font.Size);
            }
            else if (kind == BuiltInGlyphKind.BuiltIn)
            {
                FlushSfnt();
                if (snapshot.EnableKerning && builtInGlyphs.Count > 0)
                {
                    var previous = builtInGlyphs[^1];
                    var kern = metrics.GetRunKerning(
                        (char)MetricsCodepoint(previous.Codepoint),
                        (char)MetricsCodepoint(codepoint));
                    builtInKernAdvance += FontMetric.ScaleAfm(kern, font.Size);
                    builtInGlyphs[^1] = previous with
                    {
                        Advance = previous.Advance + FontMetric.ScaleAfm(kern, font.Size),
                        Kerning = FontMetric.ScaleAfm(kern, font.Size),
                    };
                }

                builtInGlyphs.Add(new CapturedGlyph(
                    FontMetric.ScaleAfm(width, font.Size),
                    0,
                    i,
                    codepoint));
                builtInDesignAdvance += width;
            }
            else if (kind != BuiltInGlyphKind.Skip)
            {
                throw MissingMetrics(snapshot, text, font, metrics);
            }

            i += codePointLength;
        }

        FlushSfnt();
        FlushBuiltIn();
        return new CapturedGlyphRun(text, spans.ToImmutable(), totalAdvance);
    }

    private static double SumAdvance(List<CapturedGlyph> glyphs)
    {
        var advance = 0.0;
        foreach (var glyph in glyphs)
        {
            advance += glyph.Advance;
        }

        return advance;
    }

    private static double MeasureBuiltIn(
        in FontCollectionSnapshot snapshot,
        string text,
        in FontPaint font,
        BuiltInFontMetrics metrics)
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
            switch (ClassifyBuiltInGlyph(snapshot, metrics, codepoint, out var width, out var face, out var glyph))
            {
                case BuiltInGlyphKind.BuiltIn:
                    if (snapshot.EnableKerning && previousBuiltIn is { } previous)
                    {
                        sum += FontMetric.ScaleAfm(
                            metrics.GetRunKerning(previous, (char)MetricsCodepoint(codepoint)),
                            font.Size);
                    }

                    sum += FontMetric.ScaleAfm(width, font.Size);
                    previousBuiltIn = (char)MetricsCodepoint(codepoint);
                    prevFallbackFace = null;
                    break;
                case BuiltInGlyphKind.Fallback:
                    if (snapshot.EnableKerning && ReferenceEquals(prevFallbackFace, face))
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
                case BuiltInGlyphKind.Skip:
                    break;
                default:
                    throw MissingMetrics(snapshot, text, font, metrics);
            }

            i += codePointLength;
        }

        return sum;
    }

    private static InvalidOperationException MissingMetrics(
        in FontCollectionSnapshot snapshot,
        string text,
        in FontPaint font,
        BuiltInFontMetrics metrics)
    {
        var offenders = new List<int>();
        var seen = new HashSet<int>();
        var i = 0;
        while (i < text.Length)
        {
            var codepoint = CodePointAt(text, i, out var length);
            if (IsReportable(codepoint)
                && ClassifyBuiltInGlyph(snapshot, metrics, codepoint, out _, out _, out _) == BuiltInGlyphKind.Missing
                && seen.Add(codepoint))
            {
                offenders.Add(codepoint);
            }

            i += length;
        }

        return MissingGlyphMetrics.Error(font.Family, offenders);
    }

    private static bool IsReportable(int codepoint) => MissingGlyphMetrics.IsReportable(codepoint);

    private static int MetricsCodepoint(int codepoint) => MissingGlyphMetrics.Substituted(codepoint);

    internal BuiltInGlyphKind ClassifyBuiltInGlyph(
        BuiltInFontMetrics metrics,
        int codepoint,
        out double width,
        out SfntFont? fallbackFace,
        out ushort fallbackGlyph)
        => ClassifyBuiltInGlyph(Freeze().Snapshot, metrics, codepoint, out width, out fallbackFace, out fallbackGlyph);

    private static BuiltInGlyphKind ClassifyBuiltInGlyph(
        in FontCollectionSnapshot snapshot,
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

        if (snapshot.TryResolveFallbackGlyph(codepoint, out var face, out fallbackGlyph))
        {
            width = 0;
            fallbackFace = face;
            return BuiltInGlyphKind.Fallback;
        }

        if (IgnorableCharacters.IsIgnorableOnMiss(codepoint))
        {
            width = 0;
            fallbackFace = null;
            fallbackGlyph = 0;
            return BuiltInGlyphKind.Skip;
        }

        if (IgnorableCharacters.IsSpaceOnMiss(codepoint) && metrics.TryGetWidth(' ', out width))
        {
            fallbackFace = null;
            fallbackGlyph = 0;
            return BuiltInGlyphKind.BuiltIn;
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
            if (!faceRetention.TryGetValue(face, out var parsed))
            {
                throw new InvalidOperationException(
                    $"The source bytes of the registered font '{face.PostScriptName}' are no longer retained, "
                    + "so it cannot be embedded. Register the family again from its font stream.");
            }

            var index = 0;
            for (var i = 0; i < parsed.Faces.Count; i++)
            {
                if (ReferenceEquals(parsed.Faces[i], face))
                {
                    index = i;
                    break;
                }
            }

            yield return new RegisteredFace(
                key.Family,
                key.Bold,
                key.Italic,
                face,
                index);
        }
    }

    internal FontCollectionSnapshot Snapshot() => Freeze().Snapshot;

    private FontCollectionSnapshot Build()
    {
        var faces = ImmutableArray.CreateBuilder<RegisteredFace>(registrationOrder.Count);
        foreach (var face in RegisteredFaces())
        {
            faces.Add(face);
        }

        return new FontCollectionSnapshot(
            faces.MoveToImmutable(),
            ImmutableArray.CreateRange(fallback),
            enableKerning);
    }

    internal SfntFont? ResolveFace(Font font) => TryResolvePrimary(font, out var face) ? face : null;

    internal bool TryResolvePrimary(Font font, out SfntFont primary)
        => Freeze().Snapshot.TryResolvePrimary(
            font.EffectiveFamily,
            font.EffectiveBold,
            font.EffectiveItalic,
            out primary);

    internal bool TryResolvePrimary(in FontPaint font, out SfntFont primary)
        => Freeze().Snapshot.TryResolvePrimary(font.Family, font.Bold, font.Italic, out primary);

    internal SfntFont ResolvePrimarySfnt(Font font)
        => Freeze().Snapshot.ResolvePrimary(
            font.EffectiveFamily, font.EffectiveBold, font.EffectiveItalic);

    private static bool IsCollection(byte[] data)
        => SfntFont.IsCollection(data);
}
