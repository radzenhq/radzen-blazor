using System.Collections.Generic;

namespace Radzen.Documents.Pdf;

#nullable enable

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

    private const double TabStop = 36.0;

    public static IReadOnlyList<LineBox> Break(
        Paragraph paragraph,
        double maxWidthPoints,
        FontCollection fonts,
        HorizontalAlignment? inheritedAlignment = null)
    {
        var boxes = new List<LineBox>();
        var indent = paragraph.LeftIndent.Point;
        var max = maxWidthPoints - indent;
        foreach (var words in Tokenize(paragraph, fonts, out var pieces))
        {
            if (words.Count == 0)
            {
                boxes.Add(EmptyLine(paragraph, fonts));
                continue;
            }

            var lineRanges = Wrap(words, max);
            for (var li = 0; li < lineRanges.Count; li++)
            {
                var (first, last) = lineRanges[li];
                var isLast = li == lineRanges.Count - 1;
                boxes.Add(BuildLine(words, pieces, first, last, max, indent, paragraph, fonts, isLast, inheritedAlignment));
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

    // Position where the word after `word` starts, given `word` ends at `position`:
    // the inter-word gap, then each tab advances to the next default tab stop.
    private static double NextStart(double position, Word word)
    {
        var p = position + word.GapAfter;
        for (var t = 0; t < word.TabsAfter; t++)
        {
            p = (System.Math.Floor((p + 1e-6) / TabStop) + 1) * TabStop;
        }

        return p;
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
        var current = default(Word);
        var hasCurrent = false;

        foreach (var run in paragraph.Inlines)
        {
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
                    var spaces = 0;
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
                            spaces++;
                        }

                        i++;
                    }

                    if (spaces > 0 && hasCurrent)
                    {
                        current.GapAfter += fonts.MeasureText(new string(' ', spaces), run.ResolvedFont);
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

    private static List<(int First, int Last)> Wrap(List<Word> words, double max)
    {
        var lines = new List<(int, int)>();
        var i = 0;
        while (i < words.Count)
        {
            var j = i;
            var end = words[i].Width;
            while (j + 1 < words.Count)
            {
                var nextEnd = NextStart(end, words[j]) + words[j + 1].Width;
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
        HorizontalAlignment? inheritedAlignment)
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
        }

        var shift = indent + x0;
        if (shift != 0)
        {
            for (var f = 0; f < span.Length; f++)
            {
                span[f].XOffset += shift;
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
        var fragments = box.Fragments;
        for (var i = 0; i < fragments.Count; i++)
        {
            var (h, asc) = FontExtent(fragments[i].Run.ResolvedFont, fonts);
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
