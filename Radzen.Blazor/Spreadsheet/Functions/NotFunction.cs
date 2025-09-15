using System;
using System.Collections.Generic;

#nullable enable

namespace Radzen.Blazor.Spreadsheet;

class NotFunction : FormulaFunction
{
    public override object? Evaluate(List<object?> arguments)
    {
        if (arguments.Count != 1)
        {
            error = CellError.Value;
            return error;
        }

        var argument = arguments[0];

        if (TryGetError(argument, out var e))
        {
            error = e;
            return e;
        }

        return !ConvertToBoolean(argument);
    }
}
