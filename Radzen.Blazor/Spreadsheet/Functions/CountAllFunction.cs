using System;
using System.Collections.Generic;
using System.Linq.Expressions;

#nullable enable

namespace Radzen.Blazor.Spreadsheet;

class CountAllFunction : FormulaFunction
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

        // Count all non-empty cells (including text, logical values, error values, empty strings)
        var countExpressions = new List<Expression>();

        foreach (var expr in expressions)
        {
            // Skip only null values (truly empty cells)
            if (IsNullValue(expr))
            {
                continue;
            }

            // Count everything else - numbers, text, logical values, error values, empty strings
            countExpressions.Add(Expression.Constant(1));
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
}
