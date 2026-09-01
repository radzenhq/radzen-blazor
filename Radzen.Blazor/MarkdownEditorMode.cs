namespace Radzen.Blazor;

/// <summary>
/// The mode of a <see cref="RadzenMarkdownEditor" />.
/// </summary>
public enum MarkdownEditorMode
{
    /// <summary>WYSIWYG editing of the rendered markdown.</summary>
    Design,
    /// <summary>Editing the raw markdown source in a textarea.</summary>
    Source
}
