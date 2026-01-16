using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace Radzen.Blazor.Spreadsheet;

#nullable enable

internal enum ValueKind
{
    None,
    String,
    Int,
    Float,
    Double,
    Decimal,
    Character,
    Null,
    True,
    False,
    Long,
    UInt,
    ULong,
}

internal enum FormulaTokenType
{
    None,
    Unknown,
    Identifier,
    Equals,
    Plus,
    Minus,
    Star,
    Slash,
    EqualsGreaterThan,
    Comma,
    CloseParen,
    OpenParen,
    Dot,
    LessThan,
    LessThanOrEqual,
    GreaterThan,
    GreaterThanOrEqual,
    Colon,
    NumericLiteral,
    StringLiteral,
    BooleanLiteral,
    CellIdentifier,
    ErrorLiteral,
    Whitespace,
}

internal enum FormulaTokenTriviaKind
{
    None,
    Whitespace,
    EndOfLine,
}

internal class FormulaTokenTrivia(FormulaTokenTriviaKind kind, string text)
{
    public FormulaTokenTriviaKind Kind { get; } = kind;
    public string Text { get; } = text;
}

internal class FormulaToken(FormulaTokenType type, string  value)
{
    public FormulaTokenType Type { get; } = type;

    public ValueKind ValueKind { get; set; } = ValueKind.None;

    public string Value { get; set; } = value;
    public float FloatValue { get; set; }
    public double DoubleValue { get; set; }
    public decimal DecimalValue { get; set; }
    public int IntValue { get; set; }
    public uint UintValue { get; set; }
    public long LongValue { get; set; }
    public ulong UlongValue { get; set; }
    public CellRef Address { get; set; }
    public CellError ErrorValue { get; set; }


    public List<FormulaTokenTrivia> LeadingTrivia { get; } = new();
    public List<FormulaTokenTrivia> TrailingTrivia { get; } = new();

    // The inclusive start index and exclusive end index of the token
    // including its leading and trailing trivia within the original expression.
    public int Start { get; set; }
    public int End { get; set; }

    public ConstantExpression ToConstantExpression()
    {
        return ValueKind switch
        {
            ValueKind.Null => Expression.Constant(null),
            ValueKind.String => Expression.Constant(Value),
            ValueKind.Int => Expression.Constant(IntValue),
            ValueKind.UInt => Expression.Constant(UintValue),
            ValueKind.Long => Expression.Constant(LongValue),
            ValueKind.ULong => Expression.Constant(UlongValue),
            ValueKind.Float => Expression.Constant(FloatValue),
            ValueKind.Double => Expression.Constant(DoubleValue),
            ValueKind.Decimal => Expression.Constant(DecimalValue),
            ValueKind.True => Expression.Constant(true),
            ValueKind.False => Expression.Constant(false),
            _ => throw new InvalidOperationException($"Unsupported value kind: {ValueKind}")
        };
    }
}

internal class FormulaLexer(string expression, bool strict = true)
{
    private int position;

    public static List<FormulaToken> Scan(string expression, bool strict = true)
    {
        var lexer = new FormulaLexer(expression, strict);

        return [.. lexer.Scan()];
    }

    public IEnumerable<FormulaToken> Scan()
    {
        while (position < expression.Length)
        {
            // Capture leading trivia (whitespace before the token)
            var tokenStart = position;
            var leadingTrivia = ScanTrivia();

            var token = ScanToken();

            if (token.Type == FormulaTokenType.None)
            {
                // If we have leading trivia but no token, create a whitespace token
                if (leadingTrivia.Count > 0)
                {
                    var whitespaceText = string.Join("", leadingTrivia.Select(t => t.Text));
                    var whitespaceToken = new FormulaToken(FormulaTokenType.Whitespace, whitespaceText);
                    whitespaceToken.LeadingTrivia.AddRange(leadingTrivia);
                    whitespaceToken.Start = tokenStart;
                    whitespaceToken.End = position;
                    yield return whitespaceToken;
                }
                yield break;
            }

            // Add leading trivia to the token
            token.LeadingTrivia.AddRange(leadingTrivia);

            // Capture trailing trivia (whitespace after the token)
            var trailingTrivia = ScanTrivia();
            token.TrailingTrivia.AddRange(trailingTrivia);

            // Set token span including leading and trailing trivia
            token.Start = tokenStart;
            token.End = position;

            yield return token;
        }

        // End token to mark completion
        var endToken = new FormulaToken(FormulaTokenType.None, string.Empty)
        {
            Start = position,
            End = position
        };
        yield return endToken;
    }

