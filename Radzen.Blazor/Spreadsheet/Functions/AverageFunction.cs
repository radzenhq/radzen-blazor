using System;
using System.Collections.Generic;

#nullable enable

namespace Radzen.Blazor.Spreadsheet;

class AverageFunction : FormulaFunction
{
    public override object? Evaluate(List<object?> arguments)
    {
        if (arguments.Count == 0)
        {
            error = CellError.Div0;
            return error;
        }

        double sum = 0d;
        int count = 0;
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

            if (IsNumeric(v))
            {
                sum += ToDouble(v);
                count++;
            }
        }

        if (count == 0)
        {
            error = CellError.Div0;
            return error;
        }

        return sum / count;
    }
}
