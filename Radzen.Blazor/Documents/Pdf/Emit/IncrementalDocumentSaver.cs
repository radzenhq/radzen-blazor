using Radzen.Documents.Pdf.Objects;
using System;
using System.Collections.Generic;
using System.IO;

namespace Radzen.Documents.Pdf.Emit;

// Serializes the changes made to a loaded Document as a PDF incremental update.
// Only the objects the caller mutated since loading are re-emitted, over the
// original bytes, via the low-level Objects.IncrementalUpdateWriter (the same
// mechanism PdfSigner/DssBuilder use). Supported edits: document /Info metadata,
// filled/checked AcroForm fields, page and annotation dictionary edits, and a
// directly rooted page-tree reorder. Anything that would need the full-save Emit
// pipeline to re-encode fails loud rather than silently falling back to a rewrite.
internal sealed class IncrementalDocumentSaver
{
    private static readonly string[] InfoKeys =
        ["Title", "Author", "Subject", "Keywords", "Creator", "Producer", "CreationDate", "ModDate"];

    private readonly Document doc;

    internal IncrementalDocumentSaver(Document document) => doc = document;

    internal void Save(Stream stream)
    {
        var loaded = doc.Loaded!;
        var reader = loaded.Source!;
        ValidateCapabilities();
        if (doc.FormFields.Count > 0)
        {
            throw new NotSupportedException(
                "Creating new form fields (Document.FormFields) incrementally is not supported. "
                + "Fill existing fields through Document.AcroForm, or use SaveToStream.");
        }

        var writer = new IncrementalUpdateWriter(loaded.SourceBytes!, reader);
        var index = reader.BuildObjectNumberIndex();
        var pageReferences = SourcePageReferences(index);
        var pageNodes = new Dictionary<Page, DictionaryObject>(loaded.SourcePages);
        var pageOverrides = new SortedDictionary<int, DictionaryObject>();

        var changed = WriteFieldEdits(writer, index);
        changed |= WritePageEdits(writer, index, pageReferences, pageNodes, pageOverrides);
        changed |= WriteAnnotationEdits(writer, pageReferences, pageNodes, pageOverrides);
        foreach (var pair in pageOverrides)
        {
            writer.Override(pair.Key, pair.Value);
        }

        changed |= WriteMetadata(writer, reader);

        if (!changed)
        {
            throw new InvalidOperationException(
                "SaveIncremental found no supported change to write. Edit metadata, fill a form field, "
                + "edit annotations or page boxes, or change the page order before saving an incremental update.");
        }

        writer.WriteTo(stream);
    }

    private void ValidateCapabilities()
    {
        if (doc.Encryption is not null)
        {
            throw Unsupported("Changing encryption");
        }

        if (doc.Xmp.IsModified)
        {
            throw Unsupported("Editing XMP metadata");
        }

        if (doc.ViewerPreferences is not null || doc.OutlineChanged || doc.PageLabelsChanged)
        {
            throw Unsupported("Editing catalog-level document features");
        }

        if (doc.Loaded!.LoadedAttachmentSnapshot is not { } attachments
            || !AttachmentSnapshot.Matches(attachments, doc.Attachments))
        {
            throw Unsupported("Editing attachments");
        }

        foreach (var page in doc.Pages)
        {
            if (!doc.Loaded!.SourcePages.ContainsKey(page))
            {
                continue;
            }

            var emission = page.BuildContent();
            doc.Loaded.SourceContents.TryGetValue(page, out var original);
            if (emission.IsEmitted || emission.Overlay is not null || !Same(original, emission.Bytes))
            {
                throw Unsupported("Editing loaded page content, including redaction and text replacement");
            }

            var settings = doc.Loaded.LoadedPageSettings[page];
            if (settings.Bleed != page.BleedBox || settings.Trim != page.TrimBox
                || settings.Art != page.ArtBox || settings.Rotate != page.Rotate)
            {
                throw Unsupported("Editing page rotation, bleed, trim, or art boxes");
            }
        }
    }

