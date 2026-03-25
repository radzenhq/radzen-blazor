using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Radzen.Documents.Spreadsheet;

#nullable enable

/// <summary>
/// Provides Excel-compatible number format code rendering.
/// </summary>
public static class NumberFormat
{

    /// <summary>
    /// Applies a format code to a value and returns the formatted string.
    /// Returns null when the format is General/null (caller should fall back to default rendering).
    /// </summary>
    public static string? Apply(string? formatCode, object? value, CellDataType type)
        => ApplyWithColor(formatCode, value, type).Text;

    /// <summary>
    /// Applies a format code to a value and returns the formatted string and optional color.
    /// The color is determined by color codes in the format string (e.g., [Red], [Green]).
    /// </summary>
    public static (string? Text, string? Color) ApplyWithColor(string? formatCode, object? value, CellDataType type)
    {
        if (value is null || string.IsNullOrEmpty(formatCode) ||
            string.Equals(formatCode, "General", StringComparison.OrdinalIgnoreCase))
        {
            return (null, null);
        }

        if (formatCode == "@")
        {
            return (value.ToString(), null);
        }

        if (type == CellDataType.String && value is string s)
        {
            return (s, null);
        }

        var parsed = NumberFormatParser.Parse(formatCode);

        if (!TryGetNumber(value, type, out var number))
        {
            return (value.ToString(), null);
        }

        var section = SelectSection(parsed, number);
        string? text;

        if (section.IsDate)
        {
            text = FormatDate(section, value, type, number);
        }
        else
        {
            text = FormatNumber(section, number);
        }

        return (text, section.Color);
    }

    /// <summary>
    /// Adjusts the number of decimal places in a format code.
    /// </summary>
    public static string AdjustDecimals(string? formatCode, int delta)
    {
        if (string.IsNullOrEmpty(formatCode) ||
            string.Equals(formatCode, "General", StringComparison.OrdinalIgnoreCase))
        {
            formatCode = "0";
        }

        // Find the decimal point and percent/suffix
        var hasPercent = formatCode.Contains('%', StringComparison.Ordinal);
        var core = hasPercent ? formatCode.Replace("%", "", StringComparison.Ordinal) : formatCode;

        // Split off scientific exponent suffix (E+00, E-00, etc.)
        var exponentSuffix = "";
        for (var idx = 0; idx < core.Length; idx++)
        {
            if ((core[idx] is 'E' or 'e') && idx + 1 < core.Length && core[idx + 1] is '+' or '-')
            {
                exponentSuffix = core[idx..];
                core = core[..idx];
                break;
            }
        }

        var dotIndex = core.IndexOf('.', StringComparison.Ordinal);
        int currentDecimals;
        string integerPart;

        if (dotIndex >= 0)
        {
            integerPart = core[..dotIndex];
            var decimalPart = core[(dotIndex + 1)..];
            currentDecimals = decimalPart.Length;
        }
        else
        {
            integerPart = core;
            currentDecimals = 0;
        }

        var newDecimals = Math.Max(0, currentDecimals + delta);

        string result;
        if (newDecimals == 0)
        {
            result = integerPart;
        }
        else
        {
            result = integerPart + "." + new string('0', newDecimals);
        }

        result += exponentSuffix;

        if (hasPercent)
        {
            result += "%";
        }

        return result;
    }

