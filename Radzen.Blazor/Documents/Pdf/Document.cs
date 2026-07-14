using Radzen.Documents.Pdf.Objects;
using System;
using System.Collections.Generic;
using System.IO;
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
    /// <summary>Initializes an empty PDF document.</summary>
    public Document()
    {
        Pages = new PageCollection(this);
    }

    // The document's load/merge/incremental state, encapsulated in one owned object.
    // Null for a freshly built document that carries no loaded or appended source
    // pages; set by DocumentLoader (fully loaded) or lazily by Append (carry only).
    internal LoadedState? Loaded { get; private set; }

    // Builds a loaded document around the state DocumentLoader constructed, instead of
    // letting the loader set the document's fields one by one from outside.
    internal static Document CreateLoaded(LoadedState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        return new Document { Loaded = state };
    }

    // Lazily creates the append-only carry state so Document.Append can record merged
    // loaded pages on a document that was not itself loaded.
    internal LoadedState EnsureLoaded() => Loaded ??= new LoadedState();

    // The eight modeled /Info fields captured as strings, for a cheap value comparison
    // between the load-time metadata and the current metadata on the incremental path.
    internal static string?[] InfoSnapshot(DocumentInfo info) =>
    [
        info.Title, info.Author, info.Subject, info.Keywords, info.Creator, info.Producer,
        info.CreationDate?.ToString("O", System.Globalization.CultureInfo.InvariantCulture),
        info.ModificationDate?.ToString("O", System.Globalization.CultureInfo.InvariantCulture),
    ];

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
    public IList<PageLabel> PageLabels { get; } = [];

    // Logical structure tree of a generated document (Tagged PDF). Set by the
    // generator; null for loaded or hand-assembled documents.
    internal StructureElement? Structure { get; set; }

    // Non-standard structure roles mapped to standard types, emitted as the
    // /StructTreeRoot /RoleMap. Set by the generator; empty maps write no /RoleMap.
    internal RoleMap RoleMap { get; set; } = new();

    // PDF/A conformance level requested at build time; drives XMP metadata,
    // the sRGB output intent, the trailer /ID and full-embedding enforcement.
    internal PdfAConformance Conformance { get; set; }

    // PDF/UA-1 identification requested at build time; drives the pdfuaid XMP
    // entry, the DisplayDocTitle viewer preference and tagging enforcement.
    internal bool PdfUA { get; set; }

    // Natural language of the document (catalog /Lang); required by PDF/UA.
    internal string? Language { get; set; }

    // Set by the generator when the authoring DOM has a List the structure tree left
    // untagged (lists are only tagged for PDF/UA). PDF/A Level-A rejects untagged real
    // content, so the conformance writer fails loud rather than claim conformance.
    internal bool HasUntaggedListContent { get; set; }

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

    /// <summary>
    /// Appends a deep copy of every page in <paramref name="other"/> to this
    /// document. Each appended page keeps its own content stream (no resource
    /// deduplication) and <paramref name="other"/> is left unchanged.
    /// </summary>
    /// <param name="other">The document whose pages are copied.</param>
    public void Append(Document other)
    {
        ArgumentNullException.ThrowIfNull(other);

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
        var snapshot = PageOperations.Snapshot(source);
        var (offset, length) = range.GetOffsetAndLength(snapshot.Pages.Count);
        return PageOperations.Import(this, snapshot, offset, length);
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
        if (!double.IsFinite(watermark.Opacity) || watermark.Opacity < 0 || watermark.Opacity > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(watermark), watermark.Opacity, "Watermark opacity must be between 0 and 1.");
        }

        if (!double.IsFinite(watermark.Rotation))
        {
            throw new ArgumentOutOfRangeException(nameof(watermark), watermark.Rotation, "Watermark rotation must be finite.");
        }

        foreach (var page in Pages)
        {
            page.Content.Add(new WatermarkContent(watermark, page.CropBox ?? page.MediaBox));
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
        ArgumentNullException.ThrowIfNull(stream);

        new DocumentSaver(this).Save(stream);
    }

    /// <summary>
    /// Saves the document as a PDF incremental update (ISO 32000-1 section 7.5.6):
    /// the original file bytes are preserved verbatim as an exact prefix of the
    /// output, and only the objects the caller changed since loading - edited
    /// metadata, filled form fields, and appended pages - are written afterwards,
    /// followed by a new cross-reference section chained to the original via
    /// <c>/Prev</c>. Valid only for a document obtained from
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
