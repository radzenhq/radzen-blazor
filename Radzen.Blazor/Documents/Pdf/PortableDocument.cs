using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Radzen.Documents.Fonts;
using Radzen.Documents.Internal;
using Radzen.Documents.LaidOut;
using Radzen.Documents.Pdf.Output;
using Radzen.Documents.Pdf.Objects;
using Radzen.Documents.Pdf.Render;
using Radzen.Documents.Pdf.Signing;
using Radzen.Documents.Pdf.Write;
namespace Radzen.Documents.Pdf;


/// <summary>
/// A physical PDF document: an ordered collection of pages plus document
/// metadata. Serialized through the object model as a classic PDF file.
/// </summary>
public sealed class PortableDocument
{
    private readonly TrackedList<OutlineItem> outline;
    private readonly TrackedList<PageLabel> pageLabels;
    private readonly TrackedList<FormFieldDefinition> formFields;
    private DocumentInfo info = new();
    private readonly AttachmentCollection attachments = [];
    private DocumentXmpMetadata xmp = new();
    private ViewerPreferences? viewerPreferences;
    private AcroForm? acroForm;
    private EncryptionOptions? encryption;
    private bool compressOutput;
    private bool includeDocumentId;
    private readonly object facadeLock = new();
    private bool infoLoaded = true;
    private bool attachmentsLoaded = true;
    private bool outlineLoaded = true;
    private bool pageLabelsLoaded = true;
    private bool xmpLoaded = true;
    private bool viewerPreferencesLoaded = true;
    private bool acroFormLoaded = true;

    /// <summary>Initializes an empty PDF document.</summary>
    public PortableDocument()
    {
        Pages = new PageCollection(this);
        outline = new TrackedList<OutlineItem>(InvalidateMaterializedGraph);
        pageLabels = new TrackedList<PageLabel>(InvalidateMaterializedGraph);
        formFields = new TrackedList<FormFieldDefinition>(InvalidateMaterializedGraph);
        attachments.OwnedBy(InvalidateMaterializedGraph);
        info.OwnedBy(InvalidateMaterializedGraph);
        xmp.OwnedBy(InvalidateMaterializedGraph);
    }

    internal LoadedState? Loaded { get; private set; }

    internal DocumentObjectGraph? MaterializedGraph { get; set; }

    private int loadingThread;

    internal void InvalidateMaterializedGraph()
    {
        if (loadingThread != Environment.CurrentManagedThreadId)
        {
            MaterializedGraph = null;
        }
    }

    private void LoadFacade(Action load)
    {
        lock (facadeLock)
        {
            var previous = loadingThread;
            loadingThread = Environment.CurrentManagedThreadId;
            try
            {
                load();
            }
            finally
            {
                loadingThread = previous;
            }
        }
    }

    internal void AdoptMaterializedGraph(DocumentObjectGraph graph)
    {
        var reader = new DocumentReader(graph);
        var state = new LoadedState(reader)
        {
            SourceCatalog = graph.Catalog,
        };

        for (var i = 0; i < Pages.Count; i++)
        {
            var page = Pages[i];
            var pageNode = graph.Pages[i];
            state.SourcePages[page] = pageNode;
            state.LoadedPages.Add(page);
            state.LoadedPageSettings[page] = (page.BleedBox, page.TrimBox, page.ArtBox, page.Rotate);
            if (reader.GetDictionary(pageNode, "Resources") is { } resources)
            {
                state.SourceResources[page] = resources;
                page.SetTextFonts(DocumentLoader.BuildTextFonts(reader, resources));
                page.SetReservedResourceNames(PageResourceBuilder.ResourceNames(reader, resources));
            }

        }

        if (graph.Catalog is { } catalog && reader.GetDictionary(catalog, "AcroForm") is { } form)
        {
            state.SourceAcroForm = form;
        }

        if (reader.GetDictionary(reader.Trailer, "Info") is { } info)
        {
            state.SourceInfo = info;
        }

        Loaded = state;
        ResetGraphFacades();
        encryption = graph.Encryption;
        MaterializedGraph = graph;
    }

    internal static PortableDocument CreateLoaded(LoadedState state)
    {
        var document = new PortableDocument { Loaded = state };
        document.ResetGraphFacades();
        return document;
    }

