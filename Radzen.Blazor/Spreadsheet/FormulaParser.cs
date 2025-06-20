using System;
using System.Collections.Generic;

namespace Radzen.Blazor.Spreadsheet;

internal interface IFormulaSyntaxNodeVisitor
{
    void VisitNumberLiteral(NumberLiteralSyntaxNode numberLiteralSyntaxNode);
    void VisitBinaryExpression(BinaryExpressionSyntaxNode binaryExpressionSyntaxNode);
    void VisitCell(CellSyntaxNode cellSyntaxNode);
    void VisitFunction(FunctionSyntaxNode functionSyntaxNode);
    void VisitRange(RangeSyntaxNode rangeSyntaxNode);
}

internal abstract class FormulaSyntaxNode
{
    public abstract void Accept(IFormulaSyntaxNodeVisitor visitor);
}

internal enum BinaryOperator
{
    Plus,
    Minus,
    Multiply,
    Divide,
}

static class FormulaTokenTypeExtensions
{
    public static BinaryOperator ToBinaryOperator(this FormulaTokenType tokenType)
    {
        return tokenType switch
        {
            FormulaTokenType.Plus => BinaryOperator.Plus,
            FormulaTokenType.Minus => BinaryOperator.Minus,
            FormulaTokenType.Star => BinaryOperator.Multiply,
            FormulaTokenType.Slash => BinaryOperator.Divide,
            _ => throw new ArgumentOutOfRangeException(nameof(tokenType), tokenType, null)
        };
    }
}

internal class BinaryExpressionSyntaxNode(FormulaSyntaxNode left, FormulaSyntaxNode right, BinaryOperator @operator) : FormulaSyntaxNode
{
    public FormulaSyntaxNode Left { get; } = left;
    public BinaryOperator Operator { get; } = @operator;
    public FormulaSyntaxNode Right { get; } = right;

    public override void Accept(IFormulaSyntaxNodeVisitor visitor)
    {
        visitor.VisitBinaryExpression(this);
    }
}

internal class NumberLiteralSyntaxNode(FormulaToken token) : FormulaSyntaxNode
{
    public FormulaToken Token { get; } = token;

    public override void Accept(IFormulaSyntaxNodeVisitor visitor)
    {
        visitor.VisitNumberLiteral(this);
    }
}

internal class FunctionSyntaxNode(string name, List<FormulaSyntaxNode> arguments) : FormulaSyntaxNode
{
    public string Name { get; } = name;
    public List<FormulaSyntaxNode> Arguments { get; } = arguments;

    public override void Accept(IFormulaSyntaxNodeVisitor visitor)
    {
        visitor.VisitFunction(this);
    }
}

internal class CellSyntaxNode(FormulaToken token) : FormulaSyntaxNode
{
    public FormulaToken Token { get; } = token;

    public override void Accept(IFormulaSyntaxNodeVisitor visitor)
    {
        visitor.VisitCell(this);
    }
}

internal class RangeSyntaxNode(CellSyntaxNode start, CellSyntaxNode end) : FormulaSyntaxNode
{
    public CellSyntaxNode Start { get; } = start;
    public CellSyntaxNode End { get; } = end;

    public override void Accept(IFormulaSyntaxNodeVisitor visitor)
    {
        visitor.VisitRange(this);
    }
}

internal class FormulaParser
{
    private int position = 0;
    private readonly List<FormulaToken> tokens;

    private FormulaParser(string expression)
    {
        tokens = FormulaLexer.Scan(expression);
    }

    public static FormulaSyntaxNode Parse(string expression)
    {
        var parser = new FormulaParser(expression);
        return parser.Parse();
    }

    private FormulaSyntaxNode Parse()
    {
        Expect(FormulaTokenType.Equals);

        var node = ParseExpression();

        Expect(FormulaTokenType.None);

        return node;
    }

    private FormulaSyntaxNode ParseExpression()
    {
        var left = ParseTerm();

        while (Peek().Type is FormulaTokenType.Plus or FormulaTokenType.Minus)
        {
            var token = tokens[position];
            Advance(1);
            left = new BinaryExpressionSyntaxNode(left, ParseTerm(), token.Type.ToBinaryOperator());
        }

        return left;
    }

    private FormulaSyntaxNode ParseTerm()
    {
        var left = ParseFactor();

        while (Peek().Type is FormulaTokenType.Star or FormulaTokenType.Slash)
        {
            var token = Peek();
            Advance(1);
            left = new BinaryExpressionSyntaxNode(left, ParseFactor(), token.Type.ToBinaryOperator());
        }

        return left;
    }

    private FormulaSyntaxNode ParseFactor()
    {
        var token = Peek();

        if (token.Type == FormulaTokenType.OpenParen)
        {
            Advance(1);
            var expr = ParseExpression();
            Expect(FormulaTokenType.CloseParen);
            return expr;
        }

        if (token.Type == FormulaTokenType.Identifier)
        {
            return ParseFunctionCall();
        }

        if (token.Type == FormulaTokenType.CellIdentifier)
        {
            Advance(1);
            var start = new CellSyntaxNode(token);

            if (Peek().Type == FormulaTokenType.Colon)
            {
                Advance(1);
                var endToken = Expect(FormulaTokenType.CellIdentifier);
                return new RangeSyntaxNode(start, new CellSyntaxNode(endToken));
            }

            return start;
        }

        return ParseNumberLiteral();
    }

    private FormulaSyntaxNode ParseFunctionCall()
    {
        var token = Expect(FormulaTokenType.Identifier);
        var name = token.Value!;
        Expect(FormulaTokenType.OpenParen);

        var arguments = new List<FormulaSyntaxNode>();

        if (Peek().Type != FormulaTokenType.CloseParen)
        {
            arguments.Add(ParseExpression());
            
            while (Peek().Type == FormulaTokenType.Comma)
            {
                Advance(1);
                arguments.Add(ParseExpression());
            }
        }

        Expect(FormulaTokenType.CloseParen);

        return new FunctionSyntaxNode(name, arguments);
    }

    private FormulaSyntaxNode ParseNumberLiteral()
    {
        var token = Expect(FormulaTokenType.NumericLiteral);

        return new NumberLiteralSyntaxNode(token);
    }

    FormulaToken Expect(FormulaTokenType type)
    {
        if (position >= tokens.Count)
        {
            throw new InvalidOperationException($"Unexpected end of expression. Expected token: {type}");
        }

        var token = tokens[position];

        if (token.Type != type)
        {
            throw new InvalidOperationException($"Unexpected token: {token.Type}. Expected: {type}");
        }

        position++;

        return token;
    }

    private void Advance(int count)
    {
        position += count;
    }

    private FormulaToken Peek(int offset = 0)
    {
        if (position + offset >= tokens.Count)
        {
            return new FormulaToken(FormulaTokenType.None);
        }

        return tokens[position + offset];
    }
}