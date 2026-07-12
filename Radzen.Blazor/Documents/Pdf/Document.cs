using Radzen.Documents.Pdf.Objects;
using Radzen.Documents.Pdf.Objects.Filters;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace Radzen.Documents.Pdf;


/// <summary>
/// A physical PDF document: an ordered collection of pages plus document
/// metadata. Serialized through the object model as a classic PDF file.
/// </summary>
public sealed class Document
{
    internal readonly Dictionary<Page, DictionaryObject> sourcePages = [];
    internal readonly Dictionary<Page, DictionaryObject> sourceResources = [];
    internal readonly Dictionary<Page, ArrayObject> sourceBoxes = [];
    internal readonly Dictionary<Page, ArrayObject> sourceCropBoxes = [];
    internal readonly Dictionary<Page, int> sourceRotations = [];
    internal readonly Dictionary<Page, (DocumentReader Reader, DictionaryObject Resources)> appendedResources = [];
    internal readonly Dictionary<Page, (DocumentReader Reader, DictionaryObject Node)> appendedPages = [];
    internal readonly Dictionary<DocumentReader, DictionaryObject> appendedAcroForms = [];
    internal DocumentReader? source;
    internal DictionaryObject? sourceCatalog;
    internal DictionaryObject? sourceAcroForm;

    /// <summary>Gets the document metadata.</summary>
    public DocumentInfo Info { get; } = new();

    /// <summary>Gets the ordered collection of pages.</summary>
    public PageCollection Pages { get; } = [];

    /// <summary>
    /// Gets the interactive form of a loaded document, or <c>null</c> when the
    /// document has no AcroForm.
    /// </summary>
    public AcroForm? AcroForm { get; internal set; }

    /// <summary>
    /// Gets the form fields to create on this document. Each definition is
    /// saved as a widget annotation on its page and listed in the catalog
    /// <c>/AcroForm /Fields</c> with a generated appearance stream.
    /// </summary>
    public IList<FormFieldDefinition> FormFields { get; } = [];

    /// <summary>
    /// Gets or sets the encryption to apply when saving. When <c>null</c> the
    /// document is written unencrypted.
    /// </summary>
    public Objects.Encryption.EncryptionOptions? Encryption { get; set; }

    /// <summary>
    /// Gets or sets whether to pack indirect objects into compressed object
    /// streams (<c>/ObjStm</c>) with a cross-reference stream (<c>/XRef</c>),
    /// which typically shrinks the output. Not compatible with PDF/A-1;
    /// leave <c>false</c> for maximum reader compatibility.
    /// </summary>
    public bool CompressOutput { get; set; }

    /// <summary>
    /// Gets or sets whether a deterministic trailer <c>/ID</c> (ISO 32000-1 7.5.5)
    /// is written on the unencrypted save path. The value derives only from the
    /// document content and metadata, never from the clock or a random source.
    /// Defaults to <c>false</c> so a document that does not opt in stays byte
    /// identical. Encrypted and PDF/A output always carry an <c>/ID</c> regardless.
    /// </summary>
    public bool IncludeDocumentId { get; set; }

    /// <summary>
    /// Gets or sets the viewer preferences written to the document catalog
    /// (page layout, page mode, and the <c>/ViewerPreferences</c> flags). When
    /// <c>null</c> no viewer-preference keys are written and the output is
    /// unchanged.
    /// </summary>
    public ViewerPreferences? ViewerPreferences { get; set; }

    /// <summary>
    /// Gets the page-label ranges written to the catalog <c>/PageLabels</c> number
    /// tree. Each <see cref="PageLabel"/> starts a range whose pages a viewer numbers
    /// with the given style, prefix and start ordinal. When empty no <c>/PageLabels</c>
    /// entry is written.
    /// </summary>
    public IList<PageLabel> PageLabels { get; } = [];

    // Logical structure tree of a generated document (Tagged PDF). Set by the
    // generator; null for loaded or hand-assembled documents.
    internal StructureElement? Structure { get; set; }

    // PDF/A conformance level requested at build time; drives XMP metadata,
    // the sRGB output intent, the trailer /ID and full-embedding enforcement.
    internal PdfAConformance Conformance { get; set; }

