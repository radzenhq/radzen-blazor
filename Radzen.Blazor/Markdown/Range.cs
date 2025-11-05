namespace Radzen.Blazor.Markdown;

/// <summary>
/// Represents a range in a markdown document.
/// </summary>
internal struct Range
{
    /// <summary>
    /// Gets or sets the start position.
    /// </summary>
    public Position Start;

    /// <summary>
    /// Gets or sets the end position.
    /// </summary>
    public Position End;
}