    private void ResetGraphFacades()
    {
        info = new DocumentInfo();
        info.OwnedBy(InvalidateMaterializedGraph);
        attachments.Clear();
        outline.Clear();
        pageLabels.Clear();
        formFields.Clear();
        xmp = new DocumentXmpMetadata();
        xmp.OwnedBy(InvalidateMaterializedGraph);
        viewerPreferences?.OwnedBy(null);
        viewerPreferences = null;
        acroForm = null;
        RoleMap = new RoleMap();
        infoLoaded = false;
        attachmentsLoaded = false;
        outlineLoaded = false;
        pageLabelsLoaded = false;
        xmpLoaded = false;
        viewerPreferencesLoaded = false;
        acroFormLoaded = false;
    }

    internal LoadedState EnsureLoaded() => Loaded ??= new LoadedState();

    internal void CarryForeignPage(Page page, PortableDocument donor)
    {
        if (donor.Loaded is { } origin)
        {
            EnsureLoaded().CarryForeign(page, origin);
        }
    }

    /// <summary>Gets the document metadata.</summary>
    public DocumentInfo Info
    {
        get
        {
            EnsureInfoLoaded();
            return info;
        }
    }

    private void EnsureInfoLoaded() => LoadFacade(() =>
    {
        if (!infoLoaded && Loaded?.Source is { } reader)
        {
            infoLoaded = true;
            Loaded.SourceInfo = DocumentLoader.ReadInfo(reader, info);
            info.AcceptChanges();
        }
    });

    /// <summary>Gets the ordered collection of pages.</summary>
    public PageCollection Pages { get; }

    /// <summary>
    /// Gets the interactive form of a loaded document, or <c>null</c> when the
    /// document has no AcroForm.
    /// </summary>
    public AcroForm? AcroForm
    {
        get
        {
            LoadFacade(() =>
            {
                if (!acroFormLoaded)
                {
                    acroFormLoaded = true;
                    if (Loaded?.Source is { } reader && Loaded.SourceAcroForm is { } form)
                    {
                        acroForm = new AcroForm(reader, form, this);
                    }
                }
            });

            return acroForm;
        }
        internal set
        {
            acroFormLoaded = true;
            acroForm = value;
        }
    }

    /// <summary>
    /// Gets the form fields to create on this document. Each definition is
    /// saved as a widget annotation on its page and listed in the catalog
    /// <c>/AcroForm /Fields</c> with a generated appearance stream. A rendered document starts
    /// from a copy of <see cref="DocumentRenderer.FormFields"/>; from then on this list is what
    /// <see cref="SaveToStream"/> writes.
    /// </summary>
    public IList<FormFieldDefinition> FormFields
    {
        get => formFields;
    }

    /// <summary>
    /// Gets or sets the encryption to apply when saving. When <c>null</c> the
    /// document is written unencrypted. A rendered document starts from
    /// <see cref="DocumentRenderer.Encryption"/>; from then on this value is what
    /// <see cref="SaveToStream"/> applies.
    /// </summary>
    public EncryptionOptions? Encryption
    {
        get => encryption;
        set
        {
            InvalidateMaterializedGraph();
            encryption = value;
        }
    }

    /// <summary>
    /// Gets or sets whether to pack indirect objects into compressed object
    /// streams (<c>/ObjStm</c>) with a cross-reference stream (<c>/XRef</c>),
    /// which typically shrinks the output. Not compatible with PDF/A-1;
    /// leave <c>false</c> for maximum reader compatibility. A rendered document starts from
    /// <see cref="DocumentRenderer.CompressOutput"/>; from then on this value is what
    /// <see cref="SaveToStream"/> applies.
    /// </summary>
    public bool CompressOutput
    {
        get => compressOutput;
        set
        {
            InvalidateMaterializedGraph();
            compressOutput = value;
        }
    }

    /// <summary>
    /// Gets or sets whether a deterministic trailer <c>/ID</c> (ISO 32000-1 7.5.5)
    /// is written on the unencrypted save path. The value derives only from the
    /// document content and metadata, never from the clock or a random source.
    /// Defaults to <c>false</c> so a document that does not opt in stays byte
    /// identical. Encrypted and PDF/A output always carry an <c>/ID</c> regardless. A rendered
    /// document starts from <see cref="DocumentRenderer.IncludeDocumentId"/>; from then on this
    /// value is what <see cref="SaveToStream"/> applies.
    /// </summary>
    public bool IncludeDocumentId
    {
        get => includeDocumentId;
        set
        {
            InvalidateMaterializedGraph();
            includeDocumentId = value;
        }
    }

