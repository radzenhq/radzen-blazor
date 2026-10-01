using System;
using System.Text;

namespace Radzen.Documents.Spreadsheet;

#nullable enable

internal class FormulaRewriter : FormulaSyntaxNodeVisitorBase
{
    private readonly Func<FormulaToken, CellRef> adjust;
    private readonly StringBuilder builder = new();

    private FormulaRewriter(Func<FormulaToken, CellRef> adjust)
    {
        this.adjust = adjust;
    }

    public static string Rewrite(string original, FormulaSyntaxTree tree, Func<FormulaToken, CellRef> adjust)
    {
        var rewriter = new FormulaRewriter(adjust);
        rewriter.builder.Append('=');
        tree.Root.Accept(rewriter);
        return rewriter.builder.ToString();
    }

    public override void VisitNumberLiteral(NumberLiteralSyntaxNode numberLiteralSyntaxNode)
    {
        builder.Append(numberLiteralSyntaxNode.Token.Value);
    }

    public override void VisitStringLiteral(StringLiteralSyntaxNode stringLiteralSyntaxNode)
    {
        builder.Append('"');
        builder.Append(stringLiteralSyntaxNode.Token.Value);
        builder.Append('"');
    }

    public override void VisitBooleanLiteral(BooleanLiteralSyntaxNode booleanLiteralSyntaxNode)
    {
        builder.Append(booleanLiteralSyntaxNode.Token.Value);
    }

    public override void VisitErrorLiteral(ErrorLiteralSyntaxNode errorLiteralSyntaxNode)
    {
        builder.Append(errorLiteralSyntaxNode.Token.Value);
    }

    public override void VisitName(NameSyntaxNode nameSyntaxNode)
    {
        builder.Append(nameSyntaxNode.Name);
    }

    public override void VisitUnaryExpression(UnaryExpressionSyntaxNode unaryExpressionSyntaxNode)
    {
        builder.Append(unaryExpressionSyntaxNode.Operator == UnaryOperator.Negate ? '-' : '+');
        AppendOperand(unaryExpressionSyntaxNode.Operand, Precedence(unaryExpressionSyntaxNode), wrapEqual: false);
    }

    public override void VisitBinaryExpression(BinaryExpressionSyntaxNode binaryExpressionSyntaxNode)
    {
        var precedence = Precedence(binaryExpressionSyntaxNode);
        AppendOperand(binaryExpressionSyntaxNode.Left, precedence, wrapEqual: false);
        builder.Append(TokenToOperator(binaryExpressionSyntaxNode.Token));
        AppendOperand(binaryExpressionSyntaxNode.Right, precedence, wrapEqual: true);
    }

    private void AppendOperand(FormulaSyntaxNode operand, int parentPrecedence, bool wrapEqual)
    {
        var precedence = Precedence(operand);
        var wrap = precedence < parentPrecedence || (wrapEqual && precedence == parentPrecedence);

        if (wrap)
        {
            builder.Append('(');
        }

        operand.Accept(this);

        if (wrap)
        {
            builder.Append(')');
        }
    }

    private static int Precedence(FormulaSyntaxNode node)
    {
        return node switch
        {
            BinaryExpressionSyntaxNode { Operator: BinaryOperator.Multiply or BinaryOperator.Divide } => 4,
            BinaryExpressionSyntaxNode { Operator: BinaryOperator.Plus or BinaryOperator.Minus } => 3,
            BinaryExpressionSyntaxNode { Operator: BinaryOperator.Concat } => 2,
            BinaryExpressionSyntaxNode => 1,
            UnaryExpressionSyntaxNode => 5,
            _ => 6
        };
    }

    public override void VisitCell(CellSyntaxNode cellSyntaxNode)
    {
        var token = cellSyntaxNode.Token;
        var adjusted = adjust(token);

        AppendWorksheetPrefix(token.Address.Worksheet);

        if (token.Address.IsColumnAbsolute)
        {
            builder.Append('$');
        }
        builder.Append(ColumnRef.ToString(adjusted.Column));
        if (token.Address.IsRowAbsolute)
        {
            builder.Append('$');
        }
        builder.Append(adjusted.Row + 1);
    }

    public override void VisitFunction(FunctionSyntaxNode functionSyntaxNode)
    {
        builder.Append(functionSyntaxNode.Name);
        builder.Append('(');
        for (int i = 0; i < functionSyntaxNode.Arguments.Count; i++)
        {
            if (i > 0)
            {
                builder.Append(',');
            }
            functionSyntaxNode.Arguments[i].Accept(this);
        }
        builder.Append(')');
    }