    /// <summary>
    /// Determines whether a format code represents a date/time format.
    /// </summary>
    internal static bool IsDateFormat(string formatCode)
    {
        if (string.IsNullOrEmpty(formatCode))
        {
            return false;
        }

        var inLiteral = false;
        var escaped = false;
        var inBracket = false;

        for (var i = 0; i < formatCode.Length; i++)
        {
            var c = formatCode[i];

            if (escaped)
            {
                escaped = false;
                continue;
            }

            if (c == '\\')
            {
                escaped = true;
                continue;
            }

            if (c == '"')
            {
                inLiteral = !inLiteral;
                continue;
            }

            if (inLiteral)
            {
                continue;
            }

            if (c == '[')
            {
                inBracket = true;
                continue;
            }

            if (c == ']')
            {
                inBracket = false;
                continue;
            }

            if (inBracket)
            {
                continue;
            }

            switch (c)
            {
                case 'y' or 'Y':
                case 'd' or 'D':
                    return true;
                case 'h' or 'H':
                    return true;
                case 's' or 'S':
                    return true;
                case 'm' or 'M':
                    // m is date if not preceded by h (which would make it minutes)
                    // but even minutes make the format a "date/time" format
                    return true;
            }
        }

        return false;
    }

    private static bool TryGetNumber(object? value, CellDataType type, out double number)
    {
        number = 0;

        switch (value)
        {
            case double d:
                number = d;
                return true;
            case int i:
                number = i;
                return true;
            case float f:
                number = f;
                return true;
            case decimal dec:
                number = (double)dec;
                return true;
            case long l:
                number = l;
                return true;
            case DateTime dt:
                number = dt.ToNumber();
                return true;
            default:
                if (type == CellDataType.Number || type == CellDataType.Date)
                {
                    return double.TryParse(value?.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out number);
                }
                return false;
        }
    }

    private static FormatSection SelectSection(ParsedFormat parsed, double value)
    {
        if (parsed.Sections.Count == 1)
        {
            return parsed.Sections[0];
        }

        if (parsed.Sections.Count == 2)
        {
            return value >= 0 ? parsed.Sections[0] : parsed.Sections[1];
        }

        if (parsed.Sections.Count >= 3)
        {
            if (value > 0)
            {
                return parsed.Sections[0];
            }

            if (value < 0)
            {
                return parsed.Sections[1];
            }
            return parsed.Sections[2];
        }

        return parsed.Sections[0];
    }

    private static string FormatDate(FormatSection section, object? value, CellDataType type, double number)
    {
        DateTime dt;
        if (value is DateTime dateTime)
        {
            dt = dateTime;
        }
        else
        {
            dt = number.ToDate();
            // Preserve fractional day as time
            var fractional = number - Math.Truncate(number);
            if (fractional > 0)
            {
                dt = dt.Date.AddDays(fractional);
            }
        }

        var sb = new StringBuilder();
        var tokens = section.Tokens;

        for (var i = 0; i < tokens.Count; i++)
        {
            var token = tokens[i];
            switch (token.Type)
            {
                case TokenType.Literal:
                    sb.Append(token.Text);
                    break;
                case TokenType.Year:
                    sb.Append(token.Text.Length switch
                    {
                        1 or 2 => dt.ToString("yy", CultureInfo.InvariantCulture),
                        _ => dt.ToString("yyyy", CultureInfo.InvariantCulture)
                    });
                    break;
                case TokenType.Month:
                    sb.Append(token.Text.Length switch
                    {
                        1 => dt.Month.ToString(CultureInfo.InvariantCulture),
                        2 => dt.ToString("MM", CultureInfo.InvariantCulture),
                        3 => dt.ToString("MMM", CultureInfo.InvariantCulture),
                        4 => dt.ToString("MMMM", CultureInfo.InvariantCulture),
                        _ => dt.ToString("MMMM", CultureInfo.InvariantCulture)
                    });
                    break;
                case TokenType.Day:
                    sb.Append(token.Text.Length switch
                    {
                        1 => dt.Day.ToString(CultureInfo.InvariantCulture),
                        2 => dt.ToString("dd", CultureInfo.InvariantCulture),
                        3 => dt.ToString("ddd", CultureInfo.InvariantCulture),
                        _ => dt.ToString("dddd", CultureInfo.InvariantCulture)
                    });
                    break;
                case TokenType.Hour:
                    if (section.Is12Hour)
                    {
                        var h = dt.Hour % 12;
                        if (h == 0)
                        {
                            h = 12;
                        }
                        sb.Append(h.ToString(CultureInfo.InvariantCulture));
                    }
                    else
                    {
                        sb.Append(token.Text.Length >= 2
                            ? dt.ToString("HH", CultureInfo.InvariantCulture)
                            : dt.Hour.ToString(CultureInfo.InvariantCulture));
                    }
                    break;
                case TokenType.Minute:
                    sb.Append(token.Text.Length >= 2
                        ? dt.ToString("mm", CultureInfo.InvariantCulture)
                        : dt.Minute.ToString(CultureInfo.InvariantCulture));
                    break;
                case TokenType.Second:
                    sb.Append(token.Text.Length >= 2
                        ? dt.ToString("ss", CultureInfo.InvariantCulture)
                        : dt.Second.ToString(CultureInfo.InvariantCulture));
                    break;
                case TokenType.AmPm:
                    sb.Append(dt.Hour >= 12 ? "PM" : "AM");
                    break;
            }
        }

        return sb.ToString();
    }

