using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Radzen.Blazor
{
    internal static class GanttHeaderDateFormat
    {
        internal static string Resolve(string? headerDateFormat, CultureInfo culture)
        {
            if (!string.IsNullOrWhiteSpace(headerDateFormat))
            {
                return headerDateFormat;
            }

            return MonthDayPattern(culture);
        }

        internal static string MonthDayPattern(CultureInfo culture)
        {
            var tokens = Tokenize(culture.DateTimeFormat.ShortDatePattern);

            var i = 0;

            while (i < tokens.Count)
            {
                if (tokens[i].Kind != TokenKind.Year)
                {
                    i++;
                    continue;
                }

                tokens.RemoveAt(i);

                if (i < tokens.Count && tokens[i].Kind == TokenKind.Separator)
                {
                    tokens.RemoveAt(i);
                }
                else if (i > 0 && tokens[i - 1].Kind == TokenKind.Separator)
                {
                    tokens.RemoveAt(i - 1);
                    i--;
                }
            }

            var hasDay = tokens.Any(t => t.Kind == TokenKind.Field && t.Text[0] == 'd');
            var hasMonth = tokens.Any(t => t.Kind == TokenKind.Field && t.Text[0] == 'M');

            if (!hasDay || !hasMonth)
            {
                return culture.DateTimeFormat.MonthDayPattern;
            }

            return string.Concat(tokens.Select(t => t.Text)).Trim();
        }

        private enum TokenKind
        {
            Field,
            Year,
            Separator
        }

        private readonly struct Token
        {
            public Token(TokenKind kind, string text)
            {
                Kind = kind;
                Text = text;
            }

            public TokenKind Kind { get; }
            public string Text { get; }
        }

        private static List<Token> Tokenize(string pattern)
        {
            var tokens = new List<Token>();
            var separator = new StringBuilder();

            void FlushSeparator()
            {
                if (separator.Length > 0)
                {
                    tokens.Add(new Token(TokenKind.Separator, separator.ToString()));
                    separator.Clear();
                }
            }

            var i = 0;

            while (i < pattern.Length)
            {
                var c = pattern[i];

                if (char.IsLetter(c))
                {
                    FlushSeparator();

                    var start = i;

                    while (i < pattern.Length && pattern[i] == c)
                    {
                        i++;
                    }

                    var text = pattern.Substring(start, i - start);
                    tokens.Add(new Token(c == 'y' || c == 'g' ? TokenKind.Year : TokenKind.Field, text));
                }
                else if (c == '\'' || c == '"')
                {
                    var end = pattern.IndexOf(c, i + 1);

                    if (end < 0)
                    {
                        end = pattern.Length - 1;
                    }

                    separator.Append(pattern, i, end - i + 1);
                    i = end + 1;
                }
                else if (c == '\\' && i + 1 < pattern.Length)
                {
                    separator.Append(pattern, i, 2);
                    i += 2;
                }
                else
                {
                    separator.Append(c);
                    i++;
                }
            }

            FlushSeparator();

            return tokens;
        }
    }
}