    /// <summary>
    /// Gets or sets the viewer preferences written to the document catalog
    /// (page layout, page mode, and the <c>/ViewerPreferences</c> flags). When
    /// <c>null</c> no viewer-preference keys are written and the output is
    /// unchanged. A rendered document starts from <see cref="DocumentRenderer.ViewerPreferences"/>;
    /// from then on this value is what <see cref="SaveToStream"/> writes.
    /// </summary>
    public ViewerPreferences? ViewerPreferences
    {
        get
        {
            LoadFacade(() =>
            {
                if (!viewerPreferencesLoaded)
                {
                    viewerPreferencesLoaded = true;
                    viewerPreferences = DocumentLoader.ReadViewerPreferences(Loaded?.Source, Loaded?.SourceCatalog);
                    viewerPreferences?.OwnedBy(InvalidateMaterializedGraph);
                }
            });

            return viewerPreferences;
        }
        set
        {
            InvalidateMaterializedGraph();
            viewerPreferencesLoaded = true;
            viewerPreferences?.OwnedBy(null);
            viewerPreferences = value;
            viewerPreferences?.OwnedBy(InvalidateMaterializedGraph);
        }
    }

    /// <summary>
    /// Gets the page-label ranges written to the catalog <c>/PageLabels</c> number
    /// tree. Each <see cref="PageLabel"/> starts a range whose pages a viewer numbers
    /// with the given style, prefix and start ordinal. When empty no <c>/PageLabels</c>
    /// entry is written. A rendered document starts from a copy of
    /// <see cref="DocumentRenderer.PageLabels"/>; from then on this list is what
    /// <see cref="SaveToStream"/> writes.
    /// </summary>
    public IList<PageLabel> PageLabels
    {
        get
        {
            EnsurePageLabelsLoaded();
            return pageLabels;
        }
    }

    /// <summary>
    /// Gets the root entries of the document outline (bookmark) tree. A rendered document starts
    /// from a copy of <see cref="DocumentRenderer.Outline"/>; from then on this list is what
    /// <see cref="SaveToStream"/> writes.
    /// </summary>
    public IList<OutlineItem> Outline
    {
        get
        {
            EnsureOutlineLoaded();
            return outline;
        }
    }

    /// <summary>
    /// Gets the files embedded in the document. A rendered document starts from a copy of
    /// <see cref="DocumentRenderer.Attachments"/>; from then on this collection is what
    /// <see cref="SaveToStream"/> embeds.
    /// </summary>
    public AttachmentCollection Attachments
    {
        get
        {
            EnsureAttachmentsLoaded();
            return attachments;
        }
    }

    /// <summary>
    /// Gets the document XMP metadata. Caller-set XMP takes precedence over automatic
    /// non-conformance metadata. Editing XMP on PDF/A or PDF/UA output is rejected.
    /// </summary>
    public DocumentXmpMetadata Xmp
    {
        get
        {
            EnsureXmpLoaded();
            return xmp;
        }
    }

    private void EnsureAttachmentsLoaded() => LoadFacade(() =>
    {
        if (!attachmentsLoaded && Loaded?.Source is { } reader && Loaded.SourceCatalog is { } catalog)
        {
            attachmentsLoaded = true;
            DocumentLoader.ReadAttachments(reader, catalog, this, reader.Limits);
            attachments.AcceptChanges();
        }
    });

    private void EnsureOutlineLoaded() => LoadFacade(() =>
    {
        if (!outlineLoaded && Loaded?.Source is { } reader && Loaded.SourceCatalog is { } catalog)
        {
            outlineLoaded = true;
            DocumentLoader.ReadOutline(reader, catalog, this, Loaded, reader.Limits);
            TrackedChanges.Accept(outline);
        }
    });

    private void EnsurePageLabelsLoaded() => LoadFacade(() =>
    {
        if (!pageLabelsLoaded && Loaded?.Source is { } reader && Loaded.SourceCatalog is { } catalog)
        {
            pageLabelsLoaded = true;
            DocumentLoader.ReadPageLabels(reader, catalog, this, reader.Limits);
            TrackedChanges.Accept(pageLabels);
        }
    });

