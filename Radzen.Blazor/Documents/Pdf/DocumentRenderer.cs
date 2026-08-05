using System;
using System.IO;

using Radzen.Documents.Pdf.Render;
using Radzen.Documents.Pdf.Write;

namespace Radzen.Documents.Pdf;

/// <summary>
/// Renders a <see cref="Document"/> into a physical PDF <see cref="PortableDocument"/>. Carries
/// the settings that govern what rendering emits: conformance, accessibility, the structure role
/// map, the font permissions and the image decoders. Everything the saved file carries afterwards
/// - encryption, attachments, outline, page labels, form fields, viewer preferences and output
/// compression - is set on the produced <see cref="PortableDocument"/>.
/// </summary>
public sealed class DocumentRenderer
{
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
    /// referencing a standard-14 font by name throws. The Level A parts -
    /// <see cref="PdfAConformance.PdfA2A"/> and <see cref="PdfAConformance.PdfA3A"/> -
    /// require Tagged PDF, so they turn tagging on exactly as
    /// <see cref="Accessibility"/> does; the Level B parts do not.
    /// </summary>
    public PdfAConformance Conformance { get; set; }

    /// <summary>
    /// Gets or sets the PDF/UA accessibility conformance level of the output. When
    /// <see cref="PdfUaConformance.PdfUa1"/> the saved file carries pdfuaid:part 1 in
    /// its XMP metadata, is marked as Tagged PDF (/MarkInfo /Marked true with a
    /// /StructTreeRoot) and sets the DisplayDocTitle viewer preference; every font must
    /// be an embedded subset. Composable with <see cref="Conformance"/>. Requires
    /// <see cref="Document.Language"/> to be set. Tagging is emitted only when this
    /// property or a Level A <see cref="Conformance"/> asks for it: left at
    /// <see cref="PdfUaConformance.None"/> the output carries no structure tree, no
    /// <c>/MarkInfo</c>, no <c>/StructTreeRoot</c> and no marked content in its page
    /// streams.
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
    /// Gets or sets what happens when text uses a character that neither its font nor any
    /// fallback covers. Defaults to <see cref="UnsupportedCharacterPolicy.Throw"/>, so rendering
    /// fails naming every uncovered character and its font. Read once by <see cref="Render(Document)"/>,
    /// which applies it while it draws text and watermarks; it is not carried on the laid-out scene
    /// or on the produced document, so changing it afterwards has no effect on an already-rendered
    /// document.
    /// </summary>
    public UnsupportedCharacterPolicy UnsupportedCharacters { get; set; }

    /// <summary>
    /// Gets or sets a callback invoked after <see cref="Render(Document)"/> completes under
    /// <see cref="UnsupportedCharacterPolicy.Substitute"/> - once per distinct uncovered
    /// character and font, in the order they were first encountered. Never invoked while the
    /// document is being drawn.
    /// </summary>
    public Action<UnsupportedCharacter>? UnsupportedCharacterFound { get; set; }

    /// <summary>
    /// Gets or sets whether a registered font whose OS/2 fsType marks it as Restricted License
    /// Embedding may still be embedded. Defaults to <see langword="false"/>, so rendering a
    /// document that registers such a font throws unless the caller explicitly opts in.
    /// Checked once by <see cref="Render(Document)"/>, against the fonts that document uses. The
    /// produced document does not carry the permission, so saving it again does not re-check it.
    /// </summary>
    public bool AllowRestrictedEmbedding { get; set; }

    internal ImageDecoders ImageDecoders { get; set; } = ImageDecoders.BuiltIn;

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

        var request = RenderRequest.From(this);
        var output = DocumentRenderEngine.Generate(
            request,
            Layout.DocumentLayouter.Layout(document, ImageDecoders.Probes));
        output.AdoptMaterializedGraph(new DocumentGraphBuilder(output, renderTime: true).Build());
        if (UnsupportedCharacterFound is { } found)
        {
            foreach (var character in request.Unsupported.Entries)
            {
                found(character);
            }
        }

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
