using System.Collections.Generic;

namespace Radzen.Documents.Markdown;

internal static class BlankLines
{
    public static bool IsSealed(Block block) => block is Table or ThematicBreak or HtmlBlock or FencedCodeBlock { Closed: true };

    public static List<int> Placeholders(string source, int from, int to, bool hasPrevious, bool hasNext)
    {
        var placeholders = new List<int>();

        if (to < from)
        {
            return placeholders;
        }

        var lines = new List<int>();
        var firstNewline = source.IndexOf('\n', from, to - from);
        var lineStart = from == 0 || !hasPrevious || source[from - 1] == '\n' ? from : firstNewline < 0 ? -1 : firstNewline + 1;

        while (lineStart >= 0 && lineStart <= to)
        {
            var newline = source.IndexOf('\n', lineStart, to - lineStart);
            var lineEnd = newline >= 0 ? newline : to;

            if (newline < 0 && to < source.Length && source[to] != '\n')
            {
                break;
            }

            var content = lineStart;

            while (content < lineEnd && source[content] is ' ' or '\t' or '>')
            {
                content++;
            }

            if (content == lineEnd)
            {
                lines.Add(content);
            }

            if (newline < 0)
            {
                break;
            }

            lineStart = newline + 1;
        }

        for (var index = 0; index < lines.Count; index++)
        {
            if ((index == 0 && hasPrevious) || (index == lines.Count - 1 && hasNext))
            {
                continue;
            }

            placeholders.Add(lines[index]);
        }

        return placeholders;
    }
}