    private void EnsureXmpLoaded() => LoadFacade(() =>
    {
        if (!xmpLoaded && Loaded?.Source is { } reader && Loaded.SourceCatalog is { } catalog)
        {
            xmpLoaded = true;
            DocumentLoader.ReadXmp(reader, catalog, xmp);
        }
    });

    internal DocumentOutput? Output { get; set; }

    /// <summary>
    /// Gets the map of non-standard structure roles to standard ISO 32000-1 structure types
    /// written as <c>/StructTreeRoot /RoleMap</c>. It is captured from
    /// <see cref="DocumentRenderer.RoleMap"/> when the document is rendered; a role map is
    /// meaningful only alongside a structure tree, which only rendering produces, so a loaded
    /// document cannot be given one.
    /// </summary>
    public RoleMap RoleMap { get; internal set; } = new();

    private PdfAConformance conformance;

    /// <summary>
    /// Gets or sets the PDF/A conformance level applied when this document is saved. When not
    /// <see cref="PdfAConformance.None"/> the saved file carries the PDF/A identification in its
    /// XMP metadata, an sRGB output intent and a document identifier, and the PDF/A rules the
    /// library can verify are enforced at save: encryption is refused, embedded files are
    /// restricted by part, DeviceCMYK images are refused against the sRGB intent, and every font
    /// must be an embedded subset.
    /// <para>
    /// Setting this on a document loaded from an existing PDF re-saves it at that level. The
    /// library verifies the fonts and image color spaces it can read from each page's resources
    /// and refuses a page whose content it cannot inspect at all; it does not otherwise revalidate
    /// content it did not produce, so declaring a level on a loaded document asserts that the
    /// source content already conforms.
    /// </para>
    /// <para>
    /// A Level A value (<see cref="PdfAConformance.PdfA2A"/>, <see cref="PdfAConformance.PdfA3A"/>)
    /// additionally requires Tagged PDF logical structure. On a document that already has pages but
    /// neither a render-time structure tree nor a preservable loaded one, setting a Level A value
    /// throws: tagging is a render-time capability, produced by
    /// <see cref="DocumentRenderer.Render(Document)"/>.
    /// </para>
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// A Level A value was set on a populated document that has no preservable structure tree.
    /// </exception>
    public PdfAConformance Conformance
    {
        get => conformance;
        set
        {
            if (Pages.Count > 0
                && value is PdfAConformance.PdfA2A or PdfAConformance.PdfA3A
                && Output?.Structure is null
                && !HasPreservableStructureGraph)
            {
                throw new InvalidOperationException(
                    $"{value} is a Tagged PDF conformance level and this document has no structure tree to write. "
                    + "Tagging is produced while rendering, so a document that was loaded or assembled page by page "
                    + "cannot claim it. Render the document with DocumentRenderer to obtain a tagged document, or set "
                    + "a Level B conformance (PdfA2B, PdfA3B) instead.");
            }

            InvalidateMaterializedGraph();
            conformance = value;
        }
    }

    /// <summary>
    /// Gets or sets the image decoders this document decodes <see cref="ImageContent"/> and
    /// watermark images with. Seeded from <see cref="ImageDecoders.Default"/>, so a decoder
    /// registered with <see cref="ImageDecoder.Register(IImageDecoder)"/> before this document was
    /// created is already in the set. Assign <c>ImageDecoders.BuiltIn.Add(...)</c> to reach a
    /// custom format from this document alone.
    /// </summary>
    public ImageDecoders ImageDecoders { get; set; } = ImageDecoders.Default;

    internal FontCollectionSnapshot? FontSnapshot { get; set; }

    internal Fonts.FontScope FontScope => new(
        null,
        FontSnapshot,
        Conformance != PdfAConformance.None ? "PDF/A" : IsPdfUa ? "PDF/UA" : null,
        CanEmbed: false);

    /// <summary>
    /// Gets the PDF/UA accessibility conformance level of this document, captured from
    /// <see cref="DocumentRenderer.Accessibility"/> when it was rendered. PDF/UA requires Tagged
    /// PDF logical structure, and tagging is a render-time capability: the structure tree is built
    /// while the model is laid out. A document that was loaded from an existing PDF or assembled
    /// page by page therefore cannot be given a PDF/UA level - render it with
    /// <see cref="DocumentRenderer"/> instead.
    /// </summary>
    public PdfUaConformance Accessibility { get; internal set; }

