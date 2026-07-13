using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Radzen.Documents.Pdf.Emit;


internal struct LineFragment
{
    public required Run Run { get; init; }

    // The resolved font of this fragment (the run's font run through the style cascade, or the
    // synthesized font of a marker/hyphen/leader fragment). Carried on the display-list layer so
    // emission never reads a resolved font off the shared authoring model.
    public required Font Font { get; init; }

    public required string Text { get; init; }

    public required int Start { get; init; }

    public required int Length { get; init; }

    public double XOffset { get; set; }

    public required double Advance { get; init; }

    // A list-item marker fragment; in tagged output it is tagged into the item's Lbl element.
    public bool IsMarker { get; init; }
}

internal sealed class LineBox
{
    public required IReadOnlyList<LineFragment> Fragments { get; init; }

    public double Width { get; set; }

    public double Height { get; set; }

    public double Baseline { get; set; }
}

internal static class LineBreaker
{
    private readonly struct Piece
    {
        public required Run Run { get; init; }
        public required Font Font { get; init; }
        public required int Start { get; init; }
        public required int Length { get; init; }
        public required string Text { get; init; }
        public required double Advance { get; init; }
    }

    // Pieces of a word are contiguous in the shared piece list: [PieceStart, PieceStart + PieceCount).
    private struct Word
    {
        public int PieceStart { get; init; }
        public int PieceCount { get; set; }
        public double Width { get; set; }
        public double GapAfter { get; set; }
        public int TabsAfter { get; set; }

        // A zero-width break opportunity follows this word (soft hyphen U+00AD or ZWSP
        // U+200B). Such a boundary carries no inter-word gap and is excluded from
        // justification, so an unbroken word is placed exactly as if it were never split.
        public bool OptionalBreakAfter { get; set; }

        // The optional break after this word is a soft hyphen: a hyphen is rendered when
        // the line breaks there, and nothing otherwise.
        public bool SoftHyphenAfter { get; set; }

        // The advance of the '-' rendered when a soft-hyphen break is taken after this word,
        // measured in the preceding text's font. Reserved by the wrap fit and by alignment
        // so the hyphen never spills past the measure.
        public double HyphenWidth { get; set; }

        // The boundary after this word carries no inter-word whitespace and is not a word
        // space (an inline-image edge: text[img] or [img]text with no space). Excluded from
        // justification like an optional break so no stretch gap is inserted there.
        public bool NoGapBoundary { get; set; }
    }

    private const double DefaultTabStopWidth = 36.0;

    internal static double AdvanceToTabStop(double position)
        => (Math.Floor((position + 1e-6) / DefaultTabStopWidth) + 1) * DefaultTabStopWidth;

    // The resolved font of a run (its authored font run through the style cascade), from the
    // per-save StyleResolution when present, else the authored font: a synthesized run (TOC,
    // field, barcode) carries its resolved font as its authored Font, and a direct-test
    // paragraph has no resolution at all.
    private static Font ResolvedFont(StyleResolution? resolution, Run run)
        => resolution?.RunFont(run) ?? run.Font;

    private static Font ResolvedFont(StyleResolution? resolution, Paragraph paragraph)
        => resolution?.ParagraphFont(paragraph) ?? paragraph.Font;

    public static IReadOnlyList<LineBox> Break(
        Paragraph paragraph,
        double maxWidthPoints,
        FontCollection fonts,
        HorizontalAlignment? inheritedAlignment = null,
        StyleResolution? resolution = null)
    {
        var boxes = new List<LineBox>();
        var indent = paragraph.LeftIndent.Point;
        var max = maxWidthPoints - indent;
        var wrapStops = SortedTabStops(paragraph);
        // The marker (bullet/number) is carried by the first CONTENT line, not the first box:
        // an item whose text starts with a break produces a leading empty line that cannot hold
        // it, so the marker stays pending until a non-empty line is built.
        var markerPending = true;
        foreach (var words in Tokenize(paragraph, fonts, resolution, out var pieces))
        {
            if (words.Count == 0)
            {
                boxes.Add(EmptyLine(paragraph, fonts, resolution));
                continue;
            }

            var lineRanges = Wrap(words, max, wrapStops);
            for (var li = 0; li < lineRanges.Count; li++)
            {
                var (first, last) = lineRanges[li];
                var isLast = li == lineRanges.Count - 1;
                var includeMarker = markerPending;
                markerPending = false;
                if (paragraph.TabStops.Count == 0 && first == last && words[first].Width > max
                    && IsBreakable(words[first], pieces))
                {
                    BreakOversizedWord(boxes, words[first], pieces, max, indent, paragraph, fonts, inheritedAlignment, includeMarker, resolution);
                }
                else
                {
                    boxes.Add(BuildLine(words, pieces, first, last, max, indent, paragraph, fonts, isLast, inheritedAlignment, includeMarker, wrapStops, resolution));
                }
            }
        }

        return boxes;
    }

