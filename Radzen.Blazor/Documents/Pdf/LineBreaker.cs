using System.Collections.Generic;

namespace Radzen.Documents.Pdf;


internal struct LineFragment
{
    public required Run Run { get; init; }

    public required string Text { get; init; }

    public required int Start { get; init; }

    public required int Length { get; init; }

    public double XOffset { get; set; }

    public required double Advance { get; init; }
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
    }

    private const double DefaultTabStopWidth = 36.0;

    internal static double AdvanceToTabStop(double position)
        => (System.Math.Floor((position + 1e-6) / DefaultTabStopWidth) + 1) * DefaultTabStopWidth;

    public static IReadOnlyList<LineBox> Break(
        Paragraph paragraph,
        double maxWidthPoints,
        FontCollection fonts,
        HorizontalAlignment? inheritedAlignment = null)
    {
        var boxes = new List<LineBox>();
        var indent = paragraph.LeftIndent.Point;
        var max = maxWidthPoints - indent;
        var wrapStops = SortedTabStops(paragraph);
        foreach (var words in Tokenize(paragraph, fonts, out var pieces))
        {
            if (words.Count == 0)
            {
                boxes.Add(EmptyLine(paragraph, fonts));
                continue;
            }

            var lineRanges = Wrap(words, max, wrapStops);
            for (var li = 0; li < lineRanges.Count; li++)
            {
                var (first, last) = lineRanges[li];
                var isLast = li == lineRanges.Count - 1;
                var includeMarker = boxes.Count == 0;
                if (paragraph.TabStops.Count == 0 && first == last && words[first].Width > max
                    && IsBreakable(words[first], pieces))
                {
                    BreakOversizedWord(boxes, words[first], pieces, max, indent, paragraph, fonts, inheritedAlignment, includeMarker);
                }
                else
                {
                    boxes.Add(BuildLine(words, pieces, first, last, max, indent, paragraph, fonts, isLast, inheritedAlignment, includeMarker));
                }
            }
        }

        return boxes;
    }

    // An empty segment (empty paragraph or blank forced-break line) occupies one line
    // of the paragraph's resolved font instead of collapsing to zero height.
    private static LineBox EmptyLine(Paragraph paragraph, FontCollection fonts)
    {
        var font = paragraph.EffectiveFont ?? paragraph.Font;
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
            p = stops is not null && TryNextStop(stops, p, out var stopPos, out _)
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

    private static bool IsInlineWhitespace(char c) => c is ' ' or '\t';

    private static bool IsLineBreak(char c) => c is '\n' or '\r';

    // Splits the paragraph into forced-break segments ('\n', '\r' and "\r\n"), each a
    // list of words separated by breakable whitespace (' ' and '\t'). NBSP is a word
    // character; control characters never enter fragment text.
    private static List<List<Word>> Tokenize(Paragraph paragraph, FontCollection fonts, out List<Piece> pieces)
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
            if (run is InlineImage inlineImage)
            {
                if (hasCurrent)
                {
                    words.Add(current);
                    hasCurrent = false;
                }

                var advance = inlineImage.EffectiveSize().Width;
                words.Add(new Word
                {
                    PieceStart = pieces.Count,
                    PieceCount = 1,
                    Width = advance,
                });
                pieces.Add(new Piece
                {
                    Run = inlineImage,
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

                            current.GapAfter += SpaceWidth(fonts, run.ResolvedFont, spaceWidths);
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

                    var segment = text[start..i];
                    var advance = fonts.MeasureText(segment, run.ResolvedFont);

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
                        Start = start,
                        Length = i - start,
                        Text = segment,
                        Advance = advance,
                    });
                    current.PieceCount++;
                    current.Width += advance;
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

            lines.Add((i, j));
            i = j + 1;
        }

        return lines;
    }

    // A word is emergency-breakable only when every piece carries real text; inline images and
    // empty pieces cannot be split at character granularity, and a non-breaking space (U+00A0)
    // keeps its word intact (it overflows rather than breaks, honoring the author's intent).
    private static bool IsBreakable(Word word, List<Piece> pieces)
    {
        for (var p = word.PieceStart; p < word.PieceStart + word.PieceCount; p++)
        {
            var piece = pieces[p];
            if (piece.Run is InlineImage || piece.Text.Length == 0 || piece.Text.Contains('\u00A0', System.StringComparison.Ordinal))
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
        bool includeMarker)
    {
        var fragments = new List<LineFragment>();
        var lineWidth = 0.0;
        var markerPending = includeMarker;

        for (var p = word.PieceStart; p < word.PieceStart + word.PieceCount; p++)
        {
            var piece = pieces[p];
            var text = piece.Text;
            var font = piece.Run.ResolvedFont;
            var fragStart = 0;
            var fragAdvance = 0.0;
            var i = 0;
            while (i < text.Length)
            {
                var cpLen = char.IsHighSurrogate(text[i]) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1])
                    ? 2
                    : 1;
                var advance = fonts.MeasureText(text.Substring(i, cpLen), font);
                if ((lineWidth > 0 || fragAdvance > 0) && lineWidth + fragAdvance + advance > max)
                {
                    if (i > fragStart)
                    {
                        fragments.Add(MakeCharFragment(piece, fragStart, i, fragAdvance));
                        lineWidth += fragAdvance;
                    }

                    boxes.Add(FinishOversizedLine(fragments, lineWidth, max, indent, paragraph, fonts, inheritedAlignment, ref markerPending));
                    fragments = [];
                    lineWidth = 0.0;
                    fragStart = i;
                    fragAdvance = 0.0;
                }

                fragAdvance += advance;
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
            boxes.Add(FinishOversizedLine(fragments, lineWidth, max, indent, paragraph, fonts, inheritedAlignment, ref markerPending));
        }
    }

    private static LineFragment MakeCharFragment(Piece piece, int startInText, int endInText, double advance)
        => new()
        {
            Run = piece.Run,
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
        ref bool markerPending)
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

        var span = System.Runtime.InteropServices.CollectionsMarshal.AsSpan(fragments);
        var cursor = indent + x0;
        for (var f = 0; f < span.Length; f++)
        {
            span[f].XOffset = cursor;
            cursor += span[f].Advance;
        }

        if (markerPending && paragraph.MarkerText is { Length: > 0 } markerText)
        {
            var markerFont = paragraph.EffectiveFont ?? paragraph.Font;
            fragments.Insert(0, new LineFragment
            {
                Run = new Run(markerText) { EffectiveFont = markerFont },
                Text = markerText,
                Start = 0,
                Length = markerText.Length,
                XOffset = paragraph.MarkerIndent.Point,
                Advance = fonts.MeasureText(markerText, markerFont),
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
        bool includeMarker)
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

        var span = System.Runtime.InteropServices.CollectionsMarshal.AsSpan(fragments);

        if (paragraph.TabStops.Count > 0)
        {
            return BuildTabStopLine(span, fragments, words, first, last, indent, paragraph, fonts);
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

        var wordCount = last - first + 1;
        var gapCount = wordCount - 1;

        var alignment = paragraph.ResolveAlignment(inheritedAlignment);
        var justify = alignment == HorizontalAlignment.Justify && !isLast && gapCount > 0 && !hasTabs;

        double x0;
        if (justify)
        {
            x0 = 0;
            var justifiedGap = (max - advances) / gapCount;
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
                    cursor += justifiedGap;
                }
            }
        }
        else
        {
            x0 = alignment switch
            {
                HorizontalAlignment.Right or HorizontalAlignment.End => max - naturalWidth,
                HorizontalAlignment.Center => (max - naturalWidth) / 2.0,
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

        if (includeMarker && paragraph.MarkerText is { Length: > 0 } markerText)
        {
            var markerFont = paragraph.EffectiveFont ?? paragraph.Font;
            fragments.Insert(0, new LineFragment
            {
                Run = new Run(markerText) { EffectiveFont = markerFont },
                Text = markerText,
                Start = 0,
                Length = markerText.Length,
                XOffset = paragraph.MarkerIndent.Point,
                Advance = fonts.MeasureText(markerText, markerFont),
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
        System.Span<LineFragment> span,
        List<LineFragment> fragments,
        List<Word> words,
        int first,
        int last,
        double indent,
        Paragraph paragraph,
        FontCollection fonts)
    {
        var stops = new List<TabStop>(paragraph.TabStops.Count);
        for (var s = 0; s < paragraph.TabStops.Count; s++)
        {
            stops.Add(paragraph.TabStops[s]);
        }

        stops.Sort((a, b) => a.Position.Point.CompareTo(b.Position.Point));

        double naturalWidth = 0;
        var cursor = 0.0;
        var fi = 0;
        var w = first;
        var tabsBefore = 0;
        while (w <= last)
        {
            var segEnd = w;
            while (segEnd < last && words[segEnd].TabsAfter == 0)
            {
                segEnd++;
            }

            var alignment = TabAlignment.Left;
            var stopPos = cursor;
            for (var t = 0; t < tabsBefore; t++)
            {
                if (TryNextStop(stops, cursor, out var nextPos, out var nextAlign))
                {
                    stopPos = nextPos;
                    alignment = nextAlign;
                }
                else
                {
                    stopPos = AdvanceToTabStop(cursor);
                    alignment = TabAlignment.Left;
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
            naturalWidth = System.Math.Max(naturalWidth, cursor);
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

        var box = new LineBox { Fragments = fragments, Width = naturalWidth };
        Measure(box, paragraph.LineSpacing, fonts);
        return box;
    }

    // Segment width (advances plus interior word gaps) and the offset from the segment start to its
    // first '.' (decimal alignment); falls back to the full width when there is no separator.
    private static (double Width, double DecimalOffset) MeasureSegment(
        System.Span<LineFragment> span, List<Word> words, int wStart, int wEnd, int fiStart, FontCollection fonts)
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
                    var dot = fragment.Text.IndexOf('.', System.StringComparison.Ordinal);
                    if (dot >= 0)
                    {
                        decimalOffset = width + fonts.MeasureText(fragment.Text[..dot], fragment.Run.ResolvedFont);
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

    private static bool TryNextStop(List<TabStop> stops, double cursor, out double position, out TabAlignment alignment)
    {
        for (var i = 0; i < stops.Count; i++)
        {
            if (stops[i].Position.Point > cursor + 1e-6)
            {
                position = stops[i].Position.Point;
                alignment = stops[i].Alignment;
                return true;
            }
        }

        position = 0;
        alignment = TabAlignment.Left;
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
                : FontExtent(fragments[i].Run.ResolvedFont, fonts);
            natural = System.Math.Max(natural, h);
            baseline = System.Math.Max(baseline, asc);
        }

        box.Height = natural * lineSpacing;
        box.Baseline = baseline;
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
