using Radzen.Documents.Pdf.Objects;
using System.Collections.Generic;
using Radzen.Documents.Geometry;

namespace Radzen.Documents.Pdf.Emit;

internal static class StructureWriter
{
    public static ReferenceObject WriteStructureTree(
        DocumentWriter writer,
        StructureElement structure,
        List<(Page Page, DictionaryObject Node, ReferenceObject Reference)> pageNodes,
        RoleMap roleMap)
    {
        var root = new DictionaryObject { ["Type"] = new NameObject("StructTreeRoot") };
        var rootRef = writer.Add(root);

        if (roleMap.Count > 0)
        {
            var map = new DictionaryObject();
            foreach (var (role, structureType) in roleMap.Entries)
            {
                map[role] = new NameObject(structureType);
            }

            root["RoleMap"] = map;
        }

        var parents = new Dictionary<int, List<DocumentObject>>();
        var annotationKey = pageNodes.Count;
        root["K"] = WriteStructureElement(writer, structure, rootRef, pageNodes, parents, ref annotationKey);

        var keys = new List<int>(parents.Keys);
        keys.Sort();

        var nums = new ArrayObject();
        foreach (var key in keys)
        {
            var entries = parents[key];
            if (key < pageNodes.Count)
            {
                var array = new ArrayObject();
                foreach (var entry in entries)
                {
                    array.Add(entry);
                }

                nums.Add(new NumberObject(key));
                nums.Add(writer.Add(array));
                pageNodes[key].Node["StructParents"] = new NumberObject(key);
            }
            else
            {
                // ISO 32000-1 14.7.4.4: the parent tree value of an object key is the owning element itself.
                nums.Add(new NumberObject(key));
                nums.Add(entries[0]);
            }
        }

        root["ParentTree"] = writer.Add(new DictionaryObject { ["Nums"] = nums });
        root["ParentTreeNextKey"] = new NumberObject(keys.Count == 0 ? 0 : keys[^1] + 1);
        return rootRef;
    }

    // ISO 14289-1 7.5: a TH must carry a Scope; ISO 32000-1 Table 345 defines the permitted values.
    private static string? ScopeName(SemanticHeaderScope scope)
        => scope switch
        {
            SemanticHeaderScope.ColumnHeader => "Column",
            SemanticHeaderScope.RowHeader => "Row",
            SemanticHeaderScope.ColumnAndRowHeader => "Both",
            _ => null,
        };

    private static ReferenceObject WriteStructureElement(
        DocumentWriter writer,
        StructureElement element,
        ReferenceObject parentRef,
        List<(Page Page, DictionaryObject Node, ReferenceObject Reference)> pageNodes,
        Dictionary<int, List<DocumentObject>> parents,
        ref int annotationKey)
    {
        var dictionary = new DictionaryObject
        {
            ["Type"] = new NameObject("StructElem"),
            ["S"] = new NameObject(element.Type),
            ["P"] = parentRef,
        };

        if (element.Alt is { } alt)
        {
            dictionary["Alt"] = StringObject.FromText(alt);
        }

        if (element.ActualText is { } actualText)
        {
            dictionary["ActualText"] = StringObject.FromText(actualText);
        }

        if (ScopeName(element.HeaderScope) is { } scope)
        {
            dictionary["A"] = new DictionaryObject
            {
                ["O"] = new NameObject("Table"),
                ["Scope"] = new NameObject(scope),
            };
        }

        var reference = writer.Add(dictionary);

        var kids = new ArrayObject();
        var firstPage = element.Marks.Count > 0 ? element.Marks[0].PageIndex : -1;
        if (firstPage >= 0)
        {
            dictionary["Pg"] = pageNodes[firstPage].Reference;
        }

        foreach (var kid in element.Kids)
        {
            if (kid.Child is { } child)
            {
                kids.Add(WriteStructureElement(writer, child, reference, pageNodes, parents, ref annotationKey));
                continue;
            }

            if (kid.PageIndex == firstPage)
            {
                kids.Add(new NumberObject(kid.Mcid));
            }
            else
            {
                kids.Add(new DictionaryObject
                {
                    ["Type"] = new NameObject("MCR"),
                    ["Pg"] = pageNodes[kid.PageIndex].Reference,
                    ["MCID"] = new NumberObject(kid.Mcid),
                });
            }

            if (!parents.TryGetValue(kid.PageIndex, out var entries))
            {
                entries = [];
                parents[kid.PageIndex] = entries;
            }

            while (entries.Count <= kid.Mcid)
            {
                entries.Add(new NullObject());
            }

            entries[kid.Mcid] = reference;
        }

        // ISO 32000-1 14.7.4.3: an annotation joins the structure tree through an object reference (OBJR)
        // kid and points back into the parent tree through its own /StructParent key.
        foreach (var annotation in element.Annotations)
        {
            kids.Add(new DictionaryObject
            {
                ["Type"] = new NameObject("OBJR"),
                ["Pg"] = pageNodes[annotation.PageIndex].Reference,
                ["Obj"] = annotation.Reference,
            });

            annotation.Annotation["StructParent"] = new NumberObject(annotationKey);
            parents[annotationKey] = [reference];
            annotationKey++;

            if (firstPage < 0)
            {
                firstPage = annotation.PageIndex;
                dictionary["Pg"] = pageNodes[annotation.PageIndex].Reference;
            }
        }

        if (kids.Count > 0)
        {
            dictionary["K"] = kids;
        }

        return reference;
    }
}
