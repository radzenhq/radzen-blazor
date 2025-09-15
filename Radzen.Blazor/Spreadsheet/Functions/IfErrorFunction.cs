using System;
using System.Collections.Generic;

#nullable enable

namespace Radzen.Blazor.Spreadsheet;

/// <summary>
/// Implements the IFERROR function which returns a value if a formula evaluates to an error, otherwise returns the result of the formula.
/// </summary>
class IfErrorFunction : FormulaFunction
{
    public override bool CanHandleErrors => true;

    public override object? Evaluate(List<object?> arguments)
    {
        if (arguments.Count != 2)
        {
            error = CellError.Value;
            return error;
        }

        var value = arguments[0];
        var valueIfError = arguments[1];

        if (TryGetError(value, out _))
        {
            if (TryGetError(valueIfError, out var e))
            {
                error = e;
                return e;
            }

            return valueIfError is null ? "" : valueIfError;
        }

        if (TryGetError(valueIfError, out var secondArgError))
        {
            error = secondArgError;
            return secondArgError;
        }

        return value is null ? "" : value;
    }
}