    // PDF/UA-1 identification requested at build time; drives the pdfuaid XMP
    // entry, the DisplayDocTitle viewer preference and tagging enforcement.
    internal bool PdfUA { get; set; }

    // Natural language of the document (catalog /Lang); required by PDF/UA.
    internal string? Language { get; set; }

    // Files embedded on save (EmbeddedFiles name tree + /AF associated files).
    internal List<Attachment> Attachments { get; } = [];

    // Outline (bookmark) tree copied from DocumentBuilder.Outline; emitted on
    // save as the catalog /Outlines tree.
    internal List<OutlineItem> Outline { get; } = [];

    // Named destinations recorded at emit time (Run.Anchor); emitted on save as
    // the catalog /Names /Dests name tree.
    internal Dictionary<string, GeneratedAnchor> Anchors { get; } = new(StringComparer.Ordinal);

    /// <summary>
    /// Loads a physical document from a stream. The stream is read in full and
    /// parsed through the internal reader; each page's raw content-stream bytes
    /// are retained verbatim so untouched pages re-serialize unchanged.
    /// </summary>
    /// <param name="stream">The source stream.</param>
    /// <param name="options">Load options such as the decryption password.</param>
    /// <returns>The loaded document.</returns>
    public static Document LoadFromStream(Stream stream, LoadOptions? options = null)
        => LoadFromStream(stream, ReaderLimits.Default, options);

    /// <summary>
    /// Loads a physical document from a stream, applying the supplied resource
    /// limits while parsing untrusted input. See <see cref="ReaderLimits"/>.
    /// </summary>
    /// <param name="stream">The source stream.</param>
    /// <param name="limits">The resource limits to enforce while reading.</param>
    /// <param name="options">Load options such as the decryption password.</param>
    /// <returns>The loaded document.</returns>
    public static Document LoadFromStream(Stream stream, ReaderLimits limits, LoadOptions? options = null)
        => DocumentLoader.Load(stream, limits, options);

    /// <summary>
    /// Extracts the visible text of every page in reading order, concatenated in
    /// page order with a newline between pages.
    /// </summary>
    /// <returns>The document text, or an empty string when there is no text.</returns>
    public string ExtractText()
    {
        var builder = new System.Text.StringBuilder();
        foreach (var page in Pages)
        {
            var text = page.ExtractText();
            if (text.Length == 0)
            {
                continue;
            }

            if (builder.Length > 0)
            {
                builder.Append('\n');
            }

            builder.Append(text);
        }

        return builder.ToString();
    }

    /// <summary>
    /// Appends a deep copy of every page in <paramref name="other"/> to this
    /// document. Each appended page keeps its own content stream (no resource
    /// deduplication) and <paramref name="other"/> is left unchanged.
    /// </summary>
    /// <param name="other">The document whose pages are copied.</param>
    public void Append(Document other)
    {
        ArgumentNullException.ThrowIfNull(other);

        foreach (var source in other.Pages)
        {
            var page = new Page(source.Width, source.Height);
            var content = source.GetContent();
            if (content is not null)
            {
                page.SetContent([.. content]);
            }

            // Carry the source page's resource closure so appended fonts/images still
            // resolve: a built page keeps its GeneratedPage; a loaded page keeps a
            // handle to its reader and effective /Resources, imported lazily on save.
            if (source.Generated is { } generated)
            {
                page.Generated = generated;
            }
            else if (other.source is not null && other.sourceResources.TryGetValue(source, out var loadedResources))
            {
                appendedResources[page] = (other.source, loadedResources);
                page.SetTextFonts(DocumentLoader.BuildTextFonts(other.source, loadedResources));
            }

            // A loaded appended page keeps a handle to its source node so its
            // /Annots (and any widget-annotation form fields) survive the copy.
            if (other.source is not null && other.sourcePages.TryGetValue(source, out var sourceNode))
            {
                appendedPages[page] = (other.source, sourceNode);
                if (other.sourceAcroForm is not null)
                {
                    appendedAcroForms[other.source] = other.sourceAcroForm;
                }
            }

            if (other.sourceBoxes.TryGetValue(source, out var box))
            {
                sourceBoxes[page] = box;
            }

            Pages.Insert(Pages.Count, page);
        }
    }