    internal bool IsPdfUa => Accessibility != PdfUaConformance.None;

    /// <summary>
    /// Gets the document's natural language (catalog <c>/Lang</c>, e.g. <c>"en-US"</c>), captured
    /// from <see cref="Document.Language"/> when the document was rendered. It is written for a
    /// rendered document only; a loaded document keeps whatever <c>/Lang</c> its catalog already
    /// carries, because the language of tagged content is determined while rendering.
    /// </summary>
    public string? Language { get; internal set; }

    internal bool OutlineChanged => Loaded?.Source is null
        || Loaded.OutlineRequiresRewrite
        || (outlineLoaded && TrackedChanges.AnyModified(outline));

    internal bool PageLabelsChanged => Loaded?.Source is null
        || (pageLabelsLoaded && TrackedChanges.AnyModified(pageLabels));

    internal bool HasPreservableStructureGraph
        => Loaded is { Source: { } reader, SourceCatalog: { } catalog } loaded
            && reader.GetDictionary(catalog, "StructTreeRoot") is not null
            && Pages.Count == loaded.SourcePages.Count
            && Pages.All(loaded.SourcePages.ContainsKey);

    internal void AcceptMetadataChanges()
    {
        if (infoLoaded)
        {
            info.AcceptChanges();
        }

        if (attachmentsLoaded)
        {
            attachments.AcceptChanges();
        }

        if (outlineLoaded)
        {
            TrackedChanges.Accept(outline);
        }

        if (pageLabelsLoaded)
        {
            TrackedChanges.Accept(pageLabels);
        }
    }

    /// <summary>
    /// Loads a physical document from a stream. The stream is read in full and
    /// parsed through the internal reader; each page's raw content-stream bytes
    /// are retained verbatim so untouched pages re-serialize unchanged.
    /// </summary>
    /// <param name="stream">The source stream.</param>
    /// <param name="options">Load options such as the decryption password.</param>
    /// <returns>The loaded document.</returns>
    public static PortableDocument LoadFromStream(Stream stream, LoadOptions? options = null)
        => LoadFromStream(stream, ReaderLimits.Default, options);

    /// <summary>
    /// Loads a physical document from a stream, applying the supplied resource
    /// limits while parsing untrusted input. See <see cref="ReaderLimits"/>.
    /// </summary>
    /// <param name="stream">The source stream.</param>
    /// <param name="limits">The resource limits to enforce while reading.</param>
    /// <param name="options">Load options such as the decryption password.</param>
    /// <returns>The loaded document.</returns>
    public static PortableDocument LoadFromStream(Stream stream, ReaderLimits limits, LoadOptions? options = null)
        => DocumentLoader.Load(stream, limits, options);

    /// <summary>
    /// Extracts the visible text of every page in reading order, concatenated in
    /// page order with a newline between pages.
    /// </summary>
    /// <returns>The document text, or an empty string when there is no text.</returns>
    public string ExtractText()
    {
        var builder = new StringBuilder();
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

    /// <summary>Finds text across all pages and reports each match's page index.</summary>
    /// <remarks>
    /// Matches may span adjacent text-show operators within one page but never span
    /// pages. Form XObject text and complex shaping or ligature cluster mapping are not included.
    /// </remarks>
    /// <param name="text">The non-empty text to find.</param>
    /// <param name="options">The matching options, or <c>null</c> for defaults.</param>
    /// <returns>The matches in page and reading order.</returns>
    public IReadOnlyList<TextHit> FindText(string text, TextSearchOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (text.Length == 0)
        {
            throw new ArgumentException("Search text cannot be empty.", nameof(text));
        }

        var hits = new List<TextHit>();
        for (var pageIndex = 0; pageIndex < Pages.Count; pageIndex++)
        {
            hits.AddRange(Pages[pageIndex].FindText(text, options, pageIndex));
        }

        return hits;
    }

    /// <summary>Replaces matching text on every page using each source font encoding.</summary>
    /// <remarks>Matches may span contiguous <c>Tj</c> operators with the same font and text state. Unsupported show operators or incompatible text states cause an exception.</remarks>
    /// <param name="search">The non-empty text to find.</param>
    /// <param name="replacement">The replacement text.</param>
    /// <param name="options">The matching and layout options, or <c>null</c> for defaults.</param>
    /// <returns>The total number of replacements.</returns>
    public int ReplaceText(string search, string replacement, ReplaceTextOptions? options = null)
    {
        var count = 0;
        foreach (var page in Pages)
        {
            count += page.ReplaceText(search, replacement, options);
        }

        return count;
    }

    /// <summary>Irreversibly removes content intersecting page-specific redaction regions.</summary>
    /// <param name="areas">The redaction regions with their zero-based page indexes.</param>
    /// <param name="options">The redaction appearance options, or <c>null</c> for no fill.</param>
    public void Redact(IEnumerable<PageRedaction> areas, RedactionOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(areas);
        foreach (var group in areas.GroupBy(static area => area.PageIndex))
        {
            if (group.Key < 0 || group.Key >= Pages.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(areas), group.Key, "A redaction page index is outside the document.");
            }

            Pages[group.Key].Redact(group.Select(static area => area.Area), options);
        }
    }

