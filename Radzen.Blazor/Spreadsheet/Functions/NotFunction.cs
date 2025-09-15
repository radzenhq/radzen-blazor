using System;
using System.Collections.Generic;
using System.Linq.Expressions;

#nullable enable

namespace Radzen.Blazor.Spreadsheet;

class NotFunction : FormulaFunction
{
    public override Expression Evaluate(List<Expression> arguments)
    {
        if (arguments.Count != 1)
        {
            error = CellError.Value;
            return Expression.Constant(CellError.Value);
        }

        var argument = arguments[0];

        // Check for errors in the argument first
        if (TryGetError(argument, out error))
        {
            return Expression.Constant(error);
        }

        // Convert the argument to boolean expression following Excel semantics
        var booleanExpr = ConvertToBooleanExpression(argument);

        // Return the logical NOT of the boolean expression
        return Expression.Not(booleanExpr);
    }
}
