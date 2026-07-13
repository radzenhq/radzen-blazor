#nullable enable
using System.Collections.Generic;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;

namespace Radzen.Blazor.Pdf.Tests;

internal static class StructureTestHelpers
{
    public static ArrayObject? RootKids(DocumentReader reader)
    {
        var catalog = ContentTestHelpers.Catalog(reader);
        if (!catalog.TryGetValue("StructTreeRoot", out var rootObject)
            || reader.Resolve(rootObject!) is not DictionaryObject root
            || !root.TryGetValue("K", out var k))
        {
            return null;
        }

        return reader.Resolve(k!) as ArrayObject ?? new ArrayObject { reader.Resolve(k!) };
    }

    public static void CollectTypes(DocumentReader reader, ArrayObject? kids, List<string> types)
    {
        if (kids is null)
        {
            return;
        }

        foreach (var kid in kids)
        {
            if (reader.Resolve(kid) is not DictionaryObject dict
                || !dict.TryGetValue("S", out var s)
                || reader.Resolve(s!) is not NameObject type)
            {
                continue;
            }

            types.Add(type.Value);
            if (dict.TryGetValue("K", out var k))
            {
                var childKids = reader.Resolve(k!) as ArrayObject ?? new ArrayObject { reader.Resolve(k!) };
                CollectTypes(reader, childKids, types);
            }
        }
    }

    public static DictionaryObject? FindElement(DocumentReader reader, string type)
        => FindElement(reader, RootKids(reader), type);

    private static DictionaryObject? FindElement(DocumentReader reader, ArrayObject? kids, string type)
    {
        if (kids is null)
        {
            return null;
        }

        foreach (var kid in kids)
        {
            if (reader.Resolve(kid) is not DictionaryObject dict
                || !dict.TryGetValue("S", out var s)
                || reader.Resolve(s!) is not NameObject name)
            {
                continue;
            }

            if (name.Value == type)
            {
                return dict;
            }

            if (dict.TryGetValue("K", out var k))
            {
                var childKids = reader.Resolve(k!) as ArrayObject ?? new ArrayObject { reader.Resolve(k!) };
                if (FindElement(reader, childKids, type) is { } found)
                {
                    return found;
                }
            }
        }

        return null;
    }
}