    private static NotSupportedException Unsupported(string edit)
        => new($"{edit} cannot be saved incrementally. Use SaveToStream for a full save.");

    private Dictionary<Page, ReferenceObject> SourcePageReferences(IReadOnlyDictionary<DocumentObject, int> index)
    {
        var result = new Dictionary<Page, ReferenceObject>();
        foreach (var pair in doc.Loaded!.SourcePages)
        {
            if (!index.TryGetValue(pair.Value, out var number))
            {
                throw Unsupported("A page whose dictionary is not an indirect object");
            }

            result[pair.Key] = new ReferenceObject(number, 0);
        }

        return result;
    }

    // Re-emits each loaded field/widget/form dictionary a caller mutated through
    // AcroForm, hoisting any newly-built inline appearance stream to its own
    // appended object. Overrides are applied in object-number order so the update
    // is deterministic regardless of the tracking set's iteration order.
    private bool WriteFieldEdits(IncrementalUpdateWriter writer, IReadOnlyDictionary<DocumentObject, int> index)
    {
        if (doc.AcroForm is not { } form || form.ChangedObjects.Count == 0)
        {
            return false;
        }

        var overrides = new SortedDictionary<int, DictionaryObject>();
        foreach (var changed in form.ChangedObjects)
        {
            if (changed is not DictionaryObject dictionary)
            {
                continue;
            }

            if (!index.TryGetValue(changed, out var number))
            {
                throw new NotSupportedException(
                    "A changed form object is not a loaded indirect object and cannot be updated incrementally.");
            }

            overrides[number] = dictionary;
        }

        foreach (var pair in overrides)
        {
            writer.Override(pair.Key, (DictionaryObject)HoistStreams(pair.Value, writer));
        }

        return overrides.Count > 0;
    }

