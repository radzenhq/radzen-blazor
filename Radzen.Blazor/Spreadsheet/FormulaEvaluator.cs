using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Radzen.Blazor.Spreadsheet;

#nullable enable

class RangeExpression(List<Expression> expressions) : Expression
{
    public List<Expression> Expressions { get; } = expressions;
    public override ExpressionType NodeType => ExpressionType.Extension;
    public override Type Type => typeof(void); // Not used directly
}

/// <summary>
/// Represents errors that can occur during formula evaluation in a spreadsheet.
/// </summary>
public enum CellError
{
    /// <summary>
    /// Indicates a value error, such as type mismatch or invalid argument.
    /// </summary>
    Value,  // #VALUE! - Type mismatch or invalid argument
    /// <summary>
    /// Indicates a division by zero error.
    /// </summary>
    Div0,   // #DIV/0! - Division by zero
    /// <summary>
    /// Indicates an invalid cell reference error, such as referencing a cell that does not exist.
    /// </summary>
    Ref,    // #REF! - Invalid cell reference
    /// <summary>
    /// Indicates an invalid name error, such as using an undefined function or variable name.
    /// </summary>
    Name,   // #NAME? - Invalid function name
    /// <summary>
    /// Indicates an invalid number error, such as using a number that is too large or too small for the context.
    /// </summary>
    Num,    // #NUM! - Invalid number
    /// <summary>
    /// Indicates that a value is not available, such as when a formula cannot return a result.
    /// </summary>
    NA,      // #N/A - Value not available
    /// <summary>
    /// Indicates a circular reference error, which occurs when a formula refers back to its own cell either directly or indirectly.
    /// </summary>
    Circular // #CIRCULAR - Circular reference
}

class FormulaEvaluator(Sheet sheet) : IFormulaSyntaxNodeVisitor
{
    private readonly Sheet sheet = sheet;
    private Expression? expression;
    private CellError? error;
    private readonly HashSet<Cell> evaluationStack = [];

    public void VisitNumberLiteral(NumberLiteralSyntaxNode numberLiteralSyntaxNode)
    {
        expression = numberLiteralSyntaxNode.Token.ToConstantExpression();
    }

    public void VisitStringLiteral(StringLiteralSyntaxNode stringLiteralSyntaxNode)
    {
        expression = Expression.Constant(stringLiteralSyntaxNode.Token.Value);
    }

    private static Expression ConvertIfNeeded(Expression expression, Type targetType)
    {
        if (expression is not LambdaExpression)
        {
            return expression.Type == targetType ? expression : Expression.Convert(expression, targetType);
        }

        return expression;
    }

    private static bool IsNumericType(Type type)
    {
        return type == typeof(int) || type == typeof(uint) ||
               type == typeof(long) || type == typeof(ulong) ||
               type == typeof(float) || type == typeof(double) ||
               type == typeof(short) || type == typeof(ushort) ||
               type == typeof(decimal);
    }

    private static bool IsNullValue(Expression expr)
    {
        if (expr is ConstantExpression constantExpr)
        {
            return constantExpr.Value == null;
        }
        return false;
    }

    private static bool TryGetError(Expression expr, out CellError? error)
    {
        if (expr is ConstantExpression constantExpr && constantExpr.Value is CellError cellError)
        {
            error = cellError;
            return true;
        }

        error = null;

        return false;
    }

    private static Type GetResultType(Type left, Type right)
    {
        if (left == typeof(double) || right == typeof(double))
        {
            return typeof(double);
        }

        if (left == typeof(float) || right == typeof(float))
        {
            return typeof(float);
        }

        if (left == typeof(decimal) || right == typeof(decimal))
        {
            return typeof(decimal);
        }

        if (left == typeof(ulong) || right == typeof(ulong))
        {
            return typeof(ulong);
        }

        if (left == typeof(long) || right == typeof(long))
        {
            return typeof(long);
        }

        return left == typeof(uint) || right == typeof(uint) ? typeof(uint) : typeof(int);
    }

