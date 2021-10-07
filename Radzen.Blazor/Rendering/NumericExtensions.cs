using System.Globalization;

namespace Radzen.Blazor.Rendering
{
    /// <summary>
    /// Class NumericExtensions.
    /// </summary>
    public static class NumericExtensions
    {
        /// <summary>
        /// Converts to invariantstring.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>System.String.</returns>
        public static string ToInvariantString(this double value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Converts to invariantstring.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>System.String.</returns>
        public static string ToInvariantString(this double? value)
        {
            return value.Value.ToInvariantString();
        }

        /// <summary>
        /// Converts to invariantstring.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>System.String.</returns>
        public static string ToInvariantString(this decimal value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Converts to invariantstring.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>System.String.</returns>
        public static string ToInvariantString(this decimal? value)
        {
            return value.Value.ToInvariantString();
        }
    }
}