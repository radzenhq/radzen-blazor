using System;
using System.Collections.Generic;
using System.Linq.Expressions;

#nullable enable

namespace Radzen.Blazor.Spreadsheet;

class CountFunction : FormulaFunction
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
            return Expression.Constant(0);
        }

        // Count only numeric values, dates, logical values, and text representations of numbers
        var countExpressions = new List<Expression>();

        foreach (var expr in expressions)
        {
            if (TryGetError(expr, out error))
            {
                // Error values are not counted - skip them
                continue;
            }

            // Skip null values (empty cells)
            if (IsNullValue(expr))
            {
                continue;
            }

            // Count numeric types (including dates which are typically stored as numbers)
            if (IsNumericType(expr.Type))
            {
                countExpressions.Add(Expression.Constant(1));
            }
            // Count boolean values (logical values are counted in Excel)
            else if (expr.Type == typeof(bool))
            {
                countExpressions.Add(Expression.Constant(1));
            }
            // For string values, check if they represent numbers at runtime
            else if (expr.Type == typeof(string))
            {
                var isNumericString = Expression.Call(
                    typeof(CountFunction),
                    nameof(IsNumericString),
                    null,
                    expr
                );

                var conditionalCount = Expression.Condition(
                    isNumericString,
                    Expression.Constant(1),
                    Expression.Constant(0)
                );

                countExpressions.Add(conditionalCount);
            }
        }

        // Sum all the count expressions
        if (countExpressions.Count == 0)
        {
            return Expression.Constant(0);
        }

        Expression? sum = countExpressions[0];
        for (int i = 1; i < countExpressions.Count; i++)
        {
            sum = Expression.Add(sum, countExpressions[i]);
        }

        return sum!;
    }

    private static bool IsNumericString(string value)
    {
        if (string.IsNullOrEmpty(value))
            return false;

        // Try to parse as double first (covers most numeric formats)
        if (double.TryParse(value, out _))
            return true;

        // Try to parse as decimal (for more precision)
        if (decimal.TryParse(value, out _))
            return true;

        // Try to parse as int
        if (int.TryParse(value, out _))
            return true;

        return false;
    }
}
