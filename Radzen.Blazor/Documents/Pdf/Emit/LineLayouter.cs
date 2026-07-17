using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Radzen.Documents.Pdf.Emit;

internal static class LineLayouter
{
    private const double DefaultTabStopWidth = 36.0;

    internal static double AdvanceToTabStop(double position)
        => (Math.Floor((position + 1e-6) / DefaultTabStopWidth) + 1) * DefaultTabStopWidth;

    private static Font ResolvedFont(StyleResolution? resolution, Paragraph paragraph)
        => resolution?.ParagraphFont(paragraph) ?? paragraph.Font;

    public static IReadOnlyList<LineBox> Layout(
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
        var context = new LineLayoutContext
        {
            Paragraph = paragraph,
            Fonts = fonts,
            MaxWidth = max,
            Indent = indent,
            InheritedAlignment = inheritedAlignment,
            Resolution = resolution,
            SortedTabStops = wrapStops,
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
            var text = piece.Text;
            var font = piece.Font;
            var fragStart = 0;
            var fragAdvance = 0.0;
            var hyphenBreak = -1;
            var hyphenAdvance = 0.0;
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
                    if (hyphenBreak > fragStart && hyphenBreak <= i)
                    {
                        fragments.Add(MakeCharFragment(piece, fragStart, hyphenBreak, hyphenAdvance));
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
                        fragments.Add(MakeCharFragment(piece, fragStart, i, fragAdvance));
                        lineWidth += fragAdvance;
                    }

                    boxes.Add(FinishOversizedLine(fragments, lineWidth, context, ref markerPending));
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
            boxes.Add(FinishOversizedLine(fragments, lineWidth, context, ref markerPending));
        }
    }

    private static LineFragment MakeCharFragment(LinePiece piece, int startInText, int endInText, double advance)
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
        LineLayoutContext context,
        ref bool markerPending)
    {
        var max = context.MaxWidth;
        var indent = context.Indent;
        var inheritedAlignment = context.InheritedAlignment;
        var paragraph = context.Paragraph;
        var alignment = paragraph.ResolveAlignment(inheritedAlignment);
        var x0 = alignment switch
        {
            HorizontalAlignment.Right or HorizontalAlignment.End => max - width,
            HorizontalAlignment.Center => (max - width) / 2.0,
            _ => 0,
        };

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
        var fragments = CreateFragments(words, pieces, first, last, out var advances, out var hasTabs);
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
            var alignWidth = naturalWidth + hyphenWidth;
            x0 = alignment switch
            {
                HorizontalAlignment.Right or HorizontalAlignment.End => max - alignWidth,
                HorizontalAlignment.Center => (max - alignWidth) / 2.0,
                _ => 0,
            };

            if (x0 < 0)
            {
                x0 = 0;
            }
        }

        var shift = context.Indent + x0;
        if (shift != 0)
        {
            for (var f = 0; f < span.Length; f++)
            {
                span[f].XOffset += shift;
            }
        }

        if (breakHyphen)
        {
            var tail = span[^1];
            return new LinePlacement(
                naturalWidth,
                new HyphenPlacement(true, tail.XOffset + tail.Advance, tail.Font, hyphenWidth));
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
        if (includeMarker && paragraph.MarkerText is { Length: > 0 } markerText)
        {
            var markerFont = ResolvedFont(context.Resolution, paragraph);
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

        if (hyphen.Include)
        {
            fragments.Add(new LineFragment
            {
                Run = new Run("-"),
                Font = hyphen.Font!,
                Text = "-",
                Start = 0,
                Length = 1,
                XOffset = hyphen.XOffset,
                Advance = hyphen.Width,
            });
        }

        var box = new LineBox { Fragments = fragments, Width = width };
        Measure(box, paragraph.LineSpacing, fonts);
        return box;
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

        return new LinePlacement(naturalWidth, default);
    }

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

    private static (double Height, double Ascent) ScriptExtent(Run run, Font font, double height, double ascent)
    {
        var scale = run.ScriptScale;
        var rise = run.ScriptRise(font.Size);
        var descent = (height - ascent) * scale;
        ascent = (ascent * scale) + Math.Max(rise, 0);
        return (ascent + descent + Math.Max(-rise, 0), ascent);
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

    private readonly record struct LinePlacement(double Width, HyphenPlacement Hyphen);

    private readonly record struct HyphenPlacement(bool Include, double XOffset, Font? Font, double Width);
}
