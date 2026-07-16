using System;
using System.Collections.Generic;
using System.Text;
using Radzen.Documents.Pdf.Fonts;
using static Radzen.Documents.Pdf.Content.ContentOperands;
using Token = Radzen.Documents.Pdf.Content.ContentTokenizer.Token;
using TokenKind = Radzen.Documents.Pdf.Content.ContentTokenizer.TokenKind;

using Radzen.Documents.Pdf.Content;
namespace Radzen.Documents.Pdf;

// Resource-aware text extraction: re-walks a page content stream tracking the text
// and graphics matrices and the active font, reversing each shown char code to
// Unicode through the font's /ToUnicode, /Differences or WinAnsi encoding. Runs are
// emitted in reading order (descending Y, then ascending X).
internal static class TextExtractor
{
    private const double LineTolerance = 0.5;

    // TJ adjustments are in thousandths of an em; a leftward move (negative) beyond
    // ~0.2 em is an inter-word gap, smaller values are kerning. Third-party streams
    // rely on this for word breaks; the authoring path never emits TJ arrays.
    private const double TjSpaceThreshold = 200.0;

    // No per-glyph widths are available to extraction, so the pen advance is estimated
    // at half an em per code; this only feeds the same-line gap test, never the text.
    private const double AverageGlyphEm = 0.5;

    // Two same-line fragments read as separate words only when the X gap between the
    // pen left by one and the origin of the next exceeds this fraction of an em, which
    // sits below a space (~0.25 em) yet clears kerning and abutting fragments.
    private const double SpaceGapEm = 0.2;

    public static string Extract(byte[]? content, IReadOnlyDictionary<string, ReverseFont>? fonts)
    {
        if (content is null || content.Length == 0)
        {
            return string.Empty;
        }

        var fragments = new List<Fragment>();
        ContentTextWalker.Walk(content, fonts, (walker, op, operands, array, _) => op == "TJ"
            ? ShowArray(fragments, array, walker)
            : Show(fragments, operands, walker));
        return Compose(fragments);
    }

    // Emits one fragment and returns the text-space horizontal advance the pen moved,
    // so the caller can step the text matrix and same-line fragments abut correctly.
    private static double Show(List<Fragment> fragments, List<Token> operands, ContentTextWalker walker)
    {
        var bytes = LastString(operands);
        if (bytes is null || bytes.Length == 0)
        {
            return 0.0;
        }

        var text = (walker.Font ?? ReverseFont.WinAnsi).Decode(bytes);
        if (text.Length == 0)
        {
            return 0.0;
        }

        var fontSize = walker.FontSize;
        var advance = text.Length * AverageGlyphEm * fontSize;
        AddFragment(fragments, walker.TextMatrix * walker.Ctm, text, advance, fontSize);
        return advance;
    }

    private static double ShowArray(List<Fragment> fragments, List<Token> array, ContentTextWalker walker)
    {
        var fontSize = walker.FontSize;
        var reverse = walker.Font ?? ReverseFont.WinAnsi;
        var builder = new StringBuilder();
        var glyphEms = 0.0;
        var adjustEms = 0.0;
        foreach (var element in array)
        {
            if (element.Kind == TokenKind.String)
            {
                if (element.Bytes is { Length: > 0 } bytes)
                {
                    var decoded = reverse.Decode(bytes);
                    builder.Append(decoded);
                    glyphEms += decoded.Length * AverageGlyphEm;
                }
            }
            else
            {
                adjustEms += element.Number / 1000.0;
                if (element.Number <= -TjSpaceThreshold)
                {
                    builder.Append(' ');
                }
            }
        }

        if (builder.Length == 0)
        {
            return 0.0;
        }

        var advance = (glyphEms - adjustEms) * fontSize;
        AddFragment(fragments, walker.TextMatrix * walker.Ctm, builder.ToString(), advance, fontSize);
        return advance;
    }

    private static void AddFragment(List<Fragment> fragments, Matrix matrix, string text, double textAdvance, double fontSize)
    {
        var origin = matrix.Transform(0, 0);
        var pen = matrix.Transform(textAdvance, 0);
        var emPoint = matrix.Transform(fontSize, 0);
        fragments.Add(new Fragment(origin.Y, origin.X, pen.X - origin.X, emPoint.X - origin.X, text));
    }

    private static string Compose(List<Fragment> fragments)
    {
        if (fragments.Count == 0)
        {
            return string.Empty;
        }

        fragments.Sort(static (a, b) =>
        {
            if (Math.Abs(a.Y - b.Y) > LineTolerance)
            {
                return b.Y.CompareTo(a.Y);
            }

            return a.X.CompareTo(b.X);
        });

        var builder = new StringBuilder();
        Fragment? previous = null;
        foreach (var fragment in fragments)
        {
            if (previous is { } prev)
            {
                if (Math.Abs(fragment.Y - prev.Y) > LineTolerance)
                {
                    builder.Append('\n');
                }
                else if (NeedsSpace(prev, fragment))
                {
                    builder.Append(' ');
                }
            }

            builder.Append(fragment.Text);
            previous = fragment;
        }

        return builder.ToString();
    }

    private static bool NeedsSpace(Fragment previous, Fragment current)
    {
        if (previous.Text.Length == 0 || current.Text.Length == 0
            || char.IsWhiteSpace(previous.Text[^1]) || char.IsWhiteSpace(current.Text[0]))
        {
            return false;
        }

        var gap = current.X - (previous.X + previous.Advance);
        var em = Math.Abs(current.Em != 0 ? current.Em : previous.Em);
        return gap > SpaceGapEm * em;
    }

    private readonly record struct Fragment(double Y, double X, double Advance, double Em, string Text);
}