    public override void VisitRange(RangeSyntaxNode rangeSyntaxNode)
    {
        var startToken = rangeSyntaxNode.Start.Token;
        var endToken = rangeSyntaxNode.End.Token;

        var startAdjusted = adjust(startToken);
        var endAdjusted = adjust(endToken);

        if (rangeSyntaxNode.Kind != RangeKind.Cells)
        {
            AppendColumnOrRowRange(rangeSyntaxNode.Kind, startToken, endToken, startAdjusted, endAdjusted);
            return;
        }

        var startCell = startAdjusted;
        var endCell = endAdjusted;

        if (startCell.Row > endCell.Row || (startCell.Row == endCell.Row && startCell.Column > endCell.Column))
        {
            (startCell, endCell) = CellRef.Swap(startCell, endCell);
        }

        AppendWorksheetPrefix(startToken.Address.Worksheet);

        if (startToken.Address.IsColumnAbsolute)
        {
            builder.Append('$');
        }
        builder.Append(ColumnRef.ToString(startCell.Column));
        if (startToken.Address.IsRowAbsolute)
        {
            builder.Append('$');
        }
        builder.Append(startCell.Row + 1);

        builder.Append(':');

        if (endToken.Address.IsColumnAbsolute)
        {
            builder.Append('$');
        }
        builder.Append(ColumnRef.ToString(endCell.Column));
        if (endToken.Address.IsRowAbsolute)
        {
            builder.Append('$');
        }
        builder.Append(endCell.Row + 1);
    }

    private void AppendColumnOrRowRange(RangeKind kind, FormulaToken startToken, FormulaToken endToken, CellRef startAdjusted, CellRef endAdjusted)
    {
        var isColumns = kind == RangeKind.Columns;
        var start = isColumns ? startAdjusted.Column : startAdjusted.Row;
        var end = isColumns ? endAdjusted.Column : endAdjusted.Row;
        var max = isColumns ? Worksheet.MaxColumns : Worksheet.MaxRows;

        if (start < 0 || end < 0 || start >= max || end >= max)
        {
            builder.Append("#REF!");
            return;
        }

        var startAbsolute = isColumns ? startToken.Address.IsColumnAbsolute : startToken.Address.IsRowAbsolute;
        var endAbsolute = isColumns ? endToken.Address.IsColumnAbsolute : endToken.Address.IsRowAbsolute;

        if (start > end)
        {
            (start, end, startAbsolute, endAbsolute) = (end, start, endAbsolute, startAbsolute);
        }

        AppendWorksheetPrefix(startToken.Address.Worksheet);
        AppendColumnOrRow(isColumns, start, startAbsolute);
        builder.Append(':');
        AppendColumnOrRow(isColumns, end, endAbsolute);
    }

    private void AppendColumnOrRow(bool isColumn, int index, bool isAbsolute)
    {
        if (isAbsolute)
        {
            builder.Append('$');
        }

        if (isColumn)
        {
            builder.Append(ColumnRef.ToString(index));
        }
        else
        {
            builder.Append(index + 1);
        }
    }

    private void AppendWorksheetPrefix(string? worksheet)
    {
        if (string.IsNullOrEmpty(worksheet))
        {
            return;
        }

        if (NeedsQuoting(worksheet))
        {
            builder.Append('\'');
            builder.Append(worksheet.Replace("'", "''", StringComparison.Ordinal));
            builder.Append('\'');
        }
        else
        {
            builder.Append(worksheet);
        }

        builder.Append('!');
    }

    private static bool NeedsQuoting(string name)
    {
        if (char.IsDigit(name[0]))
        {
            return true;
        }

        foreach (var ch in name)
        {
            if (ch == ' ' || ch == '\'')
            {
                return true;
            }
        }

        return false;
    }

    private static string TokenToOperator(FormulaToken token)
    {
        return token.Type switch
        {
            FormulaTokenType.Plus => "+",
            FormulaTokenType.Minus => "-",
            FormulaTokenType.Star => "*",
            FormulaTokenType.Slash => "/",
            FormulaTokenType.Ampersand => "&",
            FormulaTokenType.Equals => "=",
            FormulaTokenType.GreaterThan => ">",
            FormulaTokenType.GreaterThanOrEqual => ">=",
            FormulaTokenType.LessThan => "<",
            FormulaTokenType.LessThanOrEqual => "<=",
            FormulaTokenType.EqualsGreaterThan => ">=",
            FormulaTokenType.LessThanGreaterThan => "<>",
            _ => throw new InvalidOperationException($"Unsupported operator token: {token.Type}")
        };
    }
}
