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
                return attribute?.GetDescription();

            return enumValue.ToString();
        }

        public static IEnumerable<object> EnumAsKeyValuePair(Type enumType)
        {
            var values = Enum.GetValues(enumType).Cast<Enum>().ToList();

            var items = new List<object>(values.Count);
            foreach (var val in values.GroupBy(v => Convert.ToInt32(v)))
            {
                var description = string.Join('/', val.Select(v => v.GetDisplayDescription()));

                items.Add(new { Value = val.Key, Text = description });
            }

            return items;
        }

    }
}

