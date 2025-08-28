using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

/// <summary>
/// Serializes LINQ Expression Trees into C# string representations.
/// </summary>
public class ExpressionSerializer : ExpressionVisitor
{
    private readonly StringBuilder _sb = new StringBuilder();

    /// <summary>
    /// Serializes a given LINQ Expression into a C# string.
    /// </summary>
    /// <param name="expression">The expression to serialize.</param>
    /// <returns>A string representation of the expression.</returns>
    public string Serialize(Expression expression)
    {
        _sb.Clear();
        Visit(expression);
        return _sb.ToString();
    }

    /// <inheritdoc/>
    protected override Expression VisitLambda<T>(Expression<T> node)
    {
        if (node.Parameters.Count > 1)
        {
            _sb.Append("(");
            for (int i = 0; i < node.Parameters.Count; i++)
            {
                if (i > 0) _sb.Append(", ");
                _sb.Append(node.Parameters[i].Name);
            }
            _sb.Append(") => ");
        }
        else
        {
            _sb.Append(node.Parameters[0].Name);
            _sb.Append(" => ");
        }
        Visit(node.Body);
        return node;
    }

    /// <inheritdoc/>
    protected override Expression VisitParameter(ParameterExpression node)
    {
        _sb.Append(node.Name);
        return node;
    }

    /// <inheritdoc/>
    protected override Expression VisitMember(MemberExpression node)
    {
        if (node.Expression != null)
        {
            Visit(node.Expression);
            _sb.Append($".{node.Member.Name}");
        }
        else
        {
            _sb.Append(node.Member.Name);
        }
        return node;
    }

    /// <inheritdoc/>
    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (node.Method.IsStatic && node.Arguments.Count > 0 &&
            (node.Method.DeclaringType == typeof(Enumerable) || 
                node.Method.DeclaringType == typeof(Queryable)))
        {
            Visit(node.Arguments[0]);
            _sb.Append($".{node.Method.Name}(");

            for (int i = 1; i < node.Arguments.Count; i++) 
            {
                if (i > 1) _sb.Append(", ");

                if (node.Arguments[i] is NewArrayExpression arrayExpr)
                {
                    VisitNewArray(arrayExpr);
                }
                else
                {
                    Visit(node.Arguments[i]);
                }
            }

            _sb.Append(")");
        }
        else if (node.Method.IsStatic)
        {
            _sb.Append($"{node.Method.DeclaringType.Name}.{node.Method.Name}(");

            for (int i = 0; i < node.Arguments.Count; i++)
            {
                if (i > 0) _sb.Append(", ");
                Visit(node.Arguments[i]);
            }

            _sb.Append(")");
        }
        else
        {
            if (node.Object != null)
            {
                Visit(node.Object);
                _sb.Append($".{node.Method.Name}(");
            }
            else
            {
                _sb.Append($"{node.Method.Name}(");
            }

            for (int i = 0; i < node.Arguments.Count; i++)
            {
                if (i > 0) _sb.Append(", ");
                Visit(node.Arguments[i]);
            }

            _sb.Append(")");
        }

