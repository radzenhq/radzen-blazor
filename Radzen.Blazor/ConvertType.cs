using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Radzen;

/// <summary>
/// Converts values to different types. Used internally.
/// </summary>
public static class ConvertType
{
    /// <summary>
    /// Changes the type of an object.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="type">The type.</param>
    /// <param name="culture">The culture.</param>
    /// <returns>System.Object</returns>
    public static object? ChangeType(object value, Type type, CultureInfo? culture = null)
    {
        ArgumentNullException.ThrowIfNull(type);

        // CA1062: Validate 'value' is non-null before using it
        if (value == null)
        {
            if (Nullable.GetUnderlyingType(type) != null)
            {
                return null;
            }
            throw new ArgumentNullException(nameof(value));
        }

        if (culture == null)
        {
            culture = CultureInfo.CurrentCulture;
        }

        if ((Nullable.GetUnderlyingType(type) ?? type) == typeof(Guid) && value is string)
        {
            return Guid.Parse((string)value);
        }

        var underlyingEnumType = Nullable.GetUnderlyingType(type);
        if (underlyingEnumType?.IsEnum == true)
        {
            var valueString = value.ToString();
            if (valueString == null)
            {
                throw new ArgumentNullException(nameof(value), "Enum value cannot be null.");
            }
            return Enum.Parse(underlyingEnumType, valueString);
        }

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
        {
            Type itemType = type.GetGenericArguments()[0];
            var enumerable = value as IEnumerable<object>;

            if (enumerable != null)
            {
                return enumerable.AsQueryable().Cast(itemType);
            }
        }

        return value is IConvertible ? Convert.ChangeType(value, Nullable.GetUnderlyingType(type) ?? type, culture) : value;
    }
}