    // An empty segment (empty paragraph or blank forced-break line) occupies one line
    // of the paragraph's resolved font instead of collapsing to zero height.
    private static LineBox EmptyLine(Paragraph paragraph, FontCollection fonts, StyleResolution? resolution)
    {
        var font = ResolvedFont(resolution, paragraph);
        var (height, ascent) = FontExtent(font, fonts);
        return new LineBox
        {
            Fragments = [],
            Width = 0,
            Height = height * paragraph.LineSpacing,
            Baseline = ascent,
        };
    }

    // Position where the word after `word` starts, given `word` ends at `position`: the inter-word
    // gap, then each tab advances to the next stop. With explicit `stops` this mirrors
    // BuildTabStopLine's placement (next stop beyond the cursor, else the default grid) so wrapping
    // fit and final placement agree; with no stops it is the plain 36pt-grid default.
    private static double NextStart(double position, Word word, List<TabStop>? stops = null)
    {
        var p = position + word.GapAfter;
        for (var t = 0; t < word.TabsAfter; t++)
        {
            p = stops is not null && TryNextStop(stops, p, out var stopPos, out _, out _)
                ? stopPos
                : AdvanceToTabStop(p);
        }

        return p;
    }

    // Paragraph tab stops sorted by position for the wrap fit; null when there are none so the
    // default-grid path stays byte-identical.
    private static List<TabStop>? SortedTabStops(Paragraph paragraph)
    {
        if (paragraph.TabStops.Count == 0)
        {
            return null;
        }

        var stops = new List<TabStop>(paragraph.TabStops.Count);
        for (var s = 0; s < paragraph.TabStops.Count; s++)
        {
            stops.Add(paragraph.TabStops[s]);
        }

        stops.Sort((a, b) => a.Position.Point.CompareTo(b.Position.Point));
        return stops;
    }

    // Spaces carry no kerning, so a run of them measures as count * one space width; cache the
    // per-font single-space advance to avoid allocating and measuring a fresh space string per gap.
    private static double SpaceWidth(FontCollection fonts, Font font, Dictionary<Font, double> cache)
    {
        if (!cache.TryGetValue(font, out var width))
        {
            width = fonts.MeasureText(" ", font);
            cache[font] = width;
        }

        return width;
    }

    private const char SoftHyphen = '\u00AD';
    private const char ZeroWidthSpace = '\u200B';
    private const char EnDash = '\u2013';
    private const char EmDash = '\u2014';

    private static bool IsInlineWhitespace(char c) => c is ' ' or '\t';

    private static bool IsLineBreak(char c) => c is '\n' or '\r';

    // Zero-width conditional break characters removed from the rendered text.
    private static bool IsConditionalBreak(char c) => c == SoftHyphen || c == ZeroWidthSpace;

    // A break may be taken after these visible characters in the emergency word breaker.
    private static bool IsHyphenBreak(char c) => c is '-' or EnDash or EmDash;

