#nullable enable

namespace Radzen.Documents.Spreadsheet;

static class Wildcard
{
    private static bool Match(string text, int textIndex, string pattern, bool allowTrailing)
    {
        int t = textIndex;
        int p = 0;
        int starP = -1;
        int starT = -1;

        while (t < text.Length)
        {
            if (p < pattern.Length)
            {
                char pc = pattern[p];
                if (pc == '~' && p + 1 < pattern.Length)
                {
                    if (CharEquals(text[t], pattern[p + 1]))
                    {
                        t++;
                        p += 2;
                        continue;
                    }
                }
                else if (pc == '*')
                {
                    starP = p++;
                    starT = t;
                    continue;
                }
                else if (pc == '?')
                {
                    t++;
                    p++;
                    continue;
                }
                else if (CharEquals(text[t], pc))
                {
                    t++;
                    p++;
                    continue;
                }
            }
            else if (allowTrailing)
            {
                return true;
            }

            if (starP >= 0)
            {
                p = starP + 1;
                t = ++starT;
                continue;
            }
            return false;
        }

        while (p < pattern.Length && pattern[p] == '*')
        {
            p++;
        }
        return p == pattern.Length || (p == pattern.Length - 1 && pattern[p] == '~');
    }

    private static bool CharEquals(char a, char b)
        => char.ToUpperInvariant(a) == char.ToUpperInvariant(b);

    public static bool IsFullMatch(string text, string pattern)
        => Match(text, 0, pattern, allowTrailing: false);

    public static int FindFirstIndex(string text, string pattern, int startIndex)
    {
        if (startIndex < 0)
        {
            startIndex = 0;
        }
        for (int s = startIndex; s <= text.Length; s++)
        {
            if (Match(text, s, pattern, allowTrailing: true))
            {
                return s;
            }
        }
        return -1;
    }
}
