using System.Collections.Generic;

namespace Radzen.Documents.Pdf;

#nullable enable

internal sealed class LineFragment
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
    private sealed class Piece
    {
        public required Run Run { get; init; }
        public required int Start { get; init; }
        public required int Length { get; init; }
        public required string Text { get; init; }
        public required double Advance { get; init; }
    }

    private sealed class Word
    {
        public List<Piece> Pieces { get; } = [];
        public double Width { get; set; }
        public double GapAfter { get; set; }
    }

    public static IReadOnlyList<LineBox> Break(Paragraph paragraph, double maxWidthPoints, FontCollection fonts)
    {
        var words = Tokenize(paragraph, fonts);
        var boxes = new List<LineBox>();
        if (words.Count == 0)
        {
            return boxes;
        }

        var lineRanges = Wrap(words, maxWidthPoints);
        for (var li = 0; li < lineRanges.Count; li++)
        {
            var (first, last) = lineRanges[li];
            var isLast = li == lineRanges.Count - 1;
            boxes.Add(BuildLine(words, first, last, maxWidthPoints, paragraph, fonts, isLast));
        }

        return boxes;
    }

    private static List<Word> Tokenize(Paragraph paragraph, FontCollection fonts)
    {
        var words = new List<Word>();
        Word? current = null;

        foreach (var run in paragraph.Inlines)
        {
            var text = run.Text;
            var i = 0;
            while (i < text.Length)
            {
                if (text[i] == ' ')
                {
                    var start = i;
                    while (i < text.Length && text[i] == ' ')
                    {
                        i++;
                    }

                    var spaces = text[start..i];
                    var w = fonts.MeasureText(spaces, run.Font);
                    current?.GapAfter += w;
                }
                else
                {
                    var start = i;
                    while (i < text.Length && text[i] != ' ')
                    {
                        i++;
                    }

                    var segment = text[start..i];
                    var advance = fonts.MeasureText(segment, run.Font);
                    var piece = new Piece
                    {
                        Run = run,
                        Start = start,
                        Length = i - start,
                        Text = segment,
                        Advance = advance,
                    };

                    if (current == null || current.GapAfter > 0)
                    {
                        current = new Word();
                        words.Add(current);
                    }

                    current.Pieces.Add(piece);
                    current.Width += advance;
                }
            }
        }

        return words;
    }

    private static List<(int First, int Last)> Wrap(List<Word> words, double max)
    {
        var lines = new List<(int, int)>();
        var i = 0;
        while (i < words.Count)
        {
            var j = i;
            var width = words[i].Width;
            while (j + 1 < words.Count)
            {
                var gap = words[j].GapAfter;
                if (width + gap + words[j + 1].Width <= max)
                {
                    width += gap + words[j + 1].Width;
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

    private static LineBox BuildLine(
        List<Word> words,
        int first,
        int last,
        double max,
        Paragraph paragraph,
        FontCollection fonts,
        bool isLast)
    {
        var fragments = new List<LineFragment>();
        double advances = 0;
        double naturalGaps = 0;
        for (var w = first; w <= last; w++)
        {
            foreach (var piece in words[w].Pieces)
            {
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

            if (w < last)
            {
                naturalGaps += words[w].GapAfter;
            }
        }

        var naturalWidth = advances + naturalGaps;
        var wordCount = last - first + 1;
        var gapCount = wordCount - 1;

        var justify = paragraph.Alignment == HorizontalAlignment.Justify && !isLast && gapCount > 0;

        double x0;
        double justifiedGap = 0;
        if (justify)
        {
            x0 = 0;
            justifiedGap = (max - advances) / gapCount;
        }
        else
        {
            x0 = paragraph.Alignment switch
            {
                HorizontalAlignment.Right or HorizontalAlignment.End => max - naturalWidth,
                HorizontalAlignment.Center => (max - naturalWidth) / 2.0,
                _ => 0,
            };
        }

        var cursor = x0;
        var fi = 0;
        for (var w = first; w <= last; w++)
        {
            foreach (var _ in words[w].Pieces)
            {
                fragments[fi].XOffset = cursor;
                cursor += fragments[fi].Advance;
                fi++;
            }

            if (w < last)
            {
                cursor += justify ? justifiedGap : words[w].GapAfter;
            }
        }

        var box = new LineBox { Fragments = fragments, Width = naturalWidth };
        Measure(box, paragraph.LineSpacing, fonts);
        return box;
    }

    private static void Measure(LineBox box, double lineSpacing, FontCollection fonts)
    {
        double natural = 0;
        double baseline = 0;
        foreach (var frag in box.Fragments)
        {
            var size = frag.Run.Font.Size;
            double h;
            double asc;
            if (fonts.TryResolvePrimary(frag.Run.Font, out var face))
            {
                var upm = face.UnitsPerEm;
                asc = face.Ascent * size / upm;
                h = (face.Ascent - face.Descent + face.LineGap) * size / upm;
            }
            else
            {
                asc = size * 0.9;
                h = size * 1.2;
            }

            natural = System.Math.Max(natural, h);
            baseline = System.Math.Max(baseline, asc);
        }

        box.Height = natural * lineSpacing;
        box.Baseline = baseline;
    }
}
