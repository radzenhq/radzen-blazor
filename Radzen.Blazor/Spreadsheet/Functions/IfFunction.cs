using System;
using System.Collections.Generic;

#nullable enable

namespace Radzen.Blazor.Spreadsheet;
class IfFunction : FormulaFunction
{
    public override object? Evaluate(List<object?> arguments)
    {
        if (arguments.Count < 2 || arguments.Count > 3)
        {
            error = CellError.Value;
            return error;
        }

        var condition = arguments[0];
        var trueValue = arguments[1];
        var falseValue = arguments.Count == 3 ? arguments[2] : false;

        if (TryGetError(condition, out var e1))
        {
            error = e1;
            return e1;
        }

        if (TryGetError(trueValue, out var e2))
        {
            error = e2;
            return e2;
        }

        if (TryGetError(falseValue, out var e3))
        {
            error = e3;
            return e3;
        }

        var cond = ConvertToBoolean(condition);
        return cond ? trueValue : falseValue;
    }
}
