using System;
using System.Collections.Generic;
using System.Linq.Expressions;

#nullable enable

namespace Radzen.Blazor.Spreadsheet;

class SumFunction : FormulaFunction
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
            error = CellError.Value;
            return Expression.Constant(CellError.Value);
        }

        Expression? sum = null;

        foreach (var arg in expressions)
        {
            if (TryGetError(arg, out error))
            {
                return Expression.Constant(error);
            }

            var addend = IsNullValue(arg) ? Expression.Constant(0) : arg;

            if (sum == null)
            {
                sum = addend;
            }
            else
            {
                var resultType = GetResultType(sum.Type, addend.Type);
                sum = ConvertIfNeeded(sum, resultType);
                addend = ConvertIfNeeded(addend, resultType);
                sum = Expression.Add(sum, addend);
            }
        }

        return sum!;
    }
}
