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
    public static object ChangeType(object value, Type type, CultureInfo culture = null)
    {
        if (culture == null)
        {
            culture = CultureInfo.CurrentCulture;
        }
        if (value == null && Nullable.GetUnderlyingType(type) != null)
        {
            return value;
        }

        if ((Nullable.GetUnderlyingType(type) ?? type) == typeof(Guid) && value is string)
        {
            return Guid.Parse((string)value);
        }

        if (Nullable.GetUnderlyingType(type)?.IsEnum == true)
        {
            return Enum.Parse(Nullable.GetUnderlyingType(type), value.ToString());
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

