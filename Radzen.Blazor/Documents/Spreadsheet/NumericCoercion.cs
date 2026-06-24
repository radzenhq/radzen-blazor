using System;
using System.Globalization;

namespace Radzen.Documents.Spreadsheet;

#nullable enable

internal static class NumericCoercion
{
    public static bool TryCoerceToDouble(object? value, out double result)
    {
        switch (value)
        {
            case double d:
                result = d;
                return true;
            case int i:
                result = i;
                return true;
            case long l:
                result = l;
                return true;
            case float f:
                result = f;
                return true;
            case decimal m:
                result = (double)m;
                return true;
            case DateTime dt:
                result = dt.ToNumber();
                return true;
            case string s when double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var n):
                result = n;
                return true;
        }

        result = default;
        return false;
    }
}