    private List<FormulaTokenTrivia> ScanTrivia()
    {
        var trivia = new List<FormulaTokenTrivia>();
        var whitespaceBuffer = new StringBuilder();

        while (char.IsWhiteSpace(Peek()))
        {
            var ch = Peek();

            if (ch == '\r' && Peek(1) == '\n')
            {
                // Windows line ending - flush any accumulated whitespace first
                if (whitespaceBuffer.Length > 0)
                {
                    trivia.Add(new FormulaTokenTrivia(FormulaTokenTriviaKind.Whitespace, whitespaceBuffer.ToString()));
                    whitespaceBuffer.Clear();
                }
                Advance(2);
                trivia.Add(new FormulaTokenTrivia(FormulaTokenTriviaKind.EndOfLine, "\r\n"));
            }
            else if (ch == '\n')
            {
                // Unix line ending - flush any accumulated whitespace first
                if (whitespaceBuffer.Length > 0)
                {
                    trivia.Add(new FormulaTokenTrivia(FormulaTokenTriviaKind.Whitespace, whitespaceBuffer.ToString()));
                    whitespaceBuffer.Clear();
                }
                Advance(1);
                trivia.Add(new FormulaTokenTrivia(FormulaTokenTriviaKind.EndOfLine, "\n"));
            }
            else
            {
                // Regular whitespace - accumulate
                whitespaceBuffer.Append(ch);
                Advance(1);
            }
        }

        // Flush any remaining whitespace
        if (whitespaceBuffer.Length > 0)
        {
            trivia.Add(new FormulaTokenTrivia(FormulaTokenTriviaKind.Whitespace, whitespaceBuffer.ToString()));
        }

        return trivia;
    }

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

    bool TryAdvance(char expected)
    {
        if (Peek(1) == expected)
        {
            Advance(1);
            return true;
        }

        return false;
    }

    private FormulaToken ScanToken()
    {
        var ch = Peek();

        switch (ch)
        {
            case '"':
                return ScanStringLiteral();
            case '#':
                return ScanErrorLiteral();
            case '=':

                if (TryAdvance('>'))
                {
                    Advance(1);
                    return new FormulaToken(FormulaTokenType.EqualsGreaterThan, ">=");
                }

                Advance(1);
                return new FormulaToken(FormulaTokenType.Equals, "=");
            case '>':
                if (TryAdvance('='))
                {
                    Advance(1);
                    return new FormulaToken(FormulaTokenType.GreaterThanOrEqual, ">=");
                }
                Advance(1);
                return new FormulaToken(FormulaTokenType.GreaterThan, ">");
            case '<':
                if (TryAdvance('='))
                {
                    Advance(1);
                    return new FormulaToken(FormulaTokenType.LessThanOrEqual, "<=");
                }
                Advance(1);
                return new FormulaToken(FormulaTokenType.LessThan, "<");
            case '+':
                Advance(1);
                return new FormulaToken(FormulaTokenType.Plus, "+");
            case '-':
                Advance(1);
                return new FormulaToken(FormulaTokenType.Minus, "-");
            case '*':
                Advance(1);
                return new FormulaToken(FormulaTokenType.Star, "*");
            case '/':
                Advance(1);
                return new FormulaToken(FormulaTokenType.Slash, "/");
            case '.':
                Advance(1);
                return new FormulaToken(FormulaTokenType.Dot, ".");
            case '(':
                Advance(1);
                return new FormulaToken(FormulaTokenType.OpenParen, "(");
            case ')':
                Advance(1);
                return new FormulaToken(FormulaTokenType.CloseParen, ")");
            case ',':
                Advance(1);
                return new FormulaToken(FormulaTokenType.Comma, ",");
            case ':':
                Advance(1);
                return new FormulaToken(FormulaTokenType.Colon, ":");
            case >= '0' and <= '9':
                return ScanNumericLiteral();
            case '_':
            case (>= 'a' and <= 'z') or (>= 'A' and <= 'Z'):
                return ScanIdentifier();
            case '$':
                // Absolute reference marker is part of identifiers like $A$1
                return ScanIdentifier();

        }

        // Emit unknown token for any unrecognized character to preserve it in the token stream
        var unknown = new FormulaToken(FormulaTokenType.Unknown, Peek().ToString());
        Advance(1);
        return unknown;
    }