    private bool WritePageEdits(
        IncrementalUpdateWriter writer,
        IReadOnlyDictionary<DocumentObject, int> index,
        Dictionary<Page, ReferenceObject> pageReferences,
        Dictionary<Page, DictionaryObject> pageNodes,
        SortedDictionary<int, DictionaryObject> pageOverrides)
    {
        var loaded = doc.Loaded!;
        var reader = loaded.Source!;
        ValidateRemovedPages(reader, loaded);
        if (loaded.SourceCatalog is null
            || !loaded.SourceCatalog.TryGetValue("Pages", out var pagesValue)
            || pagesValue is not ReferenceObject pagesRef
            || reader.Resolve(pagesRef) is not DictionaryObject pagesNode)
        {
            throw new NotSupportedException(
                "Cannot edit pages incrementally: the loaded catalog has no indirect /Pages tree. Use SaveToStream.");
        }

        var changed = false;
        foreach (var page in doc.Pages)
        {
            if (loaded.SourcePages.ContainsKey(page))
            {
                continue;
            }

            var appended = AppendPage(writer, page, pagesRef);
            pageReferences[page] = appended.Reference;
            pageNodes[page] = appended.Node;
            changed = true;
        }

        var appendOnly = doc.Pages.Count >= loaded.LoadedPages.Count;
        for (var i = 0; appendOnly && i < loaded.LoadedPages.Count; i++)
        {
            appendOnly = ReferenceEquals(doc.Pages[i], loaded.LoadedPages[i]);
        }

        for (var i = loaded.LoadedPages.Count; appendOnly && i < doc.Pages.Count; i++)
        {
            appendOnly = !loaded.SourcePages.ContainsKey(doc.Pages[i]);
        }

        var unchanged = doc.Pages.Count == loaded.LoadedPages.Count;
        for (var i = 0; unchanged && i < loaded.LoadedPages.Count; i++)
        {
            unchanged = ReferenceEquals(doc.Pages[i], loaded.LoadedPages[i]);
        }

        if (!unchanged)
        {
            var updated = Copy(pagesNode);
            var kids = new ArrayObject();
            if (appendOnly)
            {
                if (reader.GetArray(pagesNode, "Kids") is { } existingKids)
                {
                    foreach (var kid in existingKids)
                    {
                        kids.Add(kid);
                    }
                }

                for (var i = loaded.LoadedPages.Count; i < doc.Pages.Count; i++)
                {
                    kids.Add(pageReferences[doc.Pages[i]]);
                }

                var count = reader.GetInt(pagesNode, "Count") ?? (kids.Count - (doc.Pages.Count - loaded.LoadedPages.Count));
                updated["Count"] = new NumberObject(count + doc.Pages.Count - loaded.LoadedPages.Count);
            }
            else
            {
                ValidateFlatPageTree(reader, pagesRef, pagesNode, loaded.LoadedPages, pageReferences);
                foreach (var page in doc.Pages)
                {
                    kids.Add(pageReferences[page]);
                }

                updated["Count"] = new NumberObject(kids.Count);
            }

            updated["Kids"] = kids;
            writer.Override(pagesRef.ObjectNumber, updated);
            changed = true;
        }

        foreach (var page in doc.Pages)
        {
            if (!loaded.SourcePages.TryGetValue(page, out var sourceNode)
                || (!page.MediaBoxSet && !page.CropBoxSet))
            {
                continue;
            }

            var number = index[sourceNode];
            if (page.CropBoxSet && page.CropBox is null && !sourceNode.ContainsKey("CropBox")
                && loaded.SourceCropBoxes.ContainsKey(page))
            {
                throw Unsupported("Removing an inherited crop box");
            }

            var updated = page.CropBoxSet && page.CropBox is null
                ? CopyExcept(sourceNode, "CropBox")
                : Copy(sourceNode);
            if (page.MediaBoxSet)
            {
                updated["MediaBox"] = PageResourceBuilder.NumberBox(page.MediaBox);
            }

            if (page.CropBoxSet && page.CropBox is { } cropBox)
            {
                updated["CropBox"] = PageResourceBuilder.NumberBox(cropBox);
            }

            pageOverrides[number] = updated;
            changed = true;
        }

        return changed;
    }

    private void ValidateRemovedPages(DocumentReader reader, LoadedState loaded)
    {
        foreach (var page in loaded.LoadedPages)
        {
            var retained = false;
            foreach (var current in doc.Pages)
            {
                retained |= ReferenceEquals(page, current);
            }

            if (retained || reader.GetArray(loaded.SourcePages[page], "Annots") is not { } annotations)
            {
                continue;
            }

            foreach (var value in annotations)
            {
                if (reader.AsDictionary(value) is { } annotation
                    && reader.GetName(annotation, "Subtype") == "Widget")
                {
                    throw Unsupported("Removing a page that owns form widgets");
                }
            }
        }
    }

    private static void ValidateFlatPageTree(
        DocumentReader reader,
        ReferenceObject pagesReference,
        DictionaryObject pagesNode,
        IReadOnlyList<Page> loadedPages,
        IReadOnlyDictionary<Page, ReferenceObject> pageReferences)
    {
        if (reader.GetArray(pagesNode, "Kids") is not { } kids || kids.Count != loadedPages.Count)
        {
            throw Unsupported("Changing a nested or shared page tree");
        }

        var seen = new HashSet<int>();
        foreach (var kid in kids)
        {
            if (kid is not ReferenceObject reference || !seen.Add(reference.ObjectNumber)
                || reader.AsDictionary(kid) is not { } dictionary || reader.GetName(dictionary, "Type") != "Page"
                || !dictionary.TryGetValue("Parent", out var parent) || parent is not ReferenceObject parentReference
                || parentReference.ObjectNumber != pagesReference.ObjectNumber)
            {
                throw Unsupported("Changing a nested or shared page tree");
            }
        }

        foreach (var page in loadedPages)
        {
            var reference = pageReferences[page];
            if (!seen.Contains(reference.ObjectNumber))
            {
                throw Unsupported("Changing a nested or shared page tree");
            }
        }
    }

