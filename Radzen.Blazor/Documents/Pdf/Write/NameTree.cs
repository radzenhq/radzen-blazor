using System.Collections.Generic;
using Radzen.Documents.Pdf.Objects;

namespace Radzen.Documents.Pdf.Write;

internal static class NameTree
{
    public static void AddCategory(
        DocumentWriter writer,
        DictionaryObject catalog,
        string category,
        IEnumerable<(string Name, DocumentObject Value)> entries)
    {
        var names = new ArrayObject();
        foreach (var (name, value) in entries)
        {
            names.Add(new StringObject(name));
            names.Add(value);
        }

        var tree = catalog.TryGetValue("Names", out var existing) && existing is DictionaryObject dictionary
            ? dictionary
            : new DictionaryObject();
        tree[category] = writer.Add(new DictionaryObject { ["Names"] = names });
        catalog["Names"] = tree;
    }
}
