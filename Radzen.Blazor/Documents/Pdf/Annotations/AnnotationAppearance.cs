namespace Radzen.Documents.Pdf;

/// <summary>Defines custom drawable content for an annotation appearance.</summary>
public sealed class AnnotationAppearance
{
    /// <summary>Gets the appearance content, positioned relative to the annotation bounds.</summary>
    public ContentCollection Content { get; } = [];

    // The elements carry their own flags; what this adds is the container truth - a removed or
    // reordered element moves the bytes without any surviving element being touched.
    internal bool IsModified => Content.IsModified;

    internal void AcceptChanges() => Content.AcceptChanges();
}
