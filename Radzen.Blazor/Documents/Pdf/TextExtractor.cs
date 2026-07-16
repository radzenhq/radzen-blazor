using System;
using System.Collections.Generic;
using System.Text;
using Radzen.Documents.Pdf.Fonts;
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
        var tokens = ContentTokenizer.Tokenize(content);

        var ctm = Matrix.Identity;
        var ctmStack = new Stack<Matrix>();
        var textMatrix = Matrix.Identity;
        var lineMatrix = Matrix.Identity;
        var leading = 0.0;
        var fontSize = 0.0;
        ReverseFont? font = null;

        var operands = new List<Token>();
        var array = new List<Token>();

        for (var i = 0; i < tokens.Count; i++)
        {
            var token = tokens[i];
            switch (token.Kind)
            {
                case TokenKind.Number:
                case TokenKind.Name:
                case TokenKind.String:
                    operands.Add(token);
                    continue;

                case TokenKind.ArrayStart:
                    array.Clear();
                    for (i++; i < tokens.Count && tokens[i].Kind != TokenKind.ArrayEnd; i++)
                    {
                        if (tokens[i].Kind is TokenKind.String or TokenKind.Number)
                        {
                            array.Add(tokens[i]);
                        }
                    }

                    continue;

                case TokenKind.ArrayEnd:
                    continue;

                case TokenKind.DictStart:
                    for (var depth = 1; depth > 0 && ++i < tokens.Count;)
                    {
                        if (tokens[i].Kind == TokenKind.DictStart)
                        {
                            depth++;
                        }
                        else if (tokens[i].Kind == TokenKind.DictEnd)
                        {
                            depth--;
                        }
                    }

                    continue;

                case TokenKind.DictEnd:
                case TokenKind.InlineImage:
                    continue;

                case TokenKind.Operator:
                    break;
            }

            switch (token.Text)
            {
                case "q":
                    ctmStack.Push(ctm);
                    break;
                case "Q":
                    if (ctmStack.Count > 0)
                    {
                        ctm = ctmStack.Pop();
                    }

                    break;
                case "cm":
                    ctm = Components(operands) * ctm;
                    break;
                case "BT":
                    textMatrix = Matrix.Identity;
                    lineMatrix = Matrix.Identity;
                    break;
                case "Tf":
                    font = LastName(operands) is { } key && fonts is not null && fonts.TryGetValue(key, out var f)
                        ? f
                        : ReverseFont.WinAnsi;
                    fontSize = LastNumber(operands);
                    break;
                case "TD":
                    leading = -Number(operands, 1);
                    goto case "Td";
                case "Td":
                    lineMatrix = Matrix.Translate(Number(operands, 0), Number(operands, 1)) * lineMatrix;
                    textMatrix = lineMatrix;
                    break;
                case "TL":
                    leading = Number(operands, 0);
                    break;
                case "Tm":
                    lineMatrix = Components(operands);
                    textMatrix = lineMatrix;
                    break;
                case "T*":
                    lineMatrix = Matrix.Translate(0, -leading) * lineMatrix;
                    textMatrix = lineMatrix;
                    break;
                case "Tj":
                    textMatrix = Matrix.Translate(Show(fragments, operands, textMatrix * ctm, font, fontSize), 0) * textMatrix;
                    break;
                case "TJ":
                    textMatrix = Matrix.Translate(ShowArray(fragments, array, textMatrix * ctm, font, fontSize), 0) * textMatrix;
                    break;
                case "'":
                case "\"":
                    lineMatrix = Matrix.Translate(0, -leading) * lineMatrix;
                    textMatrix = lineMatrix;
                    textMatrix = Matrix.Translate(Show(fragments, operands, textMatrix * ctm, font, fontSize), 0) * textMatrix;
                    break;
            }

            operands.Clear();
        }

        return Compose(fragments);
    }

    // Emits one fragment and returns the text-space horizontal advance the pen moved,
    // so the caller can step the text matrix and same-line fragments abut correctly.
    private static double Show(List<Fragment> fragments, List<Token> operands, Matrix matrix, ReverseFont? font, double fontSize)
    {
        var bytes = LastString(operands);
        if (bytes is null || bytes.Length == 0)
        {
            return 0.0;
        }

        var text = (font ?? ReverseFont.WinAnsi).Decode(bytes);
        if (text.Length == 0)
        {
            return 0.0;
        }

        var advance = text.Length * AverageGlyphEm * fontSize;
        AddFragment(fragments, matrix, text, advance, fontSize);
        return advance;
    }

    private static double ShowArray(List<Fragment> fragments, List<Token> array, Matrix matrix, ReverseFont? font, double fontSize)
    {
        var reverse = font ?? ReverseFont.WinAnsi;
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
        AddFragment(fragments, matrix, builder.ToString(), advance, fontSize);
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

    private static Matrix Components(List<Token> operands)
    {
        var n = Numbers(operands, 6);
        return Matrix.FromComponents(n[0], n[1], n[2], n[3], n[4], n[5]);
    }

    private static double[] Numbers(List<Token> operands, int count)
    {
        var numbers = new List<double>(count);
        foreach (var token in operands)
        {
            if (token.Kind == TokenKind.Number)
            {
                numbers.Add(token.Number);
            }
        }

        var result = new double[count];
        var offset = numbers.Count - count;
        for (var i = 0; i < count; i++)
        {
            var index = offset + i;
            result[i] = index >= 0 && index < numbers.Count ? numbers[index] : 0.0;
        }

        return result;
    }

    private static double Number(List<Token> operands, int index)
    {
        var count = 0;
        foreach (var token in operands)
        {
            if (token.Kind == TokenKind.Number)
            {
                if (count == index)
                {
                    return token.Number;
                }

                count++;
            }
        }

        return 0.0;
    }

    private static string? LastName(List<Token> operands)
    {
        for (var i = operands.Count - 1; i >= 0; i--)
        {
            if (operands[i].Kind == TokenKind.Name)
            {
                return operands[i].Text;
            }
        }

        return null;
    }

    private static double LastNumber(List<Token> operands)
    {
        for (var i = operands.Count - 1; i >= 0; i--)
        {
            if (operands[i].Kind == TokenKind.Number)
            {
                return operands[i].Number;
            }
        }

        return 0.0;
    }

    private static byte[]? LastString(List<Token> operands)
    {
        for (var i = operands.Count - 1; i >= 0; i--)
        {
            if (operands[i].Kind == TokenKind.String)
            {
                return operands[i].Bytes;
            }
        }

        return null;
    }

    private readonly record struct Fragment(double Y, double X, double Advance, double Em, string Text);
}
