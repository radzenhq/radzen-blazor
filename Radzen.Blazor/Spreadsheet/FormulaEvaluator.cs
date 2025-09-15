using System;
using System.Collections.Generic;

namespace Radzen.Blazor.Spreadsheet;

#nullable enable

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
    private object? value;
    private CellError? error;
    private readonly HashSet<Cell> evaluationStack = [];

    public void VisitNumberLiteral(NumberLiteralSyntaxNode numberLiteralSyntaxNode)
    {
        value = TokenToObject(numberLiteralSyntaxNode.Token);
    }

    public void VisitStringLiteral(StringLiteralSyntaxNode stringLiteralSyntaxNode)
    {
        value = stringLiteralSyntaxNode.Token.Value;
    }

    private static bool IsNumeric(object? v) => ValueHelpers.IsNumeric(v);

    private static double ToDouble(object v) => ValueHelpers.ToDouble(v);

    private static object? TokenToObject(FormulaToken token)
    {
        return token.ValueKind switch
        {
            ValueKind.Null => null,
            ValueKind.String => token.Value,
            ValueKind.True => true,
            ValueKind.False => false,
            ValueKind.Int => (double)token.IntValue,
            ValueKind.UInt => (double)token.UintValue,
            ValueKind.Long => (double)token.LongValue,
            ValueKind.ULong => (double)token.UlongValue,
            ValueKind.Float => (double)token.FloatValue,
            ValueKind.Double => token.DoubleValue,
            ValueKind.Decimal => (double)token.DecimalValue,
            _ => throw new InvalidOperationException($"Unsupported value kind: {token.ValueKind}")
        };
    }

    public void VisitBinaryExpression(BinaryExpressionSyntaxNode binaryExpressionSyntaxNode)
    {
        binaryExpressionSyntaxNode.Left.Accept(this);
        var left = value;
        binaryExpressionSyntaxNode.Right.Accept(this);
        var right = value;

        if (left is CellError le)
        {
            error = le;
            value = le;
            return;
        }

        if (right is CellError re)
        {
            error = re;
            value = re;
            return;
        }

        if (left is null)
        {
            left = 0d;
        }

        if (right is null)
        {
            right = 0d;
        }

        // For comparison operators, we don't need both sides to be numeric
        var isComparisonOperator = binaryExpressionSyntaxNode.Operator is
            BinaryOperator.Equals or BinaryOperator.NotEquals or
            BinaryOperator.LessThan or BinaryOperator.LessThanOrEqual or
            BinaryOperator.GreaterThan or BinaryOperator.GreaterThanOrEqual;

        if (!isComparisonOperator && (!IsNumeric(left) || !IsNumeric(right)))
        {
            error = CellError.Value;
            value = CellError.Value;
            return;
        }

        if (binaryExpressionSyntaxNode.Operator == BinaryOperator.Divide)
        {
            var rv = right;
            if (rv is double dv && dv == 0d || rv is int i && i == 0 || rv is float f && f == 0f || rv is decimal m && m == 0m)
            {
                error = CellError.Div0;
                value = CellError.Div0;
                return;
            }
        }

        if (isComparisonOperator)
        {
            switch (binaryExpressionSyntaxNode.Operator)
            {
                case BinaryOperator.Equals:
                    value = Equals(left, right);
                    return;
                case BinaryOperator.NotEquals:
                    value = !Equals(left, right);
                    return;
                case BinaryOperator.LessThan:
                    if (ValueHelpers.TryCompare(left, right, out var lt, out var cmpErr)) { value = lt < 0; return; }
                    error = cmpErr; value = cmpErr; return;
                case BinaryOperator.LessThanOrEqual:
                    if (ValueHelpers.TryCompare(left, right, out var lte, out var cmpErr2)) { value = lte <= 0; return; }
                    error = cmpErr2; value = cmpErr2; return;
                case BinaryOperator.GreaterThan:
                    if (ValueHelpers.TryCompare(left, right, out var gt, out var cmpErr3)) { value = gt > 0; return; }
                    error = cmpErr3; value = cmpErr3; return;
                case BinaryOperator.GreaterThanOrEqual:
                    if (ValueHelpers.TryCompare(left, right, out var gte, out var cmpErr4)) { value = gte >= 0; return; }
                    error = cmpErr4; value = cmpErr4; return;
            }
        }
        else
        {
            var l = ToDouble(left!);
            var r = ToDouble(right!);
            switch (binaryExpressionSyntaxNode.Operator)
            {
                case BinaryOperator.Plus: value = l + r; return;
                case BinaryOperator.Minus: value = l - r; return;
                case BinaryOperator.Multiply: value = l * r; return;
                case BinaryOperator.Divide: value = l / r; return;
            }
        }

        throw new InvalidOperationException($"Unsupported operator: {binaryExpressionSyntaxNode.Operator}");
    }

    private object? EvaluateCell(Cell cell)
    {
        if (!evaluationStack.Add(cell))
        {
            error = CellError.Circular;
            return CellError.Circular;
        }

        object? result;
        if (cell.FormulaSyntaxNode != null)
        {
            cell.FormulaSyntaxNode.Accept(this);
            result = value;
        }
        else if (cell.ValueType == CellValueType.Error && cell.Value is CellError cellError)
        {
            error = cellError;
            result = cellError;
        }
        else
        {
            result = cell.Value;
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
            value = CellError.Ref;
            return;
        }

        value = EvaluateCell(cell);
    }

    public object? Evaluate(FormulaSyntaxNode node)
    {
        error = null;
        node.Accept(this);

        if (error != null)
        {
            return error;
        }

        return value;
    }


    public void VisitFunction(FunctionSyntaxNode functionSyntaxNode)
    {
        var arguments = new List<object?>();

        // Get the function to check if it can handle errors
        var function = sheet.GetFormulaFunction(functionSyntaxNode.Name);
        var canHandleErrors = function.CanHandleErrors;

        foreach (var argument in functionSyntaxNode.Arguments)
        {
            argument.Accept(this);

            // Only short-circuit on errors if the function cannot handle them
            if (error != null && !canHandleErrors)
            {
                return;
            }

            if (value is List<object?> list)
            {
                arguments.AddRange(list);
            }
            else
            {
                arguments.Add(value);
            }
        }

        // Call the function with the arguments
        value = function.Evaluate(arguments);
        error = function.Error;
    }

    public void VisitRange(RangeSyntaxNode rangeSyntaxNode)
    {
        var start = rangeSyntaxNode.Start.Token.AddressValue;
        var end = rangeSyntaxNode.End.Token.AddressValue;

        if (start.Row > end.Row || (start.Row == end.Row && start.Column > end.Column))
        {
            error = CellError.Value;
            value = CellError.Value;
            return;
        }

        var cells = new List<object?>();
        for (var row = start.Row; row <= end.Row; row++)
        {
            for (var column = start.Column; column <= end.Column; column++)
            {
                if (!sheet.Cells.TryGet(row, column, out var cell))
                {
                    error = CellError.Ref;
                    value = CellError.Ref;
                    return;
                }

                var cellValue = EvaluateCell(cell);
                if (error != null)
                {
                    return;
                }
                cells.Add(cellValue);
            }
        }

        value = cells;
    }
}