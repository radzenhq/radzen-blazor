using System.Collections.Generic;
using Token = Radzen.Documents.Pdf.Content.ContentTokenizer.Token;
using TokenKind = Radzen.Documents.Pdf.Content.ContentTokenizer.TokenKind;

namespace Radzen.Documents.Pdf.Content;

internal static class ContentOperands
{
    public static Matrix Components(List<Token> operands)
    {
        var n = Numbers(operands, 6);
        return Matrix.FromRawComponents(n[0], n[1], n[2], n[3], n[4], n[5]);
    }

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
        => LastOfKind(operands, TokenKind.Number)?.Number ?? 0.0;

    public static string? LastName(List<Token> operands)
        => LastOfKind(operands, TokenKind.Name)?.Text;

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
        => LastOfKind(operands, TokenKind.String);

    public static byte[]? LastString(List<Token> operands) => LastStringToken(operands)?.Bytes;

    private static Token? LastOfKind(List<Token> operands, TokenKind kind)
    {
        for (var i = operands.Count - 1; i >= 0; i--)
        {
            if (operands[i].Kind == kind)
            {
                return operands[i];
            }
        }

        return null;
    }

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
