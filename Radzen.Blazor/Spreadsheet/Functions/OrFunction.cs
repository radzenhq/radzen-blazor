using System;
using System.Collections.Generic;

#nullable enable

namespace Radzen.Blazor.Spreadsheet;

class OrFunction : FormulaFunction
{
    public override object? Evaluate(List<object?> arguments)
    {
        if (arguments.Count == 0)
        {
            error = CellError.Value;
            return error;
        }

        if (arguments.Count == 0)
        {
            error = CellError.Value;
            return error;
        }

        foreach (var v in arguments)
        {
            if (TryGetError(v, out var e))
            {
                error = e;
                return e;
            }

            if (ConvertToBoolean(v))
            {
                return true;
            }
        }

        return false;
    }
}
