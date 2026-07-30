#nullable enable
using System.Collections.Generic;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

internal sealed class ProbeElement
{
    public string Type = "";

    public DictionaryObject Dict = null!;

    public List<ProbeElement> Children { get; } = [];

    public List<int> Mcids { get; } = [];

    public List<DictionaryObject> ObjectReferences { get; } = [];

    public List<object> Kids { get; } = [];
}

internal static class TaggedStructureProbe
{
    public static DictionaryObject StructRoot(DocumentReader reader)
    {
        var catalog = ContentTestHelpers.Catalog(reader);
        Assert.True(catalog.TryGetValue("StructTreeRoot", out var root), "catalog has /StructTreeRoot");
        return Assert.IsType<DictionaryObject>(reader.Resolve(root!));
    }

    public static ProbeElement Root(DocumentReader reader)
    {
        var root = StructRoot(reader);
        var kids = Elements(reader, root);
        return Assert.Single(kids);
    }

    public static List<ProbeElement> Elements(DocumentReader reader, DictionaryObject parent)
    {
        var elements = new List<ProbeElement>();
        foreach (var kid in KidValues(reader, parent))
        {
            if (kid is DictionaryObject dict
                && dict.TryGetValue("S", out var s)
                && reader.Resolve(s!) is NameObject type)
            {
                elements.Add(Build(reader, dict, type.Value));
            }
        }

        return elements;
    }

    private static ProbeElement Build(DocumentReader reader, DictionaryObject dict, string type)
    {
        var element = new ProbeElement { Type = type, Dict = dict };
        foreach (var kid in KidValues(reader, dict))
        {
            switch (kid)
            {
                case NumberObject mcid:
                    element.Mcids.Add(mcid.IntValue);
                    element.Kids.Add(mcid.IntValue);
                    break;
                case DictionaryObject child when child.TryGetValue("S", out var s)
                    && reader.Resolve(s!) is NameObject childType:
                    {
                        var built = Build(reader, child, childType.Value);
                        element.Children.Add(built);
                        element.Kids.Add(built);
                        break;
                    }
                case DictionaryObject mcr when Name(reader, mcr, "Type") == "MCR"
                    && mcr.TryGetValue("MCID", out var value)
                    && reader.Resolve(value!) is NumberObject number:
                    element.Mcids.Add(number.IntValue);
                    element.Kids.Add(number.IntValue);
                    break;
                case DictionaryObject objr when Name(reader, objr, "Type") == "OBJR":
                    element.ObjectReferences.Add(objr);
                    element.Kids.Add(objr);
                    break;
            }
        }

        return element;
    }

    private static string? Name(DocumentReader reader, DictionaryObject dict, string key)
        => dict.TryGetValue(key, out var value) && reader.Resolve(value!) is NameObject name ? name.Value : null;

    private static List<DocumentObject> KidValues(DocumentReader reader, DictionaryObject parent)
    {
        var values = new List<DocumentObject>();
        if (!parent.TryGetValue("K", out var k))
        {
            return values;
        }

        var resolved = reader.Resolve(k!);
        if (resolved is ArrayObject array)
        {
            foreach (var item in array)
            {
                values.Add(reader.Resolve(item));
            }
        }
        else
        {
            values.Add(resolved);
        }

        return values;
    }

    public static IEnumerable<ProbeElement> Descendants(ProbeElement element)
    {
        yield return element;
        foreach (var child in element.Children)
        {
            foreach (var descendant in Descendants(child))
            {
                yield return descendant;
            }
        }
    }

    public static ProbeElement Single(ProbeElement root, string type)
    {
        var matches = new List<ProbeElement>();
        foreach (var element in Descendants(root))
        {
            if (element.Type == type)
            {
                matches.Add(element);
            }
        }

        return Assert.Single(matches);
    }

    public static List<ProbeElement> All(ProbeElement root, string type)
    {
        var matches = new List<ProbeElement>();
        foreach (var element in Descendants(root))
        {
            if (element.Type == type)
            {
                matches.Add(element);
            }
        }

        return matches;
    }

    public static List<(string Tag, int Mcid)> MarkedContentInOrder(DocumentReader reader, int pageIndex)
    {
        var result = new List<(string, int)>();
        foreach (var operation in ContentStreamTokenizer.Parse(ContentTestHelpers.PageContent(reader, pageIndex)))
        {
            if (operation.Operator != "BDC")
            {
                continue;
            }

            for (var i = 1; i < operation.Operands.Count - 1; i++)
            {
                if (operation.Operands[i].Kind == ContentTokenKind.Name
                    && operation.Operands[i].Text == "MCID"
                    && operation.Operands[i + 1].Kind == ContentTokenKind.Number)
                {
                    result.Add((operation.Operands[0].Text, (int)operation.Operands[i + 1].Number));
                    break;
                }
            }
        }

        return result;
    }

    public static DocumentObject? ParentTreeEntry(DocumentReader reader, DictionaryObject structRoot, int key)
    {
        var tree = Assert.IsType<DictionaryObject>(reader.Resolve(structRoot["ParentTree"]));
        return Lookup(reader, tree, key);
    }

    private static DocumentObject? Lookup(DocumentReader reader, DictionaryObject node, int key)
    {
        if (node.TryGetValue("Nums", out var numsObject)
            && reader.Resolve(numsObject!) is ArrayObject nums)
        {
            for (var i = 0; i + 1 < nums.Count; i += 2)
            {
                if (reader.Resolve(nums[i]) is NumberObject number && number.IntValue == key)
                {
                    return reader.Resolve(nums[i + 1]);
                }
            }
        }

        if (node.TryGetValue("Kids", out var kidsObject)
            && reader.Resolve(kidsObject!) is ArrayObject kids)
        {
            foreach (var kid in kids)
            {
                if (reader.Resolve(kid) is DictionaryObject child && Lookup(reader, child, key) is { } found)
                {
                    return found;
                }
            }
        }

        return null;
    }

    public static DictionaryObject OwnerOfMcid(DocumentReader reader, DictionaryObject structRoot, int pageIndex, int mcid)
    {
        var entry = Assert.IsType<ArrayObject>(ParentTreeEntry(reader, structRoot, pageIndex));
        Assert.True(mcid < entry.Count, $"the parent tree entry for page {pageIndex} covers MCID {mcid}");
        return Assert.IsType<DictionaryObject>(reader.Resolve(entry[mcid]));
    }
}
