namespace Radzen.Blazor.Markdown;

/// <summary>
/// Represents a position in a markdown document.
/// </summary>
internal struct Position
{
    /// <summary>
    /// Gets or sets the line number.
    /// </summary>
    public int Line { get; set; }

    /// <summary>
    /// Gets or sets the column number.
    /// </summary>
    public int Column { get; set; }
}

