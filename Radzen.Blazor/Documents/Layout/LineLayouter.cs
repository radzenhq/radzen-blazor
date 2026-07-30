using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.InteropServices;
using Radzen.Documents.Fonts;
using Radzen.Documents.Geometry;

namespace Radzen.Documents.Layout;

internal sealed class LineLayoutContext
{
    public required Paragraph Paragraph { get; init; }

    public required FontCollection Fonts { get; init; }

    public required double MaxWidth { get; init; }

    public required double Indent { get; init; }

    public HorizontalAlignment? InheritedAlignment { get; init; }

    public LoweringContext? Resolution { get; init; }

    public List<TabStop>? SortedTabStops { get; init; }

    public required LayoutCaptureContext Capture { get; init; }
}

internal readonly record struct LineBuildRequest(
    int First,
    int Last,
    bool IsLast,
    bool IncludeMarker);

internal static class LineLayouter
{
    private const double DefaultTabStopWidth = 36.0;

    internal static double AdvanceToTabStop(double position)
        => (Math.Floor((position + LayoutTolerance.Epsilon) / DefaultTabStopWidth) + 1) * DefaultTabStopWidth;

    internal static LineBox MarkerLine(
        ListMarkerLayout marker,
        FontCollection fonts,
        LayoutCaptureContext capture)
    {
        var run = new Run(marker.Text);
        var paint = GeometryCapture.Fragment(run, marker.Font, capture);
        var fragment = new LineFragment
        {
            Source = capture.Source(run),
            Paint = paint,
            Text = marker.Text,
            Start = 0,
            Length = marker.Text.Length,
            XOffset = marker.Indent,
            Advance = fonts.MeasureText(marker.Text, marker.Font),
            IsMarker = true,
            Face = fonts.ResolveFace(marker.Font),
            GlyphRun = GeometryCapture.PositionSpans(
                fonts.CaptureGlyphRun(marker.Text, marker.Font),
                paint),
        };
        var fragments = ImmutableArray.Create(fragment);
        var (height, baseline) = Measure(fragments, 1, fonts);
        return new LineBox
        {
            Fragments = fragments,
            Width = 0,
            Height = height,
            Baseline = baseline,
        };
    }

    private static Font ResolvedFont(LoweringContext? resolution, Paragraph paragraph)
        => resolution?.ParagraphFont(paragraph) ?? paragraph.Font;

