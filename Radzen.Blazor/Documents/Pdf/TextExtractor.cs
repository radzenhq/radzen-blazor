#nullable enable
using System;
using System.Collections.Generic;
using System.Text;
using Radzen.Documents.Pdf.Fonts;
using Token = Radzen.Documents.Pdf.ContentTokenizer.Token;
using TokenKind = Radzen.Documents.Pdf.ContentTokenizer.TokenKind;

namespace Radzen.Documents.Pdf;

// Resource-aware text extraction: re-walks a page content stream tracking the text
// and graphics matrices and the active font, reversing each shown char code to
// Unicode through the font's /ToUnicode, /Differences or WinAnsi encoding. Runs are
// emitted in reading order (descending Y, then ascending X).
internal static class TextExtractor
{
    private const double LineTolerance = 0.5;

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
        ReverseFont? font = null;

        var operands = new List<Token>();
        var buffer = new List<byte>();

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
                    buffer.Clear();
                    for (i++; i < tokens.Count && tokens[i].Kind != TokenKind.ArrayEnd; i++)
                    {
                        if (tokens[i].Kind == TokenKind.String)
                        {
                            buffer.AddRange(tokens[i].Bytes!);
                        }
                    }

                    operands.Add(new Token(TokenKind.String, 0, null, [.. buffer]));
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
                    break;
                case "Td":
                case "TD":
                    lineMatrix = Matrix.Translate(Number(operands, 0), Number(operands, 1)) * lineMatrix;
                    textMatrix = lineMatrix;
                    break;
                case "Tm":
                    lineMatrix = Components(operands);
                    textMatrix = lineMatrix;
                    break;
                case "T*":
                    textMatrix = lineMatrix;
                    break;
                case "Tj":
                case "TJ":
                case "'":
                case "\"":
                    Show(fragments, operands, textMatrix * ctm, font);
                    break;
            }

            operands.Clear();
        }

        return Compose(fragments);
    }

    private static void Show(List<Fragment> fragments, List<Token> operands, Matrix matrix, ReverseFont? font)
    {
        var bytes = LastString(operands);
        if (bytes is null || bytes.Length == 0)
        {
            return;
        }

        var text = (font ?? ReverseFont.WinAnsi).Decode(bytes);
        if (text.Length == 0)
        {
            return;
        }

        var origin = matrix.Transform(0, 0);
        fragments.Add(new Fragment(origin.Y, origin.X, text));
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
        double? lineY = null;
        foreach (var fragment in fragments)
        {
            if (lineY is { } y && Math.Abs(fragment.Y - y) > LineTolerance)
            {
                builder.Append('\n');
            }
            else if (lineY is not null)
            {
                builder.Append(' ');
            }

            builder.Append(fragment.Text);
            lineY = fragment.Y;
        }

        return builder.ToString();
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

    private readonly record struct Fragment(double Y, double X, string Text);
}
