using System;
using System.Globalization;

namespace Radzen.Blazor.Rendering
{
    public static class GlobalizationExtensions
    {
        public static string ToCulturedString(this object value, CultureInfo culture)
        {
            return value is null ? null : Convert.ToString(value, culture);
        }
    }
}