using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Radzen;

/// <summary>
/// Lexer for parsing expressions into tokens.
/// </summary>
internal class ExpressionLexer(string expression)
{
    private int position;

    private char Peek(int offset = 0)
    {
        if (position + offset >= expression.Length)
        {
            return '\0';
        }

        return expression[position + offset];
    }

    private void Advance(int count)
    {
        position += count;
    }

    private bool TryAdvance(char expected)
    {
        if (Peek(1) == expected)
        {
            Advance(1);
            return true;
        }

        return false;
    }

    private void ScanTrivia()
    {
        while (char.IsWhiteSpace(Peek()))
        {
            Advance(1);
        }
    }

    /// <summary>
    /// Scans the expression and returns a list of tokens.
    /// </summary>
    /// <param name="expression">The expression to scan.</param>
    /// <returns>A list of tokens.</returns>
    public static List<Token> Scan(string expression)
    {
        var lexer = new ExpressionLexer(expression);

        return [.. lexer.Scan()];
    }

    /// <summary>
    /// Scans the expression and returns an enumerable of tokens.
    /// </summary>
    /// <returns>An enumerable of tokens.</returns>
    public IEnumerable<Token> Scan()
    {
        while (position < expression.Length)
        {
            ScanTrivia();

            var token = ScanToken();

            if (token.Type == TokenType.None)
            {
                yield break;
            }

            yield return token;
        }

        yield return new Token(TokenType.None, string.Empty);
    }

    private Token ScanToken()
    {
        var ch = Peek();

        switch (ch)
        {
            case '"':
                return ScanStringLiteral();
            case '\'':
                return ScanCharacterLiteral();
            case '=':
                if (TryAdvance('='))
                {
                    Advance(1);
                    return new Token(TokenType.EqualsEquals);
                }

                if (TryAdvance('>'))
                {
                    Advance(1);
                    return new Token(TokenType.EqualsGreaterThan);
                }

                Advance(1);
                return new Token(TokenType.Equals);
            case '!':
                if (TryAdvance('='))
                {
                    Advance(1);
                    return new Token(TokenType.NotEquals);
                }
                Advance(1);
                return new Token(TokenType.ExclamationMark);
            case '>':
                if (TryAdvance('='))
                {
                    Advance(1);
                    return new Token(TokenType.GreaterThanOrEqual);
                }
                if (TryAdvance('>'))
                {
                    Advance(1);
                    return new Token(TokenType.GreaterThanGreaterThan);
                }
                Advance(1);
                return new Token(TokenType.GreaterThan);
            case '<':
                if (TryAdvance('<'))
                {
                    Advance(1);
                    return new Token(TokenType.LessThanLessThan);
                }
                if (TryAdvance('='))
                {
                    Advance(1);
                    return new Token(TokenType.LessThanOrEqual);
                }
                Advance(1);
                return new Token(TokenType.LessThan);
            case '+':
                Advance(1);
                return new Token(TokenType.Plus);
            case '-':
                Advance(1);
                return new Token(TokenType.Minus);
            case '*':
                Advance(1);
                return new Token(TokenType.Star);
            case '/':
                Advance(1);
                return new Token(TokenType.Slash);
            case '.':
                Advance(1);
                return new Token(TokenType.Dot);
            case '(':
                Advance(1);
                return new Token(TokenType.OpenParen);
            case ')':
                Advance(1);
                return new Token(TokenType.CloseParen);
            case '[':
                Advance(1);
                return new Token(TokenType.OpenBracket);
            case ']':
                Advance(1);
                return new Token(TokenType.CloseBracket);
            case '{':
                Advance(1);
                return new Token(TokenType.OpenBrace);
            case '}':
                Advance(1);
                return new Token(TokenType.CloseBrace);
            case ',':
                Advance(1);
                return new Token(TokenType.Comma);
            case '&':
                if (TryAdvance('&'))
                {
                    Advance(1);
                    return new Token(TokenType.AmpersandAmpersand);
                }
                Advance(1);
                return new Token(TokenType.Ampersand);
            case '|':
                if (TryAdvance('|'))
                {
                    Advance(1);
                    return new Token(TokenType.BarBar);
                }
                Advance(1);
                return new Token(TokenType.Bar);
            case '?':
                if (TryAdvance('.'))
                {
                    Advance(1);
                    return new Token(TokenType.QuestionDot);
                }

                if (TryAdvance('?'))
                {
                    Advance(1);
                    return new Token(TokenType.QuestionMarkQuestionMark);
                }

                Advance(1);
                return new Token(TokenType.QuestionMark);
            case ':':
                Advance(1);
                return new Token(TokenType.Colon);
            case '^':
                Advance(1);
                return new Token(TokenType.Caret);
            case >= '0' and <= '9':
                return ScanNumericLiteral();
            case '_':
            case (>= 'a' and <= 'z') or (>= 'A' and <= 'Z'):
                var token = ScanIdentifier();

                return token.Value switch
                {
                    "null" => new Token(TokenType.NullLiteral) { ValueKind = ValueKind.Null },
                    "true" => new Token(TokenType.TrueLiteral) { ValueKind = ValueKind.True },
                    "false" => new Token(TokenType.FalseLiteral) { ValueKind = ValueKind.False },
                    "new" => new Token(TokenType.New),
                    _ => token
                };
        }

        return new Token(TokenType.None, string.Empty);
    }

