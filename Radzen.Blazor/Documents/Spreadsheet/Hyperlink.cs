namespace Radzen.Documents.Spreadsheet;

#nullable enable

/// <summary>
/// Represents a hyperlink in a spreadsheet cell.
/// </summary>
public class Hyperlink
{
    /// <summary>
    /// Gets or sets the URL of the hyperlink.
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display text of the hyperlink. If null, the URL is displayed.
    /// </summary>
    public string? DisplayText { get; set; }

    /// <summary>
    /// Creates a copy of this hyperlink.
    /// </summary>
    public Hyperlink Clone() => new() { Url = Url, DisplayText = DisplayText };
}
