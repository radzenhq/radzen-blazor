namespace Radzen.Documents.Pdf;


/// <summary>
/// The root of the document authoring model. Holds metadata, named styles and the ordered sections.
/// </summary>
public class DocumentBuilder
{
    /// <summary>Gets the document metadata.</summary>
    public DocumentInfo Info { get; } = new();

    /// <summary>Gets the named style definitions.</summary>
    public StyleCollection Styles { get; } = [];

    /// <summary>Gets the ordered sections of the document.</summary>
    public SectionCollection Sections { get; } = new();

    /// <summary>Gets the font collection used to register and resolve fonts.</summary>
    public FontCollection Fonts { get; } = new();

    /// <summary>Gets the files to embed into the produced PDF (e.g. the Factur-X invoice XML).</summary>
    public AttachmentCollection Attachments { get; } = [];

    /// <summary>Gets the root entries of the document outline (bookmark) tree.</summary>
    public System.Collections.Generic.IList<OutlineItem> Outline { get; } = [];

    /// <summary>
    /// Gets or sets the PDF/A conformance level of the output. When not
    /// <see cref="PdfAConformance.None"/> the saved file carries an XMP
    /// metadata stream with the PDF/A identification, an sRGB output intent
    /// and a document identifier; every font must be an embedded subset, so
    /// referencing a standard-14 font by name throws.
    /// </summary>
    public PdfAConformance Conformance { get; set; }

    /// <summary>
    /// Gets or sets whether the output identifies as PDF/UA-1 (ISO 14289-1,
    /// accessibility). The saved file carries pdfuaid:part 1 in its XMP metadata,
    /// is marked as Tagged PDF (/MarkInfo /Marked true with a /StructTreeRoot)
    /// and sets the DisplayDocTitle viewer preference; every font must be an
    /// embedded subset. Composable with <see cref="Conformance"/>.
    /// </summary>
    public bool PdfUA { get; set; }

    /// <summary>
    /// Gets or sets the natural language of the document as an RFC 3066 /
    /// BCP 47 tag (e.g. <c>en-US</c>), written as the catalog <c>/Lang</c>.
    /// Required when <see cref="PdfUA"/> is set, since PDF/UA demands that
    /// the document language be determinable.
    /// </summary>
    public string? Language { get; set; }

    /// <summary>
    /// Gets or sets the encryption to apply when saving. When <c>null</c> the
    /// document is written unencrypted.
    /// </summary>
    public Objects.Encryption.EncryptionOptions? Encryption { get; set; }

    /// <summary>
    /// Gets or sets whether the saved file packs its objects into compressed
    /// object streams with a cross-reference stream, shrinking the output at
    /// the cost of PDF/A-1 compatibility. Defaults to <c>false</c>.
    /// </summary>
    public bool CompressOutput { get; set; }

    /// <summary>
    /// Runs the layout engine over the sections and produces a physical <see cref="Document"/>.
    /// Paragraphs flow across pages, tables lay out and paginate (repeating header rows),
    /// images decode and scale to their box, registered fonts embed as Type0/CID and base-14
    /// families embed by name.
    /// </summary>
    /// <returns>The generated document.</returns>
    public Document Build()
    {
        var document = DocumentGenerator.Generate(this);
        document.Encryption = Encryption;
        document.CompressOutput = CompressOutput;
        document.PdfUA = PdfUA;
        document.Language = Language;
        return document;
    }

    /// <summary>Builds the document and serializes it to the given stream.</summary>
    /// <param name="stream">The destination stream.</param>
    public void SaveToStream(System.IO.Stream stream) => Build().SaveToStream(stream);

    /// <summary>Builds the document and serializes it to a byte array.</summary>
    /// <returns>The complete PDF file bytes.</returns>
    public byte[] ToArray() => Build().ToArray();
}
