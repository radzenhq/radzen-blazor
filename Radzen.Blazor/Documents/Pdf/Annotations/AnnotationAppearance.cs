namespace Radzen.Documents.Pdf;

/// <summary>Defines custom drawable content for an annotation appearance.</summary>
public sealed class AnnotationAppearance
{
    /// <summary>Gets the appearance content, positioned relative to the annotation bounds.</summary>
    public ContentCollection Content { get; } = [];

    internal bool IsModified => Content.IsModified;

    internal void AcceptChanges() => Content.AcceptChanges();

    internal void OwnedBy(System.Action? changed) => Content.OwnedBy(changed);
}