    public static IReadOnlyList<LineBox> Layout(
        Paragraph paragraph,
        double maxWidthPoints,
        FontCollection fonts,
        LayoutCaptureContext capture,
        HorizontalAlignment? inheritedAlignment = null,
        LoweringContext? resolution = null)
    {
        var boxes = new List<LineBox>();
        var indent = LoweringContext.FormatOf(resolution, paragraph).LeftIndent.Point
            + (resolution?.BlockIndent(paragraph) ?? 0);
        var max = maxWidthPoints - indent;
        var wrapStops = SortedTabStops(paragraph);
        var context = new LineLayoutContext
        {
            Paragraph = paragraph,
            Fonts = fonts,
            MaxWidth = max,
            Indent = indent,
            InheritedAlignment = inheritedAlignment,
            Resolution = resolution,
            SortedTabStops = wrapStops,
            Capture = capture,
        };
        var markerPending = true;
        var tokenization = LineTokenizer.Tokenize(paragraph, fonts, resolution);
        var pieces = tokenization.Pieces;
        foreach (var words in tokenization.Segments)
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
                    OversizedWordPlacement(boxes, words[first], pieces, context, includeMarker);
                }
                else
                {
                    boxes.Add(BuildLine(words, pieces, context, new LineBuildRequest(first, last, isLast, includeMarker)));
                }
            }
        }

        return boxes;
    }

    private static LineBox EmptyLine(Paragraph paragraph, FontCollection fonts, LoweringContext? resolution)
    {
        var font = ResolvedFont(resolution, paragraph);
        var captured = GeometryCapture.Font(font);
        var (height, ascent) = FontExtent(captured, fonts);
        return new LineBox
        {
            Fragments = [],
            Width = 0,
            Height = height * paragraph.LineSpacing,
            Baseline = ascent,
        };
    }

    private static double NextStart(double position, LineWord word, List<TabStop>? stops = null)
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

    private static List<(int First, int Last)> Wrap(List<LineWord> words, double max, List<TabStop>? stops)
    {
        var lines = new List<(int, int)>();
        var i = 0;
        while (i < words.Count)
        {
            var j = i;
            var end = words[i].Width;
            while (j + 1 < words.Count)
            {
                var (nextEnd, through) = NextSegment(words, j, end, stops);
                if (nextEnd <= max)
                {
                    end = nextEnd;
                    j = through;
                }
                else
                {
                    break;
                }
            }

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

    private static (double End, int Through) NextSegment(
        List<LineWord> words, int from, double end, List<TabStop>? stops)
    {
        var gapStart = end + words[from].GapAfter;
        var stopPosition = gapStart;
        var alignment = TabAlignment.Left;
        for (var t = 0; t < words[from].TabsAfter; t++)
        {
            if (stops is not null && TryNextStop(stopPosition, stops, out var next, out var nextAlignment))
            {
                stopPosition = next;
                alignment = nextAlignment;
            }
            else
            {
                stopPosition = AdvanceToTabStop(stopPosition);
                alignment = TabAlignment.Left;
            }
        }

        if (alignment is not (TabAlignment.Right or TabAlignment.Center))
        {
            return (stopPosition + words[from + 1].Width, from + 1);
        }

        var through = from + 1;
        var width = words[through].Width;
        while (through < words.Count - 1 && words[through].TabsAfter == 0)
        {
            width += words[through].GapAfter + words[through + 1].Width;
            through++;
        }

        var aligned = alignment == TabAlignment.Right
            ? stopPosition
            : stopPosition + (width / 2.0);
        return (Math.Max(aligned, gapStart + width), through);
    }

    private static bool TryNextStop(
        double cursor, List<TabStop> stops, out double position, out TabAlignment alignment)
    {
        var found = TryNextStop(stops, cursor, out position, out alignment, out _);
        return found;
    }

    private static double LineNaturalWidth(List<LineWord> words, int i, int j, List<TabStop>? stops)
    {
        var end = words[i].Width;
        for (var w = i; w < j; w++)
        {
            end = NextStart(end, words[w], stops) + words[w + 1].Width;
        }

        return end;
    }

    private static bool IsHyphenBreak(char c) => c is '-' or '\u2013' or '\u2014';

    private static bool IsBreakable(LineWord word, List<LinePiece> pieces)
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

    private static void OversizedWordPlacement(
        List<LineBox> boxes,
        LineWord word,
        List<LinePiece> pieces,
        LineLayoutContext context,
        bool includeMarker)
    {
        var max = context.MaxWidth;
        var paragraph = context.Paragraph;
        var fonts = context.Fonts;
        var fragments = new List<LineFragment>();
        var lineWidth = 0.0;
        var markerPending = includeMarker;
        var advances = new Dictionary<(Font, int), double>();

        for (var p = word.PieceStart; p < word.PieceStart + word.PieceCount; p++)
        {
            var piece = pieces[p];
            var styled = (TextInline)piece.Run;
            var text = piece.Text;
            var font = piece.Font;
            var fragStart = 0;
            var fragAdvance = 0.0;
            var hyphenBreak = -1;
            var hyphenAdvance = 0.0;
            var i = 0;
            while (i < text.Length)
            {
                var codepoint = FontCollection.CodePointAt(text, i, out var cpLen);
                if (!advances.TryGetValue((font, codepoint), out var baseAdvance))
                {
                    baseAdvance = fonts.MeasureText(text.Substring(i, cpLen), font);
                    advances[(font, codepoint)] = baseAdvance;
                }

                var step = RunTextAdvance.Calculate(
                    baseAdvance, 1, codepoint == ' ' ? 1 : 0, styled,
                    leadingCharacterSpacing: lineWidth > 0 || fragAdvance > 0);
                if ((lineWidth > 0 || fragAdvance > 0) && lineWidth + fragAdvance + step > max)
                {
                    if (hyphenBreak > fragStart && hyphenBreak <= i)
                    {
                        fragments.Add(MakeCharFragment(
                            piece,
                            fragStart,
                            hyphenBreak,
                            hyphenAdvance,
                            context.Capture));
                        lineWidth += hyphenAdvance;
                        boxes.Add(FinishOversizedLine(fragments, lineWidth, context, ref markerPending));
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
                        fragments.Add(MakeCharFragment(
                            piece,
                            fragStart,
                            i,
                            fragAdvance,
                            context.Capture));
                        lineWidth += fragAdvance;
                    }

                    boxes.Add(FinishOversizedLine(fragments, lineWidth, context, ref markerPending));
                    fragments = [];
                    lineWidth = 0.0;
                    fragStart = i;
                    fragAdvance = 0.0;
                    hyphenBreak = -1;
                    hyphenAdvance = 0.0;
                    step = RunTextAdvance.Calculate(
                        baseAdvance, 1, codepoint == ' ' ? 1 : 0, styled);
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
                fragments.Add(MakeCharFragment(
                    piece,
                    fragStart,
                    text.Length,
                    fragAdvance,
                    context.Capture));
                lineWidth += fragAdvance;
            }
        }

        if (fragments.Count > 0)
        {
            boxes.Add(FinishOversizedLine(fragments, lineWidth, context, ref markerPending));
        }
    }

    private static LineFragment MakeCharFragment(
        LinePiece piece,
        int startInText,
        int endInText,
        double advance,
        LayoutCaptureContext capture)
        => new()
        {
            Source = capture.Source(piece.Run),
            Paint = GeometryCapture.Fragment(piece.Run, piece.Font, capture),
            Text = piece.Text[startInText..endInText],
            Start = piece.Start + startInText,
            Length = endInText - startInText,
            Advance = advance,
            GlyphRun = CapturedGlyphRun.Empty(piece.Text[startInText..endInText]),
        };

    private static LineBox FinishOversizedLine(
        List<LineFragment> fragments,
        double width,
        LineLayoutContext context,
        ref bool markerPending)
    {
        var max = context.MaxWidth;
        var indent = context.Indent;
        var inheritedAlignment = context.InheritedAlignment;
        var paragraph = context.Paragraph;
        var alignment = paragraph.ResolveAlignment(inheritedAlignment);
        var x0 = HorizontalAlignmentOffset.Of(alignment, max, width);

        var span = CollectionsMarshal.AsSpan(fragments);
        var cursor = indent + x0;
        for (var f = 0; f < span.Length; f++)
        {
            span[f] = span[f] with { XOffset = cursor };
            cursor += span[f].Advance;
        }

        var includeMarker = markerPending;
        markerPending = false;
        return FinalizeLine(fragments, width, context, includeMarker, default);
    }

    private static LineBox BuildLine(
        List<LineWord> words,
        List<LinePiece> pieces,
        LineLayoutContext context,
        LineBuildRequest request)
    {
        var first = request.First;
        var last = request.Last;
        var paragraph = context.Paragraph;
        var fragments = CreateFragments(words, pieces, first, last, context.Capture, out var advances, out var hasTabs);
        var span = CollectionsMarshal.AsSpan(fragments);
        var placement = paragraph.TabStops.Count > 0
            ? ExplicitTabPlacement(span, fragments, words, first, last, context)
            : PlainLinePlacement(span, words, request, context, advances, hasTabs);
        return FinalizeLine(fragments, placement.Width, context, request.IncludeMarker, placement.Hyphen);
    }

    private static List<LineFragment> CreateFragments(
        List<LineWord> words,
        List<LinePiece> pieces,
        int first,
        int last,
        LayoutCaptureContext capture,
        out double advances,
        out bool hasTabs)
    {
        var count = 0;
        for (var w = first; w <= last; w++)
        {
            count += words[w].PieceCount;
        }

        var fragments = new List<LineFragment>(count);
        advances = 0;
        hasTabs = false;
        for (var w = first; w <= last; w++)
        {
            var word = words[w];
            for (var p = word.PieceStart; p < word.PieceStart + word.PieceCount; p++)
            {
                var piece = pieces[p];
                fragments.Add(new LineFragment
                {
                    Source = capture.Source(piece.Run),
                    Paint = GeometryCapture.Fragment(piece.Run, piece.Font, capture),
                    Text = piece.Text,
                    Start = piece.Start,
                    Length = piece.Length,
                    Advance = piece.Advance,
                    GlyphRun = CapturedGlyphRun.Empty(piece.Text),
                });
                advances += piece.Advance;
            }

            if (w < last && word.TabsAfter > 0)
            {
                hasTabs = true;
            }
        }

        return fragments;
    }

    private static LinePlacement PlainLinePlacement(
        Span<LineFragment> span,
        List<LineWord> words,
        LineBuildRequest request,
        LineLayoutContext context,
        double advances,
        bool hasTabs)
    {
        var first = request.First;
        var last = request.Last;
        var max = context.MaxWidth;
        var paragraph = context.Paragraph;

        var cursor = 0.0;
        var fi = 0;
        for (var w = first; w <= last; w++)
        {
            for (var p = 0; p < words[w].PieceCount; p++)
            {
                span[fi] = span[fi] with { XOffset = cursor };
                cursor += span[fi].Advance;
                fi++;
            }

            if (w < last)
            {
                cursor = NextStart(cursor, words[w]);
            }
        }

        var naturalWidth = cursor;

        var breakHyphen = words[last].SoftHyphenAfter && last < words.Count - 1 && span.Length > 0;
        var hyphenWidth = breakHyphen ? words[last].HyphenWidth : 0.0;

        var gapCount = 0;
        for (var w = first; w < last; w++)
        {
            if (!words[w].OptionalBreakAfter && !words[w].NoGapBoundary)
            {
                gapCount++;
            }
        }

        var alignment = paragraph.ResolveAlignment(context.InheritedAlignment);
        var justify = alignment == HorizontalAlignment.Justify && !request.IsLast && gapCount > 0 && !hasTabs;

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
                    span[fi] = span[fi] with { XOffset = cursor };
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
            var alignWidth = naturalWidth + hyphenWidth;
            x0 = HorizontalAlignmentOffset.Of(alignment, max, alignWidth);
        }

        var shift = context.Indent + x0;
        if (shift != 0)
        {
            for (var f = 0; f < span.Length; f++)
            {
                span[f] = span[f] with { XOffset = span[f].XOffset + shift };
            }
        }

        if (breakHyphen)
        {
            var tail = span[^1];
            return new LinePlacement(
                naturalWidth,
                new HyphenPlacement(true, tail.XOffset + tail.Advance, tail.Paint.Font, hyphenWidth));
        }

        return new LinePlacement(naturalWidth, default);
    }

    private static LineBox FinalizeLine(
        List<LineFragment> fragments,
        double width,
        LineLayoutContext context,
        bool includeMarker,
        HyphenPlacement hyphen)
    {
        var paragraph = context.Paragraph;
        var fonts = context.Fonts;
        var listMarker = context.Resolution?.ListMarker(paragraph);
        if (includeMarker && listMarker is { Text.Length: > 0 } marker)
        {
            var markerText = marker.Text;
            var markerFont = marker.Font;
            var markerRun = new Run(markerText);
            fragments.Insert(0, new LineFragment
            {
                Source = context.Capture.Source(markerRun),
                Paint = GeometryCapture.Fragment(markerRun, markerFont, context.Capture),
                Text = markerText,
                Start = 0,
                Length = markerText.Length,
                XOffset = marker.Indent,
                Advance = fonts.MeasureText(markerText, markerFont),
                IsMarker = true,
                GlyphRun = CapturedGlyphRun.Empty(markerText),
            });
        }

        if (hyphen.Include)
        {
            var hyphenRun = new Run("-");
            fragments.Add(new LineFragment
            {
                Source = context.Capture.Source(hyphenRun),
                Paint = GeometryCapture.Fragment(
                    hyphenRun,
                    hyphen.Font,
                    context.Capture),
                Text = "-",
                Start = 0,
                Length = 1,
                XOffset = hyphen.XOffset,
                Advance = hyphen.Width,
                GlyphRun = CapturedGlyphRun.Empty("-"),
            });
        }

        for (var i = 0; i < fragments.Count; i++)
        {
            var fragment = fragments[i];
            var font = fragment.Paint.Font;
            fragments[i] = fragment with
            {
                Face = fonts.ResolveFace(font),
                GlyphRun = GeometryCapture.PositionSpans(
                    fonts.CaptureGlyphRun(fragment.Text, font),
                    fragment.Paint),
            };
        }

        CaptureCoalescedGlyphRuns(fragments, fonts, context.Capture);
        var immutableFragments = fragments.ToImmutableArray();
        var (height, baseline) = Measure(immutableFragments, paragraph.LineSpacing, fonts);
        return new LineBox
        {
            Fragments = immutableFragments,
            Width = width,
            Height = height,
            Baseline = baseline,
        };
    }

    private static void CaptureCoalescedGlyphRuns(
        List<LineFragment> fragments,
        FontCollection fonts,
        LayoutCaptureContext capture)
    {
        var i = 0;
        while (i < fragments.Count)
        {
            var current = fragments[i];
            var run = current.Source;
            var paint = current.Paint;
            var text = capture.Resolve<Inline>(run) is TextInline inline ? inline.LayoutText : string.Empty;
            var end = current.Start + current.Length;
            var right = current.XOffset + current.Advance;
            var j = i + 1;
            var mergeable = paint.LetterSpacing == 0 && !paint.IsScript;
            while (j < fragments.Count && current.Length > 0 && mergeable)
            {
                var next = fragments[j];
                if (next.Source != run || next.Length == 0 || next.Start < end || next.Start > text.Length)
                {
                    break;
                }

                var allSpaces = true;
                for (var g = end; g < next.Start; g++)
                {
                    if (text[g] != ' ')
                    {
                        allSpaces = false;
                        break;
                    }
                }

                var gapWidth = next.Start == end
                    ? 0
                    : RunTextAdvance.Measure(
                        fonts, paint, text[end..next.Start],
                        leadingCharacterSpacing: true, trailingCharacterSpacing: true);
                if (!allSpaces || Math.Abs(next.XOffset - right - gapWidth) > 0.001)
                {
                    break;
                }

                end = next.Start + next.Length;
                right = next.XOffset + next.Advance;
                j++;
            }

            if (j > i + 1)
            {
                fragments[i] = current with
                {
                    CoalescedGlyphRun = GeometryCapture.PositionSpans(
                        fonts.CaptureGlyphRun(
                            text[current.Start..end],
                            paint.Font),
                        paint),
                };
                for (var merged = i + 1; merged < j; merged++)
                {
                    fragments[merged] = fragments[merged] with { SuppressTextEmission = true };
                }
            }

            i = j;
        }
    }

    private static LinePlacement ExplicitTabPlacement(
        Span<LineFragment> span,
        List<LineFragment> fragments,
        List<LineWord> words,
        int first,
        int last,
        LineLayoutContext context)
    {
        var indent = context.Indent;
        var paragraph = context.Paragraph;
        var fonts = context.Fonts;
        var stops = context.SortedTabStops!;
        var resolution = context.Resolution;
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

            if (tabsBefore > 0 && start < gapStart)
            {
                start = gapStart;
            }

            if (tabsBefore > 0 && leaderChar != '\0' && start > gapStart + LayoutTolerance.Epsilon)
            {
                var leaderFont = fi < span.Length
                    ? span[fi].Paint.Font
                    : GeometryCapture.Font(ResolvedFont(resolution, paragraph));
                (leaders ??= []).Add(BuildLeader(
                    leaderChar,
                    gapStart,
                    start,
                    indent,
                    leaderFont,
                    fonts,
                    context.Capture));
            }

            var x = start;
            for (var ww = w; ww <= segEnd; ww++)
            {
                for (var p = 0; p < words[ww].PieceCount; p++)
                {
                    span[fi] = span[fi] with { XOffset = x };
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
                span[f] = span[f] with { XOffset = span[f].XOffset + indent };
            }
        }

        if (leaders is not null)
        {
            fragments.AddRange(leaders);
        }

        return new LinePlacement(naturalWidth, default);
    }

    private static LineFragment BuildLeader(
        char leader,
        double gapStart,
        double gapEnd,
        double indent,
        in FontPaint font,
        FontCollection fonts,
        LayoutCaptureContext capture)
    {
        var text = leader.ToString();
        var leaderWidth = fonts.MeasureText(text, font);
        var count = leaderWidth > 0 ? (int)Math.Floor((gapEnd - gapStart) / leaderWidth) : 0;
        if (count <= 0)
        {
            var empty = new Run(string.Empty);
            return new LineFragment
            {
                Source = capture.Source(empty),
                Paint = GeometryCapture.Fragment(empty, font, capture),
                Text = string.Empty,
                Start = 0,
                Length = 0,
                Advance = 0,
                GlyphRun = CapturedGlyphRun.Empty(string.Empty),
            };
        }

        var advance = count * leaderWidth;
        var fill = new string(leader, count);
        var leaderRun = new Run(fill);
        return new LineFragment
        {
            Source = capture.Source(leaderRun),
            Paint = GeometryCapture.Fragment(leaderRun, font, capture),
            Text = fill,
            Start = 0,
            Length = count,
            XOffset = indent + gapEnd - advance,
            Advance = advance,
            GlyphRun = CapturedGlyphRun.Empty(fill),
        };
    }

    private static (double Width, double DecimalOffset) MeasureSegment(
        Span<LineFragment> span, List<LineWord> words, int wStart, int wEnd, int fiStart, FontCollection fonts)
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
                        decimalOffset = width + fonts.MeasureText(
                            fragment.Text[..dot],
                            fragment.Paint.Font);
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
            if (stops[i].Position.Point > cursor + LayoutTolerance.Epsilon)
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

    private static (double Height, double Baseline) Measure(
        ImmutableArray<LineFragment> fragments, double lineSpacing, FontCollection fonts)
    {
        double natural = 0;
        double baseline = 0;
        for (var i = 0; i < fragments.Length; i++)
        {
            var paint = fragments[i].Paint;
            var (h, asc) = paint.InlineImage is { } image
                ? (image.Height, image.Height)
                : FontExtent(paint.Font, fonts);
            if (paint.IsScript)
            {
                (h, asc) = ScriptExtent(paint, h, asc);
            }

            natural = Math.Max(natural, h);
            baseline = Math.Max(baseline, asc);
        }

        return (natural * lineSpacing, baseline);
    }

    private static (double Height, double Ascent) ScriptExtent(in FragmentPaint paint, double height, double ascent)
    {
        var scale = paint.ScriptScale;
        var rise = paint.Rise;
        var descent = (height - ascent) * scale;
        ascent = (ascent * scale) + Math.Max(rise, 0);
        return (ascent + descent + Math.Max(-rise, 0), ascent);
    }

    private static (double Height, double Ascent) FontExtent(
        in FontPaint font,
        FontCollection fonts)
    {
        var size = font.Size;
        if (fonts.TryResolvePrimary(font, out var face))
        {
            var upm = face.UnitsPerEm;
            return ((face.Ascent - face.Descent + face.LineGap) * size / upm, face.Ascent * size / upm);
        }

        return (size * 1.2, size * 0.9);
    }

    private readonly record struct LinePlacement(double Width, HyphenPlacement Hyphen);

    private readonly record struct HyphenPlacement(bool Include, double XOffset, FontPaint Font, double Width);
}