    private char ScanEscapeSequence()
    {
        var ch = Peek();

        Advance(1);

        switch (ch)
        {
            case '\'':
            case '"':
            case '\\':
                break;
            case '0':
                ch = '\u0000';
                break;
            case 'a':
                ch = '\u0007';
                break;
            case 'b':
                ch = '\u0008';
                break;
            case 'f':
                ch = '\u000c';
                break;
            case 'n':
                ch = '\u000a';
                break;
            case 'r':
                ch = '\u000d';
                break;
            case 't':
                ch = '\u0009';
                break;
            case 'v':
                ch = '\u000b';
                break;
            case 'u':
            case 'U':
            case 'x':
                ch = ScanUnicodeEscapeSequence(ch);
                break;
            default:
                throw new InvalidOperationException($"Invalid escape sequence '\\{ch}' at position {position}.");
        }

        return ch;
    }

    private char ScanUnicodeEscapeSequence(char ch)
    {
        var value = 0;

        var count = ch == 'U' ? 8 : 4;

        for (var i = 0; i < count; i++)
        {
            var digit = Peek();

            int digitValue;

            if (digit >= '0' && digit <= '9')
            {
                digitValue = digit - '0';
            }
            else if (digit >= 'a' && digit <= 'f')
            {
                digitValue = digit - 'a' + 10;
            }
            else if (digit >= 'A' && digit <= 'F')
            {
                digitValue = digit - 'A' + 10;
            }
            else if (ch != 'x')
            {
                throw new InvalidOperationException($"Invalid unicode escape sequence at position {position}.");
            }
            else
            {
                break;
            }

            value = (value << 4) + digitValue;

            Advance(1);
        }

        return (char)value;
    }

    private Token ScanCharacterLiteral()
    {
        Advance(1);

        var buffer = new StringBuilder();

        while (true)
        {
            var ch = Peek();

            switch (ch)
            {
                case '\0':
                    throw new InvalidOperationException($"Unexpected end of character literal at position {position}.");
                case '\\':
                    Advance(1);
                    buffer.Append(ScanEscapeSequence());
                    break;
                case '\'':
                    Advance(1);

                    return new Token(TokenType.CharacterLiteral, buffer.ToString())
                    {
                        ValueKind = ValueKind.Character
                    };
                default:
                    if (buffer.Length > 0)
                    {
                        throw new InvalidOperationException($"Too many characters in character literal at position {position}.");
                    }

                    buffer.Append(ch);
                    Advance(1);
                    break;
            }
        }
    }