    /// <summary>
    /// Flattens the interactive form into static page content: each field's
    /// current value renders onto its page, and the fields, their widget
    /// annotations and the catalog <c>/AcroForm</c> are removed. Applies both
    /// to a loaded form (<see cref="AcroForm"/>) and to pending
    /// <see cref="FormFields"/> definitions.
    /// </summary>
    public void Flatten() => new FormWriter(this).Flatten();

    /// <summary>
    /// Serializes the document to a byte array.
    /// </summary>
    /// <returns>The complete PDF file bytes.</returns>
    public byte[] ToArray()
    {
        using var stream = new PooledBufferStream(64 * 1024);
        SaveToStream(stream);
        return stream.ToArray();
    }

    /// <summary>
    /// Serializes the document to the given stream.
    /// </summary>
    /// <param name="stream">The destination stream.</param>
    public void SaveToStream(Stream stream)
    {
        System.ArgumentNullException.ThrowIfNull(stream);

        if (Conformance != PdfAConformance.None || PdfUA)
        {
            new ConformanceWriter(this).ValidateConformance();
        }

        var writer = new DocumentWriter(stream) { Encryption = Encryption, UseCompressedStreams = CompressOutput };

        var catalog = new DictionaryObject();
        var catalogRef = writer.Add(catalog);

        var pagesNode = new DictionaryObject();
        var pagesRef = writer.Add(pagesNode);

        var importer = source is not null ? new GraphImporter(source, writer) : null;
        var appendImporters = new Dictionary<DocumentReader, GraphImporter>();
        var pageNodes = new List<(Page Page, DictionaryObject Node, ReferenceObject Reference)>();

        var fontRefs = new Dictionary<GeneratedFont, DocumentObject>();
        var imageRefs = new Dictionary<GeneratedImage, ReferenceObject>();

        var kids = new ArrayObject();
        foreach (var page in Pages)
        {
            var pageNode = new DictionaryObject
            {
                ["Type"] = new NameObject("Page"),
                ["Parent"] = pagesRef,
                ["MediaBox"] = PageResourceBuilder.MediaBox(this, page),
            };

            if (sourceCropBoxes.TryGetValue(page, out var cropBox))
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
            else if (sourceRotations.TryGetValue(page, out var rotation))
            {
                pageNode["Rotate"] = new NumberObject(rotation);
            }

            var pageRef = writer.Add(pageNode);
            if (importer is not null && sourcePages.TryGetValue(page, out var sourceNode))
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
                var overlay = page.BuildOverlay(out var overlayEmitter);
                if (overlay is null)
                {
                    pageNode["Contents"] = generatedRef;
                }
                else
                {
                    pageNode["Contents"] = new ArrayObject { generatedRef, writer.Add(new StreamObject(overlay)) };
                }

                var resources = PageResourceBuilder.BuildGeneratedResources(writer, generated, fontRefs, imageRefs);
                if (overlayEmitter is not null)
                {
                    resources = PageResourceBuilder.OverlayResources(writer, resources, overlayEmitter);
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
            if (source is not null && sourceResources.TryGetValue(page, out var reservedFrom))
            {
                reservedNames = PageResourceBuilder.ResourceNames(source, reservedFrom);
            }
            else if (appendedResources.TryGetValue(page, out var reservedAppend))
            {
                reservedNames = PageResourceBuilder.ResourceNames(reservedAppend.Reader, reservedAppend.Resources);
            }

            var contentBytes = page.BuildContent(out var emitter, out var overlayBytes, out var pageOverlayEmitter, reservedNames);
            if (contentBytes is not null)
            {
                var contentRef = writer.Add(new StreamObject(contentBytes));
                pageNode["Contents"] = overlayBytes is null
                    ? contentRef
                    : new ArrayObject { contentRef, writer.Add(new StreamObject(overlayBytes)) };
            }

            var activeEmitter = emitter ?? pageOverlayEmitter;
            var emitted = activeEmitter is not null ? PageResourceBuilder.BuildResources(writer, activeEmitter) : null;
            DictionaryObject? merged;
            if (importer is not null && source is not null
                && sourceResources.TryGetValue(page, out var loadedResources))
            {
                merged = PageResourceBuilder.MergeResources(importer, source, loadedResources, emitted);
            }
            else if (appendedResources.TryGetValue(page, out var appended))
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

        if (Structure is { } structure)
        {
            catalog["MarkInfo"] = new DictionaryObject { ["Marked"] = new BooleanObject(true) };
            catalog["StructTreeRoot"] = StructureWriter.WriteStructureTree(writer, structure, pageNodes);
        }

        var formWriter = new FormWriter(this);
        var appendedFields = formWriter.AppendForms(pageNodes, appendImporters, writer);
        List<(int PageIndex, ReferenceObject Reference)> createdWidgets =
            FormFields.Count > 0 ? formWriter.WriteCreatedFields(writer, pageNodes, appendedFields) : [];

        if (importer is not null)
        {
            var catalogPreserver = new CatalogPreserver(this);
            var removed = catalogPreserver.PruneRemovedPages(importer);
            catalogPreserver.PreserveCatalog(importer, catalog, pagesRef);
            formWriter.PreserveForm(importer, catalog, pageNodes, removed, appendedFields, writer);
        }
        else if (appendedFields.Count > 0)
        {
            catalog["AcroForm"] = writer.Add(formWriter.FieldsForm(appendedFields));
        }

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

        if (Attachments.Count > 0)
        {
            new AttachmentWriter(this).WriteAttachments(writer, catalog);
        }

        if (Anchors.Count > 0)
        {
            new NavigationWriter(this).WriteDestinations(writer, catalog, pageNodes);
        }

        if (Outline.Count > 0)
        {
            catalog["Outlines"] = new NavigationWriter(this).WriteOutline(writer, pageNodes);
        }

        if (ViewerPreferences is { } preferences)
        {
            WriteViewerPreferences(catalog, preferences);
        }

        if (PageLabels.Count > 0)
        {
            catalog["PageLabels"] = PageLabelsWriter.Build([.. PageLabels]);
        }

        if (Conformance != PdfAConformance.None || PdfUA)
        {
            new ConformanceWriter(this).WriteConformance(writer, catalog);
        }
        else if (Info.Producer is not null || Info.CreationDate is not null || Info.ModificationDate is not null)
        {
            // Producer and the creation/modification dates are mirrored into an XMP
            // packet alongside the /Info dictionary. Absent all three, no metadata
            // stream is written and the output stays byte identical.
            var xmp = new XmpMetadata
            {
                Info = Info,
                Producer = Info.Producer ?? "Radzen.Documents.Pdf",
                CreationDate = Info.CreationDate,
                ModificationDate = Info.ModificationDate,
            };
            catalog["Metadata"] = writer.Add(xmp.BuildStream());
        }

        writer.Trailer["Root"] = catalogRef;

        // A deterministic trailer /ID (ISO 32000-1 7.5.5). Opt-in for plain output so
        // an untouched document stays byte identical; PDF/A and PDF/UA require a file
        // identifier so they always carry one. The encrypted path derives its own /ID
        // from the encryption seed inside DocumentWriter, so it is excluded here.
        if (Encryption is null && (IncludeDocumentId || Conformance != PdfAConformance.None || PdfUA))
        {
            writer.Trailer["ID"] = BuildDocumentId();
        }

        // PDF/A-4 (ISO 19005-4, 6.1.3) forbids the trailer /Info key; the
        // document metadata lives in the XMP stream instead.
        var isPart4 = Conformance is PdfAConformance.PdfA4 or PdfAConformance.PdfA4E or PdfAConformance.PdfA4F;
        var info = isPart4 ? null : BuildInfo();
        if (info is not null)
        {
            writer.Trailer["Info"] = writer.Add(info);
        }

        // ISO 19005-4 (6.1.2) requires the PDF 2.0 header for PDF/A-4.
        writer.Version = isPart4 ? "2.0" : "1.7";

        writer.Close();
    }

    private DictionaryObject? BuildInfo()
    {
        DictionaryObject? info = null;

        void Set(string key, string? value)
        {
            if (value is null)
            {
                return;
            }

            info ??= new DictionaryObject();
            info[key] = new StringObject(value);
        }

        Set("Title", Info.Title);
        Set("Author", Info.Author);
        Set("Subject", Info.Subject);
        Set("Keywords", Info.Keywords);
        Set("Creator", Info.Creator);
        Set("Producer", Info.Producer);
        Set("CreationDate", Info.CreationDate is { } created ? PdfDate(created) : null);
        Set("ModDate", Info.ModificationDate is { } modified ? PdfDate(modified) : null);

        return info;
    }

    // Writes /BleedBox, /TrimBox or /ArtBox to a page node. An explicit value on the Page
    // wins; otherwise a loaded page re-emits the box its source node carried (previously
    // dropped). A page with neither adds nothing, so untouched output stays byte identical.
    private void WriteAuxiliaryBox(DictionaryObject pageNode, Page page, string key, Rect? value)
    {
        if (value is { } rect)
        {
            pageNode[key] = new ArrayObject
            {
                new NumberObject(rect.X),
                new NumberObject(rect.Y),
                new NumberObject(rect.X + rect.Width),
                new NumberObject(rect.Y + rect.Height),
            };
            return;
        }

        if (source is not null && sourcePages.TryGetValue(page, out var sourceNode)
            && sourceNode.TryGetValue(key, out var boxObject)
            && source.Resolve(boxObject!) is ArrayObject box && box.Count >= 4)
        {
            pageNode[key] = PageResourceBuilder.NumberBox(box);
        }
    }

    // PageLayout and PageMode are catalog entries (ISO 32000-1 Table 28); the rest are
    // grouped in the /ViewerPreferences dictionary (Table 150). Only set options are
    // written, and a dictionary is only added when at least one of its flags is present,
    // so an all-default ViewerPreferences leaves the catalog untouched.
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

    // ISO 32000-1 7.9.4 date string: D:YYYYMMDDHHmmSS followed by the UTC offset as
    // O HH ' mm ' (O is + or -). Caller-supplied offset only; no clock is read.
    internal static string PdfDate(DateTimeOffset value)
    {
        var offset = value.Offset;
        var sign = offset < TimeSpan.Zero ? '-' : '+';
        return string.Create(CultureInfo.InvariantCulture,
            $"D:{value:yyyyMMddHHmmss}{sign}{Math.Abs(offset.Hours):D2}'{Math.Abs(offset.Minutes):D2}'");
    }

    // A stable /ID derived only from the document metadata and page content, never from
    // the clock or a random source, so repeated saves of the same document are byte
    // identical. Both halves are equal at creation time (ISO 32000-1 14.4).
    private ArrayObject BuildDocumentId()
    {
        using var seed = new MemoryStream();

        void Text(string? value)
        {
            if (value is { Length: > 0 })
            {
                var bytes = Encoding.UTF8.GetBytes(value);
                seed.Write(bytes, 0, bytes.Length);
            }

            seed.WriteByte(0);
        }

        Text(Info.Title);
        Text(Info.Author);
        Text(Info.Subject);
        Text(Info.Keywords);
        Text(Info.Creator);
        Text(Info.Producer);
        Text(Info.CreationDate?.ToString("O", CultureInfo.InvariantCulture));
        Text(Info.ModificationDate?.ToString("O", CultureInfo.InvariantCulture));
        Text(Pages.Count.ToString(CultureInfo.InvariantCulture));

        foreach (var page in Pages)
        {
            Text(page.Width.Point.ToString("R", CultureInfo.InvariantCulture));
            Text(page.Height.Point.ToString("R", CultureInfo.InvariantCulture));
            if (page.GetContent() is { } content)
            {
                seed.Write(content, 0, content.Length);
            }

            seed.WriteByte(0);
        }

        var hash = Radzen.Documents.Crypto.Sha2.Sha256(seed.ToArray());
        var id = Convert.ToHexString(hash, 0, 16);
        return [new StringObject(id), new StringObject(id)];
    }
}
