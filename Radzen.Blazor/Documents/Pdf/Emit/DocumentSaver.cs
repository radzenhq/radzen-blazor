using Radzen.Documents.Pdf.Objects;
using Radzen.Documents.Pdf.Objects.Filters;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace Radzen.Documents.Pdf.Emit;

internal sealed class DocumentSaver
{
    internal static readonly string[] InfoKeys =
        ["Title", "Author", "Subject", "Keywords", "Creator", "Producer", "CreationDate", "ModDate"];

    private readonly Document doc;

    internal DocumentSaver(Document document) => doc = document;

    internal void Save(Stream stream)
    {
        if (doc.Xmp.IsModified && (doc.Conformance != PdfAConformance.None || doc.PdfUA))
        {
            throw new InvalidOperationException(
                "Caller-edited XMP cannot be combined with PDF/A or PDF/UA output because conformance metadata has mandatory values. Clear the XMP edits or disable conformance.");
        }

        if (doc.Conformance != PdfAConformance.None || doc.PdfUA)
        {
            new ConformanceWriter(doc).ValidateConformance();
        }

        var writer = new DocumentWriter(stream) { Encryption = doc.Encryption, UseCompressedStreams = doc.CompressOutput };

        var catalog = new DictionaryObject();
        var catalogRef = writer.Add(catalog);

        var pagesNode = new DictionaryObject();
        var pagesRef = writer.Add(pagesNode);

        var loaded = doc.Loaded;
        var importer = loaded?.Source is { } docSource ? new GraphImporter(docSource, writer) : null;
        var appendImporters = new Dictionary<DocumentReader, GraphImporter>();
        var pageNodes = new List<(Page Page, DictionaryObject Node, ReferenceObject Reference)>();

        var fontRefs = new Dictionary<GeneratedFont, DocumentObject>();
        var imageRefs = new Dictionary<GeneratedImage, ReferenceObject>();
        var sharedImages = new Dictionary<ImageXObject, ReferenceObject>(ReferenceEqualityComparer.Instance);

        var kids = new ArrayObject();
        foreach (var page in doc.Pages)
        {
            var pageNode = new DictionaryObject
            {
                ["Type"] = new NameObject("Page"),
                ["Parent"] = pagesRef,
                ["MediaBox"] = PageResourceBuilder.MediaBox(doc, page),
            };

            if (page.CropBoxSet && page.CropBox is { } explicitCropBox)
            {
                pageNode["CropBox"] = PageResourceBuilder.NumberBox(explicitCropBox);
            }
            else if (!page.CropBoxSet && loaded is not null && loaded.SourceCropBoxes.TryGetValue(page, out var cropBox))
            {
                pageNode["CropBox"] = PageResourceBuilder.NumberBox(cropBox);
            }

            WriteAuxiliaryBox(pageNode, page, "BleedBox", page.BleedBox);
            WriteAuxiliaryBox(pageNode, page, "TrimBox", page.TrimBox);
            WriteAuxiliaryBox(pageNode, page, "ArtBox", page.ArtBox);

            if (page.Rotate != 0)
            {
                pageNode["Rotate"] = new NumberObject(page.Rotate);
            }
            else if (loaded is not null && loaded.SourceRotations.TryGetValue(page, out var rotation))
            {
                pageNode["Rotate"] = new NumberObject(rotation);
            }

            var pageRef = writer.Add(pageNode);
            if (importer is not null && loaded!.SourcePages.TryGetValue(page, out var sourceNode))
            {
                importer.Seed(sourceNode, pageRef);
            }

            kids.Add(pageRef);
            pageNodes.Add((page, pageNode, pageRef));
        }

        foreach (var (page, pageNode, _) in pageNodes)
        {
            if (page.Generated is { } generated)
            {
                var generatedRef = writer.Add(FlateFilter.EncodeStream(generated.Content));
                var overlay = page.BuildOverlay();
                if (overlay is null)
                {
                    pageNode["Contents"] = generatedRef;
                }
                else
                {
                    pageNode["Contents"] = new ArrayObject { generatedRef, writer.Add(new StreamObject(overlay.Bytes!)) };
                }

                var resources = PageResourceBuilder.BuildGeneratedResources(writer, generated, fontRefs, imageRefs);
                if (overlay is not null)
                {
                    resources = PageResourceBuilder.OverlayResources(writer, resources, overlay.Resources);
                }

                if (resources is not null)
                {
                    pageNode["Resources"] = resources;
                }

                if (generated.Links.Count > 0)
                {
                    pageNode["Annots"] = NavigationWriter.BuildLinkAnnotations(writer, generated.Links);
                }

                continue;
            }

            HashSet<string>? reservedNames = null;
            if (loaded?.Source is { } reservedSource && loaded.SourceResources.TryGetValue(page, out var reservedFrom))
            {
                reservedNames = PageResourceBuilder.ResourceNames(reservedSource, reservedFrom);
            }
            else if (loaded is not null && loaded.AppendedResources.TryGetValue(page, out var reservedAppend))
            {
                reservedNames = PageResourceBuilder.ResourceNames(reservedAppend.Reader, reservedAppend.Resources);
            }

            var emission = page.BuildContent(reservedNames);
            if (emission.Bytes is not null)
            {
                var contentRef = writer.Add(new StreamObject(emission.Bytes));
                pageNode["Contents"] = emission.Overlay is null
                    ? contentRef
                    : new ArrayObject { contentRef, writer.Add(new StreamObject(emission.Overlay.Bytes!)) };
            }

            var activeResources = emission.Resources.IsEmpty && emission.Overlay is not null
                ? emission.Overlay.Resources
                : emission.Resources;
            var emitted = activeResources.IsEmpty ? null : PageResourceBuilder.BuildResources(writer, activeResources, sharedImages);
            DictionaryObject? merged;
            if (importer is not null && loaded?.Source is { } mergeSource
                && loaded.SourceResources.TryGetValue(page, out var loadedResources))
            {
                merged = PageResourceBuilder.MergeResources(importer, mergeSource, loadedResources, emitted);
            }
            else if (loaded is not null && loaded.AppendedResources.TryGetValue(page, out var appended))
            {
                if (!appendImporters.TryGetValue(appended.Reader, out var appendImporter))
                {
                    appendImporter = new GraphImporter(appended.Reader, writer);
                    appendImporters[appended.Reader] = appendImporter;
                }

                merged = PageResourceBuilder.MergeResources(appendImporter, appended.Reader, appended.Resources, emitted);
            }
            else
            {
                merged = emitted;
            }

            if (merged is not null)
            {
                pageNode["Resources"] = merged;
            }
        }

        pagesNode["Type"] = new NameObject("Pages");
        pagesNode["Kids"] = kids;
        pagesNode["Count"] = new NumberObject(kids.Count);

        catalog["Type"] = new NameObject("Catalog");
        catalog["Pages"] = pagesRef;

        if (doc.Structure is { } structure)
        {
            catalog["MarkInfo"] = new DictionaryObject { ["Marked"] = new BooleanObject(true) };
            catalog["StructTreeRoot"] = StructureWriter.WriteStructureTree(writer, structure, pageNodes, doc.RoleMap);
        }

        var formWriter = new FormWriter(doc);
        var appendedFields = formWriter.AppendForms(pageNodes, appendImporters, writer);
        List<(int PageIndex, ReferenceObject Reference)> createdWidgets =
            doc.FormFields.Count > 0 ? formWriter.WriteCreatedFields(writer, pageNodes, appendedFields) : [];

        if (importer is not null)
        {
            var catalogPreserver = new CatalogPreserver(doc);
            var removed = catalogPreserver.PruneRemovedPages(importer);
            catalogPreserver.PreserveCatalog(importer, catalog, pagesRef);
            formWriter.PreserveForm(new PreserveFormRequest
            {
                Importer = importer,
                Catalog = catalog,
                PageNodes = pageNodes,
                RemovedPages = removed,
                AppendedFields = appendedFields,
                Writer = writer,
            });
        }
        else if (appendedFields.Count > 0)
        {
            catalog["AcroForm"] = writer.Add(formWriter.FieldsForm(appendedFields));
        }

        AnnotationEmitter.Write(writer, importer, pageNodes);

        foreach (var (pageIndex, reference) in createdWidgets)
        {
            var node = pageNodes[pageIndex].Node;
            if (node.TryGetValue("Annots", out var annots) && annots is ArrayObject array)
            {
                array.Add(reference);
            }
            else
            {
                node["Annots"] = new ArrayObject { reference };
            }
        }

        if (doc.Attachments.Count > 0)
        {
            new AttachmentWriter(doc).WriteAttachments(writer, catalog);
        }

        if (doc.Anchors.Count > 0)
        {
            new NavigationWriter(doc).WriteDestinations(writer, catalog, pageNodes);
        }

        if (doc.OutlineChanged && doc.Outline.Count > 0)
        {
            catalog["Outlines"] = new NavigationWriter(doc).WriteOutline(writer, pageNodes);
        }

        if (doc.ViewerPreferences is { } preferences)
        {
            WriteViewerPreferences(catalog, preferences);
        }

        if (doc.PageLabelsChanged && doc.PageLabels.Count > 0)
        {
            catalog["PageLabels"] = PageLabelsWriter.Build([.. doc.PageLabels]);
        }

        if (doc.Conformance != PdfAConformance.None || doc.PdfUA)
        {
            new ConformanceWriter(doc).WriteConformance(writer, catalog);
        }
        else if (doc.Xmp.IsModified)
        {
            if (doc.Xmp.HasPacket)
            {
                catalog["Metadata"] = writer.Add(XmpMetadata.WrapPacket(doc.Xmp.Packet));
            }
        }
        else if (doc.Info.Producer is not null || doc.Info.CreationDate is not null || doc.Info.ModificationDate is not null)
        {
            var xmp = new XmpMetadata
            {
                Info = doc.Info,
                Producer = doc.Info.Producer ?? "Radzen.Documents.Pdf",
                CreationDate = doc.Info.CreationDate,
                ModificationDate = doc.Info.ModificationDate,
            };
            catalog["Metadata"] = writer.Add(xmp.BuildStream());
        }

        writer.Trailer["Root"] = catalogRef;

        // ISO 32000-1 7.5.5: trailer /ID.
        if (doc.Encryption is null && (doc.IncludeDocumentId || doc.Conformance != PdfAConformance.None || doc.PdfUA))
        {
            writer.Trailer["ID"] = BuildDocumentId();
        }

        // ISO 19005-4 6.1.3: PDF/A-4 forbids the trailer /Info key.
        var isPart4 = doc.Conformance is PdfAConformance.PdfA4 or PdfAConformance.PdfA4E or PdfAConformance.PdfA4F;
        var info = isPart4 ? null : BuildInfo(doc.Info, doc.Loaded?.SourceInfo);
        if (info is not null)
        {
            writer.Trailer["Info"] = writer.Add(info);
        }

        // ISO 19005-4 (6.1.2) requires the PDF 2.0 header for PDF/A-4.
        writer.Version = isPart4 ? "2.0" : "1.7";

        writer.Close();
    }