    private static string FormatNumber(FormatSection section, double value)
    {
        // Section with no digit placeholders (e.g. literal "-") returns just prefix+suffix
        if (!section.HasDigitPlaceholders)
        {
            return section.Prefix + section.Suffix;
        }

        if (section.IsScientific)
        {
            return FormatScientific(section, value);
        }

        var absValue = Math.Abs(value);
        var isNegative = value < 0;

        // Percentage: multiply by 100
        if (section.IsPercentage)
        {
            absValue *= 100;
        }

        // Thousands scaling
        for (var i = 0; i < section.ThousandsScale; i++)
        {
            absValue /= 1000;
        }

        // Count integer and decimal placeholders
        var intZeros = section.IntegerZeros;
        var intHashes = section.IntegerHashes;
        var decimalPlaces = section.DecimalPlaces;
        var hasThousands = section.HasThousandsSeparator;

        // Round to decimal places
        absValue = Math.Round(absValue, decimalPlaces, MidpointRounding.AwayFromZero);

        var sb = new StringBuilder();

        // Prefix literals
        sb.Append(section.Prefix);

        // Add negative sign if this is a single-section format and value is negative
        if (isNegative && section.Index == 0 && !section.HasParens)
        {
            sb.Append('-');
        }

        // Opening paren for negative section with parens
        if (section.HasParens)
        {
            // parens are part of the format literals
        }

        // Split into integer and decimal parts
        var intPart = (long)Math.Truncate(absValue);
        var fracPart = absValue - Math.Truncate(absValue);

        // Format integer part
        var intStr = intPart.ToString(CultureInfo.InvariantCulture);

        // Pad with zeros
        var minIntDigits = Math.Max(intZeros, 1);
        if (intZeros == 0 && intHashes > 0 && intPart == 0)
        {
            // Hash hides leading zeros - don't show zero integer part
            intStr = "";
            minIntDigits = 0;
        }
        else if (intStr.Length < intZeros)
        {
            intStr = intStr.PadLeft(intZeros, '0');
        }

        // Add thousands separators
        if (hasThousands && intStr.Length > 0)
        {
            var formatted = new StringBuilder(intStr.Length + (intStr.Length - 1) / 3);
            for (var i = 0; i < intStr.Length; i++)
            {
                var remaining = intStr.Length - i;
                if (i > 0 && remaining % 3 == 0)
                {
                    formatted.Append(',');
                }
                formatted.Append(intStr[i]);
            }
            intStr = formatted.ToString();
        }

        sb.Append(intStr);

        // Format decimal part
        if (decimalPlaces > 0)
        {
            sb.Append('.');
            var fracStr = Math.Round(fracPart, decimalPlaces, MidpointRounding.AwayFromZero)
                .ToString("F" + decimalPlaces, CultureInfo.InvariantCulture);
            // Remove "0." prefix
            if (fracStr.StartsWith("0.", StringComparison.Ordinal))
            {
                fracStr = fracStr[2..];
            }
            else if (fracStr.StartsWith("1.", StringComparison.Ordinal))
            {
                // Rounding carried over - already handled by absValue rounding
                fracStr = new string('0', decimalPlaces);
            }

            // Strip trailing zeros for hash (#) decimal placeholders
            // Keep at least DecimalZeros + DecimalSpacePlaceholders digits
            var decimalZeros = section.DecimalZeros;
            var spacePlaceholders = section.DecimalSpacePlaceholders;
            var keepLen = decimalZeros + spacePlaceholders;

            if (keepLen < decimalPlaces)
            {
                while (fracStr.Length > keepLen && fracStr[^1] == '0')
                {
                    fracStr = fracStr[..^1];
                }
            }

            // Replace trailing zeros with spaces for question mark (?) placeholders
            if (spacePlaceholders > 0)
            {
                var chars = fracStr.ToCharArray();
                for (var j = chars.Length - 1; j >= decimalZeros; j--)
                {
                    if (chars[j] == '0')
                    {
                        chars[j] = ' ';
                    }
                    else
                    {
                        break;
                    }
                }
                fracStr = new string(chars);
            }

            // If all decimal digits stripped, remove the dot too
            if (fracStr.Length == 0)
            {
                sb.Length--; // remove the '.'
            }
            else
            {
                sb.Append(fracStr);
            }
        }

        // Suffix (%, etc.)
        sb.Append(section.Suffix);

        return sb.ToString();
    }

