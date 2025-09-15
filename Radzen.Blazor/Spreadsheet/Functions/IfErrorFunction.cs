using System;
using System.Collections.Generic;
using System.Linq.Expressions;

#nullable enable

namespace Radzen.Blazor.Spreadsheet;

/// <summary>
/// Implements the IFERROR function which returns a value if a formula evaluates to an error, otherwise returns the result of the formula.
/// </summary>
class IfErrorFunction : FormulaFunction
{
    public override bool CanHandleErrors => true;

    public override Expression Evaluate(List<Expression> arguments)
    {
        if (arguments.Count != 2)
        {
            error = CellError.Value;
            return Expression.Constant(CellError.Value);
        }

        var value = arguments[0];
        var valueIfError = arguments[1];

        // Check if the first argument (value) is an error
        if (TryGetError(value, out var valueError))
        {
            // If value is an error, return the valueIfError
            // But first check if valueIfError itself is an error
            if (TryGetError(valueIfError, out var valueIfErrorError))
            {
                error = valueIfErrorError;
                return Expression.Constant(valueIfErrorError);
            }

            // Convert valueIfError to string if it's an empty cell (null)
            var resultValue = IsNullValue(valueIfError) ? Expression.Constant("") : valueIfError;
            return resultValue;
        }

        // Check if the second argument (valueIfError) is an error
        if (TryGetError(valueIfError, out var secondArgError))
        {
            error = secondArgError;
            return Expression.Constant(secondArgError);
        }
        // If value is not an error, return the value itself
        // Convert to string if it's an empty cell (null) to match Excel behavior
        var result = IsNullValue(value) ? Expression.Constant("") : value;
        return result;
    }
}
