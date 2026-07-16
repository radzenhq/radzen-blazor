using System.Collections.Generic;
using Token = Radzen.Documents.Pdf.Content.ContentTokenizer.Token;
using TokenKind = Radzen.Documents.Pdf.Content.ContentTokenizer.TokenKind;

namespace Radzen.Documents.Pdf.Content;

// Operand-frame accessors shared by every consumer of the content-stream token grammar.
// The defaulting rules here (a missing operand reads as 0.0/null, Numbers aligns to the
// trailing operands) are what make search, replace and materialize agree on a stream.
internal static class ContentOperands
{
    public static Matrix Components(List<Token> operands)
    {
        var n = Numbers(operands, 6);
        return Matrix.FromComponents(n[0], n[1], n[2], n[3], n[4], n[5]);
    }

    // An operator's operands are the last <paramref name="count"/> numbers on the frame, so
    // a stream that leaves extra numbers before them still reads the right ones.
    public static double[] Numbers(List<Token> operands, int count)
    {
        var result = new double[count];
        var offset = CountNumbers(operands) - count;
        var index = 0;
        foreach (var operand in operands)
        {
            if (operand.Kind != TokenKind.Number)
            {
                continue;
            }

            var target = index++ - offset;
            if (target >= 0 && target < count)
            {
                result[target] = operand.Number;
            }
        }

        return result;
    }

    public static double[] AllNumbers(List<Token> operands)
    {
        var result = new double[CountNumbers(operands)];
        var index = 0;
        foreach (var operand in operands)
        {
            if (operand.Kind == TokenKind.Number)
            {
                result[index++] = operand.Number;
            }
        }

        return result;
    }

    public static double Number(List<Token> operands, int index)
    {
        var current = 0;
        foreach (var operand in operands)
        {
            if (operand.Kind != TokenKind.Number)
            {
                continue;
            }

            if (current++ == index)
            {
                return operand.Number;
            }
        }

        return 0.0;
    }

    public static double LastNumber(List<Token> operands)
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

    public static string? LastName(List<Token> operands)
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

    // The BDC/BMC tag is the first name operand; the optional property list may itself
    // be a name, so LastName would misread it.
    public static string? FirstName(List<Token> operands)
    {
        foreach (var operand in operands)
        {
            if (operand.Kind == TokenKind.Name)
            {
                return operand.Text;
            }
        }

        return null;
    }

    public static Token? LastStringToken(List<Token> operands)
    {
        for (var i = operands.Count - 1; i >= 0; i--)
        {
            if (operands[i].Kind == TokenKind.String)
            {
                return operands[i];
            }
        }

        return null;
    }

    public static byte[]? LastString(List<Token> operands) => LastStringToken(operands)?.Bytes;

    private static int CountNumbers(List<Token> operands)
    {
        var count = 0;
        foreach (var operand in operands)
        {
            if (operand.Kind == TokenKind.Number)
            {
                count++;
            }
        }

        return count;
    }
}
