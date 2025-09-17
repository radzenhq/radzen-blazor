using System;
using System.Collections.Generic;

namespace Radzen.Blazor.Spreadsheet;
#nullable enable

internal interface IFormulaSyntaxNodeVisitor
{
    void VisitNumberLiteral(NumberLiteralSyntaxNode numberLiteralSyntaxNode);
    void VisitStringLiteral(StringLiteralSyntaxNode stringLiteralSyntaxNode);
    void VisitBinaryExpression(BinaryExpressionSyntaxNode binaryExpressionSyntaxNode);
    void VisitCell(CellSyntaxNode cellSyntaxNode);
    void VisitFunction(FunctionSyntaxNode functionSyntaxNode);
    void VisitRange(RangeSyntaxNode rangeSyntaxNode);
}

internal abstract class FormulaSyntaxNode(FormulaToken token)
{
    public FormulaToken Token { get; } = token;

    public abstract void Accept(IFormulaSyntaxNodeVisitor visitor);

    public List<FormulaSyntaxNode> Find(Func<FormulaSyntaxNode, bool> predicate)
    {
        var results = new List<FormulaSyntaxNode>();

        if (predicate(this))
        {
            results.Add(this);
        }

        var visitor = new FindVisitor(predicate, results);

        Accept(visitor);

        return results;
    }
}

abstract class FormulaSyntaxNodeVisitorBase : IFormulaSyntaxNodeVisitor
{
    public virtual void VisitNumberLiteral(NumberLiteralSyntaxNode numberLiteralSyntaxNode)
    {
        Visit(numberLiteralSyntaxNode);
    }

    public virtual void VisitStringLiteral(StringLiteralSyntaxNode stringLiteralSyntaxNode)
    {
        Visit(stringLiteralSyntaxNode);
    }

    public virtual void VisitBinaryExpression(BinaryExpressionSyntaxNode binaryExpressionSyntaxNode)
    {
        binaryExpressionSyntaxNode.Left.Accept(this);
        binaryExpressionSyntaxNode.Right.Accept(this);

        Visit(binaryExpressionSyntaxNode);
    }

    public virtual void VisitCell(CellSyntaxNode cellSyntaxNode)
    {
        Visit(cellSyntaxNode);
    }

    public virtual void VisitFunction(FunctionSyntaxNode functionSyntaxNode)
    {
        foreach (var argument in functionSyntaxNode.Arguments)
        {
            argument.Accept(this);
        }

        Visit(functionSyntaxNode);
    }

    public virtual void VisitRange(RangeSyntaxNode rangeSyntaxNode)
    {
        rangeSyntaxNode.Start.Accept(this);
        rangeSyntaxNode.End.Accept(this);

        Visit(rangeSyntaxNode);
    }

    protected virtual void Visit(FormulaSyntaxNode node)
    {
    }
}

internal class FindVisitor(Func<FormulaSyntaxNode, bool> predicate, List<FormulaSyntaxNode> results) : FormulaSyntaxNodeVisitorBase
{
    protected override void Visit(FormulaSyntaxNode node)
    {
        if (predicate(node))
        {
            results.Add(node);
        }
    }
}

internal enum BinaryOperator
{
    Plus,
    Minus,
    Multiply,
    Divide,
    Equals,
    NotEquals,
    LessThan,
    LessThanOrEqual,
    GreaterThan,
    GreaterThanOrEqual,
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
            FormulaTokenType.Equals => BinaryOperator.Equals,
            FormulaTokenType.EqualsGreaterThan => BinaryOperator.NotEquals,
            FormulaTokenType.LessThan => BinaryOperator.LessThan,
            FormulaTokenType.LessThanOrEqual => BinaryOperator.LessThanOrEqual,
            FormulaTokenType.GreaterThan => BinaryOperator.GreaterThan,
            FormulaTokenType.GreaterThanOrEqual => BinaryOperator.GreaterThanOrEqual,
            _ => throw new ArgumentOutOfRangeException(nameof(tokenType), tokenType, null)
        };
    }
}

internal class BinaryExpressionSyntaxNode(FormulaToken token, FormulaSyntaxNode left, FormulaSyntaxNode right, BinaryOperator @operator) : FormulaSyntaxNode(token) 
{
    public FormulaSyntaxNode Left { get; } = left;
    public BinaryOperator Operator { get; } = @operator;
    public FormulaSyntaxNode Right { get; } = right;

    public override void Accept(IFormulaSyntaxNodeVisitor visitor)
    {
        visitor.VisitBinaryExpression(this);
    }
}

internal class NumberLiteralSyntaxNode(FormulaToken token) : FormulaSyntaxNode(token)
{
    public override void Accept(IFormulaSyntaxNodeVisitor visitor)
    {
        visitor.VisitNumberLiteral(this);
    }
}

internal class StringLiteralSyntaxNode(FormulaToken token) : FormulaSyntaxNode(token)
{
    public override void Accept(IFormulaSyntaxNodeVisitor visitor)
    {
        visitor.VisitStringLiteral(this);
    }
}

