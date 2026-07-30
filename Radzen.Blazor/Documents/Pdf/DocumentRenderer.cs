using System;
using System.Collections.Generic;
using System.IO;

using Radzen.Documents.Pdf.Render;
using Radzen.Documents.Pdf.Write;

namespace Radzen.Documents.Pdf;

/// <summary>
/// Renders a <see cref="Document"/> into a physical PDF
/// <see cref="PortableDocument"/>. Carries the PDF-only settings of the output: conformance,
/// accessibility, encryption, viewer preferences, attachments, outline, page labels
/// and interactive form fields.
/// </summary>
public sealed class DocumentRenderer
{
    /// <summary>
    /// Gets the files to embed into the produced PDF (e.g. the Factur-X invoice XML). Copied into the
    /// produced document at <see cref="Render(Document)"/>; afterwards
    /// <see cref="PortableDocument.Attachments"/> on that document governs what is saved, and changing
    /// this collection has no effect on an already-rendered document.
    /// </summary>
    public AttachmentCollection Attachments { get; } = [];

    /// <summary>
    /// Gets the root entries of the document outline (bookmark) tree. Copied into the produced
    /// document at <see cref="Render(Document)"/>; afterwards <see cref="PortableDocument.Outline"/>
    /// on that document governs what is saved, and changing this list has no effect on an
    /// already-rendered document.
    /// </summary>
    public IList<OutlineItem> Outline { get; } = [];

    /// <summary>
    /// Gets or sets the viewer preferences applied to the produced document
    /// (initial page layout and page mode plus the <c>/ViewerPreferences</c>
    /// flags). When <c>null</c> no viewer-preference keys are written and the
    /// output is unchanged. Captured into the produced document at <see cref="Render(Document)"/>;
    /// afterwards <see cref="PortableDocument.ViewerPreferences"/> on that document governs what is
    /// saved, and changing it here has no effect on an already-rendered document.
    /// </summary>
    public ViewerPreferences? ViewerPreferences { get; set; }

    /// <summary>
    /// Gets the page-label ranges applied to the produced document, written as
    /// the catalog <c>/PageLabels</c> number tree. When empty no <c>/PageLabels</c>
    /// entry is written. Copied into the produced document at <see cref="Render(Document)"/>;
    /// afterwards <see cref="PortableDocument.PageLabels"/> on that document governs what is saved,
    /// and changing this list has no effect on an already-rendered document.
    /// </summary>
    public IList<PageLabel> PageLabels { get; } = [];

    /// <summary>
    /// Gets the interactive form fields to create on the produced document. Each
    /// definition is saved as a widget annotation on its page and listed in the
    /// catalog <c>/AcroForm /Fields</c>. When empty no form is written. Copied into the produced
    /// document at <see cref="Render(Document)"/>; afterwards <see cref="PortableDocument.FormFields"/>
    /// on that document governs what is saved, and changing this list has no effect on an
    /// already-rendered document.
    /// </summary>
    public IList<FormFieldDefinition> FormFields { get; } = [];

    /// <summary>
    /// Gets or sets the name of the application that produced the PDF, written to the
    /// <c>/Info /Producer</c> entry and the XMP <c>pdf:Producer</c> property. When
    /// <see langword="null"/> (the default) the library's own producer name is used and
    /// no producer is added to the <c>/Info</c> dictionary.
    /// </summary>
    public string? Producer { get; set; }

    /// <summary>
    /// Gets or sets the PDF/A conformance level of the output. When not
    /// <see cref="PdfAConformance.None"/> the saved file carries an XMP
    /// metadata stream with the PDF/A identification, an sRGB output intent
    /// and a document identifier; every font must be an embedded subset, so
    /// referencing a standard-14 font by name throws.
    /// </summary>
    public PdfAConformance Conformance { get; set; }

    /// <summary>
    /// Gets or sets the PDF/UA accessibility conformance level of the output. When
    /// <see cref="PdfUaConformance.PdfUa1"/> the saved file carries pdfuaid:part 1 in
    /// its XMP metadata, is marked as Tagged PDF (/MarkInfo /Marked true with a
    /// /StructTreeRoot) and sets the DisplayDocTitle viewer preference; every font must
    /// be an embedded subset. Composable with <see cref="Conformance"/>. Requires
    /// <see cref="Document.Language"/> to be set.
    /// </summary>
    public PdfUaConformance Accessibility { get; set; }

