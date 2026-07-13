using Radzen.Documents.Pdf.Objects;
using System;
using System.Collections.Generic;
using System.IO;

namespace Radzen.Documents.Pdf.Emit;

// Serializes the changes made to a loaded Document as a PDF incremental update.
// Only the objects the caller mutated since loading are re-emitted, over the
// original bytes, via the low-level Objects.IncrementalUpdateWriter (the same
// mechanism PdfSigner/DssBuilder use). Supported edits: document /Info metadata,
// filled/checked AcroForm fields (tracked by AcroForm.ChangedObjects), and pages
// appended after load. Anything that would need the full-save Emit pipeline to
// re-encode (a generated page, authored content elements, brand-new form fields)
// fails loud rather than silently falling back to a full rewrite.
internal sealed class IncrementalDocumentSaver
{
    private static readonly string[] InfoKeys =
        ["Title", "Author", "Subject", "Keywords", "Creator", "Producer", "CreationDate", "ModDate"];

    private readonly Document doc;

    internal IncrementalDocumentSaver(Document document) => doc = document;

    internal void Save(Stream stream)
    {
        var reader = doc.source!;
        if (doc.FormFields.Count > 0)
        {
            throw new NotSupportedException(
                "Creating new form fields (Document.FormFields) incrementally is not supported. "
                + "Fill existing fields through Document.AcroForm, or use SaveToStream.");
        }

        var writer = new IncrementalUpdateWriter(doc.sourceBytes!, reader);
        var index = reader.BuildObjectNumberIndex();

        var changed = WriteFieldEdits(writer, index);
        changed |= WriteAppendedPages(writer);
        changed |= WriteMetadata(writer, reader);

        if (!changed)
        {
            throw new InvalidOperationException(
                "SaveIncremental found no supported change to write. Edit the metadata, fill a form "
                + "field, or append a page before saving an incremental update.");
        }

        writer.WriteTo(stream);
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

    // Appends the pages added to the model after load. Each new page becomes an
    // appended Page object (with its raw content stream, if any) parented to the
    // existing page-tree root, which is overridden to list the new kids and to
    // carry the grown /Count.
    private bool WriteAppendedPages(IncrementalUpdateWriter writer)
    {
        var reader = doc.source!;
        var newPages = new List<Page>();
        foreach (var page in doc.Pages)
        {
            if (!doc.sourcePages.ContainsKey(page))
            {
                newPages.Add(page);
            }
        }

        if (newPages.Count == 0)
        {
            return false;
        }

        if (doc.sourceCatalog is null
            || !doc.sourceCatalog.TryGetValue("Pages", out var pagesValue)
            || pagesValue is not ReferenceObject pagesRef
            || reader.Resolve(pagesRef) is not DictionaryObject pagesNode)
        {
            throw new NotSupportedException(
                "Cannot append a page incrementally: the loaded catalog has no indirect /Pages tree.");
        }

        var newRefs = new List<ReferenceObject>(newPages.Count);
        foreach (var page in newPages)
        {
            newRefs.Add(AppendPage(writer, page, pagesRef));
        }

        var updated = Copy(pagesNode);
        var kids = new ArrayObject();
        if (reader.GetArray(pagesNode, "Kids") is { } existingKids)
        {
            foreach (var kid in existingKids)
            {
                kids.Add(kid);
            }
        }

        foreach (var reference in newRefs)
        {
            kids.Add(reference);
        }

        updated["Kids"] = kids;

        var count = reader.GetInt(pagesNode, "Count") ?? (kids.Count - newRefs.Count);
        updated["Count"] = new NumberObject(count + newRefs.Count);

        writer.Override(pagesRef.ObjectNumber, updated);
        return true;
    }

    private ReferenceObject AppendPage(IncrementalUpdateWriter writer, Page page, ReferenceObject parent)
    {
        if (page.Generated is not null)
        {
            throw new NotSupportedException(
                "Appending a generated (DocumentBuilder) page incrementally is not supported.");
        }

        var content = page.BuildContent(out var emitter, out var overlay, out var overlayEmitter);
        if (emitter is not null || overlay is not null || overlayEmitter is not null)
        {
            throw new NotSupportedException(
                "Appending a page with authored content elements incrementally is not supported; "
                + "give the appended page raw bytes via Page.SetContent, or use SaveToStream.");
        }

        var node = new DictionaryObject
        {
            ["Type"] = new NameObject("Page"),
            ["Parent"] = parent,
            ["MediaBox"] = new ArrayObject
            {
                new NumberObject(0.0),
                new NumberObject(0.0),
                new NumberObject(page.Width.Point),
                new NumberObject(page.Height.Point),
            },
        };

        if (page.Rotate != 0)
        {
            node["Rotate"] = new NumberObject(page.Rotate);
        }

        if (content is not null)
        {
            node["Contents"] = writer.Add(new StreamObject(content));
        }

        return writer.Add(node);
    }

    // Emits an /Info override (or a new /Info object) only when a modeled metadata
    // field differs from its load-time value. Unmodeled /Info keys and the values
    // of untouched modeled keys are preserved from the original dictionary.
    private bool WriteMetadata(IncrementalUpdateWriter writer, DocumentReader reader)
    {
        var current = Document.InfoSnapshot(doc.Info);
        if (doc.loadedInfoSnapshot is { } snapshot && Same(snapshot, current))
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

        var info = original is null ? new DictionaryObject() : Copy(original);
        ApplyInfo(info);

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

    private void ApplyInfo(DictionaryObject info)
    {
        var meta = doc.Info;
        var values = new string?[]
        {
            meta.Title, meta.Author, meta.Subject, meta.Keywords, meta.Creator, meta.Producer,
            meta.CreationDate is { } created ? DocumentSaver.PdfDate(created) : null,
            meta.ModificationDate is { } modified ? DocumentSaver.PdfDate(modified) : null,
        };

        // A null modeled field is left as the original carried it: DictionaryObject
        // has no key removal, and clearing metadata is out of the incremental MVP.
        for (var i = 0; i < InfoKeys.Length; i++)
        {
            if (values[i] is { } value)
            {
                info[InfoKeys[i]] = new StringObject(value);
            }
        }
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