    private (ReferenceObject Reference, DictionaryObject Node) AppendPage(
        IncrementalUpdateWriter writer,
        Page page,
        ReferenceObject parent)
    {
        if (page.Generated is not null)
        {
            throw new NotSupportedException(
                "Appending a generated (DocumentBuilder) page incrementally is not supported.");
        }

        var emission = page.BuildContent();
        if (emission.IsEmitted || emission.Overlay is not null)
        {
            throw new NotSupportedException(
                "Appending a page with authored content elements incrementally is not supported; "
                + "give the appended page raw bytes via Page.SetContent, or use SaveToStream.");
        }

        var node = new DictionaryObject
        {
            ["Type"] = new NameObject("Page"),
            ["Parent"] = parent,
            ["MediaBox"] = page.MediaBoxSet
                ? PageResourceBuilder.NumberBox(page.MediaBox)
                :
                [
                    new NumberObject(0.0),
                    new NumberObject(0.0),
                    new NumberObject(page.Width.Point),
                    new NumberObject(page.Height.Point),
                ],
        };

        if (page.CropBoxSet && page.CropBox is { } cropBox)
        {
            node["CropBox"] = PageResourceBuilder.NumberBox(cropBox);
        }

        WriteBox(node, "BleedBox", page.BleedBox);
        WriteBox(node, "TrimBox", page.TrimBox);
        WriteBox(node, "ArtBox", page.ArtBox);

        if (page.Rotate != 0)
        {
            node["Rotate"] = new NumberObject(page.Rotate);
        }

        if (emission.Bytes is not null)
        {
            node["Contents"] = writer.Add(new StreamObject(emission.Bytes));
        }

        var reference = writer.Add(node);
        return (reference, node);
    }

    private static void WriteBox(DictionaryObject node, string key, Rect? box)
    {
        if (box is { } value)
        {
            node[key] = PageResourceBuilder.NumberBox(value);
        }
    }

    private bool WriteAnnotationEdits(
        IncrementalUpdateWriter writer,
        IReadOnlyDictionary<Page, ReferenceObject> pageReferences,
        IReadOnlyDictionary<Page, DictionaryObject> pageNodes,
        SortedDictionary<int, DictionaryObject> pageOverrides)
    {
        var pages = new List<(Page Page, DictionaryObject Node, ReferenceObject Reference)>();
        foreach (var page in doc.Pages)
        {
            pages.Add((page, pageNodes[page], pageReferences[page]));
        }

        var changed = false;
        for (var i = 0; i < pages.Count; i++)
        {
            var (page, source, reference) = pages[i];
            if (!page.Annotations.HasChanges)
            {
                continue;
            }

            var annotations = AnnotationEmitter.BuildIncremental(writer, page.Annotations, pages, i);
            if (doc.Loaded!.SourcePages.ContainsKey(page))
            {
                var updated = pageOverrides.TryGetValue(reference.ObjectNumber, out var existing)
                    ? existing
                    : Copy(source);
                updated["Annots"] = annotations.Count == 0 ? new NullObject() : annotations;
                pageOverrides[reference.ObjectNumber] = updated;
            }
            else
            {
                var node = pages[i].Node;
                node["Annots"] = annotations.Count == 0 ? new NullObject() : annotations;
            }

            changed = true;
        }

        return changed;
    }

