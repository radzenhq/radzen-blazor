using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Radzen;

/// <summary>
/// Utility class that provides property access based on strings.
/// </summary>
public static class PropertyAccess
{
    /// <summary>
    /// Creates a function that will return the specified property.
    /// </summary>
    /// <typeparam name="TItem">The owner type.</typeparam>
    /// <typeparam name="TValue">The value type.</typeparam>
    /// <param name="propertyName">Name of the property to return.</param>
    /// <param name="type">Type of the object.</param>
    /// <returns>A function which return the specified property by its name.</returns>
    public static Func<TItem, TValue> Getter<TItem, TValue>(string propertyName, Type type = null)
    {
        if (propertyName.Contains("["))
        {
            var arg = Expression.Parameter(typeof(TItem));

            return Expression.Lambda<Func<TItem, TValue>>(QueryableExtension.GetNestedPropertyExpression(arg, propertyName, type), arg).Compile();
        }
        else
        {
            var arg = Expression.Parameter(typeof(TItem));

            Expression body = arg;

            if (type != null)
            {
                body = Expression.Convert(body, type);
            }

            foreach (var member in propertyName.Split("."))
            {
                if (body.Type.IsInterface)
                {
                    body = Expression.Property(body,
                        new[] { body.Type }.Concat(body.Type.GetInterfaces()).FirstOrDefault(t => t.GetProperty(member) != null),
                        member
                    );
                }
                else
                {
                    try
                    {
                        body = Expression.PropertyOrField(body, member);
                    }
                    catch (AmbiguousMatchException)
                    {
                        var property = body.Type.GetProperty(member, BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);

                        if (property != null)
                        {
                            body = Expression.Property(body, property);
                        }
                        else
                        {
                            var field = body.Type.GetField(member, BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);

                            if (field != null)
                            {
                                body = Expression.Field(body, field);
                            }
                        }
                    }
                }
            }

            body = Expression.Convert(body, typeof(TValue));

            return Expression.Lambda<Func<TItem, TValue>>(body, arg).Compile();
        }
    }

    /// <summary>
    /// Determines whether the specified type is a <see cref="DateTime" />.
    /// </summary>
    /// <param name="source">The source.</param>
    /// <returns><c>true</c> if the specified type is a DateTime instance or nullable DateTime; otherwise, <c>false</c>.</returns>
    public static bool IsDate(Type source)
    {
        if (source == null) return false;
        var type = source.IsGenericType ? source.GetGenericArguments()[0] : source;

        if (type == typeof(DateTime) || type == typeof(DateTimeOffset))
        {
            return true;
        }
#if NET6_0_OR_GREATER
        if (type == typeof(DateOnly))
        {
            return true;
        }
#endif
        return false;
    }

    /// <summary>
    /// Determines whether the specified type is a DateOnly.
    /// </summary>
    /// <param name="source">The source.</param>
    /// <returns><c>true</c> if the specified type is a DateOnly instance or nullable DateOnly; otherwise, <c>false</c>.</returns>
    public static bool IsDateOnly(Type source)
    {
        if (source == null) return false;
        var type = source.IsGenericType ? source.GetGenericArguments()[0] : source;

#if NET6_0_OR_GREATER
        if (type == typeof(DateOnly))
        {
            return true;
        }
#endif
        return false;
    }

    /// <summary>
    /// Converts a DateTime to DateOnly.
    /// </summary>
    /// <param name="source">The source DateTime.</param>
    /// <returns>DateOnly object or null.</returns>
    public static object DateOnlyFromDateTime(DateTime source)
    {
        object result = null;
#if NET6_0_OR_GREATER
        result = DateOnly.FromDateTime(source);
#endif
        return result;
    }

