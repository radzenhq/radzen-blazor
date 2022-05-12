using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;

namespace Radzen.Blazor
{
    public static class EnumExtensions
    {
        public static string GetDisplayDescription(this Enum enumValue)
        {
            var val = enumValue.GetType().GetMember(enumValue.ToString()).FirstOrDefault();

            if (val is null)
                return enumValue.ToString();

            var attribute = val?.GetCustomAttribute<DisplayAttribute>();
            if (attribute != null)
                return attribute?.Description;

            return enumValue.ToString();
        }

        public static IEnumerable<object> EnumAsKeyValuePair(Type enumType)
        {
            var values = Enum.GetValues(enumType);
            var items = new List<object>(values.Length);

            foreach (Enum val in values)
            {
                items.Add(new { Value = Convert.ToInt32(val), Text = val.GetDisplayDescription() });
            }

            return items;
        }

    }
}

