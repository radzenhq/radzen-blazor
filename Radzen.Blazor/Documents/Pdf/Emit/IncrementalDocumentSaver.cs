using Radzen.Documents.Pdf.Objects;
using System;
using System.Collections.Generic;
using System.IO;

namespace Radzen.Documents.Pdf.Emit;

internal sealed class IncrementalDocumentSaver
{
    private readonly Document doc;
    private readonly Dictionary<DocumentReader, GraphImporter> appendImporters = [];

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

        if (doc.Attachments.IsModified)
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
            if (emission.IsEmitted || emission.Overlay is not null || page.ContentReplaced)
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
        var reader = doc.Loaded!.Source!;
        var result = new Dictionary<Page, ReferenceObject>();
        foreach (var pair in doc.Loaded!.SourcePages)
        {
            if (!index.TryGetValue(pair.Value, out var number))
            {
                throw Unsupported("A page whose dictionary is not an indirect object");
            }

            // ISO 32000-1 7.3.10: a reference matches on number AND generation.
            result[pair.Key] = new ReferenceObject(number, reader.GenerationOf(number));
        }

        return result;
    }

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
            var updated = pagesNode.Copy();
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
                ? sourceNode.Copy("CropBox")
                : sourceNode.Copy();
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
        var kept = new HashSet<Page>(doc.Pages);
        foreach (var page in loaded.LoadedPages)
        {
            if (kept.Contains(page) || reader.GetArray(loaded.SourcePages[page], "Annots") is not { } annotations)
            {
                continue;
            }

            foreach (var value in annotations)
            {
                if (reader.AsDictionary(value) is { } annotation && FormField.IsWidget(reader, annotation))
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
        };

        PageResourceBuilder.EmitPageGeometry(doc, page, node);

        if (emission.Bytes is not null)
        {
            node["Contents"] = writer.Add(new StreamObject(emission.Bytes));
        }

        if (doc.Loaded!.AppendedResources.TryGetValue(page, out var appended))
        {
            if (!appendImporters.TryGetValue(appended.Reader, out var importer))
            {
                importer = new GraphImporter(appended.Reader, writer);
                appendImporters[appended.Reader] = importer;
            }

            node["Resources"] = PageResourceBuilder.MergeResources(importer, appended.Reader, appended.Resources, null);
        }

        var reference = writer.Add(node);
        return (reference, node);
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
                    : source.Copy();
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

    private bool WriteMetadata(IncrementalUpdateWriter writer, DocumentReader reader)
    {
        if (!doc.Info.IsModified)
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

        var info = DocumentSaver.BuildInfo(doc.Info, original) ?? new DictionaryObject();

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

    private static DocumentObject HoistStreams(DocumentObject value, IncrementalUpdateWriter writer)
        => CosGraphRewriter.Rewrite(value, node => node is StreamObject stream ? writer.Add(stream) : null);
}
