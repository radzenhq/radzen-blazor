using System;
using System.Collections.Generic;

#nullable enable

namespace Radzen.Blazor.Spreadsheet;

class CountFunction : FormulaFunction
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
            if (TryGetError(v, out _))
            {
                continue;
            }

            if (v is null)
            {
                continue;
            }

            if (IsNumeric(v))
            {
                count += 1d;
            }
            else if (v is bool)
            {
                count += 1d;
            }
            else if (v is string s)
            {
                if (IsNumericString(s))
                {
                    count += 1d;
                }
            }
        }

        return count;
    }

    private static bool IsNumericString(string value)
    {
        if (string.IsNullOrEmpty(value))
            return false;

        if (double.TryParse(value, out _))
            return true;

        if (decimal.TryParse(value, out _))
            return true;

        if (int.TryParse(value, out _))
            return true;

        return false;
    }
}
