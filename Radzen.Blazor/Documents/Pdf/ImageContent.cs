using System;

using Radzen.Documents.Pdf.Content;
namespace Radzen.Documents.Pdf;


/// <summary>
/// An image placed into a rectangle. Holds a pre-encoded image XObject payload.
/// </summary>
public sealed class ImageContent : ContentElement
{
    /// <summary>
    /// Initializes a new <see cref="ImageContent"/>.
    /// </summary>
    /// <param name="encodedXObject">The pre-encoded image XObject bytes.</param>
    public ImageContent(byte[] encodedXObject)
    {
        ArgumentNullException.ThrowIfNull(encodedXObject);
        EncodedXObject = encodedXObject;
    }

    /// <summary>Gets or sets the placement rectangle in PDF user space.</summary>
    public PdfRect Bounds { get; set; }

    internal byte[] EncodedXObject { get; }

    /// <inheritdoc/>
    protected override void EmitBody(ContentWriter writer)
    {
        var key = writer.RegisterImage(EncodedXObject);

        writer.WriteRaw("q\n");
        writer.WriteNumber(Bounds.Width);
        writer.WriteRaw(" 0 0 ");
        writer.WriteNumber(Bounds.Height);
        writer.WriteRaw(" ");
        writer.WriteNumber(Bounds.Left);
        writer.WriteRaw(" ");
        writer.WriteNumber(Bounds.Bottom);
        writer.WriteRaw(" cm\n");
        writer.WriteName(key);
        writer.WriteRaw(" Do\nQ\n");
    }
}
