using Radzen.Blazor;

namespace Radzen;

/// <summary>
/// Supplies information about a <see cref="RadzenMarkdownEditor.Execute" /> event that is being raised.
/// </summary>
public class MarkdownEditorExecuteEventArgs
{
    /// <summary>
    /// Gets the editor which raised the event.
    /// </summary>
    public RadzenMarkdownEditor Editor { get; }

    internal MarkdownEditorExecuteEventArgs(RadzenMarkdownEditor editor)
    {
        Editor = editor;
    }

    /// <summary>
    /// Gets the name of the command which was executed. Custom tools set this to their <c>CommandName</c>.
    /// </summary>
    public string? CommandName { get; set; }
}