    /// <summary>
    /// Gets the map of non-standard structure roles to standard ISO 32000-1
    /// structure types. A paragraph whose <see cref="Paragraph.StyleName"/>
    /// matches a declared role is tagged with that role, and the produced
    /// document carries a <c>/StructTreeRoot /RoleMap</c> so tagged output
    /// (PDF/UA, PDF/A Level A) stays conformant. Empty by default, in which
    /// case no <c>/RoleMap</c> is written and the output is unchanged.
    /// </summary>
    public RoleMap RoleMap { get; } = new();

    /// <summary>
    /// Gets or sets the encryption to apply when saving. When <c>null</c> the
    /// document is written unencrypted. Captured into the produced document at
    /// <see cref="Render(Document)"/>; afterwards <see cref="PortableDocument.Encryption"/> on that
    /// document governs what is saved, and changing it here has no effect on an already-rendered
    /// document.
    /// </summary>
    public EncryptionOptions? Encryption { get; set; }

    /// <summary>
    /// Gets or sets whether the saved file packs its objects into compressed
    /// object streams with a cross-reference stream, shrinking the output at
    /// the cost of PDF/A-1 compatibility. Defaults to <c>false</c>. Captured into the produced
    /// document at <see cref="Render(Document)"/>; afterwards
    /// <see cref="PortableDocument.CompressOutput"/> on that document governs what is saved, and
    /// changing it here has no effect on an already-rendered document.
    /// </summary>
    public bool CompressOutput { get; set; }

    /// <summary>
    /// Gets or sets whether the saved file carries a deterministic trailer
    /// <c>/ID</c>. Defaults to <c>false</c> so output stays byte identical unless
    /// opted in. Captured into the produced document at <see cref="Render(Document)"/>; afterwards
    /// <see cref="PortableDocument.IncludeDocumentId"/> on that document governs what is saved, and
    /// changing it here has no effect on an already-rendered document.
    /// </summary>
    public bool IncludeDocumentId { get; set; }

    /// <summary>
    /// Gets or sets whether a glyph captured from a built-in metrics font that the PDF text
    /// encoding cannot represent is drawn as '?'. Defaults to <see langword="false"/>, so
    /// rendering throws and names the offending characters. Captured into the laid-out scene at
    /// <see cref="Render(Document)"/>.
    /// </summary>
    public bool AllowUnsupportedCharacters { get; set; }

    /// <summary>
    /// Gets or sets the image decoders this renderer decodes and measures images with. Seeded from
    /// <see cref="ImageDecoders.Default"/>, so a decoder registered with
    /// <see cref="ImageDecoder.Register(IImageDecoder)"/> before this renderer was created is
    /// already in the set. Assign <c>ImageDecoders.BuiltIn.Add(...)</c> to reach a custom format
    /// from this renderer alone. Captured into the produced document at <see cref="Render(Document)"/>.
    /// </summary>
    public ImageDecoders ImageDecoders { get; set; } = ImageDecoders.Default;

    /// <summary>
    /// Runs the layout engine over the model's sections and produces a physical
    /// <see cref="PortableDocument"/>. Paragraphs flow across pages, tables lay out and paginate
    /// (repeating header rows), images decode and scale to their box, registered fonts
    /// embed as Type0/CID and base-14 families embed by name.
    /// </summary>
    /// <param name="document">The document model to render.</param>
    /// <returns>The generated document.</returns>
    public PortableDocument Render(Document document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var output = DocumentGenerator.Generate(
            RenderRequest.From(this),
            Layout.DocumentLayouter.Layout(document, ImageDecoders.Probes, AllowUnsupportedCharacters));
        output.AdoptMaterializedGraph(new DocumentMaterializer(output).Materialize());
        return output;
    }

    /// <summary>Renders the model and serializes it to the given stream.</summary>
    /// <param name="document">The document model to render.</param>
    /// <param name="stream">The destination stream.</param>
    public void SaveToStream(Document document, Stream stream)
        => Render(document).SaveToStream(stream);

    /// <summary>Renders the model and serializes it to a byte array.</summary>
    /// <param name="document">The document model to render.</param>
    /// <returns>The complete PDF file bytes.</returns>
    public byte[] ToArray(Document document) => Render(document).ToArray();
}
