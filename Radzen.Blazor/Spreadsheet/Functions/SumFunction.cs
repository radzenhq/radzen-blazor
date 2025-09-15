using System;
using System.Collections.Generic;

#nullable enable

namespace Radzen.Blazor.Spreadsheet;

class SumFunction : FormulaFunction
{
    public override object? Evaluate(List<object?> arguments)
    {
        if (arguments.Count == 0)
        {
            error = CellError.Value;
            return error;
        }

        double sum = 0d;
        foreach (var v in arguments)
        {
            if (TryGetError(v, out var e))
            {
                error = e;
                return e;
            }

            if (v is null)
            {
                continue;
            }

            sum += ToDouble(v);
        }

        return sum;
    }
}
