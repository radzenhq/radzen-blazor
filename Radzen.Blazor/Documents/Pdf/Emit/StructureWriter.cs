using Radzen.Documents.Pdf.Objects;
using System.Collections.Generic;

namespace Radzen.Documents.Pdf.Emit;

// Serializes the logical structure tree (Tagged PDF) into the catalog: one
// indirect StructElem per element and the per-page ParentTree that maps marked
// content back to its element.
internal static class StructureWriter
{
    // Serializes the logical structure tree: one indirect StructElem per element
    // (marked-content kids before child elements), /Pg on elements with marks, a
    // /StructParents key per marked page and a flat Nums ParentTree whose per-page
    // arrays are indexed by MCID.
    public static ReferenceObject WriteStructureTree(
        DocumentWriter writer,
        StructureElement structure,
        List<(Page Page, DictionaryObject Node, ReferenceObject Reference)> pageNodes)
    {
        var root = new DictionaryObject { ["Type"] = new NameObject("StructTreeRoot") };
        var rootRef = writer.Add(root);

        var parents = new Dictionary<int, List<DocumentObject>>();
        root["K"] = WriteStructureElement(writer, structure, rootRef, pageNodes, parents);

        var keys = new List<int>(parents.Keys);
        keys.Sort();

        var nums = new ArrayObject();
        foreach (var pageIndex in keys)
        {
            var entries = new ArrayObject();
            foreach (var entry in parents[pageIndex])
            {
                entries.Add(entry);
            }

            nums.Add(new NumberObject(pageIndex));
            nums.Add(writer.Add(entries));
            pageNodes[pageIndex].Node["StructParents"] = new NumberObject(pageIndex);
        }

        root["ParentTree"] = writer.Add(new DictionaryObject { ["Nums"] = nums });
        root["ParentTreeNextKey"] = new NumberObject(keys.Count == 0 ? 0 : keys[^1] + 1);
        return rootRef;
    }

    private static ReferenceObject WriteStructureElement(
        DocumentWriter writer,
        StructureElement element,
        ReferenceObject parentRef,
        List<(Page Page, DictionaryObject Node, ReferenceObject Reference)> pageNodes,
        Dictionary<int, List<DocumentObject>> parents)
    {
        var dictionary = new DictionaryObject
        {
            ["Type"] = new NameObject("StructElem"),
            ["S"] = new NameObject(element.Type),
            ["P"] = parentRef,
        };

        if (element.Alt is { } alt)
        {
            dictionary["Alt"] = new StringObject(alt);
        }

        if (element.ActualText is { } actualText)
        {
            dictionary["ActualText"] = new StringObject(actualText);
        }

        // Header cells come from header rows, so their scope is the column
        // below them; PDF/UA (ISO 14289-1, 7.5) requires TH to carry a Scope.
        if (element.Type == "TH")
        {
            dictionary["A"] = new DictionaryObject
            {
                ["O"] = new NameObject("Table"),
                ["Scope"] = new NameObject("Column"),
            };
        }
        var reference = writer.Add(dictionary);

        var kids = new ArrayObject();
        var firstPage = element.Marks.Count > 0 ? element.Marks[0].PageIndex : -1;
        if (firstPage >= 0)
        {
            dictionary["Pg"] = pageNodes[firstPage].Reference;
        }

        foreach (var (pageIndex, mcid) in element.Marks)
        {
            if (pageIndex == firstPage)
            {
                kids.Add(new NumberObject(mcid));
            }
            else
            {
                kids.Add(new DictionaryObject
                {
                    ["Type"] = new NameObject("MCR"),
                    ["Pg"] = pageNodes[pageIndex].Reference,
                    ["MCID"] = new NumberObject(mcid),
                });
            }

            if (!parents.TryGetValue(pageIndex, out var entries))
            {
                entries = [];
                parents[pageIndex] = entries;
            }

            while (entries.Count <= mcid)
            {
                entries.Add(new NullObject());
            }

            entries[mcid] = reference;
        }

        foreach (var child in element.Children)
        {
            kids.Add(WriteStructureElement(writer, child, reference, pageNodes, parents));
        }

        if (kids.Count > 0)
        {
            dictionary["K"] = kids;
        }

        return reference;
    }
}
