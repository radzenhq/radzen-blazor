using System.Globalization;

namespace Radzen.Blazor.Rendering
{
    public static class NumericExtensions
    {
        public static string ToInvariantString(this double value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        public static string ToInvariantString(this double? value)
        {
            return value.Value.ToInvariantString();
        }

        public static string ToInvariantString(this decimal value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        public static string ToInvariantString(this decimal? value)
        {
            return value.Value.ToInvariantString();
        }
    }
}