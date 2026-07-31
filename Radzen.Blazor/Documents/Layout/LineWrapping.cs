using System;
using System.Collections.Generic;

namespace Radzen.Documents.Layout;

internal static partial class LineLayouter
{
    private static double NextStart(double position, LineWord word, List<TabStop>? stops = null)
    {
        var next = position + word.GapAfter;
        for (var tab = 0; tab < word.TabsAfter; tab++)
        {
            next = stops is not null && FindNextTabStop(stops, next, out var stop, out _, out _)
                ? stop
                : AdvanceToTabStop(next);
        }

        return next;
    }

    private static List<TabStop>? SortedTabStops(Paragraph paragraph)
    {
        if (paragraph.TabStops.Count == 0)
        {
            return null;
        }

        var stops = new List<TabStop>(paragraph.TabStops.Count);
        for (var index = 0; index < paragraph.TabStops.Count; index++)
        {
            stops.Add(paragraph.TabStops[index]);
        }

        stops.Sort((left, right) => left.Position.Point.CompareTo(right.Position.Point));
        return stops;
    }

    private static List<(int First, int Last)> Wrap(List<LineWord> words, double max, List<TabStop>? stops)
    {
        var lines = new List<(int, int)>();
        var first = 0;
        while (first < words.Count)
        {
            var last = first;
            var end = words[first].Width;
            while (last + 1 < words.Count)
            {
                var (nextEnd, through) = NextSegment(words, last, end, stops);
                if (nextEnd > max)
                {
                    break;
                }

                end = nextEnd;
                last = through;
            }

            while (last > first && words[last].SoftHyphenAfter && last < words.Count - 1
                && LineNaturalWidth(words, first, last, stops) + words[last].HyphenWidth > max)
            {
                last--;
            }

            lines.Add((first, last));
            first = last + 1;
        }

        return lines;
    }

    private static (double End, int Through) NextSegment(
        List<LineWord> words, int from, double end, List<TabStop>? stops)
    {
        var gapStart = end + words[from].GapAfter;
        var stopPosition = gapStart;
        var alignment = TabAlignment.Left;
        for (var tab = 0; tab < words[from].TabsAfter; tab++)
        {
            if (stops is not null && TryNextTabStop(stopPosition, stops, out var next, out var nextAlignment))
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

    private static bool TryNextTabStop(
        double cursor,
        List<TabStop> stops,
        out double position,
        out TabAlignment alignment)
        => FindNextTabStop(stops, cursor, out position, out alignment, out _);

    private static double LineNaturalWidth(List<LineWord> words, int first, int last, List<TabStop>? stops)
    {
        var end = words[first].Width;
        for (var word = first; word < last; word++)
        {
            end = NextStart(end, words[word], stops) + words[word + 1].Width;
        }

        return end;
    }

    private static bool FindNextTabStop(
        List<TabStop> stops,
        double cursor,
        out double position,
        out TabAlignment alignment,
        out char leader)
    {
        for (var index = 0; index < stops.Count; index++)
        {
            if (stops[index].Position.Point > cursor + LayoutTolerance.Epsilon)
            {
                position = stops[index].Position.Point;
                alignment = stops[index].Alignment;
                leader = stops[index].Leader;
                return true;
            }
        }

        position = 0;
        alignment = TabAlignment.Left;
        leader = '\0';
        return false;
    }
}
