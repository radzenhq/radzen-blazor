#if !NET11_0_OR_GREATER
using System;

namespace Radzen
{
    static class StringComparisonExtensions
    {
        public static bool StartsWith(this string value, char c, StringComparison comparison)
        {
            return comparison == StringComparison.Ordinal ? value.StartsWith(c) : value.StartsWith(c.ToString(), comparison);
        }

        public static bool EndsWith(this string value, char c, StringComparison comparison)
        {
            return comparison == StringComparison.Ordinal ? value.EndsWith(c) : value.EndsWith(c.ToString(), comparison);
        }

        public static int IndexOf(this string value, char c, int startIndex, StringComparison comparison)
        {
            return comparison == StringComparison.Ordinal ? value.IndexOf(c, startIndex) : value.IndexOf(c.ToString(), startIndex, comparison);
        }

        public static int IndexOf(this string value, char c, int startIndex, int count, StringComparison comparison)
        {
            return comparison == StringComparison.Ordinal ? value.IndexOf(c, startIndex, count) : value.IndexOf(c.ToString(), startIndex, count, comparison);
        }

        public static int LastIndexOf(this string value, char c, StringComparison comparison)
        {
            return comparison == StringComparison.Ordinal ? value.LastIndexOf(c) : value.LastIndexOf(c.ToString(), comparison);
        }
    }
}
#endif
