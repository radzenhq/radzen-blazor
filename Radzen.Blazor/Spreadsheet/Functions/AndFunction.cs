using System;
using System.Collections.Generic;
using System.Linq.Expressions;

#nullable enable

namespace Radzen.Blazor.Spreadsheet;

class AndFunction : FormulaFunction
{
    public override Expression Evaluate(List<Expression> arguments)
    {
        if (arguments.Count == 0)
        {
            error = CellError.Value;
            return Expression.Constant(CellError.Value);
        }

        var expressions = new List<Expression>();

        // Flatten range expressions if any
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
            error = CellError.Value;
            return Expression.Constant(CellError.Value);
        }

        // Check for errors in any argument first
        foreach (var expr in expressions)
        {
            if (TryGetError(expr, out error))
            {
                return Expression.Constant(error);
            }
        }

        // Convert all arguments to boolean expressions following Excel semantics
        var booleanExpressions = new List<Expression>();
        foreach (var expr in expressions)
        {
            booleanExpressions.Add(ConvertToBooleanExpression(expr));
        }

        // Build the AND expression by chaining all boolean expressions with logical AND
        Expression? result = null;
        foreach (var booleanExpr in booleanExpressions)
        {
            if (result == null)
            {
                result = booleanExpr;
            }
            else
            {
                result = Expression.AndAlso(result, booleanExpr);
            }
        }

        return result!;
    }
}
