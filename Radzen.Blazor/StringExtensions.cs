using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq.Dynamic.Core.CustomTypeProviders;
using System.Text;

namespace Radzen.Blazor
{
    /// <summary>
    /// Class containing various string extension methods
    /// </summary>
     [DynamicLinqType]

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
            if ((compareOptions & CompareOptions.IgnoreSymbols) != 0) 
            {
                source = source.TrimSartSymbolCharacters();
            }
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
        /// <summary>
        /// Removes all symbol and whitespace characters from the start of the string. We remove white space characters to be consistent with CompareOptions.IgnoreSymbols option
        /// </summary>
        /// <param name="str">The string to remove the symbol characters from </param>
        /// <returns>The string trimed from symbol characters at the start</returns>

        public static string TrimSartSymbolCharacters(this string str)
        {
            StringBuilder sb = new StringBuilder();
            bool foundNonSymbol = false;
            foreach (char c in str)
            {
                if (!(char.IsSymbol(c)|| char.IsWhiteSpace(c)) || foundNonSymbol)
                {
                    foundNonSymbol = true;
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }
        /// <summary>
        /// Removes all symbol and whitespace characters from the enf of the string. We remove white space characters to be consistent with CompareOptions.IgnoreSymbols option
        /// </summary>
        /// <param name="str">The string to remove the symbol characters from </param>
        /// <returns>The string trimed from symbol characters at the end</returns>
        public static string TrimEndSymbolCharacters(this string str)
        {
            StringBuilder sb = new StringBuilder();
            bool foundNonSymbol = false;
            for (int i = str.Length - 1;i >= 0; i-- )
            {
                char c  = str[i];
                if (!(char.IsSymbol(c) || char.IsWhiteSpace(c)) || foundNonSymbol)
                {
                    foundNonSymbol = true;
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }
    }
}