    // Splits the paragraph into forced-break segments ('\n', '\r' and "\r\n"), each a
    // list of words separated by breakable whitespace (' ' and '\t'). NBSP is a word
    // character; control characters never enter fragment text.
    private static List<List<Word>> Tokenize(Paragraph paragraph, FontCollection fonts, StyleResolution? resolution, out List<Piece> pieces)
    {
        var segments = new List<List<Word>>();
        var words = new List<Word>();
        segments.Add(words);
        pieces = [];
        var spaceWidths = new Dictionary<Font, double>();
        var current = default(Word);
        var hasCurrent = false;

        foreach (var run in paragraph.Inlines)
        {
            var runFont = ResolvedFont(resolution, run);
            if (run is InlineImage inlineImage)
            {
                if (hasCurrent)
                {
                    // A word butting directly against the image (no space, no tab) forms a
                    // no-gap boundary; a spaced boundary keeps its real gap.
                    if (current.GapAfter == 0 && current.TabsAfter == 0)
                    {
                        current.NoGapBoundary = true;
                    }

                    words.Add(current);
                    hasCurrent = false;
                }

                var advance = inlineImage.EffectiveSize().Width;
                // Any whitespace after the image is held by a separate empty word, so the
                // boundary immediately after an image never carries a word space.
                words.Add(new Word
                {
                    PieceStart = pieces.Count,
                    PieceCount = 1,
                    Width = advance,
                    NoGapBoundary = true,
                });
                pieces.Add(new Piece
                {
                    Run = inlineImage,
                    Font = runFont,
                    Start = 0,
                    Length = 0,
                    Text = string.Empty,
                    Advance = advance,
                });
                continue;
            }

            var text = run.Text;
            var i = 0;
            while (i < text.Length)
            {
                if (IsLineBreak(text[i]))
                {
                    if (text[i] == '\r' && i + 1 < text.Length && text[i + 1] == '\n')
                    {
                        i++;
                    }

                    i++;
                    if (hasCurrent)
                    {
                        words.Add(current);
                        hasCurrent = false;
                    }

                    words = [];
                    segments.Add(words);
                }
                else if (IsInlineWhitespace(text[i]))
                {
                    while (i < text.Length && IsInlineWhitespace(text[i]))
                    {
                        if (text[i] == '\t')
                        {
                            if (!hasCurrent)
                            {
                                current = new Word { PieceStart = pieces.Count };
                                hasCurrent = true;
                            }

                            current.TabsAfter++;
                        }
                        else
                        {
                            // A leading space (paragraph start / after '\n'), a space after an inline
                            // image, or a space after a tab has no word to attach to (or would attach
                            // ahead of the tab); start an empty word so the gap is honored in order.
                            if (!hasCurrent || current.TabsAfter > 0)
                            {
                                if (hasCurrent)
                                {
                                    words.Add(current);
                                }

                                current = new Word { PieceStart = pieces.Count };
                                hasCurrent = true;
                            }

                            current.GapAfter += SpaceWidth(fonts, runFont, spaceWidths);
                        }

                        i++;
                    }
                }
                else
                {
                    var start = i;
                    while (i < text.Length && !IsInlineWhitespace(text[i]) && !IsLineBreak(text[i]))
                    {
                        i++;
                    }

                    // A non-whitespace run is split at zero-width conditional breaks: soft
                    // hyphen (U+00AD) and ZWSP (U+200B). Each split finalizes the current
                    // word with an optional-break boundary; the special char is dropped from
                    // the rendered text. A conditional char with no left context in the word
                    // (q == sub) is not a valid hyphenation point and stays a literal char.
                    // A run with no interior conditional char produces a single word,
                    // byte-identical to the pre-split tokenizer.
                    var sub = start;
                    while (sub <= i)
                    {
                        var q = sub;
                        while (q < i && !(IsConditionalBreak(text[q]) && q > sub))
                        {
                            q++;
                        }

                        if (q > sub)
                        {
                            var segment = text[sub..q];
                            var advance = MeasureRun(fonts, run, runFont, segment);
                            if (!hasCurrent || current.GapAfter > 0 || current.TabsAfter > 0)
                            {
                                if (hasCurrent)
                                {
                                    words.Add(current);
                                }

                                current = new Word { PieceStart = pieces.Count };
                                hasCurrent = true;
                            }

                            pieces.Add(new Piece
                            {
                                Run = run,
                                Font = runFont,
                                Start = sub,
                                Length = q - sub,
                                Text = segment,
                                Advance = advance,
                            });
                            current.PieceCount++;
                            current.Width += advance;
                        }

                        if (q == i)
                        {
                            break;
                        }

                        // The break attaches to the word just built from [sub, q).
                        current.OptionalBreakAfter = true;
                        current.SoftHyphenAfter = text[q] == SoftHyphen;
                        if (current.SoftHyphenAfter)
                        {
                            current.HyphenWidth = fonts.MeasureText("-", runFont);
                        }

                        words.Add(current);
                        hasCurrent = false;

                        sub = q + 1;
                    }
                }
            }
        }

        if (hasCurrent)
        {
            words.Add(current);
        }

        return segments;
    }

