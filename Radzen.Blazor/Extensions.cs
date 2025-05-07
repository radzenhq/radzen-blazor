﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text.RegularExpressions;
using System.Xml.Linq;

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
        public static string GetDisplayDescription(this Enum enumValue, Func<string, string> translationFunction = null)
        {
            var enumValueAsString = enumValue.ToString();
            var val = enumValue.GetType().GetMember(enumValueAsString).FirstOrDefault();
            var enumVal = val?.GetCustomAttribute<DisplayAttribute>()?.GetDescription() ?? enumValueAsString;

            if (translationFunction != null)
                return translationFunction(enumVal);

            return enumVal;
        }

        /// <summary>
        /// Converts Enum to IEnumerable of Value/Text.
        /// </summary>
        public static IEnumerable<object> EnumAsKeyValuePair(Type enumType, Func<string, string> translationFunction = null)
        {
            Type underlyingType = Enum.GetUnderlyingType(enumType);
            return Enum.GetValues(enumType).Cast<Enum>().Distinct().Select(val => new { Value = Convert.ChangeType(val, underlyingType), Text = val.GetDisplayDescription(translationFunction) });
        }

        /// <summary>
        /// Converts the autocomplete type enum value to the expected
        /// autocomplete attribute value.
        /// </summary>
        /// <returns>The autocomplete attribute string value.</returns>
        public static string GetAutoCompleteValue(this AutoCompleteType typeValue)
        {
            // Handle synonyms.
            switch (typeValue)
            {
                case AutoCompleteType.FirstName:
                    return "given-name";
                case AutoCompleteType.LastName:
                    return "family-name";
                case AutoCompleteType.MiddleName:
                    return "additional-name";
                case AutoCompleteType.ZipCode:
                    return "postal-code";
                case AutoCompleteType.Province:
                    return "address-level1";
                case AutoCompleteType.State:
                    return "address-level1";
            }

            // Handle standard values.
            var value = typeValue.ToString();
            value = Regex.Replace(value, "([^A-Z])([A-Z])", "$1-$2");
            return Regex.Replace(value, "([A-Z]+)([A-Z][^A-Z$])", "$1-$2")
                .Trim().ToLower();
        }
    }
}