    /// <summary>Finds text throughout the document and irreversibly redacts every match.</summary>
    /// <param name="text">The non-empty text to redact.</param>
    /// <param name="searchOptions">The text matching options, or <c>null</c> for defaults.</param>
    /// <param name="redactionOptions">The redaction appearance options, or <c>null</c> for no fill.</param>
    /// <returns>The number of redacted matches.</returns>
    public int RedactText(string text, TextSearchOptions? searchOptions = null, RedactionOptions? redactionOptions = null)
    {
        var count = 0;
        foreach (var page in Pages)
        {
            count += page.RedactText(text, searchOptions, redactionOptions);
        }

        return count;
    }

    /// <summary>
    /// Appends a deep copy of every page in <paramref name="other"/> to this
    /// document. Each appended page keeps its own content stream (no resource
    /// deduplication) and <paramref name="other"/> is left unchanged.
    /// </summary>
    /// <param name="other">The document whose pages are copied.</param>
    public void Append(PortableDocument other)
    {
        ArgumentNullException.ThrowIfNull(other);

        foreach (var page in other.Pages)
        {
            if (!page.ContentIsIntact)
            {
                ImportPages(other, ..);
                return;
            }
        }

        DocumentMerger.Append(this, other);
    }

    /// <summary>Imports a deep copy of one page from another document.</summary>
    /// <param name="source">The source document.</param>
    /// <param name="pageIndex">The zero-based source page index.</param>
    /// <returns>The imported page.</returns>
    public Page ImportPage(PortableDocument source, int pageIndex)
    {
        ArgumentNullException.ThrowIfNull(source);
        var imported = ImportPages(source, new Range(pageIndex, pageIndex + 1));
        return imported[0];
    }

    /// <summary>Imports deep copies of a selected range of pages from another document.</summary>
    /// <param name="source">The source document.</param>
    /// <param name="range">The source page range.</param>
    /// <returns>The imported pages in source order.</returns>
    public IReadOnlyList<Page> ImportPages(PortableDocument source, Range range)
    {
        ArgumentNullException.ThrowIfNull(source);
        var (offset, length) = range.GetOffsetAndLength(source.Pages.Count);
        if (source.MaterializedGraph is not null)
        {
            return PageOperations.Import(this, source, offset, length);
        }

        if (PageOperations.CanImportDirectly(this, source, offset, length))
        {
            return PageOperations.ImportIsolated(this, source, offset, length);
        }

        return PageOperations.Import(this, PageOperations.Snapshot(source), offset, length);
    }

    /// <summary>Creates a new document containing deep copies of all pages from the supplied documents.</summary>
    /// <param name="documents">The documents to merge in order.</param>
    /// <returns>A new merged document.</returns>
    public static PortableDocument Merge(params PortableDocument[] documents)
    {
        ArgumentNullException.ThrowIfNull(documents);
        var result = new PortableDocument();
        foreach (var document in documents)
        {
            ArgumentNullException.ThrowIfNull(document);
            result.ImportPages(document, ..);
        }

        return result;
    }

