using System.Collections.Generic;
using Radzen.Documents.Pdf.Objects;

namespace Radzen.Documents.Pdf;

internal static class PageTreeWalker
{
    internal sealed record Node(DocumentObject Source, DictionaryObject Dictionary, int KidIndex);

    internal sealed record Leaf(IReadOnlyList<Node> Path)
    {
        internal Node Node => Path[^1];
    }

    internal static IReadOnlyList<Leaf> Enumerate(
        DocumentReader reader,
        DocumentObject root,
        ReaderLimits limits,
        bool rejectInvalidKids)
    {
        var leaves = new List<Leaf>();
        var path = new List<Node>();
        var visited = new HashSet<DictionaryObject>();
        Walk(reader, root, -1, limits, rejectInvalidKids, visited, path, leaves, 0);
        return leaves;
    }

    private static void Walk(
        DocumentReader reader,
        DocumentObject source,
        int kidIndex,
        ReaderLimits limits,
        bool rejectInvalidKids,
        HashSet<DictionaryObject> visited,
        List<Node> path,
        List<Leaf> leaves,
        int depth)
    {
        if (depth > limits.MaxPageTreeDepth)
        {
            throw new DocumentParseException("Maximum page tree depth exceeded.", -1);
        }

        if (reader.AsDictionary(source) is not { } node)
        {
            if (rejectInvalidKids)
            {
                throw new DocumentParseException("A page tree node is not a dictionary.", -1);
            }

            return;
        }

        if (!visited.Add(node))
        {
            throw new DocumentParseException("Cyclic page tree reference.", -1);
        }

        path.Add(new Node(source, node, kidIndex));
        if (node.TryGetValue("Kids", out var kidsValue) && kidsValue is not null)
        {
            if (reader.AsArray(kidsValue) is not { } kids)
            {
                if (rejectInvalidKids)
                {
                    throw new DocumentParseException("The page tree /Kids must be an array.", -1);
                }
            }
            else
            {
                for (var i = 0; i < kids.Count; i++)
                {
                    Walk(reader, kids[i], i, limits, rejectInvalidKids, visited, path, leaves, depth + 1);
                }

                path.RemoveAt(path.Count - 1);
                return;
            }
        }

        leaves.Add(new Leaf([.. path]));
        path.RemoveAt(path.Count - 1);
    }
}
