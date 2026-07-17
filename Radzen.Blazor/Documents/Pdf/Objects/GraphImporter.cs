using System.Collections.Generic;

namespace Radzen.Documents.Pdf.Objects;

// A cycle in the source graph terminates only because ImportInstance registers an
// object's new reference before Populate walks its contents.
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

        // A scalar has no sub-graph, so the shell/Populate path below would emit an
        // empty dictionary in place of its value.
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

    // Importing the top-most /Parent rather than the widget keeps a nested tree's
    // /Parent<->/Kids structure, so partial /T names still combine into fully
    // qualified names. Roots are returned once each: /Fields must not list one twice.
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
        foreach (var ancestor in FormField.ParentChain(reader, annotation))
        {
            root = ancestor;
        }

        if (!fieldRoots.Add(root))
        {
            return false;
        }

        reference = ImportInstance(root);
        field = instances.TryGetValue(root, out var shell) ? shell as DictionaryObject : null;
        name = reader.GetString(root, "T");
        return true;
    }

    // The destination /DR must be an inline dictionary (as ImportValue leaves it here)
    // for later sources to union into it; a /DR that arrives as a reference is skipped.
    public void MergeFormDefaults(DictionaryObject form, DictionaryObject sourceForm)
    {
        if (!form.ContainsKey("DA") && sourceForm.TryGetValue("DA", out var da))
        {
            form["DA"] = ImportValue(da!);
        }

        if (reader.GetBool(sourceForm, "NeedAppearances") == true
            && (!form.TryGetValue("NeedAppearances", out var existing)
                || existing is not BooleanObject { Value: true }))
        {
            form["NeedAppearances"] = new BooleanObject(true);
        }

        if (reader.GetDictionary(sourceForm, "DR") is not { } sourceResources)
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
                && reader.AsDictionary(sourceResources[category]) is { } sourceEntries)
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

    // Renaming rather than dropping a colliding field, by smallest unused suffix, is
    // what makes a given import order reproduce identical names.
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

    private bool IsWidget(DictionaryObject annotation) => FormField.IsWidget(reader, annotation);

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