    private Token ScanStringLiteral()
    {
        Advance(1);

        var buffer = new StringBuilder();

        while (true)
        {
            var ch = Peek();

            switch (ch)
            {
                case '\0':
                    throw new InvalidOperationException($"Unexpected end of string literal at position {position}.");
                case '\\':
                    Advance(1);
                    buffer.Append(ScanEscapeSequence());
                    break;
                case '"':
                    Advance(1);
                    return new Token(TokenType.StringLiteral, buffer.ToString())
                    {
                        ValueKind = ValueKind.String
                    };
                default:
                    buffer.Append(ch);
                    Advance(1);
                    break;
            }
        }
    }

    private Token ScanNumericLiteral()
    {
        var buffer = new StringBuilder();
        var hasDecimal = false;
        var hasFSuffix = false;
        var hasDSuffix = false;
        var hasMSuffix = false;
        var hasLSuffix = false;
        var hasExponent = false;
        var hasHex = false;
        var hasUSuffix = false;

        while (true)
        {
            var ch = Peek();

            if (ch == '0')
            {
                var next = Peek(1);

                if (next == 'x' || next == 'X')
                {
                    hasHex = true;
                    Advance(2);
                    continue;
                }
            }

            if (ch >= '0' && ch <= '9')
            {
                buffer.Append(ch);
                Advance(1);
                continue;
            }

            if (ch == '.')
            {
                if (hasDecimal)
                {
                    throw new InvalidOperationException($"Unexpected character '{ch}' at position {position}.");
                }

                hasDecimal = true;

                buffer.Append(ch);

                Advance(1);

                continue;
            }

            if (ch == 'l' || ch == 'L')
            {
                if (hasLSuffix)
                {
                    throw new InvalidOperationException($"Unexpected character '{ch}' at position {position}.");
                }

                hasLSuffix = true;

                Advance(1);

                continue;
            }

            if (ch == 'u' || ch == 'U')
            {
                if (hasUSuffix)
                {
                    throw new InvalidOperationException($"Unexpected character '{ch}' at position {position}.");
                }

                hasUSuffix = true;

                Advance(1);

                continue;
            }

            if (hasHex && ((ch >= 'a' && ch <= 'f') || (ch >= 'A' && ch <= 'F')))
            {
                buffer.Append(ch);
                Advance(1);
                continue;
            }

            if (ch == 'e' || ch == 'E')
            {
                if (hasExponent)
                {
                    throw new InvalidOperationException($"Unexpected character '{ch}' at position {position}.");
                }

                hasExponent = true;
                buffer.Append(ch);
                Advance(1);

                // Check for optional + or - after e/E
                ch = Peek();
                if (ch == '+' || ch == '-')
                {
                    buffer.Append(ch);
                    Advance(1);
                }

                // Must have at least one digit after e/E
                ch = Peek();

                if (ch < '0' || ch > '9')
                {
                    throw new InvalidOperationException($"Expected digit after exponent at position {position}.");
                }

                continue;
            }

            if (hasDecimal || hasExponent)
            {
                switch (ch)
                {
                    case 'F':
                    case 'f':
                        hasFSuffix = true;
                        Advance(1);
                        break;
                    case 'D':
                    case 'd':
                        hasDSuffix = true;
                        Advance(1);
                        break;
                    case 'M':
                    case 'm':
                        hasMSuffix = true;
                        Advance(1);
                        break;
                }
            }

            break;
        }

        var value = new Token(TokenType.NumericLiteral);

        var valueKind = ValueKind.None;

        if (hasDecimal || hasExponent)
        {
            valueKind = ValueKind.Double;
        }

        if (hasFSuffix)
        {
            valueKind = ValueKind.Float;
        }

        if (hasDSuffix)
        {
            valueKind = ValueKind.Double;
        }

        if (hasMSuffix)
        {
            valueKind = ValueKind.Decimal;
        }

        switch (valueKind)
        {
            case ValueKind.Float:
                value.ValueKind = ValueKind.Float;
                value.FloatValue = GetValueFloat(buffer.ToString());
                break;
            case ValueKind.Double:
                value.ValueKind = ValueKind.Double;
                value.DoubleValue = GetValueDouble(buffer.ToString());
                break;
            case ValueKind.Decimal:
                value.ValueKind = ValueKind.Decimal;
                value.DecimalValue = GetValueDecimal(buffer.ToString());
                break;
            default:
                var val = GetValueUInt64(buffer.ToString(), hasHex);

                if (!hasUSuffix && !hasLSuffix)
                {
                    if (val <= Int32.MaxValue)
                    {
                        value.ValueKind = ValueKind.Int;
                        value.IntValue = (int)val;
                    }
                    else if (val <= UInt32.MaxValue)
                    {
                        value.ValueKind = ValueKind.UInt;
                        value.UintValue = (uint)val;
                    }
                    else if (val <= Int64.MaxValue)
                    {
                        value.ValueKind = ValueKind.Long;
                        value.LongValue = (long)val;
                    }
                    else
                    {
                        value.ValueKind = ValueKind.ULong;
                        value.UlongValue = val;
                    }
                }
                else if (hasUSuffix && !hasLSuffix)
                {
                    if (val <= UInt32.MaxValue)
                    {
                        value.ValueKind = ValueKind.UInt;
                        value.UintValue = (uint)val;
                    }
                    else
                    {
                        value.ValueKind = ValueKind.ULong;
                        value.UlongValue = val;
                    }
                }
                else if (!hasUSuffix & hasLSuffix)
                {
                    if (val <= Int64.MaxValue)
                    {
                        value.ValueKind = ValueKind.Long;
                        value.LongValue = (long)val;
                    }
                    else
                    {
                        value.ValueKind = ValueKind.ULong;
                        value.UlongValue = val;
                    }
                }
                else
                {
                    value.ValueKind = ValueKind.ULong;
                    value.UlongValue = val;
                }
                break;
        }

        return value;
    }