        return node;
    }

    /// <inheritdoc/>
    protected override Expression VisitUnary(UnaryExpression node)
    {
        if (node.NodeType == ExpressionType.Not)
        {
            _sb.Append("(!(");
            Visit(node.Operand);
            _sb.Append("))");
        }
        else if (node.NodeType == ExpressionType.Convert) 
        {
            if (node.Operand is IndexExpression indexExpr)
            {
                _sb.Append($"({node.Type.DisplayName(true).Replace("+",".")})");

                Visit(indexExpr.Object);

                _sb.Append("[");
                Visit(indexExpr.Arguments[0]);
                _sb.Append("]");

                return node;
            }

            Visit(node.Operand);
        }
        else
        {
            _sb.Append(node.NodeType switch
            {
                ExpressionType.Negate => "-",
                ExpressionType.UnaryPlus => "+",
                _ => throw new NotSupportedException($"Unsupported unary operator: {node.NodeType}")
            });
            Visit(node.Operand);
        }
        return node;
    }

    /// <inheritdoc/>
    protected override Expression VisitConstant(ConstantExpression node)
    {
        _sb.Append(FormatValue(node.Value));
        return node;
    }

    internal static string FormatValue(object value)
    {
        if (value == null)
            return "null";

        return value switch
        {
            string s when s == string.Empty => @"""""",
            null => "null",
            string s => @$"""{s.Replace("\"", "\\\"")}""",
            char c => $"'{c}'",
            bool b => b.ToString().ToLowerInvariant(),
            DateTime dt => FormatDateTime(dt),
            DateTimeOffset dto => $"DateTime.Parse(\"{dto.UtcDateTime:yyyy-MM-ddTHH:mm:ss.fffZ}\")",
            DateOnly dateOnly => $"DateOnly.Parse(\"{dateOnly.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}\", CultureInfo.InvariantCulture)",
            TimeOnly timeOnly => $"TimeOnly.Parse(\"{timeOnly.ToString("HH:mm:ss", CultureInfo.InvariantCulture)}\", CultureInfo.InvariantCulture)",
            Guid guid => $"Guid.Parse(\"{guid.ToString("D", CultureInfo.InvariantCulture)}\")",
            IEnumerable enumerable when value is not string => FormatEnumerable(enumerable),
            _ => value.GetType().IsEnum
                ? $"({value.GetType().FullName.Replace("+", ".")})" + Convert.ChangeType(value, Enum.GetUnderlyingType(value.GetType()), CultureInfo.InvariantCulture).ToString()
                : Convert.ToString(value, CultureInfo.InvariantCulture)
        };
    }

    private static string FormatDateTime(DateTime dateTime)
    {
        var finalDate = dateTime.TimeOfDay == TimeSpan.Zero ? dateTime.Date : dateTime;
        var dateFormat = dateTime.TimeOfDay == TimeSpan.Zero ? "yyyy-MM-dd" : "yyyy-MM-ddTHH:mm:ss.fffZ";

        return $"DateTime.SpecifyKind(DateTime.Parse(\"{finalDate.ToString(dateFormat, CultureInfo.InvariantCulture)}\", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind), DateTimeKind.{Enum.GetName(finalDate.Kind)})";
    }

    private static string FormatEnumerable(IEnumerable enumerable)
    {
        var arrayType = enumerable.AsQueryable().ElementType;
        
        var items = enumerable.Cast<object>().Select(FormatValue);
        return $"new {(Nullable.GetUnderlyingType(arrayType) != null ? arrayType.DisplayName(true).Replace("+", ".") : "")}[] {{ {string.Join(", ", items)} }}";
    }

    /// <inheritdoc/>
    protected override Expression VisitNewArray(NewArrayExpression node)
    {
        bool needsParentheses = node.NodeType == ExpressionType.NewArrayInit &&
                                (node.Expressions.Count > 1 || node.Expressions[0].NodeType != ExpressionType.Constant);

        if (needsParentheses) _sb.Append("(");

        _sb.Append("new [] { ");
        bool first = true;
        foreach (var expr in node.Expressions)
        {
            if (!first) _sb.Append(", ");
            first = false;
            Visit(expr);
        }
        _sb.Append(" }");

        if (needsParentheses) _sb.Append(")");

        return node;
    }

    /// <inheritdoc/>
    protected override Expression VisitBinary(BinaryExpression node)
    {
        _sb.Append("(");
        Visit(node.Left);
        _sb.Append($" {GetOperator(node.NodeType)} ");
        Visit(node.Right);
        _sb.Append(")");
        return node;
    }

    /// <inheritdoc/>
    protected override Expression VisitConditional(ConditionalExpression node)
    {
        _sb.Append("(");
        Visit(node.Test);
        _sb.Append(" ? ");
        Visit(node.IfTrue);
        _sb.Append(" : ");
        Visit(node.IfFalse);
        _sb.Append(")");
        return node;
    }

    /// <summary>
    /// Maps an ExpressionType to its corresponding C# operator.
    /// </summary>
    /// <param name="type">The ExpressionType to map.</param>
    /// <returns>A string representation of the corresponding C# operator.</returns>
    private static string GetOperator(ExpressionType type)
    {
        return type switch
        {
            ExpressionType.Add => "+",
            ExpressionType.Subtract => "-",
            ExpressionType.Multiply => "*",
            ExpressionType.Divide => "/",
            ExpressionType.AndAlso => "&&",
            ExpressionType.OrElse => "||",
            ExpressionType.Equal => "==",
            ExpressionType.NotEqual => "!=",
            ExpressionType.LessThan => "<",
            ExpressionType.LessThanOrEqual => "<=",
            ExpressionType.GreaterThan => ">",
            ExpressionType.GreaterThanOrEqual => ">=",
            ExpressionType.Coalesce => "??",
            _ => throw new NotSupportedException($"Unsupported operator: {type}")
        };
    }  
}

