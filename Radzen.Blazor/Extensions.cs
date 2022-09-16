using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;

namespace Radzen.Blazor
{
    /// <summary>
    /// Class EnumExtensions.
    /// </summary>
    public static class EnumExtensions
    {
        /// <summary>
        /// Gets enum description.
        /// </summary>
        public static string GetDisplayDescription(this Enum enumValue)
        {
            var enumValueAsString = enumValue.ToString();
            var val = enumValue.GetType().GetMember(enumValueAsString).FirstOrDefault();

            return val?.GetCustomAttribute<DisplayAttribute>()?.GetDescription() ?? enumValueAsString;
        }

        /// <summary>
        /// Converts Enum to IEnumerable of Value/Text.
        /// </summary>
        public static IEnumerable<object> EnumAsKeyValuePair(Type enumType)
        {
            return Enum.GetValues(enumType).Cast<Enum>().Distinct().Select(val => new { Value = Convert.ToInt32(val), Text = val.GetDisplayDescription() });
        }

    }
}

