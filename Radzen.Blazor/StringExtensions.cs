using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Radzen.Blazor
{
    /// <summary>
    /// Class containing various string extension methods
    /// </summary>
   public static class StringExtensions
    {
        /// <summary>
        /// It checks if a string contains the value if another string.
        /// </summary>
        /// <param name="source">The string to check if it contains the specific value</param>
        /// <param name="value">The value that the string should contain with</param>
        /// <param name="compareOptions">Case, dicritics, symbol and more sensitivity options to apply during the comparison</param>
        /// <returns>A boolean indicating if the string contains the string or not</returns>
        public static bool Contains(this string source, string value, CompareOptions compareOptions)
        {
            var index = CultureInfo.InvariantCulture.CompareInfo.IndexOf(source, value, compareOptions);
            return index != -1;
        }
        /// <summary>
        /// It checks if a string starts with the value if another string.
        /// </summary>
        /// <param name="source">The string to check if it starts with the specific value</param>
        /// <param name="value">The value that the string should start with</param>
        /// <param name="compareOptions">Case, dicritics, symbol and more sensitivity options to apply during the comparison</param>
        /// <returns>A boolean indicating if the string starts the value or not</returns>
        public static bool StartsWith(this string source, string value, CompareOptions compareOptions)
        {
            return CultureInfo.InvariantCulture.CompareInfo.IsPrefix(source, value, compareOptions);
           
        }
        /// <summary>
        /// It checks if a string ends with the value if another string.
        /// </summary>
        /// <param name="source">The string to check if it ends with the specific value</param>
        /// <param name="value">The value that the string should end with</param>
        /// <param name="compareOptions">Case, dicritics, symbol and more sensitivity options to apply during the comparison</param>
        /// <returns>A boolean indicating if the string end the value or not</returns>

        public static bool EndsWith(this string source, string value, CompareOptions compareOptions)
        {
            return CultureInfo.InvariantCulture.CompareInfo.IsSuffix(source, value, compareOptions);

        }
    }
}