    private static string?[] InfoValues(DocumentInfo meta) =>
    [
        meta.Title,
        meta.Author,
        meta.Subject,
        meta.Keywords,
        meta.Creator,
        meta.Producer,
        meta.CreationDate is { } created ? PdfDate(created) : null,
        meta.ModificationDate is { } modified ? PdfDate(modified) : null,
    ];

    internal static DictionaryObject? BuildInfo(DocumentInfo meta, DictionaryObject? source)
    {
        DictionaryObject? info = null;

        if (source is not null)
        {
            foreach (var pair in source)
            {
                if (Array.IndexOf(InfoKeys, pair.Key) < 0)
                {
                    info ??= new DictionaryObject();
                    info[pair.Key] = pair.Value;
                }
            }
        }

        var values = InfoValues(meta);
        for (var i = 0; i < InfoKeys.Length; i++)
        {
            if (values[i] is { } value)
            {
                info ??= new DictionaryObject();
                info[InfoKeys[i]] = new StringObject(value);
            }
        }

        return info;
    }

    private void WriteAuxiliaryBox(DictionaryObject pageNode, Page page, string key, PdfRect? value)
    {
        if (value is not null)
        {
            PageBoxEmitter.WriteIfPresent(pageNode, key, value);
            return;
        }

        if (doc.Loaded?.Source is { } source && doc.Loaded.SourcePages.TryGetValue(page, out var sourceNode)
            && source.GetArray(sourceNode, key) is { } box && box.Count >= 4)
        {
            pageNode[key] = PageResourceBuilder.NumberBox(box);
        }
    }

