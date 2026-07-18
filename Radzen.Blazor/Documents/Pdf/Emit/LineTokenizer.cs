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

internal struct LineWord
{
    public int PieceStart { get; init; }
    public int PieceCount { get; set; }
    public double Width { get; set; }
    public double GapAfter { get; set; }
    public int TabsAfter { get; set; }

    public bool OptionalBreakAfter { get; set; }

    public bool SoftHyphenAfter { get; set; }

    public double HyphenWidth { get; set; }

    public bool NoGapBoundary { get; set; }
}


internal readonly record struct LineTokenization(List<List<LineWord>> Segments, List<LinePiece> Pieces);

internal static class LineTokenizer
{
    private static Font ResolvedFont(StyleResolution? resolution, Run run)
        => resolution?.RunFont(run) ?? run.Font;

    private const char SoftHyphen = '\u00AD';
    private const char ZeroWidthSpace = '\u200B';

    private static bool IsInlineWhitespace(char c) => c is ' ' or '\t';

    private static bool IsLineBreak(char c) => c is '\n' or '\r';

    private static bool IsConditionalBreak(char c) => c == SoftHyphen || c == ZeroWidthSpace;

    public static LineTokenization Tokenize(Paragraph paragraph, FontCollection fonts, StyleResolution? resolution)
    {
        var segments = new List<List<LineWord>>();
        var words = new List<LineWord>();
        segments.Add(words);
        var pieces = new List<LinePiece>();
        var current = default(LineWord);
        var hasCurrent = false;

        foreach (var run in paragraph.Inlines)
        {
            var runFont = ResolvedFont(resolution, run);
            if (run is InlineImage inlineImage)
            {
                if (hasCurrent)
                {
                    if (current.GapAfter == 0 && current.TabsAfter == 0)
                    {
                        current.NoGapBoundary = true;
                    }

                    words.Add(current);
                    hasCurrent = false;
                }

                var advance = inlineImage.EffectiveSize().Width;
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
                            if (!hasCurrent || current.TabsAfter > 0)
                            {
                                if (hasCurrent)
                                {
                                    words.Add(current);
                                }

                                current = new LineWord { PieceStart = pieces.Count };
                                hasCurrent = true;
                            }

                            current.GapAfter += RunTextAdvance.Measure(
                                fonts, run, runFont, " ",
                                leadingCharacterSpacing: true, trailingCharacterSpacing: true);
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
                            var advance = RunTextAdvance.Measure(fonts, run, runFont, segment);
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

}