    public void VisitBinaryExpression(BinaryExpressionSyntaxNode binaryExpressionSyntaxNode)
    {
        binaryExpressionSyntaxNode.Left.Accept(this);
        var left = expression!;
        binaryExpressionSyntaxNode.Right.Accept(this);
        var right = expression!;

        if (TryGetError(left, out error))
        {
            expression = Expression.Constant(error);
            return;
        }

        if (TryGetError(right, out error))
        {
            expression = Expression.Constant(error);
            return;
        }

        if (IsNullValue(left))
        {
            left = Expression.Constant(0d);
        }

        if (IsNullValue(right))
        {
            right = Expression.Constant(0d);
        }

        // For comparison operators, we don't need both sides to be numeric
        var isComparisonOperator = binaryExpressionSyntaxNode.Operator is
            BinaryOperator.Equals or BinaryOperator.NotEquals or
            BinaryOperator.LessThan or BinaryOperator.LessThanOrEqual or
            BinaryOperator.GreaterThan or BinaryOperator.GreaterThanOrEqual;

        if (!isComparisonOperator && (!IsNumericType(left.Type) || !IsNumericType(right.Type)))
        {
            error = CellError.Value;
            expression = Expression.Constant(CellError.Value);
            return;
        }

        if (binaryExpressionSyntaxNode.Operator == BinaryOperator.Divide)
        {
            var rightValue = ((ConstantExpression)right).Value;

            if (Equals(rightValue, 0d))
            {
                error = CellError.Div0;
                expression = Expression.Constant(CellError.Div0);
                return;
            }
        }

        if (isComparisonOperator)
        {
            // For comparison operators, we need to ensure both sides are the same type
            // Try to convert to a common type if possible
            if (left.Type != right.Type)
            {
                // Try to convert both to double for numeric comparisons
                if (IsNumericType(left.Type) && IsNumericType(right.Type))
                {
                    left = ConvertIfNeeded(left, typeof(double));
                    right = ConvertIfNeeded(right, typeof(double));
                }
                // For string comparisons, convert both to string
                else if (left.Type == typeof(string) || right.Type == typeof(string))
                {
                    left = ConvertIfNeeded(left, typeof(string));
                    right = ConvertIfNeeded(right, typeof(string));
                }
            }
        }
        else
        {
            var resultType = GetResultType(left.Type, right.Type);
            left = ConvertIfNeeded(left, resultType);
            right = ConvertIfNeeded(right, resultType);
        }

        expression = binaryExpressionSyntaxNode.Operator switch
        {
            BinaryOperator.Plus => Expression.Add(left, right),
            BinaryOperator.Minus => Expression.Subtract(left, right),
            BinaryOperator.Multiply => Expression.Multiply(left, right),
            BinaryOperator.Divide => Expression.Divide(left, right),
            BinaryOperator.Equals => Expression.Equal(left, right),
            BinaryOperator.NotEquals => Expression.NotEqual(left, right),
            BinaryOperator.LessThan => Expression.LessThan(left, right),
            BinaryOperator.LessThanOrEqual => Expression.LessThanOrEqual(left, right),
            BinaryOperator.GreaterThan => Expression.GreaterThan(left, right),
            BinaryOperator.GreaterThanOrEqual => Expression.GreaterThanOrEqual(left, right),
            _ => throw new InvalidOperationException($"Unsupported operator: {binaryExpressionSyntaxNode.Operator}")
        };
    }

    private Expression? EvaluateCell(Cell cell)
    {
        if (!evaluationStack.Add(cell))
        {
            error = CellError.Circular;
            return Expression.Constant(CellError.Circular);
        }

        Expression? result;
        if (cell.FormulaSyntaxNode != null)
        {
            cell.FormulaSyntaxNode.Accept(this);
            result = expression;
        }
        else if (cell.ValueType == CellValueType.Error && cell.Value is CellError cellError)
        {
            error = cellError;
            result = Expression.Constant(cellError);
        }
        else
        {
            result = Expression.Constant(cell.Value);
        }
        evaluationStack.Remove(cell);
        return result;
    }

    public void VisitCell(CellSyntaxNode cellSyntaxNode)
    {
        var address = cellSyntaxNode.Token.AddressValue;

        if (!sheet.Cells.TryGet(address.Row, address.Column, out var cell))
        {
            error = CellError.Ref;
            expression = Expression.Constant(CellError.Ref);
            return;
        }

        expression = EvaluateCell(cell);
    }

    public object? Evaluate(FormulaSyntaxNode node)
    {
        error = null;
        node.Accept(this);

        if (error != null)
        {
            return error;
        }

        var lambda = Expression.Lambda(expression!);
        return lambda.Compile().DynamicInvoke();
    }


    public void VisitFunction(FunctionSyntaxNode functionSyntaxNode)
    {
        var arguments = new List<Expression>();

        foreach (var argument in functionSyntaxNode.Arguments)
        {
            argument.Accept(this);

            if (error != null)
            {
                return;
            }

            if (expression is RangeExpression rangeExpr)
            {
                arguments.AddRange(rangeExpr.Expressions);
            }
            else
            {
                arguments.Add(expression!);
            }
        }

        FormulaFunction? function = functionSyntaxNode.Name.ToUpperInvariant() switch
        {
            "SUM" => new SumFunction(),
            "IF" => new IfFunction(),
            _ => null
        };

        if (function == null)
        {
            error = CellError.Name;
            expression = Expression.Constant(CellError.Name);
            return;
        }

        expression = function.Evaluate(arguments);
        error = function.Error;
    }

    public void VisitRange(RangeSyntaxNode rangeSyntaxNode)
    {
        var start = rangeSyntaxNode.Start.Token.AddressValue;
        var end = rangeSyntaxNode.End.Token.AddressValue;

        if (start.Row > end.Row || (start.Row == end.Row && start.Column > end.Column))
        {
            error = CellError.Value;
            expression = Expression.Constant(CellError.Value);
            return;
        }

        var cells = new List<Expression>();
        for (var row = start.Row; row <= end.Row; row++)
        {
            for (var column = start.Column; column <= end.Column; column++)
            {
                if (!sheet.Cells.TryGet(row, column, out var cell))
                {
                    error = CellError.Ref;
                    expression = Expression.Constant(CellError.Ref);
                    return;
                }

                var cellExpression = EvaluateCell(cell);
                if (error != null)
                {
                    return;
                }
                cells.Add(cellExpression!);
            }
        }

        expression = new RangeExpression(cells);
    }
}