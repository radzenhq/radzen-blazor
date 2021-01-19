using System;
using System.Globalization;

namespace Radzen.Blazor.Rendering
{
    public static class DateTimeExtensions
    {
        public static DateTime StartOfWeek(this DateTime date)
        {
            var diff = date.DayOfWeek - DateTimeFormatInfo.CurrentInfo.FirstDayOfWeek;

            if (diff < 0)
            {
                diff += 7;
            }

            return date.AddDays(-diff).Date;
        }

        public static DateTime StartOfMonth(this DateTime date)
        {
            return new DateTime(date.Year, date.Month, 1);
        }

        public static DateTime EndOfMonth(this DateTime date)
        {
            return date.StartOfMonth().AddMonths(1).AddDays(-1);
        }

        public static DateTime EndOfWeek(this DateTime date)
        {
            return date.StartOfWeek().AddDays(6);
        }
    }
}