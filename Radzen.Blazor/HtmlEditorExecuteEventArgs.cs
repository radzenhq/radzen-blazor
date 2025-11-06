using Radzen.Blazor;

namespace Radzen;

/// <summary>
/// Supplies information about a <see cref="RadzenHtmlEditor.Execute" /> event that is being raised.
/// </summary>
public class HtmlEditorExecuteEventArgs
{
    /// <summary>
    /// Gets the RadzenHtmlEditor instance which raised the event.
    /// </summary>
    public RadzenHtmlEditor Editor { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="HtmlEditorExecuteEventArgs"/> class.
    /// </summary>
    /// <param name="editor">The editor instance.</param>
    internal HtmlEditorExecuteEventArgs(RadzenHtmlEditor editor)
    {
        Editor = editor;
    }

    /// <summary>
    /// Gets the name of the command which RadzenHtmlEditor is executing.
    /// </summary>
    public string CommandName { get; set; }
}

