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
    /// Gets or sets whether a glyph captured from a built-in metrics font that the PDF text
    /// encoding cannot represent is drawn as '?'. Defaults to <see langword="false"/>, so
    /// rendering throws and names the offending characters. Read once by <see cref="Render(Document)"/>,
    /// which applies it while it draws text and watermarks; it is not carried on the laid-out scene
    /// or on the produced document, so changing it afterwards has no effect on an already-rendered
    /// document.
    /// </summary>
    public bool AllowUnsupportedCharacters { get; set; }

    /// <summary>
    /// Gets or sets whether a registered font whose OS/2 fsType marks it as Restricted License
    /// Embedding may still be embedded. Defaults to <see langword="false"/>, so rendering a
    /// document that registers such a font throws unless the caller explicitly opts in.
    /// Checked once by <see cref="Render(Document)"/>, against the fonts that document uses. The
    /// produced document does not carry the permission, so saving it again does not re-check it.
    /// </summary>
    public bool AllowRestrictedEmbedding { get; set; }

    /// <summary>
    /// Gets or sets the image decoders this renderer decodes and measures images with. Seeded from
    /// <see cref="ImageDecoders.Default"/>, so a decoder registered with
    /// <see cref="ImageDecoder.Register(IImageDecoder)"/> before this renderer was created is
    /// already in the set. Assign <c>ImageDecoders.BuiltIn.Add(...)</c> to reach a custom format
    /// from this renderer alone. Used to measure and decode the images this render draws, and
    /// carried onto the produced document so images added to it later decode the same way.
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

        var output = DocumentRenderEngine.Generate(
            RenderRequest.From(this),
            Layout.DocumentLayouter.Layout(document, ImageDecoders.Probes));
        output.AdoptMaterializedGraph(new DocumentGraphBuilder(output, renderTime: true).Build());
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
