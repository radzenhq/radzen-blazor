using System;
using System.Collections.Generic;
using System.Linq;

namespace Radzen.Documents.Markdown.Tests;

internal static class Canonical
{
    public static string Html(string markdown) => HtmlVisitor.ToHtml(Document(MarkdownParser.Parse(markdown)));

    public static Document Document(Document document)
    {
        foreach (var leaf in Descendants(document).OfType<Leaf>())
        {
            leaf.ReplaceInlines(Inlines(leaf.Children, []));
        }

        foreach (var cell in Descendants(document).OfType<Table>().SelectMany(table => table.Rows).SelectMany(row => row.Cells))
        {
            cell.ReplaceInlines(Inlines(cell.Children, []));
        }

        return document;
    }

    private static List<Inline> Inlines(IReadOnlyList<Inline> inlines, List<Type> open)
    {
        var result = new List<Inline>();

        foreach (var inline in inlines)
        {
            Add(inline);
        }

        return result;

        Inline? Last() => result.LastOrDefault(candidate => candidate is not Text { Value.Length: 0 });

        void Add(Inline inline)
        {
            if (inline is Emphasis or Strong or Strikethrough && open.Contains(inline.GetType()))
            {
                foreach (var child in ((InlineContainer)inline).Children)
                {
                    Add(child);
                }

                return;
            }

            if (inline is Code code && Last() is Code previousCode)
            {
                result[result.IndexOf(previousCode)] = new Code(previousCode.Value + code.Value);
                return;
            }

            if (inline is Emphasis or Strong or Strikethrough)
            {
                var container = (InlineContainer)inline;
                open.Add(inline.GetType());
                var children = Inlines(container.Children, open);
                open.RemoveAt(open.Count - 1);

                if (Last() is { } last && last.GetType() == inline.GetType())
                {
                    var previous = (InlineContainer)last;
                    previous.ReplaceInlines(Inlines(previous.Children.Concat(children).ToList(), [.. open, inline.GetType()]));
                    return;
                }

                container.ReplaceInlines(children);
            }
            else if (inline is InlineContainer link)
            {
                link.ReplaceInlines(Inlines(link.Children, []));
            }

            result.Add(inline);
        }
    }

    private static IEnumerable<Block> Descendants(Block block)
    {
        yield return block;

        if (block is BlockContainer container)
        {
            foreach (var child in container.Children.ToList())
            {
                foreach (var descendant in Descendants(child))
                {
                    yield return descendant;
                }
            }
        }
    }
}
