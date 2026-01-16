#nullable enable

using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Radzen.Blazor.Spreadsheet;

class TextFunction : FormulaFunction
{
    public override string Name => "TEXT";

    public override FunctionParameter[] Parameters =>
    [
        new("value", ParameterType.Single, isRequired: true),
        new("format_text", ParameterType.Single, isRequired: true)
    ];

    public override CellData Evaluate(FunctionArguments arguments)
    {
        var valueArg = arguments.GetSingle("value");
        var formatArg = arguments.GetSingle("format_text");

        if (valueArg == null || formatArg == null)
        {
            return CellData.FromError(CellError.Value);
        }

        if (valueArg.IsError) 
        {
            return valueArg;
        }

        if (formatArg.IsError) 
        {
            return formatArg;
        }

        var format = formatArg.GetValueOrDefault<string?>();
        if (string.IsNullOrEmpty(format))
        {
            return CellData.FromError(CellError.Value);
        }

        // Handle date/time formatting tokens -> map Excel-like tokens to .NET
        if (ContainsDateTimeTokens(format))
        {
            var dt = GetDateTimeFromValue(valueArg);
            var net = ConvertExcelDateFormatToDotNet(format!);
            return CellData.FromString(dt.ToString(net, CultureInfo.InvariantCulture));
        }

        // Numeric formatting
        if (!TryCoerceToDouble(valueArg, out var number))
        {
            return CellData.FromError(CellError.Value);
        }

        // Scientific/percent/currency/zeros via .NET custom numeric format
        return CellData.FromString(number.ToString(format, CultureInfo.InvariantCulture));
    }

    private static bool TryCoerceToDouble(CellData valueArg, out double number)
    {
        if (valueArg.Type == CellDataType.Number)
        {
            number = valueArg.GetValueOrDefault<double>();
            return true;
        }
        if (valueArg.Type == CellDataType.Date)
        {
            // Excel stores dates as serials; use serial number for numeric formats
            number = valueArg.GetValueOrDefault<DateTime>().ToNumber();
            return true;
        }
        if (valueArg.TryCoerceToNumber(out var n, allowBooleans: false, nonNumericTextAsZero: false))
        {
            number = n;
            return true;
        }
        number = 0;
        return false;
    }

    private static bool ContainsDateTimeTokens(string format)
    {
        var f = format.ToUpperInvariant();
#if NET8_0_OR_GREATER
        return f.Contains('Y', StringComparison.Ordinal) ||
               f.Contains('D', StringComparison.Ordinal) ||
               f.Contains('H', StringComparison.Ordinal) ||
               f.Contains('S', StringComparison.Ordinal) ||
               f.Contains("AM/PM", StringComparison.Ordinal);
#else
#pragma warning disable CA1847
        return f.Contains("Y", StringComparison.Ordinal) ||
               f.Contains("D", StringComparison.Ordinal) ||
               f.Contains("H", StringComparison.Ordinal) ||
               f.Contains("S", StringComparison.Ordinal) ||
               f.Contains("AM/PM", StringComparison.Ordinal);
#pragma warning restore CA1847
#endif
    }

    private static DateTime GetDateTimeFromValue(CellData valueArg)
    {
        if (valueArg.Type == CellDataType.Date)
        {
            return valueArg.GetValueOrDefault<DateTime>();
        }
        if (valueArg.Type == CellDataType.Number)
        {
            var serial = valueArg.GetValueOrDefault<double>();
            return serial.ToDate();
        }
        // As a fallback try coercing to number
        if (valueArg.TryCoerceToNumber(out var n, allowBooleans: false, nonNumericTextAsZero: false))
        {
            return n.ToDate();
        }
        // Default epoch
        return new DateTime(1900, 1, 1);
    }

    private static string ConvertExcelDateFormatToDotNet(string format)
    {
        // Normalize case for keywords
        string f = format;

        // AM/PM -> tt
        f = Regex.Replace(f, "AM/PM", "tt", RegexOptions.IgnoreCase);

        // Day of week: DDDD -> dddd, DDD -> ddd
        f = Regex.Replace(f, "DDDD", "dddd", RegexOptions.IgnoreCase);
        f = Regex.Replace(f, "DDD", "ddd", RegexOptions.IgnoreCase);

        // Day numeric: DD -> dd
        f = Regex.Replace(f, "DD", "dd", RegexOptions.IgnoreCase);

        // Year: YYYY -> yyyy, YY -> yy
        f = Regex.Replace(f, "YYYY", "yyyy", RegexOptions.IgnoreCase);
        f = Regex.Replace(f, "YY", "yy", RegexOptions.IgnoreCase);

        // Hours: if tt present => 12-hour 'h', else 24-hour 'H'
        var hasTt = f.Contains("tt", StringComparison.Ordinal);
        f = Regex.Replace(f, "HH", hasTt ? "hh" : "HH", RegexOptions.IgnoreCase);
        f = Regex.Replace(f, "H", hasTt ? "h" : "H", RegexOptions.IgnoreCase);

        // Minutes vs Months: if hours present, treat M as minutes; else months
        var hasHours = Regex.IsMatch(f, "h", RegexOptions.IgnoreCase) || Regex.IsMatch(f, "H", RegexOptions.IgnoreCase);
        if (hasHours)
        {
            f = Regex.Replace(f, "MM", "mm");
            f = Regex.Replace(f, "M", "m");
        }
        else
        {
            // Ensure months tokens are uppercase MM/M
            f = Regex.Replace(f, "mm", "MM");
        }

        // Seconds: SS -> ss
        f = Regex.Replace(f, "SS", "ss", RegexOptions.IgnoreCase);

        return f;
    }
}