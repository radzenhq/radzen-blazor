using System;
using System.Globalization;

namespace Radzen.Documents.Spreadsheet;

#nullable enable

/// <summary>
/// Provides shared logic for coercing arbitrary cell values to <see cref="double"/>.
/// </summary>
internal static class NumericCoercion
{
    /// <summary>
    /// Attempts to coerce the supplied value into a <see cref="double"/>.
    /// </summary>
    /// <param name="value">The value to coerce. Supported inputs are numeric primitives,
    /// <see cref="decimal"/>, <see cref="DateTime"/> (converted via the Excel serial date system),
    /// and strings parseable with <see cref="NumberStyles.Any"/> and <see cref="CultureInfo.InvariantCulture"/>.</param>
    /// <param name="result">When this method returns <see langword="true"/>, the coerced value; otherwise zero.</param>
    /// <returns><see langword="true"/> if the value was coerced; otherwise <see langword="false"/>.</returns>
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
