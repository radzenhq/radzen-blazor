using System.Collections.Generic;

namespace Radzen.Documents.Pdf.Objects;

// Copies a sub-graph from a loaded DocumentReader into a DocumentWriter, turning
// each indirect object into a freshly numbered writer object while preserving
// inline structure and sharing. Cyclic references are handled by registering an
// object's new reference before its contents are populated.
internal sealed class GraphImporter(DocumentReader reader, DocumentWriter writer)
{
    private readonly Dictionary<DocumentObject, ReferenceObject> map = [];
    private readonly HashSet<DocumentObject> pruned = [];

    // Pins a loaded object to an already-emitted writer reference so it is not
    // re-imported (e.g. page dictionaries emitted before their widget annots).
    public void Seed(DocumentObject loaded, ReferenceObject reference) => map[loaded] = reference;

    // Marks a loaded object so every reference to it imports as null rather than
    // re-materializing it - used to collapse destinations and annotation /P links
    // that point at a page removed before saving.
    public void Prune(DocumentObject loaded) => pruned.Add(loaded);

    public DocumentObject ImportValue(DocumentObject value)
    {
        switch (value)
        {
            case ReferenceObject reference:
                var resolved = reader.Resolve(reference);
                return pruned.Contains(resolved) ? new NullObject() : ImportInstance(resolved);
            case StreamObject:
                return ImportInstance(value);
            case DictionaryObject dictionary:
                var inlineDict = new DictionaryObject();
                foreach (var key in dictionary.Keys)
                {
                    inlineDict[key] = ImportValue(dictionary[key]);
                }

                return inlineDict;
            case ArrayObject array:
                var inlineArray = new ArrayObject();
                foreach (var item in array)
                {
                    inlineArray.Add(ImportValue(item));
                }

                return inlineArray;
            default:
                return value;
        }
    }

    public ReferenceObject ImportInstance(DocumentObject target)
    {
        if (map.TryGetValue(target, out var existing))
        {
            return existing;
        }

        // A resolved scalar (Number/String/Name/Boolean/...) has no sub-graph to
        // populate; register it directly so its value survives instead of becoming
        // an empty dictionary shell.
        if (target is not StreamObject and not ArrayObject and not DictionaryObject)
        {
            var scalar = writer.Add(target);
            map[target] = scalar;
            return scalar;
        }

        DocumentObject shell = target switch
        {
            StreamObject stream => new StreamObject(stream.Data),
            ArrayObject => new ArrayObject(),
            _ => new DictionaryObject(),
        };

        var reference = writer.Add(shell);
        map[target] = reference;
        Populate(shell, target);
        return reference;
    }

    private void Populate(DocumentObject shell, DocumentObject target)
    {
        switch (target)
        {
            case StreamObject stream when shell is StreamObject destination:
                foreach (var key in stream.Dictionary.Keys)
                {
                    destination.Dictionary[key] = ImportValue(stream.Dictionary[key]);
                }

                break;
            case DictionaryObject dictionary when shell is DictionaryObject destination:
                foreach (var key in dictionary.Keys)
                {
                    destination[key] = ImportValue(dictionary[key]);
                }

                break;
            case ArrayObject array when shell is ArrayObject destination:
                foreach (var item in array)
                {
                    destination.Add(ImportValue(item));
                }

                break;
        }
    }
}
