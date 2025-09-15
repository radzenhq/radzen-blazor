using System;
using System.Collections.Generic;
using System.Linq.Expressions;

#nullable enable

namespace Radzen.Blazor.Spreadsheet;

class AverageFunction : FormulaFunction
{
    public override Expression Evaluate(List<Expression> arguments)
    {
        var expressions = new List<Expression>();

        foreach (var arg in arguments)
        {
            if (arg is RangeExpression rangeExpr)
            {
                expressions.AddRange(rangeExpr.Expressions);
            }
            else
            {
                expressions.Add(arg);
            }
        }

        if (expressions.Count == 0)
        {
            error = CellError.Div0; // Excel returns #DIV/0! for empty AVERAGE
            return Expression.Constant(CellError.Div0);
        }

        // Filter out non-numeric values and null values, but keep zeros
        var numericExpressions = new List<Expression>();

        foreach (var expr in expressions)
        {
            if (TryGetError(expr, out error))
            {
                return Expression.Constant(error);
            }

            // Skip null values and non-numeric types (following Excel semantics)
            if (!IsNullValue(expr) && IsNumericType(expr.Type))
            {
                numericExpressions.Add(expr);
            }
        }

        // If no numeric values found, return #DIV/0! error (Excel behavior)
        if (numericExpressions.Count == 0)
        {
            error = CellError.Div0;
            return Expression.Constant(CellError.Div0);
        }

        // Calculate sum of all numeric values
        Expression? sum = null;
        foreach (var expr in numericExpressions)
        {
            if (sum == null)
            {
                sum = expr;
            }
            else
            {
                var resultType = GetResultType(sum.Type, expr.Type);
                sum = ConvertIfNeeded(sum, resultType);
                var convertedExpr = ConvertIfNeeded(expr, resultType);
                sum = Expression.Add(sum, convertedExpr);
            }
        }

        // Calculate count of numeric values
        var count = Expression.Constant(numericExpressions.Count);

        // Convert count to the same type as sum for division
        var sumType = sum!.Type;
        var convertedCount = ConvertIfNeeded(count, sumType);

        // Return sum / count
        return Expression.Divide(sum, convertedCount);
    }
}