    /// <summary>Adds a centered watermark overlay to every current page.</summary>
    /// <param name="watermark">The watermark options.</param>
    /// <exception cref="NotSupportedException">
    /// <paramref name="watermark"/> has text in a font this document cannot draw it with: pages of
    /// a loaded or already-built document reach only the base-14 faces by name and cannot embed a
    /// font file.
    /// </exception>
    public void AddWatermark(Watermark watermark)
    {
        ArgumentNullException.ThrowIfNull(watermark);

        if (!string.IsNullOrEmpty(watermark.Text))
        {
            Radzen.Documents.Pdf.Fonts.FontResolution.ResolveBase14Name(
                watermark.Font,
                FontScope with { Base14ForbiddenBy = null });
        }

        var images = new ImageRegistry(ImageDecoders);
        SourceId? imageSource = null;
        SceneImageData? imageData = null;
        if (watermark.Image is { } image)
        {
            imageSource = new SourceId(0);
            imageData = new SceneImageData(image.Data);
        }

        foreach (var page in Pages)
        {
            page.AppendContent(new WatermarkContent(
                watermark,
                page.CropBox ?? page.MediaBox,
                images,
                imageSource,
                imageData));
        }
    }

    /// <summary>Adds centered watermark text to every current page.</summary>
    /// <param name="text">The watermark text.</param>
    public void AddWatermark(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        AddWatermark(new Watermark { Text = text });
    }

    /// <summary>
    /// Flattens interactive forms and modeled annotations into static page
    /// content. Field widgets, modeled annotations and the catalog
    /// <c>/AcroForm</c> are removed; unsupported loaded annotations are retained.
    /// </summary>
    public void Flatten()
    {
        new FormWriter(this).Flatten();
        foreach (var page in Pages)
        {
            page.Annotations.RemoveLoadedSubtype("Widget");
        }

        AnnotationFlatteningWriter.Flatten(this);
    }

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
        ArgumentNullException.ThrowIfNull(stream);

        var graph = (MaterializedGraph ?? new DocumentGraphBuilder(this).Build()).CopyForSerialization();
        var writer = new DocumentWriter(stream, graph)
        {
            Encryption = graph.Encryption,
            UseCompressedStreams = graph.UseCompressedStreams,
        };
        writer.Close();
    }

    /// <summary>
    /// Saves the document as a PDF incremental update (ISO 32000-1 section 7.5.6):
    /// the original file bytes are preserved verbatim as an exact prefix of the
    /// output, and only the objects the caller changed since loading - edited
    /// metadata, filled form fields, annotations, page boxes, and supported page
    /// insertions, removals or reordering - are written afterwards, followed by a
    /// new cross-reference section chained to the original via <c>/Prev</c>.
    /// Content edits, encryption changes, and page-tree structures that cannot be
    /// updated safely are rejected with a direction to use <see cref="SaveToStream"/>.
    /// Valid only for a document obtained from
    /// <see cref="LoadFromStream(Stream, LoadOptions)"/>; a freshly built document
    /// has no original revision to increment and must be written with
    /// <see cref="SaveToStream"/>. The output is deterministic: identical edits on
    /// identical input produce identical bytes.
    /// </summary>
    /// <param name="stream">The destination stream.</param>
    /// <exception cref="InvalidOperationException">The document was not loaded from
    /// an existing PDF, or no supported change was made since it was loaded.</exception>
    public void SaveIncremental(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (Loaded?.Source is null || Loaded.SourceBytes is null)
        {
            throw new InvalidOperationException(
                "SaveIncremental is only valid for a document loaded from an existing PDF via LoadFromStream. "
                + "Use SaveToStream to write a newly built document.");
        }

        new IncrementalDocumentWriter(this).Save(stream);
    }

    /// <summary>
    /// Serializes this document and adds an approval signature to it as an
    /// incremental update, returning the signed bytes. Equivalent to calling
    /// <see cref="PdfSigner.Sign(byte[], SignatureOptions, ISigner)"/> with
    /// <see cref="ToArray"/>; exposed here so signing is discoverable from the
    /// document object. The library performs no cryptography itself - the
    /// caller-supplied <paramref name="signer"/> produces the detached CMS blob.
    /// </summary>
    /// <param name="options">Signature appearance and sizing options.</param>
    /// <param name="signer">Produces the detached CMS signature. See <see cref="ISigner"/>.</param>
    /// <returns>The bytes of the signed document.</returns>
    public byte[] Sign(SignatureOptions options, ISigner signer)
        => PdfSigner.Sign(ToArray(), options, signer);
}