/// <summary>
/// Provides an extension method for displaying type names.
/// </summary>
public static class SharedTypeExtensions
{
    private static readonly Dictionary<Type, string> BuiltInTypeNames = new()
    {
        { typeof(bool), "bool" },
        { typeof(byte), "byte" },
        { typeof(char), "char" },
        { typeof(decimal), "decimal" },
        { typeof(double), "double" },
        { typeof(float), "float" },
        { typeof(int), "int" },
        { typeof(long), "long" },
        { typeof(object), "object" },
        { typeof(sbyte), "sbyte" },
        { typeof(short), "short" },
        { typeof(string), "string" },
        { typeof(uint), "uint" },
        { typeof(ulong), "ulong" },
        { typeof(ushort), "ushort" },
        { typeof(void), "void" }
    };

    /// <summary>
    /// Unwraps nullable type.
    /// </summary>
    public static Type UnwrapNullableType(this Type type)
        => Nullable.GetUnderlyingType(type) ?? type;

    /// <summary>
    /// Returns a display name for the given type.
    /// </summary>
    /// <param name="type">The type to display.</param>
    /// <param name="fullName">Indicates whether to use the full name.</param>
    /// <param name="compilable">Indicates whether to use a compilable format.</param>
    /// <returns>A string representing the type name.</returns>
    public static string DisplayName(this Type type, bool fullName = true, bool compilable = false)
    {
        var stringBuilder = new StringBuilder();
        ProcessType(stringBuilder, type, fullName, compilable);
        return stringBuilder.ToString();
    }

    private static void ProcessType(StringBuilder builder, Type type, bool fullName, bool compilable)
    {
        if (type.IsGenericType)
        {
            var genericArguments = type.GetGenericArguments();
            ProcessGenericType(builder, type, genericArguments, genericArguments.Length, fullName, compilable);
        }
        else if (type.IsArray)
        {
            ProcessArrayType(builder, type, fullName, compilable);
        }
        else if (BuiltInTypeNames.TryGetValue(type, out var builtInName))
        {
            builder.Append(builtInName);
        }
        else if (!type.IsGenericParameter)
        {
            if (compilable)
            {
                if (type.IsNested)
                {
                    ProcessType(builder, type.DeclaringType!, fullName, compilable);
                    builder.Append('.');
                }
                else if (fullName)
                {
                    builder.Append(type.Namespace).Append('.');
                }

                builder.Append(type.Name);
            }
            else
            {
                builder.Append(fullName ? type.FullName : type.Name);
            }
        }
    }

    private static void ProcessArrayType(StringBuilder builder, Type type, bool fullName, bool compilable)
    {
        var innerType = type;
        while (innerType.IsArray)
        {
            innerType = innerType.GetElementType()!;
        }

        ProcessType(builder, innerType, fullName, compilable);

        while (type.IsArray)
        {
            builder.Append('[');
            builder.Append(',', type.GetArrayRank() - 1);
            builder.Append(']');
            type = type.GetElementType()!;
        }
    }

    private static void ProcessGenericType(
        StringBuilder builder,
        Type type,
        Type[] genericArguments,
        int length,
        bool fullName,
        bool compilable)
    {
        if (type.IsConstructedGenericType
            && type.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            ProcessType(builder, type.UnwrapNullableType(), fullName, compilable);
            builder.Append('?');
            return;
        }

        var offset = type.IsNested ? type.DeclaringType!.GetGenericArguments().Length : 0;

        if (compilable)
        {
            if (type.IsNested)
            {
                ProcessType(builder, type.DeclaringType!, fullName, compilable);
                builder.Append('.');
            }
            else if (fullName)
            {
                builder.Append(type.Namespace);
                builder.Append('.');
            }
        }
        else
        {
            if (fullName)
            {
                if (type.IsNested)
                {
                    ProcessGenericType(builder, type.DeclaringType!, genericArguments, offset, fullName, compilable);
                    builder.Append('+');
                }
                else
                {
                    builder.Append(type.Namespace);
                    builder.Append('.');
                }
            }
        }

        var genericPartIndex = type.Name.IndexOf('`');
        if (genericPartIndex <= 0)
        {
            builder.Append(type.Name);
            return;
        }

        builder.Append(type.Name, 0, genericPartIndex);
        builder.Append('<');

        for (var i = offset; i < length; i++)
        {
            ProcessType(builder, genericArguments[i], fullName, compilable);
            if (i + 1 == length)
            {
                continue;
            }

            builder.Append(',');
            if (!genericArguments[i + 1].IsGenericParameter)
            {
                builder.Append(' ');
            }
        }

        builder.Append('>');
    }
}