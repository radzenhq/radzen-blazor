using Radzen.Documents.Pdf.Objects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Radzen.Documents.Pdf.Emit;
using Radzen.Documents.Pdf.Signing;
namespace Radzen.Documents.Pdf;


/// <summary>
/// A physical PDF document: an ordered collection of pages plus document
/// metadata. Serialized through the object model as a classic PDF file.
/// </summary>
public sealed class Document
{
    private readonly TrackedList<OutlineItem> outline = [];
    private readonly TrackedList<PageLabel> pageLabels = [];

    /// <summary>Initializes an empty PDF document.</summary>
    public Document()
    {
        Pages = new PageCollection(this);
    }

    internal LoadedState? Loaded { get; private set; }

    internal static Document CreateLoaded(LoadedState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        return new Document { Loaded = state };
    }

    internal LoadedState EnsureLoaded() => Loaded ??= new LoadedState();

    internal void CarryForeignPage(Page page, Document donor)
    {
        if (donor.Loaded is { } origin)
        {
            EnsureLoaded().CarryForeign(page, origin);
        }
    }

    /// <summary>Gets the document metadata.</summary>
    public DocumentInfo Info { get; } = new();

    /// <summary>Gets the ordered collection of pages.</summary>
    public PageCollection Pages { get; }

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
    public IList<PageLabel> PageLabels => pageLabels;

    /// <summary>Gets the root entries of the document outline (bookmark) tree.</summary>
    public IList<OutlineItem> Outline => outline;

    /// <summary>Gets the files embedded in the document.</summary>
    public AttachmentCollection Attachments { get; } = [];

    /// <summary>
    /// Gets the document XMP metadata. Caller-set XMP takes precedence over automatic
    /// non-conformance metadata. Editing XMP on PDF/A or PDF/UA output is rejected.
    /// </summary>
    public DocumentXmpMetadata Xmp { get; } = new();

    internal StructureElement? Structure { get; set; }

    internal RoleMap RoleMap { get; set; } = new();

    internal PdfAConformance Conformance { get; set; }

    internal FontCollection? Fonts { get; set; }

    internal Fonts.FontScope FontScope => new(
        Fonts,
        Conformance != PdfAConformance.None ? "PDF/A" : PdfUA ? "PDF/UA" : null,
        CanEmbed: false);

    internal bool PdfUA { get; set; }

    internal string? Language { get; set; }

    internal bool HasUntaggedListContent { get; set; }

    internal Dictionary<string, GeneratedAnchor> Anchors { get; } = new(StringComparer.Ordinal);

    internal bool OutlineChanged => Loaded?.Source is null
        || Loaded.OutlineRequiresRewrite
        || outline.StructureChanged
        || AnyModified(outline);

    internal bool PageLabelsChanged => Loaded?.Source is null
        || pageLabels.StructureChanged
        || AnyModified(pageLabels);

    private static bool AnyModified<T>(TrackedList<T> items) where T : ITracksChanges
    {
        foreach (var item in items)
        {
            if (item.IsModified)
            {
                return true;
            }
        }

        return false;
    }

    internal void AcceptMetadataChanges()
    {
        Info.AcceptChanges();
        Attachments.AcceptChanges();
        outline.AcceptStructure();
        foreach (var item in outline)
        {
            item.AcceptChanges();
        }

        pageLabels.AcceptStructure();
        foreach (var label in pageLabels)
        {
            label.AcceptChanges();
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
    public void Append(Document other)
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
    public Page ImportPage(Document source, int pageIndex)
    {
        ArgumentNullException.ThrowIfNull(source);
        var imported = ImportPages(source, new Range(pageIndex, pageIndex + 1));
        return imported[0];
    }

    /// <summary>Imports deep copies of a selected range of pages from another document.</summary>
    /// <param name="source">The source document.</param>
    /// <param name="range">The source page range.</param>
    /// <returns>The imported pages in source order.</returns>
    public IReadOnlyList<Page> ImportPages(Document source, Range range)
    {
        ArgumentNullException.ThrowIfNull(source);
        var (offset, length) = range.GetOffsetAndLength(source.Pages.Count);
        if (PageOperations.CanImportDirectly(this, source, offset, length))
        {
            return PageOperations.ImportIsolated(this, source, offset, length);
        }

        return PageOperations.Import(this, PageOperations.Snapshot(source), offset, length);
    }

    /// <summary>Creates a new document containing deep copies of all pages from the supplied documents.</summary>
    /// <param name="documents">The documents to merge in order.</param>
    /// <returns>A new merged document.</returns>
    public static Document Merge(params Document[] documents)
    {
        ArgumentNullException.ThrowIfNull(documents);
        var result = new Document();
        foreach (var document in documents)
        {
            ArgumentNullException.ThrowIfNull(document);
            result.ImportPages(document, ..);
        }

        return result;
    }

    /// <summary>Adds a centered watermark overlay to every current page.</summary>
    /// <param name="watermark">The watermark options.</param>
    public void AddWatermark(Watermark watermark)
    {
        ArgumentNullException.ThrowIfNull(watermark);
        watermark.Validate();

        foreach (var page in Pages)
        {
            page.AppendContent(new WatermarkContent(watermark, page.CropBox ?? page.MediaBox));
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

        AnnotationFlattener.Flatten(this);
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

        new DocumentSaver(this).Save(stream);
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

        new IncrementalDocumentSaver(this).Save(stream);
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
