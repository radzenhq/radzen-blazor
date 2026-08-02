using Radzen.Documents.Pdf.Objects;
using Radzen.Documents.Pdf.Objects.Filters;
using Radzen.Documents.Pdf.Output;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace Radzen.Documents.Pdf.Write;

internal sealed class DocumentGraphBuilder(PortableDocument doc, bool renderTime)
{
    internal DocumentObjectGraph Build()
    {
        var pageMap = PageOutputMap.Build(doc.Pages);
        var conformance = new ConformanceWriter(doc, pageMap);
        if (!renderTime)
        {
            conformance.ValidateSaveTime();
        }

        if (doc.Conformance != PdfAConformance.None || doc.IsPdfUa)
        {
            conformance.ValidateRenderTime();
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
        var fontRefs = new Dictionary<OutputFont, DocumentObject>();
        var imageRefs = new Dictionary<OutputImage, ReferenceObject>();
        var sharedImages = new Dictionary<object, ReferenceObject>(ReferenceEqualityComparer.Instance);
        var emittedContent = new Dictionary<Page, List<ReadOnlyMemory<byte>>>();
        var annotationJoins = new List<AnnotationElementJoin>();

        var (pageNodes, kids) = CreatePageNodes(writer, pagesRef, importer, loaded);
        WritePageContents(
            writer,
            pageNodes,
            pageMap,
            importer,
            loaded,
            appendImporters,
            fontRefs,
            imageRefs,
            sharedImages,
            emittedContent,
            annotationJoins);

        pagesNode["Type"] = new NameObject("Pages");
        pagesNode["Kids"] = kids;
        pagesNode["Count"] = new NumberObject(kids.Count);

        catalog["Type"] = new NameObject("Catalog");
        catalog["Pages"] = pagesRef;

        WriteStructureFormsAndAnnotations(
            writer, catalog, pagesRef, pageNodes, pageMap, annotationJoins, importer, loaded, appendImporters);
        WriteCatalogFeatures(writer, catalog, pageNodes, pageMap, conformance);
        return FinishGraph(writer, catalogRef, catalog, pageNodes, emittedContent, importer);
    }

    private void WriteStructureFormsAndAnnotations(
        DocumentWriter writer,
        DictionaryObject catalog,
        ReferenceObject pagesRef,
        List<(Page Page, DictionaryObject Node, ReferenceObject Reference)> pageNodes,
        PageOutputMap pageMap,
        List<AnnotationElementJoin> annotationJoins,
        GraphImporter? importer,
        LoadedState? loaded,
        Dictionary<DocumentReader, GraphImporter> appendImporters)
    {
        if (doc.Output?.Structure is { } structure)
        {
            catalog["MarkInfo"] = new DictionaryObject { ["Marked"] = new BooleanObject(true) };
            catalog["StructTreeRoot"] = StructureWriter.WriteStructureTree(
                writer, structure, pageNodes, pageMap, doc.RoleMap.Entries, annotationJoins);
        }

        var forms = new FormWriter(doc);
        var appendedFields = forms.AppendForms(pageNodes, appendImporters, writer);
        var createdWidgets = doc.FormFields.Count > 0
            ? forms.WriteCreatedFields(writer, pageNodes, appendedFields)
            : [];

        if (importer is not null)
        {
            var catalogImporter = new CatalogImporter(doc);
            var removed = catalogImporter.PruneRemovedPages(importer);
            catalogImporter.PreserveCatalog(importer, catalog, pagesRef);
            forms.PreserveForm(new PreserveFormRequest
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
            catalog["AcroForm"] = writer.Add(forms.FieldsForm(appendedFields));
        }

        AnnotationWriter.Write(writer, importer, loaded?.Source, appendImporters, pageNodes);
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
    }

    private void WriteCatalogFeatures(
        DocumentWriter writer,
        DictionaryObject catalog,
        List<(Page Page, DictionaryObject Node, ReferenceObject Reference)> pageNodes,
        PageOutputMap pageMap,
        ConformanceWriter conformance)
    {
        if (doc.Attachments.Count > 0)
        {
            new AttachmentWriter(doc).WriteAttachments(writer, catalog);
        }

        if (doc.Output?.Anchors.Count > 0)
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
            conformance.WriteConformance(writer, catalog);
        }
        else if (doc.Xmp.IsModified && doc.Xmp.HasPacket)
        {
            catalog["Metadata"] = writer.Add(XmpMetadata.WrapPacket(doc.Xmp.Packet));
        }
        else if (!doc.Xmp.IsModified
            && (doc.Info.Producer is not null || doc.Info.CreationDate is not null || doc.Info.ModificationDate is not null))
        {
            catalog["Metadata"] = writer.Add(BaseXmp(doc.Info).BuildStream());
        }
    }

    private DocumentObjectGraph FinishGraph(
        DocumentWriter writer,
        ReferenceObject catalogRef,
        DictionaryObject catalog,
        List<(Page Page, DictionaryObject Node, ReferenceObject Reference)> pageNodes,
        Dictionary<Page, List<ReadOnlyMemory<byte>>> emittedContent,
        GraphImporter? importer)
    {
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

        // ISO 19005-4 6.1.2: PDF/A-4 requires the PDF 2.0 header.
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

    private (List<(Page Page, DictionaryObject Node, ReferenceObject Reference)> Nodes, ArrayObject Kids)
        CreatePageNodes(
            DocumentWriter writer,
            ReferenceObject pagesRef,
            GraphImporter? importer,
            LoadedState? loaded)
    {
        var nodes = new List<(Page Page, DictionaryObject Node, ReferenceObject Reference)>();
        var kids = new ArrayObject();
        foreach (var page in doc.Pages)
        {
            var node = new DictionaryObject
            {
                ["Type"] = new NameObject("Page"),
                ["Parent"] = pagesRef,
            };
            PageBoxWriter.EmitPageGeometry(doc, page, node);

            var reference = writer.Add(node);
            if (importer is not null && loaded!.SourcePages.TryGetValue(page, out var sourceNode))
            {
                importer.Seed(sourceNode, reference);
            }

            kids.Add(reference);
            nodes.Add((page, node, reference));
        }

        return (nodes, kids);
    }

    private static void WritePageContents(
        DocumentWriter writer,
        List<(Page Page, DictionaryObject Node, ReferenceObject Reference)> pageNodes,
        PageOutputMap pageMap,
        GraphImporter? importer,
        LoadedState? loaded,
        Dictionary<DocumentReader, GraphImporter> appendImporters,
        Dictionary<OutputFont, DocumentObject> fontRefs,
        Dictionary<OutputImage, ReferenceObject> imageRefs,
        Dictionary<object, ReferenceObject> sharedImages,
        Dictionary<Page, List<ReadOnlyMemory<byte>>> emittedContent,
        List<AnnotationElementJoin> annotationJoins)
    {
        for (var pageIndex = 0; pageIndex < pageNodes.Count; pageIndex++)
        {
            var (page, pageNode, _) = pageNodes[pageIndex];
            var contentBytes = new List<ReadOnlyMemory<byte>>();
            emittedContent[page] = contentBytes;
            if (pageMap.PlanAt(pageIndex) is { } generated)
            {
                WriteGeneratedPage(
                    writer, pageIndex, page, pageNode, generated, fontRefs, imageRefs, contentBytes, annotationJoins);
                continue;
            }

            var reservedNames = ReservedResourceNames(page, loaded);
            var emission = page.BuildContent(reservedNames);
            WriteEditedContent(writer, pageNode, emission, contentBytes);

            var activeResources = emission.Resources.IsEmpty && emission.Overlay is not null
                ? emission.Overlay.Resources
                : emission.Resources;
            var emitted = activeResources.IsEmpty
                ? null
                : PageResourceBuilder.BuildResources(writer, activeResources, sharedImages);
            var merged = MergePageResources(
                writer, page, emitted, importer, loaded, appendImporters);
            if (merged is not null)
            {
                pageNode["Resources"] = merged;
            }
        }
    }

    private static void WriteGeneratedPage(
        DocumentWriter writer,
        int pageIndex,
        Page page,
        DictionaryObject pageNode,
        PageOutput generated,
        Dictionary<OutputFont, DocumentObject> fontRefs,
        Dictionary<OutputImage, ReferenceObject> imageRefs,
        List<ReadOnlyMemory<byte>> contentBytes,
        List<AnnotationElementJoin> annotationJoins)
    {
        IReadOnlySet<string>? referenced = null;
        Content.ContentEmissionResult? overlay = null;
        if (page.IsEditingGenerated)
        {
            var editedContent = page.CurrentContent ?? generated.ContentArray;
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
    }

    private static HashSet<string>? ReservedResourceNames(Page page, LoadedState? loaded)
    {
        if (loaded?.Source is { } source && loaded.SourceResources.TryGetValue(page, out var sourceResources))
        {
            return PageResourceBuilder.ResourceNames(source, sourceResources);
        }

        return loaded is not null && loaded.AppendedResources.TryGetValue(page, out var appended)
            ? PageResourceBuilder.ResourceNames(appended.Reader, appended.Resources)
            : null;
    }

    private static void WriteEditedContent(
        DocumentWriter writer,
        DictionaryObject pageNode,
        Content.ContentEmissionResult emission,
        List<ReadOnlyMemory<byte>> contentBytes)
    {
        if (emission.Bytes is null)
        {
            return;
        }

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

    private static DictionaryObject? MergePageResources(
        DocumentWriter writer,
        Page page,
        DictionaryObject? emitted,
        GraphImporter? importer,
        LoadedState? loaded,
        Dictionary<DocumentReader, GraphImporter> appendImporters)
    {
        if (importer is not null && loaded?.Source is { } source
            && loaded.SourceResources.TryGetValue(page, out var sourceResources))
        {
            return PageResourceBuilder.MergeResources(importer, source, sourceResources, emitted);
        }

        if (loaded is not null && loaded.AppendedResources.TryGetValue(page, out var appended))
        {
            var appendImporter = GraphImporter.GetOrCreate(appendImporters, appended.Reader, writer);
            return PageResourceBuilder.MergeResources(appendImporter, appended.Reader, appended.Resources, emitted);
        }

        return emitted;
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
        var seed = new Radzen.Documents.Pdf.Crypto.Sha256Hasher();

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
