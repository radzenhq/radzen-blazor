using System;
using System.Collections.Generic;

#nullable enable

namespace Radzen.Blazor.Spreadsheet;

class CountAllFunction : FormulaFunction
{
    public override object? Evaluate(List<object?> arguments)
    {
        if (arguments.Count == 0)
        {
            return 0d;
        }

        double count = 0d;
        foreach (var v in arguments)
        {
            if (v is null)
            {
                continue;
            }

            count += 1d;
        }

        return count;
    }
}