    private static List<(int First, int Last)> Wrap(List<Word> words, double max, List<TabStop>? stops)
    {
        var lines = new List<(int, int)>();
        var i = 0;
        while (i < words.Count)
        {
            var j = i;
            var end = words[i].Width;
            while (j + 1 < words.Count)
            {
                var nextEnd = NextStart(end, words[j], stops) + words[j + 1].Width;
                if (nextEnd <= max)
                {
                    end = nextEnd;
                    j++;
                }
                else
                {
                    break;
                }
            }

            // If the break after the terminal word renders a soft hyphen, its width must fit
            // within the measure too; back off words that no longer fit with it (a lone word
            // at i cannot be moved and overflows only by the tiny hyphen).
            while (j > i && words[j].SoftHyphenAfter && j < words.Count - 1
                && LineNaturalWidth(words, i, j, stops) + words[j].HyphenWidth > max)
            {
                j--;
            }

            lines.Add((i, j));
            i = j + 1;
        }

        return lines;
    }

    // Natural end position of words[i..j] (advances, inter-word gaps and tab advances), the
    // same accumulation Wrap's inner loop performs; used to re-test a soft-hyphen terminal.
    private static double LineNaturalWidth(List<Word> words, int i, int j, List<TabStop>? stops)
    {
        var end = words[i].Width;
        for (var w = i; w < j; w++)
        {
            end = NextStart(end, words[w], stops) + words[w + 1].Width;
        }

        return end;
    }

