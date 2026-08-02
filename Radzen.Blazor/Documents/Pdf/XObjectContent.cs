using System;

using Radzen.Documents.Pdf.Content;
namespace Radzen.Documents.Pdf;


/// <summary>
/// A named XObject (image or form) painted with the <c>Do</c> operator. The name
/// resolves against the page's <c>/XObject</c> resources.
/// </summary>
public sealed class XObjectContent : ContentElement
{
    internal XObjectContent(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        Name = name;
    }

    /// <summary>Gets the XObject resource name, without the leading slash.</summary>
    public string Name { get; }

    internal double? Opacity { get; init; }

    internal override ContentElement DeepClone() => CopyStateTo(new XObjectContent(Name) { Opacity = Opacity });

    private protected override void EmitBody(ContentWriter writer)
    {
        if (Opacity is { } opacity)
        {
            writer.WriteRaw("q\n");
            writer.WriteName(writer.RegisterOpacity(opacity));
            writer.WriteRaw(" gs\n");
            writer.WriteName(Name);
            writer.WriteRaw(" Do\n");
            writer.WriteRaw("Q\n");
            return;
        }

        writer.WriteName(Name);
        writer.WriteRaw(" Do\n");
    }
}