    private static string FormatScientific(FormatSection section, double value)
    {
        var sb = new StringBuilder();
        sb.Append(section.Prefix);

        var isNegative = value < 0;
        var absValue = Math.Abs(value);

        if (isNegative)
        {
            sb.Append('-');
        }

        int exponent;
        double mantissa;

        if (absValue == 0)
        {
            exponent = 0;
            mantissa = 0;
        }
        else
        {
            exponent = (int)Math.Floor(Math.Log10(absValue));
            var intDigits = Math.Max(section.IntegerZeros, 1);
            exponent -= (intDigits - 1);
            mantissa = absValue / Math.Pow(10, exponent);
        }

        // Round mantissa to DecimalPlaces
        mantissa = Math.Round(mantissa, section.DecimalPlaces, MidpointRounding.AwayFromZero);

        // Check if rounding carried over (e.g., 9.995 → 10.00)
        if (mantissa != 0)
        {
            var intDigits = Math.Max(section.IntegerZeros, 1);
            var mantissaIntPart = (long)Math.Truncate(mantissa);
            var mantissaIntDigits = mantissaIntPart == 0 ? 1 : (int)Math.Floor(Math.Log10(mantissaIntPart)) + 1;
            if (mantissaIntDigits > intDigits)
            {
                exponent += (mantissaIntDigits - intDigits);
                mantissa /= Math.Pow(10, mantissaIntDigits - intDigits);
                mantissa = Math.Round(mantissa, section.DecimalPlaces, MidpointRounding.AwayFromZero);
            }
        }

        // Format mantissa
        if (section.DecimalPlaces > 0)
        {
            sb.Append(mantissa.ToString("F" + section.DecimalPlaces, CultureInfo.InvariantCulture));
        }
        else
        {
            sb.Append(((long)Math.Round(mantissa)).ToString(CultureInfo.InvariantCulture));
        }

        // Format exponent part (e.g. "E+00")
        var expFormat = section.ExponentFormat;
        var expChar = expFormat[0]; // E or e
        var expSign = expFormat[1]; // + or -
        var expDigits = expFormat.Length - 2; // number of 0/# after sign

        sb.Append(expChar);

        if (exponent >= 0)
        {
            if (expSign == '+')
            {
                sb.Append('+');
            }
        }
        else
        {
            sb.Append('-');
        }

        sb.Append(Math.Abs(exponent).ToString(CultureInfo.InvariantCulture).PadLeft(expDigits, '0'));
        sb.Append(section.Suffix);

        return sb.ToString();
    }
}
