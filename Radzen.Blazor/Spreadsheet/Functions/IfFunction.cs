using System;
using System.Collections.Generic;
using System.Linq.Expressions;

#nullable enable

namespace Radzen.Blazor.Spreadsheet;
class IfFunction : FormulaFunction
{
    public override Expression Evaluate(List<Expression> arguments)
    {
        if (arguments.Count < 2 || arguments.Count > 3)
        {
            error = CellError.Value;
            return Expression.Constant(CellError.Value);
        }

        var condition = arguments[0];
        var trueValue = arguments[1];
        var falseValue = arguments.Count == 3 ? arguments[2] : Expression.Constant(false);

        // Check for errors in any argument
        if (TryGetError(condition, out error))
        {
            return Expression.Constant(error);
        }

        if (TryGetError(trueValue, out error))
        {
            return Expression.Constant(error);
        }

        if (TryGetError(falseValue, out error))
        {
            return Expression.Constant(error);
        }

        // Convert condition to boolean following Excel semantics using expression tree
        var booleanCondition = ConvertToBooleanExpression(condition);

        // Ensure trueValue and falseValue have compatible types
        var (compatibleTrueValue, compatibleFalseValue) = EnsureCompatibleTypes(trueValue, falseValue);

        // Use Expression.Condition to create a proper conditional expression
        return Expression.Condition(booleanCondition, compatibleTrueValue, compatibleFalseValue);
    }
}
