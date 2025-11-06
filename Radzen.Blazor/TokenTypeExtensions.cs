using System;
using System.Linq.Expressions;

namespace Radzen;

/// <summary>
/// Extension methods for <see cref="TokenType"/>.
/// </summary>
internal static class TokenTypeExtensions
{
    /// <summary>
    /// Converts a token type to an expression type.
    /// </summary>
    /// <param name="tokenType">The token type to convert.</param>
    /// <returns>The corresponding expression type.</returns>
    public static ExpressionType ToExpressionType(this TokenType tokenType)
    {
        return tokenType switch
        {
            TokenType.EqualsEquals => ExpressionType.Equal,
            TokenType.NotEquals => ExpressionType.NotEqual,
            TokenType.EqualsGreaterThan => ExpressionType.GreaterThanOrEqual,
            TokenType.AmpersandAmpersand => ExpressionType.AndAlso,
            TokenType.Ampersand => ExpressionType.And,
            TokenType.BarBar => ExpressionType.OrElse,
            TokenType.Bar => ExpressionType.Or,
            TokenType.GreaterThan => ExpressionType.GreaterThan,
            TokenType.LessThan => ExpressionType.LessThan,
            TokenType.LessThanOrEqual => ExpressionType.LessThanOrEqual,
            TokenType.GreaterThanOrEqual => ExpressionType.GreaterThanOrEqual,
            TokenType.Plus => ExpressionType.Add,
            TokenType.Minus => ExpressionType.Subtract,
            TokenType.Star => ExpressionType.Multiply,
            TokenType.Slash => ExpressionType.Divide,
            TokenType.Caret => ExpressionType.ExclusiveOr,
            TokenType.GreaterThanGreaterThan => ExpressionType.RightShift,
            TokenType.LessThanLessThan => ExpressionType.LeftShift,
            _ => throw new InvalidOperationException($"Unsupported token type: {tokenType}")
        };
    }
}

