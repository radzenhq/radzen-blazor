using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Radzen.Blazor.Spreadsheet;

/// <summary>
/// Base class for formula functions that provides common functionality for expression evaluation.
/// </summary>
public abstract class FormulaFunction
{
    /// <summary>
    /// The current error state of the function evaluation.
    /// </summary>
    protected CellError? error;

    /// <summary>
    /// Gets the current error state.
    /// </summary>
    public CellError? Error => error;

    /// <summary>
    /// Gets a value indicating whether this function can handle error arguments.
    /// Functions that return true will receive error expressions as arguments instead of having evaluation short-circuited.
    /// </summary>
    public virtual bool CanHandleErrors => false;

    /// <summary>
    /// Evaluates the function with the given arguments.
    /// </summary>
    /// <param name="arguments">The function arguments as expressions.</param>
    /// <returns>The result expression.</returns>
    public abstract Expression Evaluate(List<Expression> arguments);

    /// <summary>
    /// Converts an expression to the target type if needed.
    /// </summary>
    protected static Expression ConvertIfNeeded(Expression expression, Type targetType)
    {
        if (expression is not LambdaExpression)
        {
            return expression.Type == targetType ? expression : Expression.Convert(expression, targetType);
        }

        return expression;
    }

    /// <summary>
    /// Checks if a type is numeric.
    /// </summary>
    protected static bool IsNumericType(Type type)
    {
        return type == typeof(int) || type == typeof(uint) ||
               type == typeof(long) || type == typeof(ulong) ||
               type == typeof(float) || type == typeof(double) ||
               type == typeof(short) || type == typeof(ushort) ||
               type == typeof(decimal);
    }

    /// <summary>
    /// Checks if an expression represents a null value.
    /// </summary>
    protected static bool IsNullValue(Expression expr)
    {
        if (expr is ConstantExpression constantExpr)
        {
            return constantExpr.Value == null;
        }
        return false;
    }

    /// <summary>
    /// Tries to extract a cell error from an expression.
    /// </summary>
    protected static bool TryGetError(Expression expr, out CellError? error)
    {
        if (expr is ConstantExpression constantExpr && constantExpr.Value is CellError cellError)
        {
            error = cellError;
            return true;
        }

        error = null;
        return false;
    }

    /// <summary>
    /// Gets the result type for binary operations between two types.
    /// </summary>
    protected static Type GetResultType(Type left, Type right)
    {
        if (left == typeof(double) || right == typeof(double))
        {
            return typeof(double);
        }

        if (left == typeof(float) || right == typeof(float))
        {
            return typeof(float);
        }

        if (left == typeof(decimal) || right == typeof(decimal))
        {
            return typeof(decimal);
        }

        if (left == typeof(ulong) || right == typeof(ulong))
        {
            return typeof(ulong);
        }

        if (left == typeof(long) || right == typeof(long))
        {
            return typeof(long);
        }

        return left == typeof(uint) || right == typeof(uint) ? typeof(uint) : typeof(int);
    }

    /// <summary>
    /// Converts an expression to a boolean expression following Excel semantics.
    /// </summary>
    protected Expression ConvertToBooleanExpression(Expression condition)
    {
        // Handle different types and convert to boolean following Excel semantics
        if (condition.Type == typeof(bool))
        {
            return condition;
        }

        if (condition.Type == typeof(double))
        {
            return Expression.NotEqual(condition, Expression.Constant(0.0));
        }

        if (condition.Type == typeof(int))
        {
            return Expression.NotEqual(condition, Expression.Constant(0));
        }

        if (condition.Type == typeof(string))
        {
            return Expression.NotEqual(
                Expression.Call(typeof(string), nameof(string.IsNullOrEmpty), null, condition),
                Expression.Constant(true)
            );
        }

        // For other numeric types, convert to double first
        if (IsNumericType(condition.Type))
        {
            return Expression.NotEqual(ConvertIfNeeded(condition, typeof(double)), Expression.Constant(0.0));
        }

        // For null values, return false
        if (IsNullValue(condition))
        {
            return Expression.Constant(false);
        }

        return Expression.NotEqual(Expression.Convert(condition, typeof(double)), Expression.Constant(0.0));
    }

    /// <summary>
    /// Converts an expression to a string expression.
    /// </summary>
    protected Expression ConvertToStringExpression(Expression expression)
    {
        if (expression.Type == typeof(string))
        {
            return expression;
        }

        if (expression.Type == typeof(bool))
        {
            return Expression.Condition(
                expression,
                Expression.Constant("True"),
                Expression.Constant("False")
            );
        }

        // For other types, use ToString()
        return Expression.Call(expression, "ToString", null);
    }

    /// <summary>
    /// Ensures two expressions have compatible types for conditional operations.
    /// </summary>
    protected (Expression trueValue, Expression falseValue) EnsureCompatibleTypes(Expression trueValue, Expression falseValue)
    {
        // If types are already compatible, return as-is
        if (trueValue.Type == falseValue.Type)
        {
            return (trueValue, falseValue);
        }

        // If one is a string, convert both to string
        // But if falseValue is the default boolean false, keep it as boolean
        if ((trueValue.Type == typeof(string) || falseValue.Type == typeof(string)) &&
            !(falseValue is ConstantExpression constantFalse && constantFalse.Value is bool boolVal && !boolVal))
        {
            var stringTrueValue = ConvertToStringExpression(trueValue);
            var stringFalseValue = ConvertToStringExpression(falseValue);
            return (stringTrueValue, stringFalseValue);
        }

        // If one is boolean and the other is not string, convert the non-boolean to boolean
        if ((trueValue.Type == typeof(bool) && falseValue.Type != typeof(string)) ||
            (falseValue.Type == typeof(bool) && trueValue.Type != typeof(string)))
        {
            var boolTrueValue = ConvertToBooleanExpression(trueValue);
            var boolFalseValue = ConvertToBooleanExpression(falseValue);
            return (boolTrueValue, boolFalseValue);
        }

        // If both are numeric, convert to the common numeric type
        if (IsNumericType(trueValue.Type) && IsNumericType(falseValue.Type))
        {
            var commonType = GetResultType(trueValue.Type, falseValue.Type);
            var convertedTrueValue = ConvertIfNeeded(trueValue, commonType);
            var convertedFalseValue = ConvertIfNeeded(falseValue, commonType);
            return (convertedTrueValue, convertedFalseValue);
        }

        // Default: convert both to object
        var objectTrueValue = ConvertIfNeeded(trueValue, typeof(object));
        var objectFalseValue = ConvertIfNeeded(falseValue, typeof(object));
        return (objectTrueValue, objectFalseValue);
    }
}