    // ISO 32000-1 Table 28: PageLayout/PageMode catalog entries. Table 150: /ViewerPreferences.
    private static void WriteViewerPreferences(DictionaryObject catalog, ViewerPreferences preferences)
    {
        if (preferences.PageLayout is { } layout)
        {
            catalog["PageLayout"] = new NameObject(layout.ToString());
        }

        if (preferences.PageMode is { } mode)
        {
            catalog["PageMode"] = new NameObject(mode.ToString());
        }

        DictionaryObject? dictionary = null;

        void Flag(string key, bool value)
        {
            if (value)
            {
                dictionary ??= new DictionaryObject();
                dictionary[key] = new BooleanObject(true);
            }
        }

        Flag("HideToolbar", preferences.HideToolbar);
        Flag("HideMenubar", preferences.HideMenubar);
        Flag("FitWindow", preferences.FitWindow);
        Flag("CenterWindow", preferences.CenterWindow);
        Flag("DisplayDocTitle", preferences.DisplayDocTitle);

        if (preferences.Direction is { } direction)
        {
            dictionary ??= new DictionaryObject();
            dictionary["Direction"] = new NameObject(direction == PdfReadingDirection.RightToLeft ? "R2L" : "L2R");
        }

        if (dictionary is not null)
        {
            catalog["ViewerPreferences"] = dictionary;
        }
    }

