using System;
using System.Linq.Expressions;

namespace Radzen;

/// <summary>
/// Represents a token in an expression.
/// </summary>
internal class Token
{
    /// <summary>
    /// Gets or sets the type of the token.
    /// </summary>
    public TokenType Type { get; set; }

    /// <summary>
    /// Gets or sets the value of the token.
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the value kind of the token.
    /// </summary>
    public ValueKind ValueKind { get; set; } = ValueKind.None;

    /// <summary>
    /// Gets or sets the integer value of the token.
    /// </summary>
    public int IntValue { get; internal set; }

    /// <summary>
    /// Gets or sets the unsigned integer value of the token.
    /// </summary>
    public uint UintValue { get; internal set; }

    /// <summary>
    /// Gets or sets the long value of the token.
    /// </summary>
    public long LongValue { get; internal set; }

    /// <summary>
    /// Gets or sets the unsigned long value of the token.
    /// </summary>
    public ulong UlongValue { get; internal set; }

    /// <summary>
    /// Gets or sets the decimal value of the token.
    /// </summary>
    public decimal DecimalValue { get; internal set; }

    /// <summary>
    /// Gets or sets the float value of the token.
    /// </summary>
    public float FloatValue { get; internal set; }

    /// <summary>
    /// Gets or sets the double value of the token.
    /// </summary>
    public double DoubleValue { get; internal set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Token"/> class.
    /// </summary>
    /// <param name="type">The type of the token.</param>
    public Token(TokenType type)
    {
        Type = type;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Token"/> class.
    /// </summary>
    /// <param name="type">The type of the token.</param>
    /// <param name="value">The value of the token.</param>
    public Token(TokenType type, string value)
    {
        Type = type;
        Value = value;
    }

    /// <summary>
    /// Converts the token to a constant expression.
    /// </summary>
    /// <returns>A constant expression representing the token value.</returns>
    public ConstantExpression ToConstantExpression()
    {
        return ValueKind switch
        {
            ValueKind.Null => Expression.Constant(null),
            ValueKind.String => Expression.Constant(Value),
            ValueKind.Character => Expression.Constant(Value[0]),
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

