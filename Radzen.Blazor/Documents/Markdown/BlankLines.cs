using System.Collections.Generic;
using System.Linq;

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

    public static Document Inflate(Document document, string source)
    {
        Inflate((BlockContainer)document, source);
        Pad(document, source.Length, null);
        return document;
    }

    public static void Pad(Document document, int end, INode? keep)
    {
        foreach (var stale in document.Children.OfType<Paragraph>().Where(paragraph => paragraph.Virtual && paragraph != keep).ToList())
        {
            document.Remove(stale);
        }

        if (document.Children.Count == 0)
        {
            document.Add(new Paragraph());
        }

        var index = 0;

        while (index < document.Children.Count)
        {
            var child = document.Children[index];
            var previous = index > 0 ? document.Children[index - 1] : null;

            if (IsSealed(child) && (previous == null || IsSealed(previous)))
            {
                document.Insert(index, new Paragraph { Virtual = true, SourceStart = child.SourceStart, SourceEnd = child.SourceStart });
                index++;
            }

            index++;
        }

        if (IsSealed(document.LastChild!))
        {
            document.Add(new Paragraph { Virtual = true, SourceStart = end, SourceEnd = end });
        }
    }

    private static void Inflate(BlockContainer container, string source)
    {
        var start = container is Document ? 0 : container.SourceStart;
        var end = container is Document ? source.Length : container.SourceEnd;
        var children = container.Children.ToList();
        var index = 0;
        var insertAt = 0;

        foreach (var child in children)
        {
            foreach (var offset in Placeholders(source, start, child.SourceStart, index > 0, true))
            {
                container.Insert(insertAt++, new Paragraph { SourceStart = offset, SourceEnd = offset, Pristine = true });
            }

            insertAt++;

            if (child is BlockContainer nested)
            {
                Inflate(nested, source);
            }

            start = child.SourceEnd;
            index++;
        }

        foreach (var offset in Placeholders(source, start, end, index > 0, false))
        {
            container.Insert(insertAt++, new Paragraph { SourceStart = offset, SourceEnd = offset, Pristine = true });
        }
    }
}
