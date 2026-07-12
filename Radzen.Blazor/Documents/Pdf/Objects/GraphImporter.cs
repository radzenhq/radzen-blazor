using System;
using System.Collections.Generic;

namespace Radzen.Documents.Pdf.Objects;

// Copies a sub-graph from a loaded DocumentReader into a DocumentWriter, turning
// each indirect object into a freshly numbered writer object while preserving
// inline structure and sharing. Cyclic references are handled by registering an
// object's new reference before its contents are populated.
internal sealed class GraphImporter(DocumentReader reader, DocumentWriter writer)
{
    private readonly Dictionary<DocumentObject, ReferenceObject> map = [];
    private readonly Dictionary<DocumentObject, DocumentObject> instances = [];
    private readonly HashSet<DocumentObject> fieldRoots = [];
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
            instances[target] = target;
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
        instances[target] = shell;
        Populate(shell, target);
        return reference;
    }

    // Imports the AcroForm field tree a widget annotation belongs to and returns
    // the tree root - the object to list in the catalog /AcroForm /Fields. A
    // merged field/widget (no /Parent) is its own root; a kid widget contributes
    // its top-most /Parent, so a nested tree keeps its /Parent<->/Kids structure
    // and the partial /T names still combine into fully qualified names. Each
    // root is returned once per importer so /Fields never lists it twice.
    // `field` is the imported (destination-space, still mutable) root dictionary
    // and `name` its partial /T, for collision handling by the caller. Returns
    // false for non-field annotations and for roots already returned.
    public bool TryImportFieldRoot(DictionaryObject annotation, out ReferenceObject reference, out DictionaryObject? field, out string? name)
    {
        reference = null!;
        field = null;
        name = null;

        if (!IsWidget(annotation) || (!annotation.ContainsKey("FT") && !annotation.ContainsKey("Parent")))
        {
            return false;
        }

        var root = annotation;
        for (var depth = 0; depth < 32 && root.TryGetValue("Parent", out var parent)
            && reader.Resolve(parent!) is DictionaryObject next; depth++)
        {
            root = next;
        }

        if (!fieldRoots.Add(root))
        {
            return false;
        }

        reference = ImportInstance(root);
        field = instances.TryGetValue(root, out var shell) ? shell as DictionaryObject : null;
        name = root.TryGetValue("T", out var title) && reader.Resolve(title!) is StringObject text
            ? text.Value
            : null;
        return true;
    }

    // Folds a source /AcroForm's form-wide defaults into an assembled destination
    // form dictionary: /DR resource categories are unioned entry-by-entry with
    // destination entries winning, /DA is adopted only when the destination has
    // none, and /NeedAppearances becomes true when either side requires it.
    // /Fields is never touched. The destination /DR must be an inline dictionary
    // (as this method itself creates) for later sources to union into it.
    public void MergeFormDefaults(DictionaryObject form, DictionaryObject sourceForm)
    {
        if (!form.ContainsKey("DA") && sourceForm.TryGetValue("DA", out var da))
        {
            form["DA"] = ImportValue(da!);
        }

        if (sourceForm.TryGetValue("NeedAppearances", out var needed)
            && reader.Resolve(needed!) is BooleanObject { Value: true }
            && (!form.TryGetValue("NeedAppearances", out var existing)
                || existing is not BooleanObject { Value: true }))
        {
            form["NeedAppearances"] = new BooleanObject(true);
        }

        if (!sourceForm.TryGetValue("DR", out var drObject)
            || reader.Resolve(drObject!) is not DictionaryObject sourceResources)
        {
            return;
        }

        if (!form.ContainsKey("DR"))
        {
            form["DR"] = ImportValue(sourceResources);
            return;
        }

        if (form["DR"] is not DictionaryObject resources)
        {
            return;
        }

        foreach (var category in sourceResources.Keys)
        {
            if (!resources.TryGetValue(category, out var destinationCategory))
            {
                resources[category] = ImportValue(reader.Resolve(sourceResources[category]));
            }
            else if (destinationCategory is DictionaryObject entries
                && reader.Resolve(sourceResources[category]) is DictionaryObject sourceEntries)
            {
                foreach (var entry in sourceEntries.Keys)
                {
                    if (!entries.ContainsKey(entry))
                    {
                        entries[entry] = ImportValue(sourceEntries[entry]);
                    }
                }
            }
        }
    }

    // Registers a root field's top-level name and renames the imported field when
    // the name is already taken: the first collision becomes "name_2", then
    // "name_3", and so on - the smallest unused suffix, so a given import order
    // always yields the same names and no colliding field is ever dropped.
    public static void DisambiguateFieldName(DictionaryObject field, string? name, HashSet<string> usedNames)
    {
        if (name is null || usedNames.Add(name))
        {
            return;
        }

        var index = 2;
        string candidate;
        do
        {
            candidate = name + "_" + index++;
        }
        while (!usedNames.Add(candidate));

        field["T"] = new StringObject(candidate);
    }

    private bool IsWidget(DictionaryObject annotation)
        => annotation.TryGetValue("Subtype", out var subtype)
            && reader.Resolve(subtype!) is NameObject name
            && string.Equals(name.Value, "Widget", StringComparison.Ordinal);

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