    // ISO 32000-1 7.9.4 date string: D:YYYYMMDDHHmmSS then UTC offset O HH ' mm '.
    internal static string PdfDate(DateTimeOffset value)
    {
        var offset = value.Offset;
        var sign = offset < TimeSpan.Zero ? '-' : '+';
        return string.Create(CultureInfo.InvariantCulture,
            $"D:{value:yyyyMMddHHmmss}{sign}{Math.Abs(offset.Hours):D2}'{Math.Abs(offset.Minutes):D2}'");
    }

    // ISO 32000-1 14.4: both /ID halves equal at creation time.
    private ArrayObject BuildDocumentId()
    {
        var seed = new Radzen.Documents.Crypto.Sha256Hasher();

        void Text(string? value)
        {
            if (value is { Length: > 0 })
            {
                seed.Append(Encoding.UTF8.GetBytes(value));
            }

            seed.Append((byte)0);
        }

        var meta = doc.Info;
        Text(meta.Title);
        Text(meta.Author);
        Text(meta.Subject);
        Text(meta.Keywords);
        Text(meta.Creator);
        Text(meta.Producer);
        Text(meta.CreationDate?.ToString("O", CultureInfo.InvariantCulture));
        Text(meta.ModificationDate?.ToString("O", CultureInfo.InvariantCulture));
        Text(doc.Pages.Count.ToString(CultureInfo.InvariantCulture));

        foreach (var page in doc.Pages)
        {
            Text(page.Width.Point.ToString("R", CultureInfo.InvariantCulture));
            Text(page.Height.Point.ToString("R", CultureInfo.InvariantCulture));
            if (page.GetContent() is { } content)
            {
                seed.Append(content);
            }

            seed.Append((byte)0);
        }

        var hash = seed.Finish();
        var id = Convert.ToHexString(hash, 0, 16);
        return [new StringObject(id), new StringObject(id)];
    }
}
