using System;

using Radzen.Documents.Pdf.Content;
namespace Radzen.Documents.Pdf;


/// <summary>
/// A named XObject (image or form) painted with the <c>Do</c> operator. The name
/// resolves against the page's <c>/XObject</c> resources.
/// </summary>
public sealed class XObjectContent : ContentElement
{
    /// <summary>
    /// Initializes a new <see cref="XObjectContent"/>.
    /// </summary>
    /// <param name="name">The XObject resource name, without the leading slash.</param>
    public XObjectContent(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        Name = name;
    }

    /// <summary>Gets the XObject resource name, without the leading slash.</summary>
    public string Name { get; }

    /// <inheritdoc/>
    protected override void EmitBody(ContentWriter writer)
    {
        writer.WriteName(Name);
        writer.WriteRaw(" Do\n");
    }
}
