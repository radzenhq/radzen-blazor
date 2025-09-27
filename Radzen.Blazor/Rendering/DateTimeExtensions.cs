using System;
using System.Globalization;

namespace Radzen.Blazor.Rendering
{
    /// <summary>
    /// Class DateTimeExtensions.
    /// </summary>
    public static class DateTimeExtensions
    {
        /// <summary>
        /// Starts the of week.
        /// </summary>
        /// <param name="date">The date.</param>
        /// <returns>DateTime.</returns>
        public static DateTime StartOfWeek(this DateTime date) => date.StartOfWeek(CultureInfo.CurrentCulture);

        /// <summary>
        /// Starts the of week.
        /// </summary>
        /// <param name="date">The date.</param>
        /// <param name="info">The information.</param>
        /// <returns>DateTime.</returns>
        public static DateTime StartOfWeek(this DateTime date, CultureInfo info)
        {
            var diff = date.DayOfWeek - info.DateTimeFormat.FirstDayOfWeek;

            if (diff < 0)
            {
                diff += 7;
            }

            return date.AddDays(-diff).Date;
        }
        /// <summary>
        /// Starts the of month.
        /// </summary>
        /// <param name="date">The date.</param>
        /// <returns>DateTime.</returns>
        public static DateTime StartOfMonth(this DateTime date) => new(date.Year, date.Month, 1);

        /// <summary>
        /// Ends the of month.
        /// </summary>
        /// <param name="date">The date.</param>
        /// <returns>DateTime.</returns>
        public static DateTime EndOfMonth(this DateTime date) => date.StartOfMonth().AddMonths(1).AddDays(-1);

        /// <summary>
        /// Ends the of week.
        /// </summary>
        /// <param name="date">The date.</param>
        /// <returns>DateTime.</returns>
        public static DateTime EndOfWeek(this DateTime date) => date.EndOfWeek(CultureInfo.CurrentCulture);

        /// <summary>
        /// Ends the of week.
        /// </summary>
        /// <param name="date">The date.</param>
        /// <param name="info">The information.</param>
        /// <returns>DateTime.</returns>
        public static DateTime EndOfWeek(this DateTime date, CultureInfo info) => date.StartOfWeek(info).AddDays(6);
    }
}