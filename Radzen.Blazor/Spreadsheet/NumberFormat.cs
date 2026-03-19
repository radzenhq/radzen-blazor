using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Radzen.Blazor.Spreadsheet;

#nullable enable

/// <summary>
/// Provides Excel-compatible number format code rendering.
/// </summary>
public static class NumberFormat
{
    private static readonly ConcurrentDictionary<string, ParsedFormat> Cache = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Applies a format code to a value and returns the formatted string.
    /// Returns null when the format is General/null (caller should fall back to default rendering).
    /// </summary>
    public static string? Apply(string? formatCode, object? value, CellDataType type)
    {
        if (value == null || string.IsNullOrEmpty(formatCode) ||
            string.Equals(formatCode, "General", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        // Text format: @ returns value as-is
        if (formatCode == "@")
        {
            return value.ToString();
        }

        // For string values with non-@ format, pass through raw
        if (type == CellDataType.String && value is string s)
        {
            return s;
        }

        var parsed = Cache.GetOrAdd(formatCode, static code => ParseFormatCode(code));

        // Get numeric value
        if (!TryGetNumber(value, type, out var number))
        {
            return value.ToString();
        }

        // Select section based on sign
        var section = SelectSection(parsed, number);

        if (section.IsDate)
        {
            return FormatDate(section, value, type, number);
        }

        return FormatNumber(section, number);
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
            var formatted = new StringBuilder();
            var count = 0;
            for (var i = intStr.Length - 1; i >= 0; i--)
            {
                if (count > 0 && count % 3 == 0)
                {
                    formatted.Insert(0, ',');
                }
                formatted.Insert(0, intStr[i]);
                count++;
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
            // Keep at least DecimalZeros digits (from 0 placeholders)
            var decimalZeros = section.DecimalZeros;
            if (decimalZeros < decimalPlaces)
            {
                var minLen = decimalZeros;
                while (fracStr.Length > minLen && fracStr[^1] == '0')
                {
                    fracStr = fracStr[..^1];
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
            else
            {
                sb.Append(fracStr);
            }
        }

        // Suffix (%, etc.)
        sb.Append(section.Suffix);

        return sb.ToString();
    }

    #region Parsing

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
        var hasThousands = false;
        var thousandsScale = 0;
        var integerZeros = 0;
        var integerHashes = 0;
        var decimalPlaces = 0;
        var decimalZeros = 0;
        var inDecimal = false;
        var is12Hour = false;
        var hasParens = false;
        var hasDigitPlaceholders = false;
        var prefix = new StringBuilder();
        var suffix = new StringBuilder();

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
                    case TokenType.Decimal:
                        inDecimal = true;
                        break;
                    case TokenType.Comma:
                        if (seenDigit && !inDecimal)
                        {
                            // Look ahead: if next non-comma token is a digit placeholder, it's thousands separator
                            // Otherwise it's scaling
                            hasThousands = true;
                        }
                        break;
                    case TokenType.Percent:
                        isPercentage = true;
                        afterDigits = seenDigit;
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
            // In Excel, commas after the last digit placeholder scale down by 1000 each
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
            hasDigitPlaceholders, prefix.ToString(), isPercentage ? "%" : suffix.ToString());
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

            // Bracket (color codes, conditions) — skip
            if (c == '[')
            {
                var end = section.IndexOf(']', i + 1);
                if (end > i)
                {
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
        OpenParen, CloseParen
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

        public FormatSection(List<FormatToken> tokens, int index, bool isDate, bool isPercentage,
            bool hasThousandsSeparator, int thousandsScale, int integerZeros, int integerHashes,
            int decimalPlaces, int decimalZeros, bool is12Hour, bool hasParens,
            bool hasDigitPlaceholders, string prefix, string suffix)
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
        }
    }

    #endregion
}