    // Emits an /Info override (or a new /Info object) only when a modeled metadata
    // field differs from its load-time value. Unmodeled /Info keys and the values
    // of untouched modeled keys are preserved from the original dictionary.
    private bool WriteMetadata(IncrementalUpdateWriter writer, DocumentReader reader)
    {
        var current = Document.InfoSnapshot(doc.Info);
        if (doc.Loaded!.LoadedInfoSnapshot is { } snapshot && Same(snapshot, current))
        {
            return false;
        }

        DictionaryObject? original = null;
        int? number = null;
        if (reader.Trailer.TryGetValue("Info", out var infoValue) && infoValue is not null)
        {
            if (infoValue is ReferenceObject infoRef)
            {
                number = infoRef.ObjectNumber;
            }

            original = reader.AsDictionary(infoValue);
        }

        var info = BuildInfo(original);

        if (number is int existing)
        {
            writer.Override(existing, info);
        }
        else
        {
            writer.Trailer["Info"] = writer.Add(info);
        }

        return true;
    }

    // The emitted modeled keys are exactly the non-null modeled values, matching what a
    // full save writes (DocumentSaver.BuildInfo): a field cleared to null is omitted from
    // the override, which removes it. Unmodeled keys carry over from the original.
    private DictionaryObject BuildInfo(DictionaryObject? original)
    {
        var meta = doc.Info;
        var values = new string?[]
        {
            meta.Title, meta.Author, meta.Subject, meta.Keywords, meta.Creator, meta.Producer,
            meta.CreationDate is { } created ? DocumentSaver.PdfDate(created) : null,
            meta.ModificationDate is { } modified ? DocumentSaver.PdfDate(modified) : null,
        };

        var info = new DictionaryObject();
        if (original is not null)
        {
            foreach (var pair in original)
            {
                if (Array.IndexOf(InfoKeys, pair.Key) < 0)
                {
                    info[pair.Key] = pair.Value;
                }
            }
        }

        for (var i = 0; i < InfoKeys.Length; i++)
        {
            if (values[i] is { } value)
            {
                info[InfoKeys[i]] = new StringObject(value);
            }
        }

        return info;
    }

    // Deep-copies a dictionary, replacing any inline stream (an appearance stream a
    // form mutator built and hung directly under the field) with a freshly appended
    // indirect object, since a PDF stream is only legal as an indirect object.
    // References and scalars pass through unchanged - a loaded dictionary's own
    // sub-objects stay referenced in the preserved original revision.
    private static DocumentObject HoistStreams(DocumentObject value, IncrementalUpdateWriter writer)
    {
        switch (value)
        {
            case StreamObject stream:
                return writer.Add(stream);
            case DictionaryObject dictionary:
                var copiedDictionary = new DictionaryObject();
                foreach (var key in dictionary.Keys)
                {
                    copiedDictionary[key] = HoistStreams(dictionary[key], writer);
                }

                return copiedDictionary;
            case ArrayObject array:
                var copiedArray = new ArrayObject();
                foreach (var item in array)
                {
                    copiedArray.Add(HoistStreams(item, writer));
                }

                return copiedArray;
            default:
                return value;
        }
    }

    private static DictionaryObject Copy(DictionaryObject source)
    {
        var copy = new DictionaryObject();
        foreach (var pair in source)
        {
            copy[pair.Key] = pair.Value;
        }

        return copy;
    }

    private static DictionaryObject CopyExcept(DictionaryObject source, string omittedKey)
    {
        var copy = new DictionaryObject();
        foreach (var pair in source)
        {
            if (!string.Equals(pair.Key, omittedKey, StringComparison.Ordinal))
            {
                copy[pair.Key] = pair.Value;
            }
        }

        return copy;
    }

    private static bool Same(byte[]? a, byte[]? b)
    {
        if (a is null || b is null)
        {
            return a is null && b is null;
        }

        return a.AsSpan().SequenceEqual(b);
    }

    private static bool Same(string?[] a, string?[] b)
    {
        if (a.Length != b.Length)
        {
            return false;
        }

        for (var i = 0; i < a.Length; i++)
        {
            if (!string.Equals(a[i], b[i], StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }
}
