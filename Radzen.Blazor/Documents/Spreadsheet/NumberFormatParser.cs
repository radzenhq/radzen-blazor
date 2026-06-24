using System;
using System.Collections.Generic;
using System.Text;

namespace Radzen.Documents.Spreadsheet;

#nullable enable

static class NumberFormatParser
{
    public static ParsedFormat Parse(string formatCode)
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

        // Single pass: extract color, detect date tokens, and parse number format
        var seenDigit = false;

        foreach (var token in tokens)
        {
            if (token.Type == TokenType.ColorCode)
            {
                sectionColor ??= token.Text;
                continue;
            }

            if (token.Type == TokenType.AmPm)
            {
                is12Hour = true;
            }

            if (token.Type is TokenType.Year or TokenType.Month or TokenType.Day
                or TokenType.Hour or TokenType.Minute or TokenType.Second or TokenType.AmPm)
            {
                isDate = true;
            }

            if (!isDate)
            {
                switch (token.Type)
                {
                    case TokenType.Zero:
                        seenDigit = true;
                        hasDigitPlaceholders = true;
                        if (inDecimal)
                        {
                            decimalPlaces += token.Text.Length;
                            decimalZeros += token.Text.Length;
                        }
                        else
                        {
                            integerZeros += token.Text.Length;
                        }
                        break;
                    case TokenType.Hash:
                        seenDigit = true;
                        hasDigitPlaceholders = true;
                        if (inDecimal)
                        {
                            decimalPlaces += token.Text.Length;
                        }
                        else
                        {
                            integerHashes += token.Text.Length;
                        }
                        break;
                    case TokenType.QuestionMark:
                        seenDigit = true;
                        hasDigitPlaceholders = true;
                        if (inDecimal)
                        {
                            decimalPlaces += token.Text.Length;
                            decimalSpacePlaceholders += token.Text.Length;
                        }
                        else
                        {
                            integerHashes += token.Text.Length;
                        }
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
                        break;
                    case TokenType.Exponent:
                        isScientific = true;
                        exponentFormat = token.Text;
                        break;
                    case TokenType.Underscore:
                        if (!seenDigit)
                        {
                            prefix.Append(' ');
                        }
                        else
                        {
                            suffix.Append(' ');
                        }
                        break;
                    case TokenType.Asterisk:
                        // Fill-repeat not applicable in HTML — skip
                        break;
                    case TokenType.Literal:
                        if (!seenDigit)
                        {
                            prefix.Append(token.Text);
                        }
                        else
                        {
                            suffix.Append(token.Text);
                        }
                        break;
                    case TokenType.OpenParen:
                        hasParens = true;
                        if (!seenDigit)
                        {
                            prefix.Append('(');
                        }
                        break;
                    case TokenType.CloseParen:
                        suffix.Append(')');
                        break;
                }
            }
        }

        if (!isDate)
        {
            // Check for trailing commas (scaling)
            var trimmed = section.TrimEnd();
            var trailingCommas = 0;
            for (var i = trimmed.Length - 1; i >= 0; i--)
            {
                if (trimmed[i] == ',')
                {
                    trailingCommas++;
                }
                else
                {
                    break;
                }
            }
            if (trailingCommas > 0 && !hasThousands)
            {
                thousandsScale = trailingCommas;
            }
        }

        return new FormatSection
        {
            Tokens = tokens,
            Index = index,
            IsDate = isDate,
            IsPercentage = isPercentage,
            HasThousandsSeparator = hasThousands,
            ThousandsScale = thousandsScale,
            IntegerZeros = integerZeros,
            IntegerHashes = integerHashes,
            DecimalPlaces = decimalPlaces,
            DecimalZeros = decimalZeros,
            Is12Hour = is12Hour,
            HasParens = hasParens,
            HasDigitPlaceholders = hasDigitPlaceholders,
            Prefix = prefix.ToString(),
            Suffix = isPercentage ? "%" + suffix.ToString() : suffix.ToString(),
            IsScientific = isScientific,
            ExponentFormat = exponentFormat,
            DecimalSpacePlaceholders = decimalSpacePlaceholders,
            Color = sectionColor,
        };
    }

    private static List<FormatToken> Tokenize(string section)
    {
        var tokens = new List<FormatToken>();
        var i = 0;
        var hasH = false;

        while (i < section.Length)
        {
            var c = section[i];

            if (c == '\\' && i + 1 < section.Length)
            {
                tokens.Add(new FormatToken(TokenType.Literal, section[i + 1].ToString()));
                i += 2;
                continue;
            }

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
                    if (colorCss is not null)
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
                    for (var j = i + len; j < section.Length; j++)
                    {
                        var nc = section[j];
                        if (nc is ':' or ' ')
                        {
                            continue;
                        }

                        if (nc is 's' or 'S')
                        {
                            isMinute = true;
                            break;
                        }
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
                i += 2;
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
        while (i < s.Length && char.ToLowerInvariant(s[i]) == lower)
        {
            i++;
        }

        return i - start;
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
}

internal sealed class ParsedFormat
{
    public List<FormatSection> Sections { get; }
    public ParsedFormat(List<FormatSection> sections) => Sections = sections;
}

internal enum TokenType
{
    Literal, Zero, Hash, Decimal, Comma, Percent,
    Year, Month, Day, Hour, Minute, Second, AmPm,
    OpenParen, CloseParen,
    Exponent, Underscore, Asterisk, QuestionMark,
    ColorCode
}

internal sealed class FormatToken
{
    public TokenType Type { get; }
    public string Text { get; }
    public FormatToken(TokenType type, string text) { Type = type; Text = text; }
}

internal sealed class FormatSection
{
    public List<FormatToken> Tokens { get; init; } = new();
    public int Index { get; init; }
    public bool IsDate { get; init; }
    public bool IsPercentage { get; init; }
    public bool HasThousandsSeparator { get; init; }
    public int ThousandsScale { get; init; }
    public int IntegerZeros { get; init; }
    public int IntegerHashes { get; init; }
    public int DecimalPlaces { get; init; }
    public int DecimalZeros { get; init; }
    public bool Is12Hour { get; init; }
    public bool HasParens { get; init; }
    public bool HasDigitPlaceholders { get; init; }
    public string Prefix { get; init; } = string.Empty;
    public string Suffix { get; init; } = string.Empty;
    public bool IsScientific { get; init; }
    public string ExponentFormat { get; init; } = string.Empty;
    public int DecimalSpacePlaceholders { get; init; }
    public string? Color { get; init; }
}
