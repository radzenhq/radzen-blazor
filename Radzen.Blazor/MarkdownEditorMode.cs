namespace Radzen;

/// <summary>
/// Specifies the mode of <c>RadzenMarkdownEditor</c>.
/// </summary>
public enum MarkdownEditorMode
{
    /// <summary>
    /// Only the markdown source textarea is visible.
    /// </summary>
    Edit = 0,

    /// <summary>
    /// Only the rendered preview is visible.
    /// </summary>
    Preview = 1,

    /// <summary>
    /// The textarea and the rendered preview are shown side by side.
    /// </summary>
    Split = 2
}
