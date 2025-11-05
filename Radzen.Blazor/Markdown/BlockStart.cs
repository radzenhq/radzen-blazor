namespace Radzen.Blazor.Markdown;

/// <summary>
/// Represents the result of attempting to start a new block.
/// </summary>
internal enum BlockStart
{
    /// <summary>
    /// Skip this block.
    /// </summary>
    Skip,

    /// <summary>
    /// Start a container block.
    /// </summary>
    Container,

    /// <summary>
    /// Start a leaf block.
    /// </summary>
    Leaf
}