    private FormulaToken ScanStringLiteral()
    {
        Advance(1);

        var buffer = new StringBuilder();

        while (true)
        {
            var ch = Peek();

            switch (ch)
            {
                case '\0':
                    if (strict)
                    {
                        throw new InvalidOperationException($"Unexpected end of string literal at position {position}.");
                    }
                    // In non-strict mode, return the unclosed string as is
                    return new FormulaToken(FormulaTokenType.StringLiteral, buffer.ToString());
                case '\\':
                    Advance(1);
                    buffer.Append(ScanEscapeSequence());
                    break;
                case '"':
                    Advance(1);
                    return new FormulaToken(FormulaTokenType.StringLiteral, buffer.ToString());
                default:
                    buffer.Append(ch);
                    Advance(1);
                    break;
            }
        }
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

    private FormulaToken ScanErrorLiteral()
    {
        // Support only #REF! for now
        const string RefLiteral = "#REF!";
        if (expression.AsSpan(position).StartsWith(RefLiteral, StringComparison.Ordinal))
        {
            position += RefLiteral.Length;
            return new FormulaToken(FormulaTokenType.ErrorLiteral, RefLiteral)
            {
                ErrorValue = CellError.Ref
            };
        }

        // Unknown error literal
        return new FormulaToken(FormulaTokenType.None, string.Empty);
    }

    private FormulaToken ScanNumericLiteral()
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

        var value = new FormulaToken(FormulaTokenType.NumericLiteral, string.Empty);

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

        value.Value = buffer.ToString();

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

    private FormulaToken ScanIdentifier()
    {
        var startOffset = position;
        var hasLetters = false;
        var hasNumbers = false;

        while (true)
        {
            if (position == expression.Length)
            {
                return CreateIdentifierToken(expression[startOffset..position], hasLetters, hasNumbers);
            }

            switch (Peek())
            {
                case '\0':
                case ' ':
                case '\r':
                case '\n':
                case '\t':
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
                    return CreateIdentifierToken(expression[startOffset..position], hasLetters, hasNumbers);
                case >= '0' and <= '9':
                    hasNumbers = true;
                    if (position == startOffset)
                    {
                        break;
                    }
                    else
                    {
                        goto case '_';
                    }
                case (>= 'a' and <= 'z') or (>= 'A' and <= 'Z'):
                    hasLetters = true;
                    Advance(1);
                    continue;
                case '_':
                    Advance(1);
                    continue;
                case '!':
                    Advance(1);
                    continue;
                case '$':
                    Advance(1);
                    continue;

                default:
                    throw new InvalidOperationException($"Unexpected character '{Peek()}' at position {position}.");
            }
        }
    }

    private static FormulaToken CreateIdentifierToken(string value, bool hasLetters, bool hasNumbers)
    {
        if (string.Equals(value, "TRUE", StringComparison.OrdinalIgnoreCase))
        {
            return new FormulaToken(FormulaTokenType.BooleanLiteral, value)
            {
                ValueKind = ValueKind.True
            };
        }

        if (string.Equals(value, "FALSE", StringComparison.OrdinalIgnoreCase))
        {
            return new FormulaToken(FormulaTokenType.BooleanLiteral, value)
            {
                ValueKind = ValueKind.False
            };
        }

        if (hasLetters && hasNumbers && CellRef.TryParse(value, out var cellIndex))
        {
            return new FormulaToken(FormulaTokenType.CellIdentifier, value)
            {
                Address = cellIndex,
            };
        }

        return new FormulaToken(FormulaTokenType.Identifier, value);
    }
}