namespace Radzen;

/// <summary>
/// Supplies information about a <see cref="Radzen.Blazor.RadzenHtmlEditor.Paste" /> event that is being raised.
/// </summary>
public class HtmlEditorPasteEventArgs
{
    /// <summary>
    /// Gets or sets the HTML content that is pasted in RadzenHtmlEditor. Use the setter to filter unwanted markup from the pasted value.
    /// </summary>
    /// <value>The HTML.</value>
    public string Html { get; set; }
}