    /// <summary>
    /// Gets the type of the element of a collection time.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <returns>The type of the collection element.</returns>
    public static Type GetElementType(Type type)
    {
        if (type.IsArray)
        {
            return type.GetElementType();
        }

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
        {
            return type.GetGenericArguments()[0];
        }

        var enumType = type.GetInterfaces()
                                .Where(t => t.IsGenericType &&
                                       t.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                                .Select(t => t.GenericTypeArguments[0]).FirstOrDefault();
        return enumType ?? type;
    }

    /// <summary>
    /// Converts the property to a value that can be used by Dynamic LINQ.
    /// </summary>
    /// <param name="property">The property.</param>
    /// <returns>The property string.</returns>
    public static string GetProperty(string property)
    {
        return property;
    }

    /// <summary>
    /// Gets the value of the specified expression via reflection.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="path">The path.</param>
    /// <returns>The value of the specified expression or <paramref name="value"/> if not found.</returns>
    public static object GetValue(object value, string path)
    {
        Type currentType = value.GetType();

        foreach (string propertyName in path.Split('.'))
        {
            var property = currentType.GetProperty(propertyName);
            if (property != null)
            {
                if (value != null)
                {
                    value = property.GetValue(value, null);
                }

                currentType = property.PropertyType;
            }
        }
        return value;
    }

    /// <summary>
    /// Creates a function that returns the specified property.
    /// </summary>
    /// <typeparam name="T">The value type</typeparam>
    /// <param name="data">The value.</param>
    /// <param name="propertyName">The name of the property to return.</param>
    /// <returns>A function that returns the specified property.</returns>
    public static Func<object, T> Getter<T>(object data, string propertyName)
    {
        var type = data.GetType();
        var arg = Expression.Parameter(typeof(object));
        var body = Expression.Convert(Expression.Property(Expression.Convert(arg, type), propertyName), typeof(T));

        return Expression.Lambda<Func<object, T>>(body, arg).Compile();
    }

    /// <summary>
    /// Tries to get a property by its name.
    /// </summary>
    /// <typeparam name="T">The target type</typeparam>
    /// <param name="item">The item.</param>
    /// <param name="property">The property.</param>
    /// <param name="result">The property value.</param>
    /// <returns><c>true</c> if successful, <c>false</c> otherwise.</returns>
    public static bool TryGetItemOrValueFromProperty<T>(object item, string property, out T result)
    {
        object r = GetItemOrValueFromProperty(item, property);

        if (r != null)
        {
            result = (T)r;
            return true;
        }
        else
        {
            result = default;
            return false;
        }
    }

    /// <summary>
    /// Gets the item or value from property.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <param name="property">The property.</param>
    /// <returns>System.Object.</returns>
    public static object GetItemOrValueFromProperty(object item, string property)
    {
        if (item == null)
        {
            return null;
        }

        if (Convert.GetTypeCode(item) != TypeCode.Object || string.IsNullOrEmpty(property))
        {
            return item;
        }

        return PropertyAccess.GetValue(item, property);
    }

    /// <summary>
    /// Determines whether the specified type is numeric.
    /// </summary>
    /// <param name="source">The type.</param>
    /// <returns><c>true</c> if the specified source is numeric; otherwise, <c>false</c>.</returns>
    public static bool IsNumeric(Type source)
    {
        if (source == null)
            return false;

        var type = source.IsGenericType ? source.GetGenericArguments()[0] : source;

        switch (Type.GetTypeCode(type))
        {
            case TypeCode.Byte:
            case TypeCode.SByte:
            case TypeCode.UInt16:
            case TypeCode.UInt32:
            case TypeCode.UInt64:
            case TypeCode.Int16:
            case TypeCode.Int32:
            case TypeCode.Int64:
            case TypeCode.Decimal:
            case TypeCode.Double:
            case TypeCode.Single:
                return true;
            default:
                return false;
        }
    }

    /// <summary>
    /// Determines whether the specified type is an enum.
    /// </summary>
    /// <param name="source">The type.</param>
    /// <returns><c>true</c> if the specified source is an enum; otherwise, <c>false</c>.</returns>
    public static bool IsEnum(Type source)
    {
        if (source == null)
            return false;

        return source.IsEnum;
    }

    /// <summary>
    /// Determines whether the specified type is a Nullable enum.
    /// </summary>
    /// <param name="source">The type.</param>
    /// <returns><c>true</c> if the specified source is an enum; otherwise, <c>false</c>.</returns>
    public static bool IsNullableEnum(Type source)
    {
        if (source == null) return false;
        Type u = Nullable.GetUnderlyingType(source);
        return (u != null) && u.IsEnum;
    }

    /// <summary>
    /// Determines whether the specified type is anonymous.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <returns><c>true</c> if the specified type is anonymous; otherwise, <c>false</c>.</returns>
    public static bool IsAnonymous(this Type type)
    {
        if (type.IsGenericType)
        {
            var d = type.GetGenericTypeDefinition();
            if (d.IsClass && d.IsSealed && d.Attributes.HasFlag(System.Reflection.TypeAttributes.NotPublic))
            {
                var attributes = d.GetCustomAttributes(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute), false);
                if (attributes != null && attributes.Length > 0)
                {
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// Method to only replace first occurence of a substring in a string
    /// </summary>
    /// <param name="text">The text to search in.</param>
    /// <param name="search">The string to search for.</param>
    /// <param name="replace">The replacement string.</param>
    /// <returns>The modified string.</returns>
    public static string ReplaceFirst(this string text, string search, string replace)
    {
        int pos = text.IndexOf(search, StringComparison.Ordinal);
        if (pos < 0)
        {
            return text;
        }
        return text.Substring(0, pos) + replace + text.Substring(pos + search.Length);
    }

    /// <summary>
    /// Gets the type of the property.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <param name="property">The property.</param>
    /// <returns>Type.</returns>
    public static Type GetPropertyType(Type type, string property)
    {
        if (property.Contains("."))
        {
            var part = property.Split('.').FirstOrDefault();
            return GetPropertyType(GetPropertyTypeIncludeInterface(type, part), property.ReplaceFirst($"{part}.", ""));
        }

        return GetPropertyTypeIncludeInterface(type, property);
    }

    private static Type GetPropertyTypeIncludeInterface(Type type, string property)
    {
        if (type != null)
        {
            return !type.IsInterface ?
                type.GetProperty(property)?.PropertyType :
                    new Type[] { type }
                    .Concat(type.GetInterfaces())
                    .FirstOrDefault(t => t.GetProperty(property) != null)?
                    .GetProperty(property)?.PropertyType;
        }

        return null;
    }

    /// <summary>
    /// Gets the property by its name. If the type is an interface, it will search through all interfaces implemented by the type.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <param name="property">The property.</param>
    /// <returns>PropertyInfo.</returns>
    public static PropertyInfo GetProperty(Type type, string property)
    {
        if (type.IsInterface)
        {
            var interfaces = type.GetInterfaces();

            foreach (var @interface in interfaces)
            {
                var propertyInfo = @interface.GetProperty(property);

                if (propertyInfo != null)
                {
                    return propertyInfo;
                }
            }
        }

        return type.GetProperty(property);
    }

    /// <summary>
    /// Gets the dynamic property expression when binding to IDictionary.
    /// </summary>
    /// <param name="name">The property name.</param>
    /// <param name="type">The property type.</param>
    /// <returns>Dynamic property expression.</returns>
    public static string GetDynamicPropertyExpression(string name, Type type)
    {
        var isEnum = type.IsEnum || Nullable.GetUnderlyingType(type)?.IsEnum == true;
        var typeName = isEnum ? "Enum" : (Nullable.GetUnderlyingType(type) ?? type).Name;
        var typeFunc = $@"{typeName}{(!isEnum && Nullable.GetUnderlyingType(type) != null ? "?" : "")}";

        return $@"({typeFunc})it[""{name}""]";
    }
}

