#nullable enable

using System;
using System.Text;
using System.Text.RegularExpressions;

namespace Radzen.Blazor.Spreadsheet;

static class Wildcard
{
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(100);

    static Regex BuildRegex(string pattern, bool full)
    {
        var builder = StringBuilderCache.Acquire(pattern.Length);

        for (int i = 0; i < pattern.Length; i++)
        {
            var ch = pattern[i];
            if (ch == '~')
            {
                if (i + 1 < pattern.Length)
                {
                    var next = pattern[++i];
                    builder.Append(Regex.Escape(next.ToString()));
                }
                else
                {
                    builder.Append(Regex.Escape("~"));
                }
            }
            else if (ch == '*')
            {
                builder.Append(".*");
            }
            else if (ch == '?')
            {
                builder.Append('.');
            }
            else
            {
                builder.Append(Regex.Escape(ch.ToString()));
            }
        }

        var regexBody = StringBuilderCache.GetStringAndRelease(builder);
        var regexPattern = full ? "^" + regexBody + "$" : regexBody;
        return new Regex(regexPattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled, RegexTimeout);
    }

    public static bool IsFullMatch(string text, string pattern)
    {
        try
        {
            return BuildRegex(pattern, full: true).IsMatch(text);
        }
        catch (RegexMatchTimeoutException)
        {
            return false;
        }
    }

    public static int FindFirstIndex(string text, string pattern, int startIndex)
    {
        try
        {
            var rx = BuildRegex(pattern, full: false);
            var input = startIndex > 0 ? text[startIndex..] : text;
            var match = rx.Match(input);
            return match.Success ? startIndex + match.Index : -1;
        }
        catch (RegexMatchTimeoutException)
        {
            return -1;
        }
    }
}
