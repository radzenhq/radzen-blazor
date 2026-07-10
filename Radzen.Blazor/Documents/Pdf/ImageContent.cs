namespace Radzen.Documents.Pdf;

#nullable enable

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
        System.ArgumentNullException.ThrowIfNull(encodedXObject);
        EncodedXObject = encodedXObject;
    }

    /// <summary>Gets or sets the placement rectangle.</summary>
    public Rect Rect { get; set; }

    internal byte[] EncodedXObject { get; }

    internal override void EmitBody(ContentWriter writer)
    {
    }
}
