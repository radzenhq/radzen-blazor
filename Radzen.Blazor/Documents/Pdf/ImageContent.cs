using System;

using Radzen.Documents.Pdf.Content;
namespace Radzen.Documents.Pdf;


/// <summary>
/// An image placed into a rectangle. Holds a pre-encoded image XObject payload.
/// </summary>
public sealed class ImageContent : ContentElement
{
    private PdfRect bounds;

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
    public PdfRect Bounds
    {
        get => bounds;
        set => Set(ref bounds, value);
    }

    internal byte[] EncodedXObject { get; }

    internal override ContentElement DeepClone()
        => CopyStateTo(new ImageContent([.. EncodedXObject]) { Bounds = Bounds });

    private protected override void EmitBody(ContentWriter writer)
    {
        var key = writer.RegisterImage(EncodedXObject);
        ContentEmitter.WriteImagePlacement(
            writer, key, Bounds.Left, Bounds.Bottom, Bounds.Width, Bounds.Height);
    }
}
