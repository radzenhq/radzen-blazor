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
    public static string? Apply(string? formatCode, object? value, CellDataType type, Dictionary<string, object>? cache = null)
        => ApplyWithColor(formatCode, value, type, cache).Text;

    /// <summary>
    /// Applies a format code to a value and returns the formatted string and optional color.
    /// The color is determined by color codes in the format string (e.g., [Red], [Green]).
    /// When <paramref name="cache"/> is provided, parsed format codes are cached for reuse across multiple calls.
    /// </summary>
    public static (string? Text, string? Color) ApplyWithColor(string? formatCode, object? value, CellDataType type, Dictionary<string, object>? cache = null)
    {
        if (value == null || string.IsNullOrEmpty(formatCode) ||
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

        var parsed = GetOrParseFormatCode(formatCode, cache);

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
        if (value is double d) { number = d; return true; }
        if (value is int i) { number = i; return true; }
        if (value is float f) { number = f; return true; }
        if (value is decimal dec) { number = (double)dec; return true; }
        if (value is long l) { number = l; return true; }
        if (value is DateTime dt) { number = dt.ToNumber(); return true; }
        if (type == CellDataType.Number || type == CellDataType.Date)
        {
            return double.TryParse(value?.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out number);
        }
        return false;
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
            if (value > 0) return parsed.Sections[0];
            if (value < 0) return parsed.Sections[1];
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
                        if (h == 0) h = 12;
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
                    if (chars[j] == '0') chars[j] = ' ';
                    else break;
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
            if (expSign == '+') sb.Append('+');
        }
        else
        {
            sb.Append('-');
        }

        sb.Append(Math.Abs(exponent).ToString(CultureInfo.InvariantCulture).PadLeft(expDigits, '0'));
        sb.Append(section.Suffix);

        return sb.ToString();
    }

    #region Parsing

    private static ParsedFormat GetOrParseFormatCode(string formatCode, Dictionary<string, object>? cache)
    {
        if (cache != null)
        {
            if (cache.TryGetValue(formatCode, out var cached))
            {
                return (ParsedFormat)cached;
            }

            var result = ParseFormatCode(formatCode);
            cache[formatCode] = result;
            return result;
        }

        return ParseFormatCode(formatCode);
    }

    private static ParsedFormat ParseFormatCode(string formatCode)
    {
        var parts = SplitSections(formatCode);
        var sections = new List<FormatSection>();

        for (var i = 0; i < parts.Count; i++)
        {
            sections.Add(ParseSection(parts[i], i));
        }

        return new ParsedFormat(sections);
    }

    private static List<string> SplitSections(string formatCode)
    {
        var sections = new List<string>();
        var current = new StringBuilder();
        var inLiteral = false;
        var escaped = false;

        foreach (var c in formatCode)
        {
            if (escaped)
            {
                current.Append(c);
                escaped = false;
                continue;
            }

            if (c == '\\')
            {
                current.Append(c);
                escaped = true;
                continue;
            }

            if (c == '"')
            {
                current.Append(c);
                inLiteral = !inLiteral;
                continue;
            }

            if (c == ';' && !inLiteral)
            {
                sections.Add(current.ToString());
                current.Clear();
                continue;
            }

            current.Append(c);
        }

        if (current.Length > 0)
        {
            sections.Add(current.ToString());
        }

        return sections;
    }

    private static FormatSection ParseSection(string section, int index)
    {
        var tokens = Tokenize(section);
        var isDate = false;
        var isPercentage = false;
        var isScientific = false;
        var exponentFormat = "";
        var hasThousands = false;
        var thousandsScale = 0;
        var integerZeros = 0;
        var integerHashes = 0;
        var decimalPlaces = 0;
        var decimalZeros = 0;
        var decimalSpacePlaceholders = 0;
        var inDecimal = false;
        var is12Hour = false;
        var hasParens = false;
        var hasDigitPlaceholders = false;
        var prefix = new StringBuilder();
        var suffix = new StringBuilder();
        string? sectionColor = null;

        // Extract color code from tokens
        foreach (var token in tokens)
        {
            if (token.Type == TokenType.ColorCode)
            {
                sectionColor = token.Text;
                break;
            }
        }

        // Check for AM/PM and date tokens
        foreach (var token in tokens)
        {
            if (token.Type == TokenType.AmPm) is12Hour = true;
            if (token.Type is TokenType.Year or TokenType.Month or TokenType.Day
                or TokenType.Hour or TokenType.Minute or TokenType.Second or TokenType.AmPm)
            {
                isDate = true;
            }
        }

        if (!isDate)
        {
            // Parse number format tokens
            var seenDigit = false;
            var afterDigits = false;

            foreach (var token in tokens)
            {
                switch (token.Type)
                {
                    case TokenType.Zero:
                        seenDigit = true;
                        hasDigitPlaceholders = true;
                        if (inDecimal) { decimalPlaces += token.Text.Length; decimalZeros += token.Text.Length; }
                        else integerZeros += token.Text.Length;
                        break;
                    case TokenType.Hash:
                        seenDigit = true;
                        hasDigitPlaceholders = true;
                        if (inDecimal) decimalPlaces += token.Text.Length;
                        else integerHashes += token.Text.Length;
                        break;
                    case TokenType.QuestionMark:
                        seenDigit = true;
                        hasDigitPlaceholders = true;
                        if (inDecimal) { decimalPlaces += token.Text.Length; decimalSpacePlaceholders += token.Text.Length; }
                        else integerHashes += token.Text.Length;
                        break;
                    case TokenType.Decimal:
                        inDecimal = true;
                        break;
                    case TokenType.Comma:
                        if (seenDigit && !inDecimal)
                        {
                            hasThousands = true;
                        }
                        break;
                    case TokenType.Percent:
                        isPercentage = true;
                        afterDigits = seenDigit;
                        break;
                    case TokenType.Exponent:
                        isScientific = true;
                        exponentFormat = token.Text;
                        break;
                    case TokenType.Underscore:
                        if (!seenDigit) prefix.Append(' ');
                        else suffix.Append(' ');
                        break;
                    case TokenType.Asterisk:
                        // Fill-repeat not applicable in HTML — skip
                        break;
                    case TokenType.Literal:
                        if (!seenDigit) prefix.Append(token.Text);
                        else suffix.Append(token.Text);
                        break;
                    case TokenType.OpenParen:
                        hasParens = true;
                        if (!seenDigit) prefix.Append('(');
                        break;
                    case TokenType.CloseParen:
                        suffix.Append(')');
                        break;
                }
            }

            // Check for trailing commas (scaling)
            var trimmed = section.TrimEnd();
            var trailingCommas = 0;
            for (var i = trimmed.Length - 1; i >= 0; i--)
            {
                if (trimmed[i] == ',') trailingCommas++;
                else break;
            }
            if (trailingCommas > 0 && !hasThousands)
            {
                thousandsScale = trailingCommas;
            }
        }

        return new FormatSection(
            tokens, index, isDate, isPercentage, hasThousands, thousandsScale,
            integerZeros, integerHashes, decimalPlaces, decimalZeros, is12Hour, hasParens,
            hasDigitPlaceholders, prefix.ToString(), isPercentage ? "%" : suffix.ToString(),
            isScientific, exponentFormat, decimalSpacePlaceholders, sectionColor);
    }

    private static List<FormatToken> Tokenize(string section)
    {
        var tokens = new List<FormatToken>();
        var i = 0;
        var hasH = false;

        while (i < section.Length)
        {
            var c = section[i];

            // Escaped character
            if (c == '\\' && i + 1 < section.Length)
            {
                tokens.Add(new FormatToken(TokenType.Literal, section[i + 1].ToString()));
                i += 2;
                continue;
            }

            // Quoted literal
            if (c == '"')
            {
                var end = section.IndexOf('"', i + 1);
                if (end > i)
                {
                    tokens.Add(new FormatToken(TokenType.Literal, section[(i + 1)..end]));
                    i = end + 1;
                }
                else
                {
                    i++;
                }
                continue;
            }

            // Bracket (color codes, conditions)
            if (c == '[')
            {
                var end = section.IndexOf(']', i + 1);
                if (end > i)
                {
                    var bracketContent = section[(i + 1)..end];
                    var colorCss = MapColorCode(bracketContent);
                    if (colorCss != null)
                    {
                        tokens.Add(new FormatToken(TokenType.ColorCode, colorCss));
                    }
                    i = end + 1;
                }
                else
                {
                    i++;
                }
                continue;
            }

            // AM/PM
            if (i + 4 < section.Length &&
                section.Substring(i, 5).Equals("AM/PM", StringComparison.OrdinalIgnoreCase))
            {
                tokens.Add(new FormatToken(TokenType.AmPm, "AM/PM"));
                i += 5;
                continue;
            }

            // Date/time tokens
            if (c is 'y' or 'Y')
            {
                var len = ConsumeRun(section, i, c);
                tokens.Add(new FormatToken(TokenType.Year, section.Substring(i, len)));
                i += len;
                continue;
            }

            if (c is 'd' or 'D')
            {
                var len = ConsumeRun(section, i, c);
                tokens.Add(new FormatToken(TokenType.Day, section.Substring(i, len)));
                i += len;
                continue;
            }

            if (c is 'h' or 'H')
            {
                hasH = true;
                var len = ConsumeRun(section, i, c);
                tokens.Add(new FormatToken(TokenType.Hour, section.Substring(i, len)));
                i += len;
                continue;
            }

            if (c is 's' or 'S')
            {
                var len = ConsumeRun(section, i, c);
                tokens.Add(new FormatToken(TokenType.Second, section.Substring(i, len)));
                i += len;
                continue;
            }

            if (c is 'm' or 'M')
            {
                var len = ConsumeRun(section, i, c);
                var text = section.Substring(i, len);

                // Determine if this is month or minute
                // m is minute if preceded by h or followed by s
                var isMinute = false;
                if (hasH)
                {
                    isMinute = true;
                }
                else
                {
                    // Look ahead for s
                    for (var j = i + len; j < section.Length; j++)
                    {
                        var nc = section[j];
                        if (nc is ':' or ' ') continue;
                        if (nc is 's' or 'S') { isMinute = true; break; }
                        break;
                    }
                }

                tokens.Add(new FormatToken(isMinute ? TokenType.Minute : TokenType.Month, text));
                i += len;
                continue;
            }

            // Number format tokens
            if (c == '0')
            {
                var len = ConsumeRun(section, i, '0');
                tokens.Add(new FormatToken(TokenType.Zero, section.Substring(i, len)));
                i += len;
                continue;
            }

            if (c == '#')
            {
                var len = ConsumeRun(section, i, '#');
                tokens.Add(new FormatToken(TokenType.Hash, section.Substring(i, len)));
                i += len;
                continue;
            }

            if (c == '.')
            {
                tokens.Add(new FormatToken(TokenType.Decimal, "."));
                i++;
                continue;
            }

            if (c == ',')
            {
                tokens.Add(new FormatToken(TokenType.Comma, ","));
                i++;
                continue;
            }

            if (c == '%')
            {
                tokens.Add(new FormatToken(TokenType.Percent, "%"));
                i++;
                continue;
            }

            if (c == '(')
            {
                tokens.Add(new FormatToken(TokenType.OpenParen, "("));
                i++;
                continue;
            }

            if (c == ')')
            {
                tokens.Add(new FormatToken(TokenType.CloseParen, ")"));
                i++;
                continue;
            }

            // Separators (: / - space) as literals
            if (c is ':' or '/' or '-' or ' ')
            {
                tokens.Add(new FormatToken(TokenType.Literal, c.ToString()));
                i++;
                continue;
            }

            // Currency and other literal characters
            if (c is '$' or '€' or '£' or '¥')
            {
                tokens.Add(new FormatToken(TokenType.Literal, c.ToString()));
                i++;
                continue;
            }

            // Scientific notation: E+00, E-00, e+00, e-00
            if (c is 'E' or 'e' && i + 1 < section.Length && section[i + 1] is '+' or '-')
            {
                var start = i;
                i += 2; // skip E and sign
                while (i < section.Length && section[i] is '0' or '#')
                {
                    i++;
                }
                tokens.Add(new FormatToken(TokenType.Exponent, section[start..i]));
                continue;
            }

            // Underscore: _X produces a space with the width of X
            if (c == '_' && i + 1 < section.Length)
            {
                tokens.Add(new FormatToken(TokenType.Underscore, section[i + 1].ToString()));
                i += 2;
                continue;
            }

            // Asterisk: *X repeats character X to fill (ignored in HTML)
            if (c == '*' && i + 1 < section.Length)
            {
                tokens.Add(new FormatToken(TokenType.Asterisk, section[i + 1].ToString()));
                i += 2;
                continue;
            }

            // Question mark: digit placeholder that pads with spaces
            if (c == '?')
            {
                var len = ConsumeRun(section, i, '?');
                tokens.Add(new FormatToken(TokenType.QuestionMark, section.Substring(i, len)));
                i += len;
                continue;
            }

            // Skip unknown
            i++;
        }

        return tokens;
    }

    private static int ConsumeRun(string s, int start, char c)
    {
        var i = start;
        var lower = char.ToLowerInvariant(c);
        while (i < s.Length && char.ToLowerInvariant(s[i]) == lower) i++;
        return i - start;
    }

    #endregion

    #region Types

    private sealed class ParsedFormat
    {
        public List<FormatSection> Sections { get; }
        public ParsedFormat(List<FormatSection> sections) => Sections = sections;
    }

    private enum TokenType
    {
        Literal, Zero, Hash, Decimal, Comma, Percent,
        Year, Month, Day, Hour, Minute, Second, AmPm,
        OpenParen, CloseParen,
        Exponent, Underscore, Asterisk, QuestionMark,
        ColorCode
    }

    private sealed class FormatToken
    {
        public TokenType Type { get; }
        public string Text { get; }
        public FormatToken(TokenType type, string text) { Type = type; Text = text; }
    }

    private sealed class FormatSection
    {
        public List<FormatToken> Tokens { get; }
        public int Index { get; }
        public bool IsDate { get; }
        public bool IsPercentage { get; }
        public bool HasThousandsSeparator { get; }
        public int ThousandsScale { get; }
        public int IntegerZeros { get; }
        public int IntegerHashes { get; }
        public int DecimalPlaces { get; }
        public int DecimalZeros { get; }
        public bool Is12Hour { get; }
        public bool HasParens { get; }
        public bool HasDigitPlaceholders { get; }
        public string Prefix { get; }
        public string Suffix { get; }
        public bool IsScientific { get; }
        public string ExponentFormat { get; }
        public int DecimalSpacePlaceholders { get; }
        public string? Color { get; }

        public FormatSection(List<FormatToken> tokens, int index, bool isDate, bool isPercentage,
            bool hasThousandsSeparator, int thousandsScale, int integerZeros, int integerHashes,
            int decimalPlaces, int decimalZeros, bool is12Hour, bool hasParens,
            bool hasDigitPlaceholders, string prefix, string suffix,
            bool isScientific, string exponentFormat, int decimalSpacePlaceholders,
            string? color)
        {
            Tokens = tokens;
            Index = index;
            IsDate = isDate;
            IsPercentage = isPercentage;
            HasThousandsSeparator = hasThousandsSeparator;
            ThousandsScale = thousandsScale;
            IntegerZeros = integerZeros;
            IntegerHashes = integerHashes;
            DecimalPlaces = decimalPlaces;
            DecimalZeros = decimalZeros;
            Is12Hour = is12Hour;
            HasParens = hasParens;
            HasDigitPlaceholders = hasDigitPlaceholders;
            Prefix = prefix;
            Suffix = suffix;
            IsScientific = isScientific;
            ExponentFormat = exponentFormat;
            DecimalSpacePlaceholders = decimalSpacePlaceholders;
            Color = color;
        }
    }

    private static string? MapColorCode(string code)
    {
        return code.ToUpperInvariant() switch
        {
            "RED" => "red",
            "GREEN" => "green",
            "BLUE" => "blue",
            "YELLOW" => "#CCCC00",
            "CYAN" => "cyan",
            "MAGENTA" => "magenta",
            "WHITE" => "white",
            "BLACK" => "black",
            _ => null
        };
    }

    #endregion
}
