using System.Collections.Generic;
using Token = Radzen.Documents.Pdf.Content.ContentTokenizer.Token;
using TokenKind = Radzen.Documents.Pdf.Content.ContentTokenizer.TokenKind;

namespace Radzen.Documents.Pdf.Content;

internal sealed class ContentOperandFrame
{
    public bool IsInlineImage { get; internal set; }

    public Token InlineImage { get; internal set; }

    public Token Operator { get; internal set; }

    public List<Token> Operands { get; } = [];

    public List<Token> Array { get; } = [];

    public bool HasArray { get; internal set; }

    public int ArrayStart { get; internal set; }

    public int ArrayEnd { get; internal set; }

    public int ArrayOperandIndex { get; internal set; }

    public int OperandStart { get; internal set; }

    public int FrameStart { get; internal set; }
}

internal static class ContentOperandScan
{
    public static IEnumerable<ContentOperandFrame> Scan(IReadOnlyList<Token> tokens)
    {
        var frame = new ContentOperandFrame();
        Reset(frame);
        for (var i = 0; i < tokens.Count; i++)
        {
            var token = tokens[i];
            switch (token.Kind)
            {
                case TokenKind.Number:
                case TokenKind.Name:
                case TokenKind.String:
                    if (frame.OperandStart < 0)
                    {
                        frame.OperandStart = token.Start;
                    }

                    if (frame.FrameStart < 0)
                    {
                        frame.FrameStart = token.Start;
                    }

                    frame.Operands.Add(token);
                    continue;
                case TokenKind.ArrayStart:
                    if (frame.FrameStart < 0)
                    {
                        frame.FrameStart = token.Start;
                    }

                    frame.HasArray = true;
                    frame.ArrayStart = token.Start;
                    frame.ArrayOperandIndex = frame.Operands.Count;
                    frame.Array.Clear();
                    for (i++; i < tokens.Count && tokens[i].Kind != TokenKind.ArrayEnd; i++)
                    {
                        if (tokens[i].Kind is TokenKind.String or TokenKind.Number)
                        {
                            frame.Array.Add(tokens[i]);
                        }
                    }

                    frame.ArrayEnd = i < tokens.Count ? tokens[i].End : tokens[^1].End;
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
                case TokenKind.InlineImage:
                    frame.IsInlineImage = true;
                    frame.InlineImage = token;
                    yield return frame;
                    Reset(frame);
                    continue;
                case TokenKind.Operator:
                    frame.Operator = token;
                    if (frame.FrameStart < 0)
                    {
                        frame.FrameStart = token.Start;
                    }

                    yield return frame;
                    Reset(frame);
                    continue;
            }
        }
    }

    private static void Reset(ContentOperandFrame frame)
    {
        frame.IsInlineImage = false;
        frame.Operands.Clear();
        frame.Array.Clear();
        frame.HasArray = false;
        frame.ArrayStart = -1;
        frame.ArrayEnd = -1;
        frame.ArrayOperandIndex = -1;
        frame.OperandStart = -1;
        frame.FrameStart = -1;
    }
}
