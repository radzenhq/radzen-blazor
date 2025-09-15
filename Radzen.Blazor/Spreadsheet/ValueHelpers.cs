using System;

namespace Radzen.Blazor.Spreadsheet;

#nullable enable

internal static class ValueHelpers
{
    public static bool TryGetError(object? value, out CellError errorValue)
    {
        if (value is CellError e)
        {
            errorValue = e;
            return true;
        }

        errorValue = default;
        return false;
    }

    public static bool IsNumeric(object? value)
    {
        return value is sbyte or byte or short or ushort or int or uint or long or ulong or float or double or decimal;
    }

    public static double ToDouble(object value)
    {
        return value switch
        {
            double d => d,
            float f => f,
            decimal m => (double)m,
            int i => i,
            uint ui => ui,
            long l => l,
            ulong ul => ul,
            short s => s,
            ushort us => us,
            byte b => b,
            sbyte sb => sb,
            _ => Convert.ToDouble(value)
        };
    }

    public static bool ConvertToBoolean(object? value)
    {
        if (value is null)
        {
            return false;
        }

        return value switch
        {
            bool b => b,
            string s => !string.IsNullOrEmpty(s),
            _ when IsNumeric(value) => Math.Abs(ToDouble(value)) != 0,
            _ => false
        };
    }

    public static string ConvertToString(object? value)
    {
        if (value is bool b)
        {
            return b ? "True" : "False";
        }

        return value?.ToString() ?? string.Empty;
    }

    public static bool TryCompare(object? left, object? right, out int result, out CellError? error)
    {
        result = 0;
        error = null;

        if (IsNumeric(left) && IsNumeric(right))
        {
            var l = ToDouble(left!);
            var r = ToDouble(right!);
            result = l.CompareTo(r);
            return true;
        }

        if (left is string || right is string)
        {
            var ls = Convert.ToString(left);
            var rs = Convert.ToString(right);
            result = string.Compare(ls, rs, StringComparison.Ordinal);
            return true;
        }

        error = CellError.Value;
        return false;
    }
}


