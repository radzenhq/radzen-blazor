using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
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
    [UnconditionalSuppressMessage(TrimMessages.Trimming, TrimMessages.IL2075, Justification = TrimMessages.EnumTypePreserved)]
    public static class EnumExtensions
    {
        /// <summary>
        /// Gets the display text for an enum value. Resolution order:
        /// <see cref="DisplayAttribute.GetDescription"/>, then <see cref="DisplayAttribute.GetName"/>,
        /// then <see cref="System.ComponentModel.DescriptionAttribute.Description"/>, then <see cref="Enum.ToString()"/>.
        /// </summary>
        [UnconditionalSuppressMessage(TrimMessages.Trimming, TrimMessages.IL2070, Justification = TrimMessages.EnumTypePreserved)]
        public static string GetDisplayDescription(this Enum enumValue, Func<string, string>? translationFunction = null)
        {
            ArgumentNullException.ThrowIfNull(enumValue);
            var enumValueAsString = enumValue.ToString();
            var val = enumValue.GetType().GetMember(enumValueAsString).FirstOrDefault();

            string? enumVal = null;
            if (val != null)
            {
                var displayAttr = val.GetCustomAttribute<DisplayAttribute>();
                if (displayAttr != null)
                {
                    enumVal = displayAttr.GetDescription() ?? displayAttr.GetName();
                }

                if (enumVal == null)
                {
                    enumVal = val.GetCustomAttribute<System.ComponentModel.DescriptionAttribute>()?.Description;
                }
            }

            enumVal ??= enumValueAsString;

            if (translationFunction != null)
                return translationFunction(enumVal);

            return enumVal;
        }

        /// <summary>
        /// Converts Enum to IEnumerable of Value/Text.
        /// </summary>
        [UnconditionalSuppressMessage(TrimMessages.Trimming, TrimMessages.IL2067, Justification = TrimMessages.EnumTypePreserved)]
        public static IEnumerable<object> EnumAsKeyValuePair([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] Type enumType, Func<string, string>? translationFunction = null)
        {
            ArgumentNullException.ThrowIfNull(enumType);

            Type underlyingType = Enum.GetUnderlyingType(enumType);
            return Enum.GetValues(enumType).Cast<Enum>().Distinct().Select(val => new DropDownItem<object> { Value = Convert.ChangeType(val, underlyingType, CultureInfo.InvariantCulture), Text = val.GetDisplayDescription(translationFunction) });
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
                .Trim().ToLowerInvariant();
        }
    }
}