    private static decimal GetValueDecimal(string text)
    {
        if (!decimal.TryParse(text, NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent, CultureInfo.InvariantCulture, out var result))
        {
            throw new InvalidOperationException($"Invalid numeric literal: {text}");
        }

        return result;
    }

    private static float GetValueFloat(string text)
    {
        if (!float.TryParse(text, NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent, CultureInfo.InvariantCulture, out var result))
        {
            throw new InvalidOperationException($"Invalid numeric literal: {text}");
        }

        return result;
    }

    private static double GetValueDouble(string text)
    {
        if (!double.TryParse(text, NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent, CultureInfo.InvariantCulture, out var result))
        {
            throw new InvalidOperationException($"Invalid numeric literal: {text}");
        }

        return result;
    }

    private static ulong GetValueUInt64(string text, bool isHex)
    {
        if (!UInt64.TryParse(text, isHex ? NumberStyles.AllowHexSpecifier : NumberStyles.None, CultureInfo.InvariantCulture, out var result))
        {
            throw new InvalidOperationException($"Invalid numeric literal: {text}");
        }

        return result;
    }

    private Token ScanIdentifier()
    {
        var startOffset = position;

        while (true)
        {
            if (position == expression.Length)
            {
                var length = position - startOffset;

                return new Token(TokenType.Identifier, expression.Substring(startOffset, length));
            }

            switch (Peek())
            {
                case '\0':
                case ' ':
                case '\r':
                case '\n':
                case '\t':
                case '!':
                case '%':
                case '(':
                case ')':
                case '*':
                case '+':
                case ',':
                case '-':
                case '.':
                case '/':
                case ':':
                case ';':
                case '<':
                case '=':
                case '>':
                case '?':
                case '[':
                case ']':
                case '^':
                case '{':
                case '|':
                case '}':
                case '~':
                case '"':
                case '\'':
                    // All of the following characters are not valid in an 
                    // identifier.  If we see any of them, then we know we're
                    // done.
                    return new Token(TokenType.Identifier, expression[startOffset..position]);
                case >= '0' and <= '9':
                    if (position == startOffset)
                    {
                        break;
                    }
                    else
                    {
                        goto case '_';
                    }
                case (>= 'a' and <= 'z') or (>= 'A' and <= 'Z'):
                case '_':
                    // All of these characters are valid inside an identifier.
                    // consume it and keep processing.
                    Advance(1);
                    continue;

                default:
                    throw new InvalidOperationException($"Unexpected character '{Peek()}' at position {position}.");
            }
        }
    }
}
