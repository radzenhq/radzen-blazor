using System.Collections.Generic;

namespace Radzen.Documents.Pdf.Emit;

internal readonly struct LinePiece
{
    public required Run Run { get; init; }
    public required Font Font { get; init; }
    public required int Start { get; init; }
    public required int Length { get; init; }
    public required string Text { get; init; }
    public required double Advance { get; init; }
}

// Pieces of a word are contiguous in the shared piece list: [PieceStart, PieceStart + PieceCount).
internal struct LineWord
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


internal readonly record struct LineTokenization(List<List<LineWord>> Segments, List<LinePiece> Pieces);

internal static class LineTokenizer
{
    private static Font ResolvedFont(StyleResolution? resolution, Run run)
        => resolution?.RunFont(run) ?? run.Font;

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

    private static bool IsInlineWhitespace(char c) => c is ' ' or '\t';

    private static bool IsLineBreak(char c) => c is '\n' or '\r';

    // Zero-width conditional break characters removed from the rendered text.
    private static bool IsConditionalBreak(char c) => c == SoftHyphen || c == ZeroWidthSpace;

    // Splits the paragraph into forced-break segments ('\n', '\r' and "\r\n"), each a
    // list of words separated by breakable whitespace (' ' and '\t'). NBSP is a word
    // character; control characters never enter fragment text.
    public static LineTokenization Tokenize(Paragraph paragraph, FontCollection fonts, StyleResolution? resolution)
    {
        var segments = new List<List<LineWord>>();
        var words = new List<LineWord>();
        segments.Add(words);
        var pieces = new List<LinePiece>();
        var spaceWidths = new Dictionary<Font, double>();
        var current = default(LineWord);
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
                words.Add(new LineWord
                {
                    PieceStart = pieces.Count,
                    PieceCount = 1,
                    Width = advance,
                    NoGapBoundary = true,
                });
                pieces.Add(new LinePiece
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
                                current = new LineWord { PieceStart = pieces.Count };
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

                                current = new LineWord { PieceStart = pieces.Count };
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

                                current = new LineWord { PieceStart = pieces.Count };
                                hasCurrent = true;
                            }

                            pieces.Add(new LinePiece
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

        return new LineTokenization(segments, pieces);
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

}