    // A word is emergency-breakable only when every piece carries real text; inline images and
    // empty pieces cannot be split at character granularity, and a non-breaking space (U+00A0)
    // keeps its word intact (it overflows rather than breaks, honoring the author's intent).
    private static bool IsBreakable(Word word, List<Piece> pieces)
    {
        for (var p = word.PieceStart; p < word.PieceStart + word.PieceCount; p++)
        {
            var piece = pieces[p];
            if (piece.Run is InlineImage || piece.Text.Length == 0 || piece.Text.Contains('\u00A0', StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    // A single token wider than the measure is split at character (code point) granularity so no
    // line exceeds max. Surrogate pairs stay intact; at least one code point is placed per line so
    // progress is guaranteed even when a single glyph is wider than max.
    private static void BreakOversizedWord(
        List<LineBox> boxes,
        Word word,
        List<Piece> pieces,
        double max,
        double indent,
        Paragraph paragraph,
        FontCollection fonts,
        HorizontalAlignment? inheritedAlignment,
        bool includeMarker,
        StyleResolution? resolution)
    {
        var fragments = new List<LineFragment>();
        var lineWidth = 0.0;
        var markerPending = includeMarker;
        // A long oversized token (e.g. a URL) is measured one code point at a time; cache the
        // per-(font, code point) advance so a repeated character is measured once, not per
        // occurrence with a fresh substring each time.
        var advances = new Dictionary<(Font, int), double>();

        for (var p = word.PieceStart; p < word.PieceStart + word.PieceCount; p++)
        {
            var piece = pieces[p];
            var text = piece.Text;
            var font = piece.Font;
            var fragStart = 0;
            var fragAdvance = 0.0;
            var hyphenBreak = -1;      // char index just past the latest '-'/dash in [fragStart, i)
            var hyphenAdvance = 0.0;   // fragAdvance accumulated up to hyphenBreak
            var i = 0;
            while (i < text.Length)
            {
                var cpLen = char.IsHighSurrogate(text[i]) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1])
                    ? 2
                    : 1;
                var codepoint = cpLen == 2 ? char.ConvertToUtf32(text[i], text[i + 1]) : text[i];
                if (!advances.TryGetValue((font, codepoint), out var baseAdvance))
                {
                    baseAdvance = fonts.MeasureText(text.Substring(i, cpLen), font);
                    advances[(font, codepoint)] = baseAdvance;
                }

                var advance = baseAdvance * piece.Run.ScriptScale;
                var step = fragAdvance > 0 ? advance + piece.Run.LetterSpacing.Point : advance;
                if ((lineWidth > 0 || fragAdvance > 0) && lineWidth + fragAdvance + step > max)
                {
                    // Prefer breaking just after a hyphen/dash over splitting mid-glyph; the
                    // tail after the hyphen is re-measured fresh on the next line.
                    if (hyphenBreak > fragStart && hyphenBreak <= i)
                    {
                        fragments.Add(MakeCharFragment(piece, fragStart, hyphenBreak, hyphenAdvance));
                        lineWidth += hyphenAdvance;
                        boxes.Add(FinishOversizedLine(fragments, lineWidth, max, indent, paragraph, fonts, inheritedAlignment, ref markerPending, resolution));
                        fragments = [];
                        lineWidth = 0.0;
                        i = hyphenBreak;
                        fragStart = hyphenBreak;
                        fragAdvance = 0.0;
                        hyphenBreak = -1;
                        hyphenAdvance = 0.0;
                        continue;
                    }

                    if (i > fragStart)
                    {
                        fragments.Add(MakeCharFragment(piece, fragStart, i, fragAdvance));
                        lineWidth += fragAdvance;
                    }

                    boxes.Add(FinishOversizedLine(fragments, lineWidth, max, indent, paragraph, fonts, inheritedAlignment, ref markerPending, resolution));
                    fragments = [];
                    lineWidth = 0.0;
                    fragStart = i;
                    fragAdvance = 0.0;
                    hyphenBreak = -1;
                    hyphenAdvance = 0.0;
                    step = advance;
                }

                fragAdvance += step;
                if (cpLen == 1 && IsHyphenBreak(text[i]))
                {
                    hyphenBreak = i + cpLen;
                    hyphenAdvance = fragAdvance;
                }

                i += cpLen;
            }

            if (text.Length > fragStart)
            {
                fragments.Add(MakeCharFragment(piece, fragStart, text.Length, fragAdvance));
                lineWidth += fragAdvance;
            }
        }

        if (fragments.Count > 0)
        {
            boxes.Add(FinishOversizedLine(fragments, lineWidth, max, indent, paragraph, fonts, inheritedAlignment, ref markerPending, resolution));
        }
    }

    private static LineFragment MakeCharFragment(Piece piece, int startInText, int endInText, double advance)
        => new()
        {
            Run = piece.Run,
            Font = piece.Font,
            Text = piece.Text[startInText..endInText],
            Start = piece.Start + startInText,
            Length = endInText - startInText,
            Advance = advance,
        };

    private static LineBox FinishOversizedLine(
        List<LineFragment> fragments,
        double width,
        double max,
        double indent,
        Paragraph paragraph,
        FontCollection fonts,
        HorizontalAlignment? inheritedAlignment,
        ref bool markerPending,
        StyleResolution? resolution)
    {
        var alignment = paragraph.ResolveAlignment(inheritedAlignment);
        var x0 = alignment switch
        {
            HorizontalAlignment.Right or HorizontalAlignment.End => max - width,
            HorizontalAlignment.Center => (max - width) / 2.0,
            _ => 0,
        };

        // A lone glyph wider than max would drive x0 negative; clamp so the line never shifts left of the indent.
        if (x0 < 0)
        {
            x0 = 0;
        }

        var span = CollectionsMarshal.AsSpan(fragments);
        var cursor = indent + x0;
        for (var f = 0; f < span.Length; f++)
        {
            span[f].XOffset = cursor;
            cursor += span[f].Advance;
        }

        if (markerPending && paragraph.MarkerText is { Length: > 0 } markerText)
        {
            var markerFont = ResolvedFont(resolution, paragraph);
            fragments.Insert(0, new LineFragment
            {
                Run = new Run(markerText),
                Font = markerFont,
                Text = markerText,
                Start = 0,
                Length = markerText.Length,
                XOffset = paragraph.MarkerIndent.Point,
                Advance = fonts.MeasureText(markerText, markerFont),
                IsMarker = true,
            });
        }

        markerPending = false;

        var box = new LineBox { Fragments = fragments, Width = width };
        Measure(box, paragraph.LineSpacing, fonts);
        return box;
    }

    private static LineBox BuildLine(
        List<Word> words,
        List<Piece> pieces,
        int first,
        int last,
        double max,
        double indent,
        Paragraph paragraph,
        FontCollection fonts,
        bool isLast,
        HorizontalAlignment? inheritedAlignment,
        bool includeMarker,
        List<TabStop>? sortedStops,
        StyleResolution? resolution)
    {
        var count = 0;
        for (var w = first; w <= last; w++)
        {
            count += words[w].PieceCount;
        }

        var fragments = new List<LineFragment>(count);
        double advances = 0;
        var hasTabs = false;
        for (var w = first; w <= last; w++)
        {
            var word = words[w];
            for (var p = word.PieceStart; p < word.PieceStart + word.PieceCount; p++)
            {
                var piece = pieces[p];
                fragments.Add(new LineFragment
                {
                    Run = piece.Run,
                    Font = piece.Font,
                    Text = piece.Text,
                    Start = piece.Start,
                    Length = piece.Length,
                    Advance = piece.Advance,
                });
                advances += piece.Advance;
            }

            if (w < last && word.TabsAfter > 0)
            {
                hasTabs = true;
            }
        }

        var span = CollectionsMarshal.AsSpan(fragments);

        if (paragraph.TabStops.Count > 0)
        {
            return BuildTabStopLine(span, fragments, words, first, last, indent, paragraph, fonts, sortedStops!, resolution);
        }

        // Natural placement from 0; tab stops are relative to the line origin.
        var cursor = 0.0;
        var fi = 0;
        for (var w = first; w <= last; w++)
        {
            for (var p = 0; p < words[w].PieceCount; p++)
            {
                span[fi].XOffset = cursor;
                cursor += span[fi].Advance;
                fi++;
            }

            if (w < last)
            {
                cursor = NextStart(cursor, words[w]);
            }
        }

        var naturalWidth = cursor;

        if (paragraph.RightTabStop)
        {
            var lastTab = -1;
            for (var w = first; w < last; w++)
            {
                if (words[w].TabsAfter > 0)
                {
                    lastTab = w;
                }
            }

            var delta = max - naturalWidth;
            if (lastTab >= 0 && delta > 0)
            {
                var skip = 0;
                for (var w = first; w <= lastTab; w++)
                {
                    skip += words[w].PieceCount;
                }

                for (var f = skip; f < span.Length; f++)
                {
                    span[f].XOffset += delta;
                }

                naturalWidth = max;
            }
        }

        // A soft-hyphen break renders a trailing '-' in the preceding text's font; its width is
        // reserved here so right/center alignment and justification account for it and it never
        // spills past the measure.
        var breakHyphen = words[last].SoftHyphenAfter && last < words.Count - 1 && span.Length > 0;
        var hyphenWidth = breakHyphen ? words[last].HyphenWidth : 0.0;

        // Optional-break boundaries (soft hyphen / ZWSP) and no-gap boundaries (inline-image
        // edges with no space) carry no word space and are not stretched by justification, so
        // only real inter-word gaps are counted and widened.
        var gapCount = 0;
        for (var w = first; w < last; w++)
        {
            if (!words[w].OptionalBreakAfter && !words[w].NoGapBoundary)
            {
                gapCount++;
            }
        }

        var alignment = paragraph.ResolveAlignment(inheritedAlignment);
        var justify = alignment == HorizontalAlignment.Justify && !isLast && gapCount > 0 && !hasTabs;

        double x0;
        if (justify)
        {
            x0 = 0;
            var justifiedGap = (max - advances - hyphenWidth) / gapCount;
            cursor = 0;
            fi = 0;
            for (var w = first; w <= last; w++)
            {
                for (var p = 0; p < words[w].PieceCount; p++)
                {
                    span[fi].XOffset = cursor;
                    cursor += span[fi].Advance;
                    fi++;
                }

                if (w < last)
                {
                    cursor += words[w].OptionalBreakAfter || words[w].NoGapBoundary ? 0 : justifiedGap;
                }
            }
        }
        else
        {
            // The reserved hyphen widens the line for right/center placement so the glyphs shift
            // left by its width and the hyphen ends exactly at the measure.
            var alignWidth = naturalWidth + hyphenWidth;
            x0 = alignment switch
            {
                HorizontalAlignment.Right or HorizontalAlignment.End => max - alignWidth,
                HorizontalAlignment.Center => (max - alignWidth) / 2.0,
                _ => 0,
            };

            // An over-wide word would drive x0 negative; clamp so the line never shifts left of the indent.
            if (x0 < 0)
            {
                x0 = 0;
            }
        }

        var shift = indent + x0;
        if (shift != 0)
        {
            for (var f = 0; f < span.Length; f++)
            {
                span[f].XOffset += shift;
            }
        }

        // The hyphen is placed after the span shift (final positions) and before the marker
        // insert (which shifts list indices, invalidating the span).
        var hyphenEnd = 0.0;
        Font? hyphenFont = null;
        if (breakHyphen)
        {
            var tail = span[^1];
            hyphenEnd = tail.XOffset + tail.Advance;
            hyphenFont = tail.Font;
        }

        if (includeMarker && paragraph.MarkerText is { Length: > 0 } markerText)
        {
            var markerFont = ResolvedFont(resolution, paragraph);
            fragments.Insert(0, new LineFragment
            {
                Run = new Run(markerText),
                Font = markerFont,
                Text = markerText,
                Start = 0,
                Length = markerText.Length,
                XOffset = paragraph.MarkerIndent.Point,
                Advance = fonts.MeasureText(markerText, markerFont),
                IsMarker = true,
            });
        }

        if (breakHyphen)
        {
            fragments.Add(new LineFragment
            {
                Run = new Run("-"),
                Font = hyphenFont!,
                Text = "-",
                Start = 0,
                Length = 1,
                XOffset = hyphenEnd,
                Advance = hyphenWidth,
            });
        }

        var box = new LineBox { Fragments = fragments, Width = naturalWidth };
        Measure(box, paragraph.LineSpacing, fonts);
        return box;
    }

    // Explicit tab stops: place each tab-delimited segment against the next stop at or beyond the
    // cursor, applying that stop's alignment. Paragraph-alignment shifting is not applied here so the
    // stops stay put; wrapped lines with no tabs still land left at the indent.
    private static LineBox BuildTabStopLine(
        Span<LineFragment> span,
        List<LineFragment> fragments,
        List<Word> words,
        int first,
        int last,
        double indent,
        Paragraph paragraph,
        FontCollection fonts,
        List<TabStop> stops,
        StyleResolution? resolution)
    {
        double naturalWidth = 0;
        var cursor = 0.0;
        var fi = 0;
        var w = first;
        var tabsBefore = 0;
        List<LineFragment>? leaders = null;
        while (w <= last)
        {
            var segEnd = w;
            while (segEnd < last && words[segEnd].TabsAfter == 0)
            {
                segEnd++;
            }

            var gapStart = cursor;
            var alignment = TabAlignment.Left;
            var leaderChar = '\0';
            var stopPos = cursor;
            for (var t = 0; t < tabsBefore; t++)
            {
                if (TryNextStop(stops, cursor, out var nextPos, out var nextAlign, out var nextLeader))
                {
                    stopPos = nextPos;
                    alignment = nextAlign;
                    leaderChar = nextLeader;
                }
                else
                {
                    stopPos = AdvanceToTabStop(cursor);
                    alignment = TabAlignment.Left;
                    leaderChar = '\0';
                }

                cursor = stopPos;
            }

            var (segWidth, decimalOffset) = MeasureSegment(span, words, w, segEnd, fi, fonts);

            var start = tabsBefore == 0
                ? cursor
                : alignment switch
                {
                    TabAlignment.Right => stopPos - segWidth,
                    TabAlignment.Center => stopPos - (segWidth / 2.0),
                    TabAlignment.Decimal => stopPos - decimalOffset,
                    _ => stopPos,
                };

            // A right/center/decimal segment wider than the space before its stop would start
            // left of where the previous segment ended and paint over it; clamp to gapStart so
            // it flows left-aligned from the cursor instead, matching word-processor behavior.
            if (tabsBefore > 0 && start < gapStart)
            {
                start = gapStart;
            }

            if (tabsBefore > 0 && leaderChar != '\0' && start > gapStart + 1e-6)
            {
                var leaderFont = fi < span.Length ? span[fi].Font : ResolvedFont(resolution, paragraph);
                (leaders ??= []).Add(BuildLeader(leaderChar, gapStart, start, indent, leaderFont, fonts));
            }

            var x = start;
            for (var ww = w; ww <= segEnd; ww++)
            {
                for (var p = 0; p < words[ww].PieceCount; p++)
                {
                    span[fi].XOffset = x;
                    x += span[fi].Advance;
                    fi++;
                }

                if (ww < segEnd)
                {
                    x += words[ww].GapAfter;
                }
            }

            cursor = x;
            naturalWidth = Math.Max(naturalWidth, cursor);
            tabsBefore = segEnd < last ? words[segEnd].TabsAfter : 0;
            w = segEnd + 1;
        }

        if (indent != 0)
        {
            for (var f = 0; f < span.Length; f++)
            {
                span[f].XOffset += indent;
            }
        }

        if (leaders is not null)
        {
            fragments.AddRange(leaders);
        }

        var box = new LineBox { Fragments = fragments, Width = naturalWidth };
        Measure(box, paragraph.LineSpacing, fonts);
        return box;
    }

    // A run of the leader character right-aligned to the tab position (gapEnd), filling as
    // much of [gapStart, gapEnd) as whole leader glyphs allow. XOffset already carries the
    // paragraph indent so leaders share the segments' coordinate space.
    private static LineFragment BuildLeader(char leader, double gapStart, double gapEnd, double indent, Font font, FontCollection fonts)
    {
        var text = leader.ToString();
        var leaderWidth = fonts.MeasureText(text, font);
        var count = leaderWidth > 0 ? (int)Math.Floor((gapEnd - gapStart) / leaderWidth) : 0;
        if (count <= 0)
        {
            return new LineFragment { Run = new Run(string.Empty), Font = font, Text = string.Empty, Start = 0, Length = 0, Advance = 0 };
        }

        var advance = count * leaderWidth;
        var fill = new string(leader, count);
        return new LineFragment
        {
            Run = new Run(fill),
            Font = font,
            Text = fill,
            Start = 0,
            Length = count,
            XOffset = indent + gapEnd - advance,
            Advance = advance,
        };
    }

    // Segment width (advances plus interior word gaps) and the offset from the segment start to its
    // first '.' (decimal alignment); falls back to the full width when there is no separator.
    private static (double Width, double DecimalOffset) MeasureSegment(
        Span<LineFragment> span, List<Word> words, int wStart, int wEnd, int fiStart, FontCollection fonts)
    {
        double width = 0;
        double decimalOffset = -1;
        var f = fiStart;
        for (var ww = wStart; ww <= wEnd; ww++)
        {
            for (var p = 0; p < words[ww].PieceCount; p++)
            {
                var fragment = span[f];
                if (decimalOffset < 0)
                {
                    var dot = fragment.Text.IndexOf('.', StringComparison.Ordinal);
                    if (dot >= 0)
                    {
                        decimalOffset = width + fonts.MeasureText(fragment.Text[..dot], fragment.Font);
                    }
                }

                width += fragment.Advance;
                f++;
            }

            if (ww < wEnd)
            {
                width += words[ww].GapAfter;
            }
        }

        return (width, decimalOffset < 0 ? width : decimalOffset);
    }

    private static bool TryNextStop(List<TabStop> stops, double cursor, out double position, out TabAlignment alignment, out char leader)
    {
        for (var i = 0; i < stops.Count; i++)
        {
            if (stops[i].Position.Point > cursor + 1e-6)
            {
                position = stops[i].Position.Point;
                alignment = stops[i].Alignment;
                leader = stops[i].Leader;
                return true;
            }
        }

        position = 0;
        alignment = TabAlignment.Left;
        leader = '\0';
        return false;
    }

    private static void Measure(LineBox box, double lineSpacing, FontCollection fonts)
    {
        double natural = 0;
        double baseline = 0;
        var fragments = box.Fragments;
        for (var i = 0; i < fragments.Count; i++)
        {
            // An inline image sits on the baseline: its full height is both extent and ascent.
            var (h, asc) = fragments[i].Run is InlineImage image
                ? (image.EffectiveSize().Height, image.EffectiveSize().Height)
                : FontExtent(fragments[i].Font, fonts);
            if (fragments[i].Run.VerticalAlign != RunVerticalAlign.None)
            {
                (h, asc) = ScriptExtent(fragments[i].Run, fragments[i].Font, h, asc);
            }

            natural = Math.Max(natural, h);
            baseline = Math.Max(baseline, asc);
        }

        box.Height = natural * lineSpacing;
        box.Baseline = baseline;
    }

    // Scales the full-size extent down to the script size and grows it by the text
    // rise (above the baseline for superscript, below for subscript) so the line
    // reserves the risen glyphs' actual vertical span.
    private static (double Height, double Ascent) ScriptExtent(Run run, Font font, double height, double ascent)
    {
        var scale = run.ScriptScale;
        var rise = run.ScriptRise(font.Size);
        var descent = (height - ascent) * scale;
        ascent = (ascent * scale) + Math.Max(rise, 0);
        return (ascent + descent + Math.Max(-rise, 0), ascent);
    }

    // A run's measured advance: the plain measurement scaled to the script size, plus
    // letter spacing per inter-glyph gap (spacing * (code points - 1)).
    private static double MeasureRun(FontCollection fonts, Run run, Font font, string text)
    {
        var advance = fonts.MeasureText(text, font) * run.ScriptScale;
        var spacing = run.LetterSpacing.Point;
        if (spacing != 0 && text.Length > 0)
        {
            advance += spacing * (CountCodePoints(text) - 1);
        }

        return advance;
    }

    private static int CountCodePoints(string text)
    {
        var count = 0;
        var i = 0;
        while (i < text.Length)
        {
            i += char.IsHighSurrogate(text[i]) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]) ? 2 : 1;
            count++;
        }

        return count;
    }

    private static (double Height, double Ascent) FontExtent(Font font, FontCollection fonts)
    {
        var size = font.Size;
        if (fonts.TryResolvePrimary(font, out var face))
        {
            var upm = face.UnitsPerEm;
            return ((face.Ascent - face.Descent + face.LineGap) * size / upm, face.Ascent * size / upm);
        }

        return (size * 1.2, size * 0.9);
    }
}