internal class FunctionSyntaxNode(FormulaToken token, FormulaToken openParenToken, FormulaToken closeParenToken, List<FormulaSyntaxNode> arguments) : FormulaSyntaxNode(token)
{
    public string Name { get; } = token.Value;

    public FormulaToken OpenParenToken => openParenToken;

    public FormulaToken CloseParenToken => closeParenToken;

    public List<FormulaSyntaxNode> Arguments { get; } = arguments;

    public override void Accept(IFormulaSyntaxNodeVisitor visitor)
    {
        visitor.VisitFunction(this);
    }

    public bool IsInside(int position)
    {
        return position > Token.Start && position <= CloseParenToken.Start;
    }

    public int GetArgumentIndexAtPosition(int position)
    {
        if (position > CloseParenToken.Start)
        {
            return -1;
        }

        if (position < OpenParenToken.Start)
        {
            return -1;
        }

        if (Arguments.Count == 0)
        {
            return -1;
        }

        if (position < Arguments[0].Token.Start)
        {
            return -1;
        }

        for (int i = Arguments.Count - 1; i >= 0; i--)
        {
            if (position >= Arguments[i].Token.Start)
            {
                return i;
            }
        }

        return -1;
    }
}

internal class CellSyntaxNode(FormulaToken token) : FormulaSyntaxNode(token)
{
    public override void Accept(IFormulaSyntaxNodeVisitor visitor)
    {
        visitor.VisitCell(this);
    }
}

internal class RangeSyntaxNode(FormulaToken token, CellSyntaxNode start, CellSyntaxNode end) : FormulaSyntaxNode(token)
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
    private readonly bool strict;

    private FormulaParser(string expression, bool strict = true) : this(FormulaLexer.Scan(expression, strict), strict)
    {
    }

    private FormulaParser(List<FormulaToken> tokens, bool strict = true)
    {
        this.tokens = tokens;
        this.strict = strict;
    }

    public static FormulaSyntaxNode Parse(List<FormulaToken> tokens, bool strict = true)
    {
        var parser = new FormulaParser(tokens, strict);
        return parser.Parse();
    }

    public static FormulaSyntaxNode Parse(string expression, bool strict = true)
    {
        var parser = new FormulaParser(expression, strict);
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
        var left = ParseComparison();

        return left;
    }

    private FormulaSyntaxNode ParseComparison()
    {
        var left = ParseArithmetic();

        while (Peek().Type is FormulaTokenType.Equals or FormulaTokenType.EqualsGreaterThan or
               FormulaTokenType.LessThan or FormulaTokenType.LessThanOrEqual or
               FormulaTokenType.GreaterThan or FormulaTokenType.GreaterThanOrEqual)
        {
            var token = tokens[position];
            Advance(1);
            left = new BinaryExpressionSyntaxNode(token, left, ParseArithmetic(), token.Type.ToBinaryOperator());
        }

        return left;
    }

    private FormulaSyntaxNode ParseArithmetic()
    {
        var left = ParseTerm();

        while (Peek().Type is FormulaTokenType.Plus or FormulaTokenType.Minus)
        {
            var token = tokens[position];
            Advance(1);
            left = new BinaryExpressionSyntaxNode(token, left, ParseTerm(), token.Type.ToBinaryOperator());
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
            left = new BinaryExpressionSyntaxNode(token, left, ParseFactor(), token.Type.ToBinaryOperator());
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
                return new RangeSyntaxNode(token, start, new CellSyntaxNode(endToken));
            }

            return start;
        }

        if (token.Type == FormulaTokenType.StringLiteral)
        {
            Advance(1);
            return new StringLiteralSyntaxNode(token);
        }

        return ParseNumberLiteral();
    }

    private FormulaSyntaxNode ParseFunctionCall()
    {
        var token = Expect(FormulaTokenType.Identifier);

        var openParenToken = Expect(FormulaTokenType.OpenParen);

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

        var closeParenToken = Expect(FormulaTokenType.CloseParen);

        return new FunctionSyntaxNode(token, openParenToken, closeParenToken, arguments);
    }

    private FormulaSyntaxNode ParseNumberLiteral()
    {
        var token = Expect(FormulaTokenType.NumericLiteral);

        return new NumberLiteralSyntaxNode(token);
    }

    FormulaToken Expect(FormulaTokenType type)
    {
        var token = Peek();

        if (token.Type == type)
        {
            Advance(1);
            return token;
        }

        if (strict)
        {
            throw new InvalidOperationException($"Unexpected token: {token.Type}. Expected: {type}");
        }

        return new FormulaToken(type, string.Empty) { Start = token.Start, End = token.End };
    }

    private void Advance(int count)
    {
        position += count;
    }

    private FormulaToken Peek(int offset = 0)
    {
        if (position + offset >= tokens.Count)
        {
            return tokens[^1];
        }

        return tokens[position + offset];
    }
}