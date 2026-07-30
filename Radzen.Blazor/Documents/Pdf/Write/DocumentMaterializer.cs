using Radzen.Documents.Pdf.Objects;
using Radzen.Documents.Pdf.Objects.Filters;
using Radzen.Documents.Pdf.Emission;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace Radzen.Documents.Pdf.Write;

internal sealed class DocumentMaterializer
{
    private readonly PortableDocument doc;

    internal DocumentMaterializer(PortableDocument document) => doc = document;

    internal DocumentObjectGraph Materialize()
    {
        if (doc.Xmp.IsModified && (doc.Conformance != PdfAConformance.None || doc.IsPdfUa))
        {
            throw new InvalidOperationException(
                "Caller-edited XMP cannot be combined with PDF/A or PDF/UA output because conformance metadata has mandatory values. Clear the XMP edits or disable conformance.");
        }

        if (doc.Conformance != PdfAConformance.None || doc.IsPdfUa)
        {
            new ConformanceWriter(doc).ValidateConformance();
        }

        var writer = new DocumentWriter(Stream.Null)
        {
            Encryption = doc.Encryption,
            UseCompressedStreams = doc.CompressOutput,
        };

        var catalog = new DictionaryObject();
        var catalogRef = writer.Add(catalog);

        var pagesNode = new DictionaryObject();
        var pagesRef = writer.Add(pagesNode);

        var loaded = doc.Loaded;
        var importer = loaded?.Source is { } docSource ? new GraphImporter(docSource, writer) : null;
        var appendImporters = new Dictionary<DocumentReader, GraphImporter>();
        var pageNodes = new List<(Page Page, DictionaryObject Node, ReferenceObject Reference)>();

        var fontRefs = new Dictionary<EmissionFont, DocumentObject>();
        var imageRefs = new Dictionary<EmissionImage, ReferenceObject>();
        var sharedImages = new Dictionary<object, ReferenceObject>(ReferenceEqualityComparer.Instance);
        var emittedContent = new Dictionary<Page, List<ReadOnlyMemory<byte>>>();
        var annotationJoins = new List<AnnotationElementJoin>();

        var kids = new ArrayObject();
        foreach (var page in doc.Pages)
        {
            var pageNode = new DictionaryObject
            {
                ["Type"] = new NameObject("Page"),
                ["Parent"] = pagesRef,
            };

            PageResourceBuilder.EmitPageGeometry(doc, page, pageNode);

            var pageRef = writer.Add(pageNode);
            if (importer is not null && loaded!.SourcePages.TryGetValue(page, out var sourceNode))
            {
                importer.Seed(sourceNode, pageRef);
            }

            kids.Add(pageRef);
            pageNodes.Add((page, pageNode, pageRef));
        }

        for (var pageIndex = 0; pageIndex < pageNodes.Count; pageIndex++)
        {
            var (page, pageNode, _) = pageNodes[pageIndex];
            var contentBytes = new List<ReadOnlyMemory<byte>>();
            emittedContent[page] = contentBytes;
            if (page.Generated is { } generated)
            {
                IReadOnlySet<string>? referenced = null;
                Content.ContentEmissionResult? overlay = null;
                if (page.IsEditingGenerated)
                {
                    var editedContent = page.CurrentContent ?? generated.Content.ToArray();
                    referenced = PageResourceBuilder.ReferencedResourceKeys(editedContent);
                    pageNode["Contents"] = writer.Add(new StreamObject(editedContent));
                    contentBytes.Add(editedContent);
                }
                else
                {
                    var generatedRef = writer.Add(FlateFilter.EncodeStream(generated.Content.Span));
                    overlay = page.BuildOverlay();
                    pageNode["Contents"] = overlay is null
                        ? generatedRef
                        : new ArrayObject { generatedRef, writer.Add(new StreamObject(overlay.Bytes!)) };
                    contentBytes.Add(generated.Content);
                    if (overlay is not null)
                    {
                        contentBytes.Add(overlay.Bytes!);
                    }
                }

                var resources = PageResourceBuilder.BuildGeneratedResources(writer, generated, fontRefs, imageRefs, referenced);
                if (overlay is not null)
                {
                    resources = PageResourceBuilder.OverlayResources(writer, resources, overlay.Resources);
                }

                if (resources is not null)
                {
                    pageNode["Resources"] = resources;
                }

                if (generated.Links.Length > 0)
                {
                    var links = NavigationWriter.BuildLinkAnnotations(writer, generated.Links, pageIndex);
                    pageNode["Annots"] = links.Annotations;
                    annotationJoins.AddRange(links.StructureJoins);
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
                contentBytes.Add(emission.Bytes);
                if (emission.Overlay is not null)
                {
                    contentBytes.Add(emission.Overlay.Bytes!);
                }
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
                var appendImporter = GraphImporter.GetOrCreate(appendImporters, appended.Reader, writer);
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

        var pageMap = EmissionPageMap.Build(doc.Pages);
        if (doc.EmissionPlan?.Structure is { } structure)
        {
            catalog["MarkInfo"] = new DictionaryObject { ["Marked"] = new BooleanObject(true) };
            catalog["StructTreeRoot"] = StructureWriter.WriteStructureTree(
                writer,
                structure,
                pageNodes,
                pageMap,
                doc.EmissionPlan.RoleMap,
                annotationJoins);
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

        AnnotationEmitter.Write(writer, importer, loaded?.Source, appendImporters, pageNodes);

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

        if (doc.EmissionPlan?.Anchors.Count > 0)
        {
            new NavigationWriter(doc).WriteDestinations(writer, catalog, pageNodes, pageMap);
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

        if (doc.Conformance != PdfAConformance.None || doc.IsPdfUa)
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
            catalog["Metadata"] = writer.Add(BaseXmp(doc.Info).BuildStream());
        }

        writer.Trailer["Root"] = catalogRef;

        // ISO 32000-1 7.5.5: trailer /ID.
        if (doc.Encryption is null && (doc.IncludeDocumentId || doc.Conformance != PdfAConformance.None || doc.IsPdfUa))
        {
            writer.Trailer["ID"] = BuildDocumentId(emittedContent);
        }

        // ISO 19005-4 6.1.3: PDF/A-4 forbids the trailer /Info key.
        var isPart4 = doc.Conformance is PdfAConformance.PdfA4 or PdfAConformance.PdfA4E or PdfAConformance.PdfA4F;
        var info = isPart4 ? null : BuildInfo(doc.Info, doc.Loaded?.SourceInfo, importer);
        if (info is not null)
        {
            writer.Trailer["Info"] = writer.Add(info);
        }

        // ISO 19005-4 (6.1.2) requires the PDF 2.0 header for PDF/A-4.
        writer.Version = isPart4 ? "2.0" : "1.7";
        writer.Graph.UseCompressedStreams = writer.UseCompressedStreams;
        writer.Graph.Catalog = catalog;
        var graphPages = new List<DictionaryObject>(pageNodes.Count);
        foreach (var pageNode in pageNodes)
        {
            graphPages.Add(pageNode.Node);
        }

        writer.Graph.Pages = graphPages;
        writer.Graph.Encryption = SnapshotEncryption(writer.Encryption, writer.Graph);
        return writer.Graph;
    }

    private static EncryptionOptions? SnapshotEncryption(
        EncryptionOptions? source,
        DocumentObjectGraph graph)
        => source is null
            ? null
            : new EncryptionOptions
            {
                UserPassword = source.UserPassword,
                OwnerPassword = source.OwnerPassword,
                Algorithm = source.Algorithm,
                Material = source.Material is { } material
                    ? new CapturedEncryptionMaterial(material, EncryptionRequestLimit(graph))
                    : null,
                EncryptMetadata = source.EncryptMetadata,
                AllowPrinting = source.AllowPrinting,
                AllowHighResPrinting = source.AllowHighResPrinting,
                AllowModification = source.AllowModification,
                AllowContentCopy = source.AllowContentCopy,
                AllowAnnotation = source.AllowAnnotation,
                AllowFormFill = source.AllowFormFill,
                AllowAssembly = source.AllowAssembly,
            };

    private static int EncryptionRequestLimit(DocumentObjectGraph graph)
    {
        var count = graph.Objects.Count + 8;
        foreach (var item in graph.Objects)
        {
            count += CountEncryptableValues(item);
        }

        return count;
    }

    private static int CountEncryptableValues(DocumentObject value)
        => value switch
        {
            StringObject => 1,
            StreamObject stream => 1 + CountEncryptableValues(stream.Dictionary),
            ArrayObject array => CountEncryptableSequence(array),
            DictionaryObject dictionary => CountEncryptableValues(dictionary),
            _ => 0,
        };

    private static int CountEncryptableSequence(IEnumerable<DocumentObject> values)
    {
        var count = 0;
        foreach (var value in values)
        {
            count += CountEncryptableValues(value);
        }

        return count;
    }

    private static int CountEncryptableValues(DictionaryObject dictionary)
    {
        var count = 0;
        foreach (var item in dictionary)
        {
            count += CountEncryptableValues(item.Value);
        }

        return count;
    }

    internal static XmpMetadata BaseXmp(DocumentInfo info) => new()
    {
        Info = info,
        Producer = info.Producer ?? "Radzen.Documents.Pdf",
        CreationDate = info.CreationDate,
        ModificationDate = info.ModificationDate,
    };

    internal static DictionaryObject? BuildInfo(DocumentInfo meta, DictionaryObject? source, GraphImporter? importer = null)
    {
        DictionaryObject? info = null;

        if (source is not null)
        {
            foreach (var pair in source)
            {
                if (!DocumentInfoFields.Contains(pair.Key))
                {
                    info ??= new DictionaryObject();
                    info[pair.Key] = importer is null ? pair.Value : importer.ImportValue(pair.Value);
                }
            }
        }

        foreach (var field in DocumentInfoFields.All)
        {
            if (field.Value(meta) is { } value)
            {
                info ??= new DictionaryObject();
                info[field.Key] = StringObject.FromText(value);
            }
        }

        return info;
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
    private ArrayObject BuildDocumentId(IReadOnlyDictionary<Page, List<ReadOnlyMemory<byte>>> emittedContent)
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
            if (emittedContent.TryGetValue(page, out var contentBytes))
            {
                foreach (var bytes in contentBytes)
                {
                    seed.Append(bytes.Span);
                }
            }

            seed.Append((byte)0);
        }

        var hash = seed.Finish();
        var id = Convert.ToHexString(hash, 0, 16);
        return [new StringObject(id), new StringObject(id)];
    }

}
